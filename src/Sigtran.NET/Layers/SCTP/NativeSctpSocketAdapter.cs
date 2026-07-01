using System.Net.Sockets;

using Sigtran.NET.Core.Interfaces;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Wraps a native SCTP socket as the SDK packet transport contract.
/// </summary>
public sealed class NativeSctpSocketAdapter : ISctpSocket, ISctpTransport, ISctpAssociation
{
    private readonly Socket _socket;
    private readonly SctpConnectionOptions _options;
    private readonly NativeSctpTransportOptions _transportOptions;
    private long _sentMessages;
    private long _receivedMessages;
    private long _queuedSendMessages;
    private long _queuedSendBytes;
    private long _pendingReceiveOperations;
    private long _backpressureRejectedMessages;
    private long _gracefulShutdowns;
    private bool _disposed;
    private SctpAssociationState _associationState;
    private readonly SctpAssociationJournal _associationJournal = new();
    private SctpBackpressureDecision? _lastBackpressureDecision;
    private SctpRecoveryDecision? _lastRecoveryDecision;
    private SctpOperationCancellationBudget? _activeOperationBudget;

    /// <summary>Creates a native SCTP socket adapter.</summary>
    /// <param name="socket">The native SCTP socket.</param>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="associationState">The initial association state.</param>
    /// <param name="transportOptions">The production transport behavior options.</param>
    public NativeSctpSocketAdapter(
        Socket socket,
        SctpConnectionOptions options,
        SctpAssociationState associationState = SctpAssociationState.Closed,
        NativeSctpTransportOptions? transportOptions = null)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _transportOptions = transportOptions ?? new NativeSctpTransportOptions();
        _associationState = associationState;
        if (associationState == SctpAssociationState.Established)
        {
            _associationJournal.Record(new(SctpAssociationEventType.Established, SctpAssociationState.Established, "native SCTP adapter created established"));
        }
    }

    /// <summary>The current association state.</summary>
    public SctpAssociationState AssociationState => _disposed ? SctpAssociationState.Closed : _associationState;

    /// <inheritdoc />
    public ISctpAssociation Association => this;

    /// <inheritdoc />
    public SctpAssociationState State => AssociationState;

    /// <summary>The recorded association lifecycle events.</summary>
    public IReadOnlyList<SctpAssociationJournalEntry> AssociationEvents => _associationJournal.Snapshot();

    /// <summary>The latest send backpressure decision.</summary>
    public SctpBackpressureDecision? LastBackpressureDecision => _lastBackpressureDecision;

    /// <summary>The latest recovery decision produced from a transport fault.</summary>
    public SctpRecoveryDecision? LastRecoveryDecision => _lastRecoveryDecision;

    /// <summary>The active operation timeout budget, when an operation is in progress.</summary>
    public SctpOperationCancellationBudget? ActiveOperationBudget => _activeOperationBudget;

    /// <summary>Marks the association as established.</summary>
    public void MarkEstablished()
    {
        ThrowIfDisposed();
        _associationState = SctpAssociationState.Established;
        _associationJournal.Record(new(SctpAssociationEventType.Established, SctpAssociationState.Established));
    }

    /// <summary>Marks the association as reconnecting.</summary>
    /// <param name="reason">The optional reconnect reason.</param>
    public void MarkReconnecting(string? reason = null)
    {
        ThrowIfDisposed();
        _associationState = SctpAssociationState.Reconnecting;
        _associationJournal.Record(new(SctpAssociationEventType.ReconnectStarted, SctpAssociationState.Reconnecting, reason));
    }

    /// <summary>Marks the association as failed.</summary>
    /// <param name="reason">The optional failure reason.</param>
    public void MarkFailed(string? reason = null)
    {
        if (!_disposed)
        {
            _associationState = SctpAssociationState.Failed;
            _associationJournal.Record(new(SctpAssociationEventType.Failed, SctpAssociationState.Failed, reason));
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        await SendCoreAsync(
            new SctpOutboundMessage(
                data,
                new SctpPayloadMetadata(streamId: 0, payloadProtocolIdentifier: _options.DefaultPayloadProtocolIdentifier)),
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async ValueTask ISctpTransport.SendAsync(SctpOutboundMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await SendCoreAsync(message, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        SctpReceiveResult result = await ReceiveCoreAsync(buffer, ct).ConfigureAwait(false);
        return result.BytesReceived;
    }

    /// <inheritdoc />
    async ValueTask<SctpReceiveResult> ISctpTransport.ReceiveAsync(Memory<byte> buffer, CancellationToken ct)
    {
        return await ReceiveCoreAsync(buffer, ct).ConfigureAwait(false);
    }

    /// <summary>Captures a native SCTP transport health snapshot.</summary>
    /// <returns>The transport health snapshot.</returns>
    public SctpTransportHealth GetHealthSnapshot()
    {
        return new(
            AssociationState,
            _options.RemoteEndpoint,
            _options.LocalEndpoint,
            _options.OutboundStreams,
            _options.InboundStreams,
            _options.DefaultPayloadProtocolIdentifier,
            Interlocked.Read(ref _sentMessages),
            Interlocked.Read(ref _receivedMessages));
    }

    /// <summary>Captures send and receive queue metrics for this transport.</summary>
    /// <returns>The queue metrics snapshot.</returns>
    public SctpTransportQueueMetrics GetQueueMetrics()
    {
        return new(
            checked((int)Interlocked.Read(ref _queuedSendMessages)),
            Interlocked.Read(ref _queuedSendBytes),
            checked((int)Interlocked.Read(ref _pendingReceiveOperations)),
            Interlocked.Read(ref _sentMessages),
            Interlocked.Read(ref _receivedMessages),
            Interlocked.Read(ref _backpressureRejectedMessages),
            Interlocked.Read(ref _gracefulShutdowns));
    }

    /// <summary>Captures a transport diagnostics snapshot from current state.</summary>
    /// <returns>The transport diagnostics snapshot.</returns>
    public SctpTransportDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        return new(
            GetHealthSnapshot(),
            AssociationEvents,
            lastBackpressureDecision: _lastBackpressureDecision,
            lastRecoveryDecision: _lastRecoveryDecision,
            activeOperationBudget: _activeOperationBudget);
    }

    /// <inheritdoc />
    public IReadOnlyList<SctpAssociationJournalEntry> SnapshotEvents()
    {
        return _associationJournal.Snapshot();
    }

    /// <summary>Shuts down the native SCTP association gracefully before disposal.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when shutdown has been requested.</returns>
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            return;
        }

        _associationState = SctpAssociationState.ShuttingDown;
        _associationJournal.Record(new(SctpAssociationEventType.ShutdownStarted, SctpAssociationState.ShuttingDown));
        SctpOperationCancellationBudget budget = StartBudget(SctpOperationKind.Shutdown, ct);
        try
        {
            await WaitForSocketAsync(SelectMode.SelectWrite, budget, ct).ConfigureAwait(false);
            _socket.Shutdown(SocketShutdown.Both);
            Interlocked.Increment(ref _gracefulShutdowns);
            _associationState = SctpAssociationState.Closed;
            _associationJournal.Record(new(SctpAssociationEventType.Closed, SctpAssociationState.Closed, "graceful shutdown"));
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or OperationCanceledException)
        {
            RecordFault(ToFault(ex, SctpOperationKind.Shutdown));
            throw;
        }
        finally
        {
            if (ReferenceEquals(_activeOperationBudget, budget))
            {
                _activeOperationBudget = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _associationState = SctpAssociationState.Closed;
        _associationJournal.Record(new(SctpAssociationEventType.Closed, SctpAssociationState.Closed, "disposed"));
        _socket.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeSctpSocketAdapter));
        }
    }

    private async ValueTask SendCoreAsync(SctpOutboundMessage message, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);

        SctpSendQueueSnapshot queue = new(
            checked((int)Interlocked.Read(ref _queuedSendMessages)),
            Interlocked.Read(ref _queuedSendBytes));
        _lastBackpressureDecision = _transportOptions.BackpressurePolicy.Evaluate(queue, message);
        if (!_lastBackpressureDecision.CanAccept)
        {
            Interlocked.Increment(ref _backpressureRejectedMessages);
            RecordFault(new(SctpTransportFaultKind.BackpressureRejected, _lastBackpressureDecision.Reason));
            throw new InvalidOperationException(_lastBackpressureDecision.Describe());
        }

        Interlocked.Increment(ref _queuedSendMessages);
        Interlocked.Add(ref _queuedSendBytes, message.Payload.Length);
        SctpOperationCancellationBudget budget = StartBudget(SctpOperationKind.Send, ct);
        try
        {
            await WaitForSocketAsync(SelectMode.SelectWrite, budget, ct).ConfigureAwait(false);
            int sent = NativeSctpInterop.SendMessage(_socket, message);
            if (sent != message.Payload.Length)
            {
                throw new InvalidDataException($"Native SCTP send wrote {sent} bytes for a {message.Payload.Length} byte message.");
            }

            Interlocked.Increment(ref _sentMessages);
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or OperationCanceledException or DllNotFoundException or EntryPointNotFoundException)
        {
            RecordFault(ToFault(ex, SctpOperationKind.Send));
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _queuedSendMessages);
            Interlocked.Add(ref _queuedSendBytes, -message.Payload.Length);
            if (ReferenceEquals(_activeOperationBudget, budget))
            {
                _activeOperationBudget = null;
            }
        }
    }

    private async ValueTask<SctpReceiveResult> ReceiveCoreAsync(Memory<byte> buffer, CancellationToken ct)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty)
        {
            throw new ArgumentException("Receive buffer must not be empty.", nameof(buffer));
        }

        Interlocked.Increment(ref _pendingReceiveOperations);
        SctpOperationCancellationBudget budget = StartBudget(SctpOperationKind.Receive, ct);
        try
        {
            while (true)
            {
                await WaitForSocketAsync(SelectMode.SelectRead, budget, ct).ConfigureAwait(false);
                SctpReceiveResult result = NativeSctpInterop.ReceiveMessage(_socket, buffer);
                if (result.BytesReceived > 0)
                {
                    Interlocked.Increment(ref _receivedMessages);
                    return result;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or OperationCanceledException or DllNotFoundException or EntryPointNotFoundException)
        {
            RecordFault(ToFault(ex, SctpOperationKind.Receive));
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _pendingReceiveOperations);
            if (ReferenceEquals(_activeOperationBudget, budget))
            {
                _activeOperationBudget = null;
            }
        }
    }

    private SctpOperationCancellationBudget StartBudget(SctpOperationKind operationKind, CancellationToken ct)
    {
        SctpOperationCancellationBudget budget = _transportOptions.TimeoutPolicy.CreateBudget(operationKind, DateTimeOffset.UtcNow, ct);
        _activeOperationBudget = budget;
        return budget;
    }

    private async Task WaitForSocketAsync(SelectMode mode, SctpOperationCancellationBudget budget, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (budget.IsTimedOut(DateTimeOffset.UtcNow))
            {
                throw new TimeoutException($"{budget.OperationKind} timed out at {budget.DeadlineUtc:O}.");
            }

            if (_socket.Poll(100_000, mode))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), ct).ConfigureAwait(false);
        }
    }

    private void RecordFault(SctpTransportFault fault)
    {
        SctpReconnectSchedule schedule = SctpReconnectSchedules.Create(_transportOptions.ReconnectPolicy, DateTimeOffset.UtcNow);
        _lastRecoveryDecision = SctpFaultRecovery.Decide(fault, schedule);
        if (fault.Kind is not SctpTransportFaultKind.BackpressureRejected and not SctpTransportFaultKind.CallerCancelled)
        {
            MarkFailed(fault.Reason);
        }
    }

    private SctpTransportFault ToFault(Exception exception, SctpOperationKind operationKind)
    {
        SctpTransportFaultKind kind = exception switch
        {
            OperationCanceledException => SctpTransportFaultKind.CallerCancelled,
            TimeoutException when operationKind == SctpOperationKind.Send => SctpTransportFaultKind.SendTimeout,
            TimeoutException when operationKind == SctpOperationKind.Receive => SctpTransportFaultKind.ReceiveTimeout,
            TimeoutException when operationKind == SctpOperationKind.Shutdown => SctpTransportFaultKind.ShutdownTimeout,
            DllNotFoundException or EntryPointNotFoundException => SctpTransportFaultKind.SocketUnavailable,
            SocketException => SctpTransportFaultKind.SocketError,
            _ => SctpTransportFaultKind.Unknown
        };

        return new(kind, exception.Message, exception);
    }
}
