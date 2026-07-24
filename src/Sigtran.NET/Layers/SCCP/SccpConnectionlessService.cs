using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Provides a stateful SCCP connectionless service over an MTP3 network contract.
/// </summary>
public sealed class SccpConnectionlessService : ISccpService, IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SccpConnectionlessServiceOptions _options;
    private readonly SccpReassemblyBuffer _reassembly;
    private readonly Channel<SccpDataIndication> _inbound;
    private readonly Channel<SccpReturnIndication> _returns;
    private CancellationTokenSource? _lifetime;
    private Task? _receiveLoop;
    private Exception? _lastFailure;
    private int _nextSegmentationReference;
    private long _sentMessages;
    private long _receivedMessages;
    private long _sentSegments;
    private long _reassembledMessages;
    private long _returnedMessages;
    private long _unroutableMessages;
    private long _malformedMessages;
    private bool _disposed;

    /// <summary>Creates an SCCP connectionless service.</summary>
    /// <param name="network">The lower MTP3 network contract.</param>
    /// <param name="routingLabel">The default routing label for outbound SCCP transfers.</param>
    /// <param name="networkIndicator">The MTP3 network indicator for outbound transfers.</param>
    /// <param name="messagePriority">The MTP3 message priority for outbound transfers.</param>
    /// <param name="options">The stateful service options.</param>
    /// <param name="routes">The inbound application route table.</param>
    /// <param name="translations">The outbound global-title translation table.</param>
    public SccpConnectionlessService(
        IMtp3Network network,
        Mtp3RoutingLabel routingLabel,
        byte networkIndicator = 0,
        byte messagePriority = 0,
        SccpConnectionlessServiceOptions? options = null,
        SccpRouteTable? routes = null,
        SccpGlobalTitleTranslationTable? translations = null)
    {
        Network = network ?? throw new ArgumentNullException(nameof(network));
        RoutingLabel = routingLabel;
        ServiceInformation = new(
            Mtp3ServiceIndicator.Sccp,
            networkIndicator,
            messagePriority);
        _options = options ?? new();
        Routes = routes ?? new();
        Translations = translations ?? new();
        _reassembly = new(
            _options.MaximumReassemblyContexts,
            _options.MaximumReassembledBytes,
            _options.ReassemblyTimeout);
        _inbound = Channel.CreateBounded<SccpDataIndication>(
            new BoundedChannelOptions(_options.InboundQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
        _returns = Channel.CreateBounded<SccpReturnIndication>(
            new BoundedChannelOptions(_options.ReturnQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
    }

    /// <inheritdoc />
    public IMtp3Network Network { get; }

    /// <summary>The default MTP3 routing label for outbound SCCP transfers.</summary>
    public Mtp3RoutingLabel RoutingLabel { get; }

    /// <summary>The MTP3 service information octet for outbound SCCP transfers.</summary>
    public Mtp3ServiceInformationOctet ServiceInformation { get; }

    /// <summary>The inbound application route table.</summary>
    public SccpRouteTable Routes { get; }

    /// <summary>The outbound global-title translation table.</summary>
    public SccpGlobalTitleTranslationTable Translations { get; }

    /// <summary>Whether the lower-layer receive loop is running.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _receiveLoop is not null && !_receiveLoop.IsCompleted;
            }
        }
    }

    /// <summary>The most recent unhandled receive-loop failure.</summary>
    public Exception? LastFailure
    {
        get
        {
            lock (_sync)
            {
                return _lastFailure;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask SendUnitdataAsync(
        SccpUnitdataMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendEncodedAsync(
            message.Encode(),
            message.CalledParty,
            logicalMessage: true,
            segmented: false,
            ct);
    }

    /// <inheritdoc />
    public async ValueTask<SccpUnitdataMessage> ReceiveUnitdataAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (IsRunning)
        {
            SccpDataIndication indication =
                await ReceiveAsync(ct).ConfigureAwait(false);
            return new(
                indication.ProtocolClass,
                indication.CalledParty,
                indication.CallingParty,
                indication.UserData);
        }

        Mtp3TransferMessage transfer =
            await Network.ReceiveAsync(ct).ConfigureAwait(false);
        EnsureSccpTransfer(transfer);
        if (!SccpUnitdataMessage.TryDecode(
                transfer.UserPayload.Span,
                out SccpUnitdataMessage? message,
                out string? error))
        {
            throw new InvalidDataException(error);
        }

        return message!;
    }

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsRunning)
            {
                return;
            }

            lock (_sync)
            {
                _lastFailure = null;
                _lifetime = new();
                _receiveLoop = ReceiveLoopAsync(_lifetime.Token);
            }

            await Task.Yield();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Task? receiveLoop;
            CancellationTokenSource? lifetime;
            lock (_sync)
            {
                receiveLoop = _receiveLoop;
                lifetime = _lifetime;
            }

            if (receiveLoop is null)
            {
                return;
            }

            lifetime?.Cancel();
            try
            {
                await receiveLoop.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime?.IsCancellationRequested == true)
            {
            }

            lock (_sync)
            {
                _receiveLoop = null;
                _lifetime = null;
            }

            lifetime?.Dispose();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(
        SccpDataRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfDisposed();
        SccpPartyAddress calledParty = TranslateCalledParty(request.CalledParty);
        switch (request.MessageKind)
        {
            case SccpConnectionlessMessageKind.Unitdata:
                await SendUnitdataRequestAsync(request, calledParty, ct)
                    .ConfigureAwait(false);
                break;
            case SccpConnectionlessMessageKind.ExtendedUnitdata:
                await SendExtendedRequestAsync(request, calledParty, ct)
                    .ConfigureAwait(false);
                break;
            case SccpConnectionlessMessageKind.LongUnitdata:
                await SendLongRequestAsync(request, calledParty, ct)
                    .ConfigureAwait(false);
                break;
            default:
                await SendAutomaticAsync(request, calledParty, ct)
                    .ConfigureAwait(false);
                break;
        }

        Interlocked.Increment(ref _sentMessages);
    }

    /// <inheritdoc />
    public ValueTask<SccpDataIndication> ReceiveAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (!IsRunning)
        {
            throw new InvalidOperationException(
                "Start the SCCP service before receiving stateful indications.");
        }

        return _inbound.Reader.ReadAsync(ct);
    }

    /// <inheritdoc />
    public ValueTask<SccpReturnIndication> ReceiveReturnAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (!IsRunning)
        {
            throw new InvalidOperationException(
                "Start the SCCP service before receiving return indications.");
        }

        return _returns.Reader.ReadAsync(ct);
    }

    /// <summary>Returns a point-in-time SCCP service metrics snapshot.</summary>
    /// <returns>The current metrics.</returns>
    public SccpServiceMetrics GetMetrics()
    {
        return new(
            Interlocked.Read(ref _sentMessages),
            Interlocked.Read(ref _receivedMessages),
            Interlocked.Read(ref _sentSegments),
            Interlocked.Read(ref _reassembledMessages),
            Interlocked.Read(ref _returnedMessages),
            Interlocked.Read(ref _unroutableMessages),
            Interlocked.Read(ref _malformedMessages),
            _reassembly.Count);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _disposed = true;
            _inbound.Writer.TryComplete();
            _returns.Writer.TryComplete();
            _lifecycleGate.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Mtp3TransferMessage transfer =
                    await Network.ReceiveAsync(ct).ConfigureAwait(false);
                if (transfer.ServiceInformation.ServiceIndicator
                    != Mtp3ServiceIndicator.Sccp)
                {
                    Interlocked.Increment(ref _malformedMessages);
                    continue;
                }

                await ProcessInboundAsync(transfer, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _lastFailure = ex;
            }
        }
    }

    private async Task ProcessInboundAsync(
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        if (transfer.UserPayload.IsEmpty)
        {
            Interlocked.Increment(ref _malformedMessages);
            return;
        }

        switch ((SccpMessageType)transfer.UserPayload.Span[0])
        {
            case SccpMessageType.Unitdata:
                await ProcessUnitdataAsync(transfer, ct).ConfigureAwait(false);
                break;
            case SccpMessageType.ExtendedUnitdata:
                await ProcessExtendedUnitdataAsync(transfer, ct).ConfigureAwait(false);
                break;
            case SccpMessageType.LongUnitdata:
                await ProcessLongUnitdataAsync(transfer, ct).ConfigureAwait(false);
                break;
            case SccpMessageType.UnitdataService:
                await ProcessUnitdataServiceAsync(transfer, ct).ConfigureAwait(false);
                break;
            default:
                Interlocked.Increment(ref _malformedMessages);
                break;
        }
    }

    private async Task ProcessUnitdataAsync(
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        if (!SccpUnitdataMessage.TryDecode(
                transfer.UserPayload.Span,
                out SccpUnitdataMessage? message,
                out _))
        {
            Interlocked.Increment(ref _malformedMessages);
            return;
        }

        await RouteOrReturnAsync(
            message!.ProtocolClass,
            message.CalledParty,
            message.CallingParty,
            message.UserData,
            SccpConnectionlessMessageKind.Unitdata,
            hopCounter: null,
            segmentationReference: null,
            transfer,
            ct).ConfigureAwait(false);
    }

    private async Task ProcessExtendedUnitdataAsync(
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        if (!SccpExtendedUnitdataMessage.TryDecode(
                transfer.UserPayload.Span,
                out SccpExtendedUnitdataMessage? message,
                out _))
        {
            Interlocked.Increment(ref _malformedMessages);
            return;
        }

        ReadOnlyMemory<byte> payload = message!.UserData;
        uint? segmentationReference = null;
        if (message.Segmentation.HasValue)
        {
            SccpReassemblyResult result = _reassembly.Add(
                transfer.RoutingLabel.OriginatingPointCode,
                message,
                DateTimeOffset.UtcNow);
            if (result.Status == SccpReassemblyStatus.Pending)
            {
                return;
            }

            if (result.Status != SccpReassemblyStatus.Complete)
            {
                Interlocked.Increment(ref _malformedMessages);
                return;
            }

            payload = result.Payload;
            segmentationReference = result.LocalReference;
            Interlocked.Increment(ref _reassembledMessages);
        }

        await RouteOrReturnAsync(
            message.ProtocolClass,
            message.CalledParty,
            message.CallingParty,
            payload,
            SccpConnectionlessMessageKind.ExtendedUnitdata,
            message.HopCounter,
            segmentationReference,
            transfer,
            ct).ConfigureAwait(false);
    }

    private async Task ProcessLongUnitdataAsync(
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        if (!SccpLongUnitdataMessage.TryDecode(
                transfer.UserPayload.Span,
                out SccpLongUnitdataMessage? message,
                out _))
        {
            Interlocked.Increment(ref _malformedMessages);
            return;
        }

        await RouteOrReturnAsync(
            message!.ProtocolClass,
            message.CalledParty,
            message.CallingParty,
            message.UserData,
            SccpConnectionlessMessageKind.LongUnitdata,
            message.HopCounter,
            segmentationReference: null,
            transfer,
            ct).ConfigureAwait(false);
    }

    private async Task ProcessUnitdataServiceAsync(
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        if (!SccpUnitdataServiceMessage.TryDecode(
                transfer.UserPayload.Span,
                out SccpUnitdataServiceMessage? message,
                out _))
        {
            Interlocked.Increment(ref _malformedMessages);
            return;
        }

        await _returns.Writer.WriteAsync(
            new(message!, transfer),
            ct).ConfigureAwait(false);
        Interlocked.Increment(ref _returnedMessages);
    }

    private async Task RouteOrReturnAsync(
        SccpProtocolClass protocolClass,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData,
        SccpConnectionlessMessageKind messageKind,
        byte? hopCounter,
        uint? segmentationReference,
        Mtp3TransferMessage transfer,
        CancellationToken ct)
    {
        bool hasRoutes = Routes.Snapshot().Count > 0;
        bool resolved = Routes.TryResolve(calledParty, out SccpRoute? route);
        if (hasRoutes && !resolved)
        {
            Interlocked.Increment(ref _unroutableMessages);
            if (protocolClass.ReturnMessageOnError)
            {
                await SendUnitdataReturnAsync(
                    SccpReturnCause.NoTranslationForThisSpecificAddress,
                    calledParty,
                    callingParty,
                    userData,
                    transfer,
                    ct).ConfigureAwait(false);
            }

            return;
        }

        await _inbound.Writer.WriteAsync(
            new(
                protocolClass,
                calledParty,
                callingParty,
                userData,
                messageKind,
                hopCounter,
                route?.Name,
                transfer,
                segmentationReference),
            ct).ConfigureAwait(false);
        Interlocked.Increment(ref _receivedMessages);
    }

    private async Task SendAutomaticAsync(
        SccpDataRequest request,
        SccpPartyAddress calledParty,
        CancellationToken ct)
    {
        if (request.UserData.Length <= byte.MaxValue)
        {
            byte[]? encoded = null;
            try
            {
                encoded = new SccpUnitdataMessage(
                    request.ProtocolClass,
                    calledParty,
                    request.CallingParty,
                    request.UserData).Encode();
            }
            catch (InvalidOperationException)
            {
                // Address overhead can make UDT exceed its one-octet layout.
            }

            if (encoded is not null)
            {
                await SendEncodedAsync(
                    encoded,
                    calledParty,
                    logicalMessage: false,
                    segmented: false,
                    ct).ConfigureAwait(false);
                return;
            }
        }

        int segmentSize = GetExtendedSegmentSize(calledParty, request.CallingParty);
        int segmentCount = DivideRoundUp(request.UserData.Length, segmentSize);
        if (_options.UseSegmentationForExtendedData && segmentCount <= 16)
        {
            await SendExtendedRequestAsync(request, calledParty, ct)
                .ConfigureAwait(false);
            return;
        }

        await SendLongRequestAsync(request, calledParty, ct).ConfigureAwait(false);
    }

    private ValueTask SendUnitdataRequestAsync(
        SccpDataRequest request,
        SccpPartyAddress calledParty,
        CancellationToken ct)
    {
        SccpUnitdataMessage message = new(
            request.ProtocolClass,
            calledParty,
            request.CallingParty,
            request.UserData);
        return SendEncodedAsync(
            message.Encode(),
            calledParty,
            logicalMessage: false,
            segmented: false,
            ct);
    }

    private async Task SendExtendedRequestAsync(
        SccpDataRequest request,
        SccpPartyAddress calledParty,
        CancellationToken ct)
    {
        int segmentSize = GetExtendedSegmentSize(calledParty, request.CallingParty);
        int segmentCount = DivideRoundUp(request.UserData.Length, segmentSize);
        if (segmentCount > 16)
        {
            throw new InvalidOperationException(
                "SCCP XUDT segmentation supports at most 16 segments.");
        }

        uint reference = NextSegmentationReference();
        for (int index = 0; index < segmentCount; index++)
        {
            int offset = index * segmentSize;
            int length = Math.Min(segmentSize, request.UserData.Length - offset);
            byte remaining = checked((byte)(segmentCount - index - 1));
            SccpSegmentationParameter? segmentation = segmentCount == 1
                ? null
                : new(reference, remaining, firstSegment: index == 0);
            SccpExtendedUnitdataMessage message = new(
                request.ProtocolClass,
                request.HopCounter,
                calledParty,
                request.CallingParty,
                request.UserData.Slice(offset, length),
                segmentation);
            await SendEncodedAsync(
                message.Encode(),
                calledParty,
                logicalMessage: false,
                segmented: segmentCount > 1,
                ct).ConfigureAwait(false);
        }
    }

    private ValueTask SendLongRequestAsync(
        SccpDataRequest request,
        SccpPartyAddress calledParty,
        CancellationToken ct)
    {
        SccpLongUnitdataMessage message = new(
            request.ProtocolClass,
            request.HopCounter,
            calledParty,
            request.CallingParty,
            request.UserData);
        return SendEncodedAsync(
            message.Encode(),
            calledParty,
            logicalMessage: false,
            segmented: false,
            ct);
    }

    private async Task SendUnitdataReturnAsync(
        SccpReturnCause cause,
        SccpPartyAddress originalCalledParty,
        SccpPartyAddress originalCallingParty,
        ReadOnlyMemory<byte> userData,
        Mtp3TransferMessage originalTransfer,
        CancellationToken ct)
    {
        if (userData.Length > byte.MaxValue)
        {
            return;
        }

        try
        {
            SccpUnitdataServiceMessage returned = new(
                cause,
                calledParty: originalCallingParty,
                callingParty: originalCalledParty,
                userData);
            Mtp3RoutingLabel returnLabel = new(
                originalTransfer.RoutingLabel.OriginatingPointCode,
                originalTransfer.RoutingLabel.DestinationPointCode,
                originalTransfer.RoutingLabel.SignallingLinkSelection);
            await Network.SendAsync(
                new(
                    ServiceInformation,
                    returnLabel,
                    returned.Encode(),
                    originalTransfer.NetworkAppearance,
                    originalTransfer.RoutingContext,
                    originalTransfer.CorrelationId),
                ct).ConfigureAwait(false);
            Interlocked.Increment(ref _returnedMessages);
        }
        catch (InvalidOperationException)
        {
            Interlocked.Increment(ref _malformedMessages);
        }
    }

    private async ValueTask SendEncodedAsync(
        byte[] encoded,
        SccpPartyAddress calledParty,
        bool logicalMessage,
        bool segmented,
        CancellationToken ct)
    {
        ThrowIfDisposed();
        Mtp3RoutingLabel label = calledParty.PointCode.HasValue
            ? new(
                calledParty.PointCode.Value,
                RoutingLabel.OriginatingPointCode,
                RoutingLabel.SignallingLinkSelection)
            : RoutingLabel;
        await Network.SendAsync(
            new(ServiceInformation, label, encoded),
            ct).ConfigureAwait(false);
        if (logicalMessage)
        {
            Interlocked.Increment(ref _sentMessages);
        }

        if (segmented)
        {
            Interlocked.Increment(ref _sentSegments);
        }
    }

    private SccpPartyAddress TranslateCalledParty(SccpPartyAddress calledParty)
    {
        return Translations.TryTranslate(calledParty, out SccpPartyAddress? translated, out _)
            ? translated!
            : calledParty;
    }

    private int GetExtendedSegmentSize(
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty)
    {
        int overhead = 7
            + 1 + calledParty.Encode().Length
            + 1 + callingParty.Encode().Length
            + 1
            + 1 + 1 + SccpSegmentationParameter.EncodedLength + 1;
        int wireMaximum = byte.MaxValue - overhead;
        if (wireMaximum <= 0)
        {
            throw new InvalidOperationException(
                "SCCP addresses leave no room for XUDT user data.");
        }

        return Math.Min(_options.ExtendedSegmentSize, wireMaximum);
    }

    private uint NextSegmentationReference()
    {
        int value = Interlocked.Increment(ref _nextSegmentationReference);
        return (uint)value & 0x00FF_FFFF;
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    private static void EnsureSccpTransfer(Mtp3TransferMessage transfer)
    {
        if (transfer.ServiceInformation.ServiceIndicator != Mtp3ServiceIndicator.Sccp)
        {
            throw new InvalidOperationException(
                $"Expected SCCP service indicator, received {transfer.ServiceInformation.ServiceIndicator}.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
