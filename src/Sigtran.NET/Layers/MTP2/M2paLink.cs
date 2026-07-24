using System.Buffers;
using System.Threading.Channels;

using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Implements an RFC 4165 M2PA signalling link over an SCTP transport.
/// </summary>
public sealed class M2paLink : IMtp2Link, IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly M2paLinkOptions _options;
    private readonly Channel<byte[]> _inbound;
    private readonly M2paRetrievalBuffer _retrievalBuffer;
    private ISctpTransport _transport;
    private bool _ownsTransport;
    private CancellationTokenSource? _lifetime;
    private Task? _receiveTask;
    private TaskCompletionSource<bool>? _peerAlignment;
    private TaskCompletionSource<bool>? _peerReady;
    private TaskCompletionSource<bool>? _processorRecoveryReady;
    private TaskCompletionSource<bool> _remoteAvailable = CreateCompletedSignal();
    private Mtp2LinkState _state = Mtp2LinkState.OutOfService;
    private uint _lastSentSequence = M2paProtocol.MaximumSequenceNumber;
    private uint _lastReceivedSequence = M2paProtocol.MaximumSequenceNumber;
    private bool _localBusy;
    private bool _remoteBusy;
    private bool _localProcessorOutage;
    private bool _remoteProcessorOutage;
    private long _sentUserData;
    private long _receivedUserData;
    private long _sentAcknowledgements;
    private long _receivedAcknowledgements;
    private long _sentLinkStatus;
    private long _receivedLinkStatus;
    private long _acknowledgedUserData;
    private long _discardedOutOfOrder;
    private bool _disposed;

    /// <summary>Creates an M2PA signalling link.</summary>
    /// <param name="transport">The SCTP transport with at least streams 0 and 1.</param>
    /// <param name="options">The link options.</param>
    /// <param name="ownsTransport">Whether the link disposes the SCTP transport.</param>
    public M2paLink(
        ISctpTransport transport,
        M2paLinkOptions? options = null,
        bool ownsTransport = true)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? new M2paLinkOptions();
        _ownsTransport = ownsTransport;
        _retrievalBuffer = new(_options.RetrievalCapacity);
        _inbound = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(_options.InboundQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
    }

    /// <summary>Raised when the M2PA operational link state changes.</summary>
    public event EventHandler<M2paLinkStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public Mtp2LinkState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>Whether local receive congestion is active.</summary>
    public bool LocalBusy
    {
        get
        {
            lock (_sync)
            {
                return _localBusy;
            }
        }
    }

    /// <summary>Whether the peer reported receive congestion.</summary>
    public bool RemoteBusy
    {
        get
        {
            lock (_sync)
            {
                return _remoteBusy;
            }
        }
    }

    /// <summary>Whether the local processor-outage procedure is active.</summary>
    public bool LocalProcessorOutage
    {
        get
        {
            lock (_sync)
            {
                return _localProcessorOutage;
            }
        }
    }

    /// <summary>Whether the peer processor-outage procedure is active.</summary>
    public bool RemoteProcessorOutage
    {
        get
        {
            lock (_sync)
            {
                return _remoteProcessorOutage;
            }
        }
    }

    /// <summary>Starts link alignment and waits until both peers report Ready.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the link is in service.</returns>
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (State == Mtp2LinkState.InService)
            {
                return;
            }

            if (_receiveTask is not null)
            {
                throw new InvalidOperationException("M2PA link alignment is already running.");
            }

            _peerAlignment = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _peerReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _lifetime = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_lifetime.Token);
            SetState(Mtp2LinkState.Aligning, "alignment-started");

            try
            {
                await SendLinkStatusAsync(
                    M2paLinkStatus.OutOfService,
                    M2paReadyContext.Alignment,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);
                await SendLinkStatusAsync(
                    M2paLinkStatus.Alignment,
                    M2paReadyContext.Alignment,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);
                await _peerAlignment.Task.WaitAsync(_options.AlignmentTimeout, ct)
                    .ConfigureAwait(false);

                SetState(Mtp2LinkState.Proving, "peer-alignment-observed");
                M2paLinkStatus proving = _options.EmergencyProving
                    ? M2paLinkStatus.ProvingEmergency
                    : M2paLinkStatus.ProvingNormal;
                await SendLinkStatusAsync(
                    proving,
                    M2paReadyContext.Alignment,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);

                if (_options.ProvingDuration > TimeSpan.Zero)
                {
                    await Task.Delay(_options.ProvingDuration, ct).ConfigureAwait(false);
                }

                await SendLinkStatusAsync(
                    M2paLinkStatus.Ready,
                    M2paReadyContext.Alignment,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);
                await _peerReady.Task.WaitAsync(_options.AlignmentTimeout, ct)
                    .ConfigureAwait(false);
                SetState(Mtp2LinkState.InService, "alignment-ready");
            }
            catch
            {
                SetState(Mtp2LinkState.Failed, "alignment-failed");
                _lifetime.Cancel();
                await AwaitReceiveLoopAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (payload.IsEmpty)
        {
            throw new ArgumentException("M2PA User Data payload must not be empty.", nameof(payload));
        }

        await WaitForRemoteAvailabilityAsync(ct).ConfigureAwait(false);
        EnsureUserDataCanSend();
        await _sendGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            EnsureUserDataCanSend();
            if (_retrievalBuffer.Count >= _retrievalBuffer.Capacity)
            {
                throw new InvalidOperationException("M2PA retrieval buffer is full.");
            }

            uint fsn = M2paProtocol.NextSequenceNumber(_lastSentSequence);
            byte[] rented = ArrayPool<byte>.Shared.Rent(
                M2paProtocol.MinimumMessageLength + payload.Length);
            try
            {
                if (!M2paMessage.TryEncodeUserData(
                        rented,
                        _lastReceivedSequence,
                        fsn,
                        payload.Span,
                        out int written,
                        out string? error))
                {
                    throw new InvalidOperationException(error);
                }

                await _transport.SendAsync(
                    new(
                        rented.AsMemory(0, written).ToArray(),
                        CreateMetadata(M2paProtocol.UserDataStream)),
                    ct).ConfigureAwait(false);
            }
            catch
            {
                SetState(Mtp2LinkState.Failed, "user-data-send-failed");
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            _lastSentSequence = fsn;
            _retrievalBuffer.Add(new(fsn, payload, DateTimeOffset.UtcNow));
            Interlocked.Increment(ref _sentUserData);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<int> ReceiveAsync(
        Memory<byte> buffer,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] payload = await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
        if (buffer.Length < payload.Length)
        {
            throw new ArgumentException(
                $"Receive buffer requires {payload.Length} bytes.",
                nameof(buffer));
        }

        payload.CopyTo(buffer);
        return payload.Length;
    }

    /// <summary>Starts or ends local receive congestion.</summary>
    /// <param name="busy">Whether local receive congestion is active.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the status was sent.</returns>
    public async ValueTask SetLocalBusyAsync(
        bool busy,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        bool changed;
        lock (_sync)
        {
            changed = _localBusy != busy;
            _localBusy = busy;
        }

        if (!changed)
        {
            return;
        }

        await SendLinkStatusAsync(
            busy ? M2paLinkStatus.Busy : M2paLinkStatus.BusyEnded,
            M2paReadyContext.Alignment,
            ReadOnlyMemory<byte>.Empty,
            ct).ConfigureAwait(false);

        if (!busy)
        {
            await SendAcknowledgementAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Starts or ends the local processor-outage procedure.</summary>
    /// <param name="outage">Whether the local processor is unavailable.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the status was sent.</returns>
    public async ValueTask SetLocalProcessorOutageAsync(
        bool outage,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        bool changed;
        lock (_sync)
        {
            changed = _localProcessorOutage != outage;
            _localProcessorOutage = outage;
        }

        if (!changed)
        {
            return;
        }

        if (outage)
        {
            _processorRecoveryReady =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            SetState(Mtp2LinkState.ProcessorOutage, "local-processor-outage");
        }

        await SendLinkStatusAsync(
            outage
                ? M2paLinkStatus.ProcessorOutage
                : M2paLinkStatus.ProcessorRecovered,
            M2paReadyContext.ProcessorRecovery,
            ReadOnlyMemory<byte>.Empty,
            ct).ConfigureAwait(false);

        if (outage)
        {
            return;
        }

        TaskCompletionSource<bool>? recovery = _processorRecoveryReady;
        if (recovery is null)
        {
            throw new InvalidOperationException(
                "M2PA processor recovery was not initialized.");
        }

        await recovery.Task.WaitAsync(_options.AlignmentTimeout, ct)
            .ConfigureAwait(false);
        await SendLinkStatusAsync(
            M2paLinkStatus.Ready,
            M2paReadyContext.ProcessorRecovery,
            ReadOnlyMemory<byte>.Empty,
            ct).ConfigureAwait(false);
        _processorRecoveryReady = null;
        if (!RemoteProcessorOutage)
        {
            SetState(Mtp2LinkState.InService, "local-processor-recovered");
        }
    }

    /// <summary>Returns User Data retained for MTP3 changeover retrieval.</summary>
    /// <returns>The unacknowledged User Data entries.</returns>
    public IReadOnlyList<M2paRetrievalEntry> RetrieveUnacknowledged()
    {
        return _retrievalBuffer.Snapshot();
    }

    /// <summary>Returns a point-in-time M2PA metrics snapshot.</summary>
    /// <returns>The current M2PA link metrics.</returns>
    public M2paLinkMetrics GetMetrics()
    {
        return new(
            State,
            Interlocked.Read(ref _sentUserData),
            Interlocked.Read(ref _receivedUserData),
            Interlocked.Read(ref _sentAcknowledgements),
            Interlocked.Read(ref _receivedAcknowledgements),
            Interlocked.Read(ref _sentLinkStatus),
            Interlocked.Read(ref _receivedLinkStatus),
            Interlocked.Read(ref _acknowledgedUserData),
            Interlocked.Read(ref _discardedOutOfOrder),
            _retrievalBuffer.Count);
    }

    /// <summary>Stops the link and sends Out of Service.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the receive loop stops.</returns>
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await StopCoreAsync(sendOutOfService: true, ct).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>Replaces a failed transport and starts a new alignment procedure.</summary>
    /// <param name="replacement">The replacement SCTP transport.</param>
    /// <param name="ownsTransport">Whether the link owns the replacement transport.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the recovered link is in service.</returns>
    public async ValueTask RecoverAsync(
        ISctpTransport replacement,
        bool ownsTransport = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await StopCoreAsync(sendOutOfService: false, ct).ConfigureAwait(false);
            if (_ownsTransport)
            {
                await _transport.DisposeAsync().ConfigureAwait(false);
            }

            _transport = replacement;
            _ownsTransport = ownsTransport;
            ResetSequenceState();
        }
        finally
        {
            _lifecycleGate.Release();
        }

        await StartAsync(ct).ConfigureAwait(false);
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
            if (_ownsTransport)
            {
                await _transport.DisposeAsync().ConfigureAwait(false);
            }

            _lifetime?.Dispose();
            _lifecycleGate.Dispose();
            _sendGate.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(_options.MaximumMessageSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                SctpReceiveResult received =
                    await _transport.ReceiveAsync(rented, ct).ConfigureAwait(false);
                if (received.BytesReceived == 0)
                {
                    throw new EndOfStreamException("The M2PA SCTP association closed.");
                }

                if (!M2paMessage.TryDecode(
                        rented.AsSpan(0, received.BytesReceived),
                        out M2paMessage? message,
                        out string? decodeError))
                {
                    continue;
                }

                if (!M2paProtocol.TryValidateSctpMetadata(
                        message!,
                        received.Metadata,
                        out _))
                {
                    continue;
                }

                int acknowledged =
                    _retrievalBuffer.AcknowledgeThrough(message!.BackwardSequenceNumber);
                if (acknowledged > 0)
                {
                    Interlocked.Add(ref _acknowledgedUserData, acknowledged);
                }

                if (message.MessageType == M2paMessageType.LinkStatus)
                {
                    await ProcessLinkStatusAsync(message, ct).ConfigureAwait(false);
                }
                else
                {
                    await ProcessUserDataAsync(message, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch
        {
            SetState(Mtp2LinkState.Failed, "receive-loop-failed");
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private async Task ProcessUserDataAsync(
        M2paMessage message,
        CancellationToken ct)
    {
        if (message.IsAcknowledgementOnly)
        {
            Interlocked.Increment(ref _receivedAcknowledgements);
            return;
        }

        uint expected = M2paProtocol.NextSequenceNumber(_lastReceivedSequence);
        if (message.ForwardSequenceNumber != expected)
        {
            Interlocked.Increment(ref _discardedOutOfOrder);
            return;
        }

        await _inbound.Writer.WriteAsync(message.Payload.ToArray(), ct)
            .ConfigureAwait(false);
        _lastReceivedSequence = message.ForwardSequenceNumber;
        Interlocked.Increment(ref _receivedUserData);

        bool deferAcknowledgement;
        lock (_sync)
        {
            deferAcknowledgement = _localBusy || _localProcessorOutage;
        }

        if (!deferAcknowledgement)
        {
            await SendAcknowledgementAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessLinkStatusAsync(
        M2paMessage message,
        CancellationToken ct)
    {
        Interlocked.Increment(ref _receivedLinkStatus);
        switch (message.LinkStatus!.Value)
        {
            case M2paLinkStatus.Alignment:
            case M2paLinkStatus.ProvingNormal:
            case M2paLinkStatus.ProvingEmergency:
                _peerAlignment?.TrySetResult(true);
                break;
            case M2paLinkStatus.Ready:
                _peerReady?.TrySetResult(true);
                _processorRecoveryReady?.TrySetResult(true);
                if (!LocalProcessorOutage
                    && !RemoteProcessorOutage
                    && State is Mtp2LinkState.ProcessorOutage or Mtp2LinkState.Busy)
                {
                    SetState(Mtp2LinkState.InService, "peer-ready");
                }

                break;
            case M2paLinkStatus.ProcessorOutage:
                lock (_sync)
                {
                    _remoteProcessorOutage = true;
                }

                SetState(Mtp2LinkState.ProcessorOutage, "remote-processor-outage");
                break;
            case M2paLinkStatus.ProcessorRecovered:
                lock (_sync)
                {
                    _remoteProcessorOutage = false;
                }

                await SendLinkStatusAsync(
                    M2paLinkStatus.Ready,
                    M2paReadyContext.ProcessorRecovery,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);
                break;
            case M2paLinkStatus.Busy:
                BeginRemoteBusy();
                SetState(Mtp2LinkState.Busy, "remote-busy");
                break;
            case M2paLinkStatus.BusyEnded:
                EndRemoteBusy();
                if (!LocalProcessorOutage && !RemoteProcessorOutage)
                {
                    SetState(Mtp2LinkState.InService, "remote-busy-ended");
                }

                break;
            case M2paLinkStatus.OutOfService:
                SetState(Mtp2LinkState.OutOfService, "remote-out-of-service");
                break;
        }
    }

    private async ValueTask SendAcknowledgementAsync(CancellationToken ct)
    {
        await _sendGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            byte[] buffer = new byte[M2paProtocol.MinimumMessageLength];
            if (!M2paMessage.TryEncodeUserData(
                    buffer,
                    _lastReceivedSequence,
                    _lastSentSequence,
                    ReadOnlySpan<byte>.Empty,
                    out int written,
                    out string? error))
            {
                throw new InvalidOperationException(error);
            }

            await _transport.SendAsync(
                new(
                    buffer.AsMemory(0, written),
                    CreateMetadata(M2paProtocol.UserDataStream)),
                ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sentAcknowledgements);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private async ValueTask SendLinkStatusAsync(
        M2paLinkStatus status,
        M2paReadyContext readyContext,
        ReadOnlyMemory<byte> provingFiller,
        CancellationToken ct)
    {
        await _sendGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            byte[] buffer = new byte[
                M2paProtocol.MinimumMessageLength
                + sizeof(uint)
                + provingFiller.Length];
            if (!M2paMessage.TryEncodeLinkStatus(
                    buffer,
                    _lastReceivedSequence,
                    _lastSentSequence,
                    status,
                    provingFiller.Span,
                    out int written,
                    out string? error))
            {
                throw new InvalidOperationException(error);
            }

            await _transport.SendAsync(
                new(
                    buffer.AsMemory(0, written),
                    CreateMetadata(M2paProtocol.GetStream(status, readyContext))),
                ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sentLinkStatus);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private async Task WaitForRemoteAvailabilityAsync(CancellationToken ct)
    {
        Task wait;
        lock (_sync)
        {
            wait = _remoteAvailable.Task;
        }

        await wait.WaitAsync(ct).ConfigureAwait(false);
    }

    private void BeginRemoteBusy()
    {
        lock (_sync)
        {
            if (_remoteBusy)
            {
                return;
            }

            _remoteBusy = true;
            _remoteAvailable =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private void EndRemoteBusy()
    {
        TaskCompletionSource<bool>? available = null;
        lock (_sync)
        {
            if (!_remoteBusy)
            {
                return;
            }

            _remoteBusy = false;
            available = _remoteAvailable;
        }

        available.TrySetResult(true);
    }

    private async Task StopCoreAsync(
        bool sendOutOfService,
        CancellationToken ct)
    {
        if (_receiveTask is null)
        {
            SetState(Mtp2LinkState.OutOfService, "link-stopped");
            return;
        }

        SetState(Mtp2LinkState.ShuttingDown, "shutdown-started");
        if (sendOutOfService)
        {
            try
            {
                await SendLinkStatusAsync(
                    M2paLinkStatus.OutOfService,
                    M2paReadyContext.Alignment,
                    ReadOnlyMemory<byte>.Empty,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is IOException
                or InvalidOperationException
                or OperationCanceledException)
            {
            }
        }

        _lifetime?.Cancel();
        await AwaitReceiveLoopAsync().ConfigureAwait(false);
        _lifetime?.Dispose();
        _lifetime = null;
        _receiveTask = null;
        SetState(Mtp2LinkState.OutOfService, "link-stopped");
    }

    private async Task AwaitReceiveLoopAsync()
    {
        if (_receiveTask is null)
        {
            return;
        }

        try
        {
            await _receiveTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch when (State == Mtp2LinkState.Failed)
        {
        }
    }

    private void EnsureUserDataCanSend()
    {
        Mtp2LinkState state = State;
        if (state != Mtp2LinkState.InService)
        {
            throw new InvalidOperationException(
                $"M2PA User Data cannot be sent while link state is {state}.");
        }
    }

    private void ResetSequenceState()
    {
        _lastSentSequence = M2paProtocol.MaximumSequenceNumber;
        _lastReceivedSequence = M2paProtocol.MaximumSequenceNumber;
        lock (_sync)
        {
            _localBusy = false;
            _remoteBusy = false;
            _localProcessorOutage = false;
            _remoteProcessorOutage = false;
            _processorRecoveryReady = null;
            _remoteAvailable = CreateCompletedSignal();
        }
    }

    private void SetState(Mtp2LinkState state, string reason)
    {
        Mtp2LinkState previous;
        lock (_sync)
        {
            previous = _state;
            if (previous == state)
            {
                return;
            }

            _state = state;
        }

        EventHandler<M2paLinkStateChangedEventArgs>? handler = StateChanged;
        if (handler is null)
        {
            return;
        }

        try
        {
            handler(
                this,
                new(previous, state, DateTimeOffset.UtcNow, reason));
        }
        catch
        {
            // Link observers must not interrupt protocol processing.
        }
    }

    private static SctpPayloadMetadata CreateMetadata(ushort streamId)
    {
        return new(
            streamId,
            SctpPayloadProtocolIdentifiers.M2pa,
            unordered: false);
    }

    private static TaskCompletionSource<bool> CreateCompletedSignal()
    {
        TaskCompletionSource<bool> signal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult(true);
        return signal;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
