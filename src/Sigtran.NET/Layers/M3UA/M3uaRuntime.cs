using System.Buffers.Binary;
using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Runs an M3UA ASP as a long-lived, reconnecting MTP3 network service.
/// </summary>
public sealed class M3uaRuntime : IMtp3Network, IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly object _heartbeatSync = new();
    private readonly IM3uaRuntimeSessionFactory _sessionFactory;
    private readonly M3uaRuntimeOptions _options;
    private readonly Channel<Mtp3TransferMessage> _outbound;
    private readonly Channel<Mtp3TransferMessage> _inbound;
    private CancellationTokenSource? _lifetime;
    private Task? _runTask;
    private TaskCompletionSource<bool>? _firstActivation;
    private TaskCompletionSource<bool>? _pendingHeartbeat;
    private byte[]? _pendingHeartbeatData;
    private string? _associationName;
    private M3uaRuntimeState _state = M3uaRuntimeState.Stopped;
    private int _outboundQueueDepth;
    private int _inboundQueueDepth;
    private long _sentTransfers;
    private long _receivedTransfers;
    private long _heartbeatsSent;
    private long _heartbeatsAcknowledged;
    private long _heartbeatTimeouts;
    private long _reconnectAttempts;
    private long _faults;
    private long _heartbeatSequence;
    private bool _disposed;

    /// <summary>Creates a long-running M3UA runtime.</summary>
    /// <param name="sessionFactory">The factory used to open or fail over transport sessions.</param>
    /// <param name="options">The runtime options.</param>
    public M3uaRuntime(
        IM3uaRuntimeSessionFactory sessionFactory,
        M3uaRuntimeOptions? options = null)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _options = options ?? new M3uaRuntimeOptions();
        _outbound = Channel.CreateBounded<Mtp3TransferMessage>(
            new BoundedChannelOptions(_options.OutboundQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        _inbound = Channel.CreateBounded<Mtp3TransferMessage>(
            new BoundedChannelOptions(_options.InboundQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
    }

    /// <summary>Raised when the runtime records a lifecycle, traffic, or fault event.</summary>
    public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

    /// <summary>The current runtime lifecycle state.</summary>
    public M3uaRuntimeState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>The active association name, when a session is open.</summary>
    public string? AssociationName
    {
        get
        {
            lock (_sync)
            {
                return _associationName;
            }
        }
    }

    /// <summary>Starts the runtime and waits for the first successful ASP activation.</summary>
    /// <param name="ct">A cancellation token for the startup wait.</param>
    /// <returns>A value task that completes when the ASP becomes active.</returns>
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        Task activationTask;
        bool starting = false;

        lock (_sync)
        {
            if (_runTask is not null)
            {
                if (_state == M3uaRuntimeState.Active)
                {
                    return;
                }

                activationTask = _firstActivation?.Task
                    ?? throw new InvalidOperationException("M3UA runtime startup state is inconsistent.");
            }
            else
            {
                _lifetime = new CancellationTokenSource();
                _firstActivation = new(TaskCreationOptions.RunContinuationsAsynchronously);
                activationTask = _firstActivation.Task;
                _state = M3uaRuntimeState.Starting;
                starting = true;
                _runTask = RunAsync(_lifetime.Token);
            }
        }

        if (starting)
        {
            RaiseEvent(M3uaRuntimeEventKind.StateChanged, "runtime-start");
        }

        await activationTask.WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        EnsureStarted();
        Interlocked.Increment(ref _outboundQueueDepth);
        try
        {
            await _outbound.Writer.WriteAsync(message, ct).ConfigureAwait(false);
        }
        catch
        {
            Interlocked.Decrement(ref _outboundQueueDepth);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Mtp3TransferMessage> ReceiveAsync(
        CancellationToken ct = default)
    {
        EnsureStarted();
        Mtp3TransferMessage message =
            await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
        Interlocked.Decrement(ref _inboundQueueDepth);
        return message;
    }

    /// <summary>Returns a point-in-time runtime metrics snapshot.</summary>
    /// <returns>The current runtime metrics.</returns>
    public M3uaRuntimeMetrics GetMetrics()
    {
        return new(
            State,
            Volatile.Read(ref _outboundQueueDepth),
            Volatile.Read(ref _inboundQueueDepth),
            Interlocked.Read(ref _sentTransfers),
            Interlocked.Read(ref _receivedTransfers),
            Interlocked.Read(ref _heartbeatsSent),
            Interlocked.Read(ref _heartbeatsAcknowledged),
            Interlocked.Read(ref _heartbeatTimeouts),
            Interlocked.Read(ref _reconnectAttempts),
            Interlocked.Read(ref _faults));
    }

    /// <summary>Stops the runtime and waits for graceful shutdown.</summary>
    /// <param name="ct">A cancellation token for the shutdown wait.</param>
    /// <returns>A value task that completes when the runtime has stopped.</returns>
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        Task? runTask;
        CancellationTokenSource? lifetime;
        bool stopping = false;

        lock (_sync)
        {
            runTask = _runTask;
            lifetime = _lifetime;
            if (runTask is null)
            {
                return;
            }

            if (_state != M3uaRuntimeState.Stopping)
            {
                _state = M3uaRuntimeState.Stopping;
                stopping = true;
            }
        }

        if (stopping)
        {
            RaiseEvent(M3uaRuntimeEventKind.StateChanged, "runtime-stop");
        }

        lifetime!.Cancel();
        await runTask.WaitAsync(_options.ShutdownTimeout, ct).ConfigureAwait(false);
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
            _outbound.Writer.TryComplete();
            _inbound.Writer.TryComplete();
            _lifetime?.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        int recoveryAttempt = 0;
        bool activatedOnce = false;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                M3uaRuntimeSessionLease? lease = null;
                try
                {
                    if (activatedOnce || recoveryAttempt > 0)
                    {
                        Transition(M3uaRuntimeState.Reconnecting, "opening-replacement-session");
                    }

                    lease = await _sessionFactory.OpenAsync(ct).ConfigureAwait(false);
                    SetAssociationName(lease.AssociationName);

                    M3uaAspClient client = new(lease.Session);
                    await client.StartAsync(_options.StartupOptions, ct).ConfigureAwait(false);

                    recoveryAttempt = 0;
                    activatedOnce = true;
                    Transition(M3uaRuntimeState.Active, "asp-active");
                    _firstActivation?.TrySetResult(true);
                    RaiseEvent(M3uaRuntimeEventKind.AspActivated, "asp-startup-complete");

                    await RunActiveSessionAsync(lease.Session, ct).ConfigureAwait(false);
                    throw new EndOfStreamException("The active M3UA session ended.");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _faults);
                    RaiseEvent(M3uaRuntimeEventKind.FaultObserved, ex.Message);
                    if (lease is not null)
                    {
                        lease.Session.TryNotifyTransportLost(out _, out _);
                    }

                    recoveryAttempt++;
                    if (!_options.ReconnectPolicy.IsEnabled
                        || recoveryAttempt > _options.ReconnectPolicy.MaxAttempts)
                    {
                        Transition(M3uaRuntimeState.Faulted, "reconnect-policy-exhausted");
                        _firstActivation?.TrySetException(ex);
                        return;
                    }

                    Interlocked.Increment(ref _reconnectAttempts);
                    TimeSpan delay = _options.ReconnectPolicy.GetDelay(recoveryAttempt);
                    RaiseEvent(
                        M3uaRuntimeEventKind.ReconnectScheduled,
                        $"attempt={recoveryAttempt} delayMs={delay.TotalMilliseconds:0}");
                    Transition(M3uaRuntimeState.Reconnecting, "reconnect-scheduled");
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ClearHeartbeat();
                    if (lease is not null)
                    {
                        await TryGracefulShutdownAsync(lease.Session).ConfigureAwait(false);
                        await lease.Session.DisposeAsync().ConfigureAwait(false);
                    }

                    SetAssociationName(null);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancellation can be observed while a reconnect delay is running,
            // outside the active-session exception boundary.
        }
        finally
        {
            bool stopped = false;
            lock (_sync)
            {
                _firstActivation?.TrySetCanceled(ct);
                _runTask = null;
                _lifetime?.Dispose();
                _lifetime = null;
                if (_state != M3uaRuntimeState.Faulted)
                {
                    stopped = _state != M3uaRuntimeState.Stopped;
                    _state = M3uaRuntimeState.Stopped;
                }
            }

            if (stopped)
            {
                RaiseEvent(M3uaRuntimeEventKind.StateChanged, "runtime-stopped");
            }

            RaiseEvent(M3uaRuntimeEventKind.ShutdownCompleted, "runtime-stopped");
        }
    }

    private async Task RunActiveSessionAsync(
        M3uaTransportSession session,
        CancellationToken ct)
    {
        using CancellationTokenSource active = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using SemaphoreSlim sendLock = new(1, 1);

        Task receiveTask = ReceiveLoopAsync(session, sendLock, active.Token);
        Task sendTask = SendLoopAsync(session, sendLock, active.Token);
        Task heartbeatTask = _options.HeartbeatsEnabled
            ? HeartbeatLoopAsync(session, sendLock, active.Token)
            : Task.Delay(Timeout.InfiniteTimeSpan, active.Token);

        Task completed = await Task.WhenAny(receiveTask, sendTask, heartbeatTask)
            .ConfigureAwait(false);
        active.Cancel();

        try
        {
            await Task.WhenAll(receiveTask, sendTask, heartbeatTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch
        {
            await completed.ConfigureAwait(false);
            throw;
        }

        if (!ct.IsCancellationRequested)
        {
            throw new EndOfStreamException("An active M3UA runtime loop stopped unexpectedly.");
        }
    }

    private async Task SendLoopAsync(
        M3uaTransportSession session,
        SemaphoreSlim sendLock,
        CancellationToken ct)
    {
        await foreach (Mtp3TransferMessage message in _outbound.Reader.ReadAllAsync(ct)
            .ConfigureAwait(false))
        {
            Interlocked.Decrement(ref _outboundQueueDepth);
            await sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await session.SendPayloadDataAsync(
                    message.UserPayload,
                    message.RoutingLabel.OriginatingPointCode,
                    message.RoutingLabel.DestinationPointCode,
                    (byte)message.ServiceInformation.ServiceIndicator,
                    message.ServiceInformation.NetworkIndicator,
                    message.ServiceInformation.MessagePriority,
                    message.RoutingLabel.SignallingLinkSelection,
                    message.NetworkAppearance,
                    message.RoutingContext,
                    message.CorrelationId,
                    ct).ConfigureAwait(false);
            }
            finally
            {
                sendLock.Release();
            }

            Interlocked.Increment(ref _sentTransfers);
            RaiseEvent(M3uaRuntimeEventKind.TransferSent);
        }
    }

    private async Task ReceiveLoopAsync(
        M3uaTransportSession session,
        SemaphoreSlim sendLock,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            M3uaInboundProcessingResult? result =
                await session.ReceiveAsync(ct).ConfigureAwait(false);
            if (result is null)
            {
                throw new EndOfStreamException("The M3UA peer closed the transport.");
            }

            if (result.TypedMessage.Kind == M3uaTypedMessageKind.PayloadData)
            {
                Mtp3TransferMessage transfer =
                    ConvertTransfer(result.TypedMessage.As<M3uaPayloadDataMessage>());
                Interlocked.Increment(ref _inboundQueueDepth);
                try
                {
                    await _inbound.Writer.WriteAsync(transfer, ct).ConfigureAwait(false);
                }
                catch
                {
                    Interlocked.Decrement(ref _inboundQueueDepth);
                    throw;
                }

                Interlocked.Increment(ref _receivedTransfers);
                RaiseEvent(M3uaRuntimeEventKind.TransferReceived);
                continue;
            }

            if (result.TypedMessage.Kind != M3uaTypedMessageKind.AspStateMaintenance)
            {
                continue;
            }

            M3uaAspStateMaintenanceMessage aspsm =
                result.TypedMessage.As<M3uaAspStateMaintenanceMessage>();
            if (aspsm.MessageType == M3uaAspsmMessageType.Heartbeat)
            {
                await sendLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await session.SendHeartbeatAckAsync(aspsm.HeartbeatData, ct)
                        .ConfigureAwait(false);
                }
                finally
                {
                    sendLock.Release();
                }
            }
            else if (aspsm.MessageType == M3uaAspsmMessageType.HeartbeatAck)
            {
                CompleteHeartbeat(aspsm.HeartbeatData.Span);
            }
        }
    }

    private async Task HeartbeatLoopAsync(
        M3uaTransportSession session,
        SemaphoreSlim sendLock,
        CancellationToken ct)
    {
        using PeriodicTimer timer = new(_options.HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            byte[] heartbeatData = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(
                heartbeatData,
                Interlocked.Increment(ref _heartbeatSequence));
            TaskCompletionSource<bool> acknowledgement =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_heartbeatSync)
            {
                _pendingHeartbeatData = heartbeatData;
                _pendingHeartbeat = acknowledgement;
            }

            await sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await session.SendHeartbeatAsync(heartbeatData, ct).ConfigureAwait(false);
                Interlocked.Increment(ref _heartbeatsSent);
            }
            finally
            {
                sendLock.Release();
            }

            try
            {
                await acknowledgement.Task.WaitAsync(_options.HeartbeatTimeout, ct)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Interlocked.Increment(ref _heartbeatTimeouts);
                throw new TimeoutException(
                    $"M3UA heartbeat acknowledgement was not received within {_options.HeartbeatTimeout}.");
            }
            finally
            {
                ClearHeartbeat(acknowledgement);
            }
        }
    }

    private async Task TryGracefulShutdownAsync(M3uaTransportSession session)
    {
        if (session.InboundProcessor.AspSession.State == M3uaAspState.Down)
        {
            return;
        }

        using CancellationTokenSource timeout = new(_options.ShutdownTimeout);
        try
        {
            if (session.InboundProcessor.AspSession.State == M3uaAspState.Active)
            {
                await session.SendAspInactiveAsync(
                    ReadOnlyMemory<byte>.Empty,
                    timeout.Token).ConfigureAwait(false);
            }

            await session.SendAspDownAsync(
                ReadOnlyMemory<byte>.Empty,
                timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex is OperationCanceledException
            or TimeoutException
            or IOException
            or InvalidOperationException)
        {
            RaiseEvent(M3uaRuntimeEventKind.FaultObserved, $"shutdown={ex.Message}");
        }
    }

    private static Mtp3TransferMessage ConvertTransfer(
        M3uaPayloadDataMessage payload)
    {
        return new(
            new(
                (Mtp3ServiceIndicator)payload.ServiceIndicator,
                payload.NetworkIndicator,
                payload.MessagePriority),
            new(
                payload.DestinationPointCode,
                payload.OriginatingPointCode,
                payload.SignallingLinkSelection),
            payload.UserPayload.ToArray(),
            payload.NetworkAppearance,
            payload.RoutingContext,
            payload.CorrelationId);
    }

    private void CompleteHeartbeat(ReadOnlySpan<byte> heartbeatData)
    {
        TaskCompletionSource<bool>? completion = null;
        lock (_heartbeatSync)
        {
            if (_pendingHeartbeatData is not null
                && heartbeatData.SequenceEqual(_pendingHeartbeatData))
            {
                completion = _pendingHeartbeat;
            }
        }

        if (completion?.TrySetResult(true) == true)
        {
            Interlocked.Increment(ref _heartbeatsAcknowledged);
            RaiseEvent(M3uaRuntimeEventKind.HeartbeatAcknowledged);
        }
    }

    private void ClearHeartbeat(TaskCompletionSource<bool>? expected = null)
    {
        lock (_heartbeatSync)
        {
            if (expected is null || ReferenceEquals(expected, _pendingHeartbeat))
            {
                _pendingHeartbeat?.TrySetCanceled();
                _pendingHeartbeat = null;
                _pendingHeartbeatData = null;
            }
        }
    }

    private void EnsureStarted()
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            if (_runTask is null)
            {
                throw new InvalidOperationException("M3UA runtime must be started before traffic is queued.");
            }

            if (_state == M3uaRuntimeState.Faulted)
            {
                throw new InvalidOperationException("M3UA runtime is faulted.");
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void SetAssociationName(string? associationName)
    {
        lock (_sync)
        {
            _associationName = associationName;
        }
    }

    private void Transition(M3uaRuntimeState state, string detail)
    {
        bool changed;
        lock (_sync)
        {
            changed = _state != state;
            _state = state;
        }

        if (changed)
        {
            RaiseEvent(M3uaRuntimeEventKind.StateChanged, detail);
        }
    }

    private void RaiseEvent(
        M3uaRuntimeEventKind kind,
        string? detail = null)
    {
        EventHandler<M3uaRuntimeEventArgs>? handler = RuntimeEvent;
        if (handler is null)
        {
            return;
        }

        try
        {
            handler(
                this,
                new(
                    kind,
                    State,
                    DateTimeOffset.UtcNow,
                    AssociationName,
                    detail));
        }
        catch
        {
            // Runtime observers must not disrupt protocol processing.
        }
    }
}
