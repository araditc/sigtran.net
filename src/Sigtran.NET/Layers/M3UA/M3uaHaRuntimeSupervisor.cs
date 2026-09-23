using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Internal lane boundary used to compose independently managed M3UA runtimes.
/// The production adapter below delegates to the existing <see cref="M3uaRuntime"/>
/// so HA composition does not bypass the supported runtime lifecycle contract.
/// </summary>
internal interface IM3uaAssociationRuntimeLane
{
    string AssociationName { get; }

    M3uaRuntimeState State { get; }

    event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

    ValueTask StartAsync(CancellationToken ct = default);

    ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default);

    ValueTask StopAsync(CancellationToken ct = default);

    M3uaRuntimeMetrics GetMetrics();
}

internal sealed class M3uaRuntimeAssociationLane : IM3uaAssociationRuntimeLane
{
    private readonly M3uaRuntime _runtime;

    internal M3uaRuntimeAssociationLane(string associationName, M3uaRuntime runtime)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName)
            ? throw new ArgumentException("Association name is required.", nameof(associationName))
            : associationName.Trim();
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string AssociationName { get; }

    public M3uaRuntimeState State => _runtime.State;

    public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent
    {
        add => _runtime.RuntimeEvent += value;
        remove => _runtime.RuntimeEvent -= value;
    }

    public ValueTask StartAsync(CancellationToken ct = default) => _runtime.StartAsync(ct);

    public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default) =>
        _runtime.ReceiveAsync(ct);

    public ValueTask StopAsync(CancellationToken ct = default) => _runtime.StopAsync(ct);

    public M3uaRuntimeMetrics GetMetrics() => _runtime.GetMetrics();
}

internal readonly struct M3uaHaInboundTransfer
{
    internal M3uaHaInboundTransfer(string associationName, Mtp3TransferMessage message)
    {
        AssociationName = associationName;
        Message = message;
    }

    internal string AssociationName { get; }

    internal Mtp3TransferMessage Message { get; }
}

internal readonly struct M3uaHaRuntimeLaneSnapshot
{
    internal M3uaHaRuntimeLaneSnapshot(
        string associationName,
        M3uaRuntimeState state,
        long receivedTransfers,
        long faultEvents,
        M3uaRuntimeMetrics metrics)
    {
        AssociationName = associationName;
        State = state;
        ReceivedTransfers = receivedTransfers;
        FaultEvents = faultEvents;
        Metrics = metrics;
    }

    internal string AssociationName { get; }

    internal M3uaRuntimeState State { get; }

    internal long ReceivedTransfers { get; }

    internal long FaultEvents { get; }

    internal M3uaRuntimeMetrics Metrics { get; }
}

internal readonly struct M3uaHaRuntimeSupervisorSnapshot
{
    internal M3uaHaRuntimeSupervisorSnapshot(
        int inboundCapacity,
        int pendingInboundTransfers,
        IReadOnlyList<M3uaHaRuntimeLaneSnapshot> lanes)
    {
        InboundCapacity = inboundCapacity;
        PendingInboundTransfers = pendingInboundTransfers;
        Lanes = lanes;
    }

    internal int InboundCapacity { get; }

    /// <summary>
    /// Transfers already accepted from association receive loops but not yet
    /// consumed by the aggregate reader. This includes writers currently
    /// backpressured by the bounded fan-in channel.
    /// </summary>
    internal int PendingInboundTransfers { get; }

    internal IReadOnlyList<M3uaHaRuntimeLaneSnapshot> Lanes { get; }
}

/// <summary>
/// Runs independent association runtimes concurrently and merges inbound MTP3
/// transfers through one bounded channel without coupling one lane's terminal
/// failure to healthy lanes.
/// </summary>
/// <remarks>
/// This component intentionally handles only runtime lifecycle isolation and
/// inbound fan-in. Outbound traffic remains governed by the HA routing,
/// dispatcher, and coordinator path so queue acceptance is not mislabeled as
/// peer/network acceptance.
/// </remarks>
internal sealed class M3uaHaRuntimeSupervisor : IAsyncDisposable
{
    private sealed class LaneContext
    {
        internal LaneContext(IM3uaAssociationRuntimeLane lane)
        {
            Lane = lane;
            State = lane.State;
        }

        internal IM3uaAssociationRuntimeLane Lane { get; }

        internal M3uaRuntimeState State { get; set; }

        internal CancellationTokenSource? Lifetime { get; set; }

        internal TaskCompletionSource<bool>? Startup { get; set; }

        internal Task? PumpTask { get; set; }

        internal EventHandler<M3uaRuntimeEventArgs>? EventHandler { get; set; }

        internal long ReceivedTransfers;

        internal long FaultEvents;
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, LaneContext> _lanes;
    private readonly Channel<M3uaHaInboundTransfer> _inbound;
    private CancellationTokenSource? _lifetime;
    private Task? _startupTask;
    private Task? _stopTask;
    private bool _startAttempted;
    private bool _running;
    private bool _stopping;
    private bool _terminal;
    private bool _disposed;
    private int _activePumps;
    private int _pendingInboundTransfers;

    internal M3uaHaRuntimeSupervisor(
        IEnumerable<IM3uaAssociationRuntimeLane> lanes,
        int inboundCapacity = 1024)
    {
        if (inboundCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inboundCapacity),
                "Inbound fan-in capacity must be positive.");
        }

        ArgumentNullException.ThrowIfNull(lanes);
        _lanes = new Dictionary<string, LaneContext>(StringComparer.OrdinalIgnoreCase);
        foreach (IM3uaAssociationRuntimeLane lane in lanes)
        {
            ArgumentNullException.ThrowIfNull(lane);
            if (string.IsNullOrWhiteSpace(lane.AssociationName))
            {
                throw new ArgumentException(
                    "Association runtime lane name is required.",
                    nameof(lanes));
            }

            if (!_lanes.TryAdd(lane.AssociationName, new LaneContext(lane)))
            {
                throw new ArgumentException(
                    $"Duplicate association runtime lane '{lane.AssociationName}'.",
                    nameof(lanes));
            }
        }

        if (_lanes.Count == 0)
        {
            throw new ArgumentException(
                "At least one association runtime lane is required.",
                nameof(lanes));
        }

        InboundCapacity = inboundCapacity;
        _inbound = Channel.CreateBounded<M3uaHaInboundTransfer>(
            new BoundedChannelOptions(inboundCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    internal int InboundCapacity { get; }

    internal async ValueTask StartAsync(CancellationToken ct = default)
    {
        Task startupTask;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_terminal)
            {
                throw new InvalidOperationException(
                    "The M3UA HA runtime supervisor has terminated and cannot be restarted.");
            }

            if (_running)
            {
                startupTask = _startupTask
                    ?? throw new InvalidOperationException(
                        "The M3UA HA runtime supervisor startup state is inconsistent.");
            }
            else
            {
                if (_startAttempted)
                {
                    throw new InvalidOperationException(
                        "The M3UA HA runtime supervisor cannot be restarted after it has stopped.");
                }

                _startAttempted = true;
                _running = true;
                _lifetime = new CancellationTokenSource();
                _activePumps = _lanes.Count;
                Task<bool>[] startupTasks = new Task<bool>[_lanes.Count];

                int index = 0;
                foreach (LaneContext context in _lanes.Values)
                {
                    context.State = M3uaRuntimeState.Starting;
                    context.Startup = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    context.Lifetime = CancellationTokenSource.CreateLinkedTokenSource(
                        _lifetime.Token);
                    context.EventHandler = (_, args) => OnRuntimeEvent(context, args);
                    context.Lane.RuntimeEvent += context.EventHandler;
                    context.PumpTask = RunLaneAsync(context, context.Lifetime.Token);
                    startupTasks[index++] = context.Startup.Task;
                }

                _startupTask = WaitForFirstActivationAsync(startupTasks);
                startupTask = _startupTask;
            }
        }

        await startupTask.WaitAsync(ct).ConfigureAwait(false);
    }

    internal async ValueTask<M3uaHaInboundTransfer> ReceiveAsync(
        CancellationToken ct = default)
    {
        lock (_sync)
        {
            if (!_startAttempted)
            {
                throw new InvalidOperationException(
                    "The M3UA HA runtime supervisor has not been started.");
            }
        }

        M3uaHaInboundTransfer transfer =
            await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
        Interlocked.Decrement(ref _pendingInboundTransfers);
        return transfer;
    }

    internal M3uaHaRuntimeSupervisorSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            M3uaHaRuntimeLaneSnapshot[] lanes = _lanes.Values
                .OrderBy(context => context.Lane.AssociationName, StringComparer.Ordinal)
                .Select(context => new M3uaHaRuntimeLaneSnapshot(
                    context.Lane.AssociationName,
                    context.State,
                    Interlocked.Read(ref context.ReceivedTransfers),
                    Interlocked.Read(ref context.FaultEvents),
                    context.Lane.GetMetrics()))
                .ToArray();

            return new M3uaHaRuntimeSupervisorSnapshot(
                InboundCapacity,
                Volatile.Read(ref _pendingInboundTransfers),
                lanes);
        }
    }

    internal async ValueTask StopAsync(CancellationToken ct = default)
    {
        Task? stopTask;
        lock (_sync)
        {
            if (_stopTask is not null)
            {
                stopTask = _stopTask;
            }
            else if (!_running)
            {
                return;
            }
            else
            {
                _running = false;
                _stopping = true;
                LaneContext[] contexts = _lanes.Values.ToArray();
                CancellationTokenSource? lifetime = _lifetime;
                _stopTask = StopCoreAsync(contexts, lifetime);
                stopTask = _stopTask;
            }
        }

        await stopTask.WaitAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task WaitForFirstActivationAsync(Task<bool>[] startupTasks)
    {
        HashSet<Task<bool>> pending = startupTasks.ToHashSet();
        while (pending.Count > 0)
        {
            Task<bool> completed = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(completed);
            if (await completed.ConfigureAwait(false))
            {
                return;
            }
        }

        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        throw new InvalidOperationException(
            "No M3UA association runtime lane reached its first active state.");
    }

    private async Task StopCoreAsync(
        LaneContext[] contexts,
        CancellationTokenSource? lifetime)
    {
        // Ensure no lane callbacks execute while StopAsync still owns _sync.
        await Task.Yield();
        lifetime?.Cancel();

        Exception? stopFailure = null;
        try
        {
            Task[] stopTasks = contexts
                .Select(StopLaneAsync)
                .ToArray();
            await Task.WhenAll(stopTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            stopFailure = ex;
        }

        Task[] pumpTasks = contexts
            .Select(context => context.PumpTask)
            .Where(static task => task is not null)
            .Cast<Task>()
            .ToArray();

        try
        {
            if (pumpTasks.Length > 0)
            {
                await Task.WhenAll(pumpTasks).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime?.IsCancellationRequested == true)
        {
            // Expected: receive pumps observe the supervisor lifetime cancellation.
        }
        catch (Exception ex)
        {
            stopFailure ??= ex;
        }
        finally
        {
            foreach (LaneContext context in contexts)
            {
                if (context.EventHandler is not null)
                {
                    context.Lane.RuntimeEvent -= context.EventHandler;
                    context.EventHandler = null;
                }

                context.Lifetime?.Dispose();
                context.Lifetime = null;
            }

            lifetime?.Dispose();
            lock (_sync)
            {
                _lifetime = null;
                _stopping = false;
                _terminal = true;
            }

            _inbound.Writer.TryComplete(stopFailure);
        }

        if (stopFailure is not null)
        {
            throw stopFailure;
        }
    }

    private async Task StopLaneAsync(LaneContext context)
    {
        long faultEventsBefore = Interlocked.Read(ref context.FaultEvents);
        try
        {
            await context.Lane.StopAsync(CancellationToken.None).ConfigureAwait(false);
            lock (_sync)
            {
                context.State = context.Lane.State;
            }
        }
        catch
        {
            lock (_sync)
            {
                context.State = M3uaRuntimeState.Faulted;
            }

            if (Interlocked.Read(ref context.FaultEvents) == faultEventsBefore)
            {
                Interlocked.Increment(ref context.FaultEvents);
            }

            throw;
        }
    }

    private async Task RunLaneAsync(LaneContext context, CancellationToken ct)
    {
        bool activated = false;
        try
        {
            await context.Lane.StartAsync(ct).ConfigureAwait(false);
            activated = true;
            lock (_sync)
            {
                context.State = context.Lane.State;
            }

            context.Startup!.TrySetResult(true);

            while (!ct.IsCancellationRequested)
            {
                Mtp3TransferMessage message =
                    await context.Lane.ReceiveAsync(ct).ConfigureAwait(false);
                M3uaHaInboundTransfer transfer = new(
                    context.Lane.AssociationName,
                    message);

                Interlocked.Increment(ref _pendingInboundTransfers);
                try
                {
                    await _inbound.Writer.WriteAsync(transfer, ct).ConfigureAwait(false);
                }
                catch
                {
                    Interlocked.Decrement(ref _pendingInboundTransfers);
                    throw;
                }

                Interlocked.Increment(ref context.ReceivedTransfers);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            context.Startup?.TrySetResult(false);
        }
        catch (Exception)
        {
            bool countTerminalFault = false;
            lock (_sync)
            {
                if (context.State != M3uaRuntimeState.Faulted)
                {
                    context.State = M3uaRuntimeState.Faulted;
                    countTerminalFault = true;
                }
            }

            if (countTerminalFault)
            {
                Interlocked.Increment(ref context.FaultEvents);
            }

            context.Startup?.TrySetResult(false);
        }
        finally
        {
            context.Startup?.TrySetResult(activated);
            if (Interlocked.Decrement(ref _activePumps) == 0)
            {
                bool stopping;
                lock (_sync)
                {
                    stopping = _stopping;
                    if (!stopping)
                    {
                        _terminal = true;
                    }
                }

                if (!stopping)
                {
                    _inbound.Writer.TryComplete(new InvalidOperationException(
                        "All M3UA association runtime lanes stopped."));
                }
            }
        }
    }

    private void OnRuntimeEvent(
        LaneContext context,
        M3uaRuntimeEventArgs args)
    {
        if (args.Kind == M3uaRuntimeEventKind.FaultObserved)
        {
            Interlocked.Increment(ref context.FaultEvents);
        }

        lock (_sync)
        {
            context.State = args.State;
        }

        if (args.State == M3uaRuntimeState.Faulted)
        {
            context.Lifetime?.Cancel();
        }
    }
}
