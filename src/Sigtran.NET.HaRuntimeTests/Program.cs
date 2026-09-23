using System.Threading.Channels;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

await RunAsync("HA runtime rejects duplicate lane names", DuplicateLaneNamesFailClosedAsync);
await RunAsync("HA runtime rejects mismatched route-pool membership", RoutePoolMembershipMustMatchRuntimeLanesAsync);
await RunAsync("HA runtime fails when no lane activates", AllLaneStartupFailureFailsClosedAsync);
await RunAsync("HA runtime isolates startup failure", StartupFailureDoesNotBlockHealthyLaneAsync);
await RunAsync("HA runtime does not wait for a slow peer lane", SlowStartupDoesNotGateHealthyLaneAsync);
await RunAsync("HA runtime converges concurrent startup waits", ConcurrentStartupWaitersShareReadinessAsync);
await RunAsync("HA runtime keeps startup alive after one waiter cancels", CancelledStartupWaiterDoesNotCancelSupervisorAsync);
await RunAsync("HA runtime applies bounded aggregate backpressure", BoundedFanInPreservesAssociationIdentityAsync);
await RunAsync("HA runtime isolates terminal lane fault", TerminalLaneFaultDoesNotStopHealthyLaneAsync);
await RunAsync("HA runtime feeds live health into route admission", RuntimeHealthFeedsRouteAdmissionAsync);
await RunAsync("HA runtime converges concurrent shutdown waits", ConcurrentStopWaitersShareShutdownAsync);
await RunAsync("HA runtime keeps shared shutdown alive after one waiter cancels", CancelledStopWaiterDoesNotCancelSupervisorAsync);
await RunAsync("HA runtime stops peer lanes after a synchronous stop failure", SynchronousStopFailureDoesNotSkipPeerShutdownAsync);
await RunAsync("HA runtime ignores a captured late event after lane detach", CapturedLateRuntimeEventIsIgnoredAfterDetachAsync);
await RunAsync("HA runtime diagnostics cannot recover a faulted route", RuntimeHealthEventRegressions.DiagnosticsCannotRecoverFaultedRouteAsync);
await RunAsync("HA runtime diagnostics cannot clear terminal fault", RuntimeHealthEventRegressions.DiagnosticsCannotClearTerminalFaultAsync);
await RunAsync("HA runtime startup completion cannot clear an observed fault", RuntimeHealthEventRegressions.StartupCompletionCannotClearObservedFaultAsync);

static async Task DuplicateLaneNamesFailClosedAsync()
{
    FakeRuntimeLane first = new("dup");
    FakeRuntimeLane second = new("DUP");

    await ThrowsAsync<ArgumentException>(() =>
    {
        _ = new M3uaHaRuntimeSupervisor([first, second], inboundCapacity: 1);
        return Task.CompletedTask;
    }).ConfigureAwait(false);
}

static async Task RoutePoolMembershipMustMatchRuntimeLanesAsync()
{
    FakeRuntimeLane onlyLane = new("a");
    M3uaAssociationPool pool = new(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        [
            new M3uaAssociationDefinition(
                "a",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100]),
            new M3uaAssociationDefinition(
                "b",
                "sg-b",
                0,
                M3uaAssociationOperationalState.Active,
                [100])
        ]);

    await ThrowsAsync<ArgumentException>(() =>
    {
        _ = new M3uaHaRuntimeSupervisor(
            [onlyLane],
            inboundCapacity: 1,
            routePool: pool);
        return Task.CompletedTask;
    }).ConfigureAwait(false);
}

static async Task AllLaneStartupFailureFailsClosedAsync()
{
    FakeRuntimeLane first = new("a", failStart: true);
    FakeRuntimeLane second = new("b", failStart: true);
    await using M3uaHaRuntimeSupervisor supervisor = new([first, second], inboundCapacity: 2);

    await ThrowsAsync<InvalidOperationException>(async () =>
        await supervisor.StartAsync().ConfigureAwait(false)).ConfigureAwait(false);
}

static async Task StartupFailureDoesNotBlockHealthyLaneAsync()
{
    FakeRuntimeLane failed = new("failed", failStart: true);
    FakeRuntimeLane healthy = new("healthy");
    await using M3uaHaRuntimeSupervisor supervisor = new([failed, healthy], inboundCapacity: 2);

    await supervisor.StartAsync().ConfigureAwait(false);

    M3uaHaRuntimeSupervisorSnapshot started = supervisor.GetSnapshot();
    Equal(M3uaRuntimeState.Faulted, Lane(started, "failed").State, "A failed startup lane must remain faulted.");
    Equal(M3uaRuntimeState.Active, Lane(started, "healthy").State, "A healthy lane must activate independently.");

    healthy.Emit(CreateTransfer(sls: 3, routingContext: 100));
    M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync().ConfigureAwait(false);
    Equal("healthy", inbound.AssociationName, "Inbound fan-in must preserve the source association name.");
    Equal((byte)3, inbound.Message.RoutingLabel.SignallingLinkSelection, "Healthy-lane traffic must remain available after another lane fails startup.");
}

static async Task SlowStartupDoesNotGateHealthyLaneAsync()
{
    FakeRuntimeLane slow = new("slow", stallStart: true);
    FakeRuntimeLane healthy = new("healthy");
    await using M3uaHaRuntimeSupervisor supervisor = new([slow, healthy], inboundCapacity: 2);
    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));

    await supervisor.StartAsync(timeout.Token).ConfigureAwait(false);

    M3uaHaRuntimeSupervisorSnapshot started = supervisor.GetSnapshot();
    Equal(M3uaRuntimeState.Starting, Lane(started, "slow").State, "A slow lane may remain in startup while another association becomes usable.");
    Equal(M3uaRuntimeState.Active, Lane(started, "healthy").State, "A healthy lane must not wait for an unrelated peer startup attempt.");

    healthy.Emit(CreateTransfer(sls: 4, routingContext: 100));
    M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync(timeout.Token).ConfigureAwait(false);
    Equal("healthy", inbound.AssociationName, "Healthy traffic must flow while another association is still starting.");
}

static async Task ConcurrentStartupWaitersShareReadinessAsync()
{
    FakeRuntimeLane delayed = new("delayed", manualStart: true);
    await using M3uaHaRuntimeSupervisor supervisor = new([delayed], inboundCapacity: 1);

    Task first = supervisor.StartAsync().AsTask();
    await WaitUntilAsync(
        () => delayed.State == M3uaRuntimeState.Starting,
        "The synthetic lane did not enter startup.")
        .ConfigureAwait(false);

    Task second = supervisor.StartAsync().AsTask();
    await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
    Equal(false, first.IsCompleted, "The first startup waiter must remain pending before activation.");
    Equal(false, second.IsCompleted, "A concurrent startup waiter must share readiness instead of returning early.");

    delayed.Activate();
    await WaitAllAsync(first, second).ConfigureAwait(false);
    Equal(M3uaRuntimeState.Active, Lane(supervisor.GetSnapshot(), "delayed").State, "Both startup waiters must converge on the same activation.");
}

static async Task CancelledStartupWaiterDoesNotCancelSupervisorAsync()
{
    FakeRuntimeLane delayed = new("delayed", manualStart: true);
    await using M3uaHaRuntimeSupervisor supervisor = new([delayed], inboundCapacity: 1);

    Task owner = supervisor.StartAsync().AsTask();
    await WaitUntilAsync(
        () => delayed.State == M3uaRuntimeState.Starting,
        "The synthetic lane did not enter startup.")
        .ConfigureAwait(false);

    using CancellationTokenSource cancelledWaiter = new();
    Task observer = supervisor.StartAsync(cancelledWaiter.Token).AsTask();
    cancelledWaiter.Cancel();
    await ThrowsAsync<OperationCanceledException>(() => observer).ConfigureAwait(false);
    Equal(false, owner.IsCompleted, "Cancelling one startup wait must not cancel the shared supervisor startup.");

    delayed.Activate();
    await WaitAllAsync(owner).ConfigureAwait(false);
    Equal(M3uaRuntimeState.Active, Lane(supervisor.GetSnapshot(), "delayed").State, "The original startup wait must still complete after activation.");
}

static async Task BoundedFanInPreservesAssociationIdentityAsync()
{
    FakeRuntimeLane a = new("a");
    FakeRuntimeLane b = new("b");
    await using M3uaHaRuntimeSupervisor supervisor = new([a, b], inboundCapacity: 1);

    await supervisor.StartAsync().ConfigureAwait(false);

    a.Emit(CreateTransfer(sls: 1, routingContext: 100));
    b.Emit(CreateTransfer(sls: 2, routingContext: 200));

    await WaitUntilAsync(
        () => supervisor.GetSnapshot().PendingInboundTransfers == 2,
        "Both accepted lane transfers should be visible while one producer is backpressured by capacity=1.")
        .ConfigureAwait(false);

    M3uaHaRuntimeSupervisorSnapshot pressured = supervisor.GetSnapshot();
    Equal(1, pressured.InboundCapacity, "The supervisor must retain the configured aggregate inbound capacity.");
    Equal(2, pressured.PendingInboundTransfers, "Pending accounting must include the queued transfer and the blocked producer.");

    M3uaHaInboundTransfer first = await supervisor.ReceiveAsync().ConfigureAwait(false);
    M3uaHaInboundTransfer second = await supervisor.ReceiveAsync().ConfigureAwait(false);
    HashSet<string> sources = new(StringComparer.OrdinalIgnoreCase)
    {
        first.AssociationName,
        second.AssociationName
    };

    Equal(2, sources.Count, "Both independently managed association lanes must reach the aggregate reader.");
    Equal(true, sources.Contains("a"), "Association a must be represented in fan-in output.");
    Equal(true, sources.Contains("b"), "Association b must be represented in fan-in output.");

    await WaitUntilAsync(
        () =>
        {
            M3uaHaRuntimeSupervisorSnapshot snapshot = supervisor.GetSnapshot();
            return snapshot.PendingInboundTransfers == 0
                && Lane(snapshot, "a").ReceivedTransfers == 1
                && Lane(snapshot, "b").ReceivedTransfers == 1;
        },
        "Pending and per-lane receive accounting must settle after both transfers are consumed.")
        .ConfigureAwait(false);
}

static async Task TerminalLaneFaultDoesNotStopHealthyLaneAsync()
{
    FakeRuntimeLane a = new("a");
    FakeRuntimeLane b = new("b");
    await using M3uaHaRuntimeSupervisor supervisor = new([a, b], inboundCapacity: 2);

    await supervisor.StartAsync().ConfigureAwait(false);
    a.Fault("synthetic association loss");

    await WaitUntilAsync(
        () => Lane(supervisor.GetSnapshot(), "a").State == M3uaRuntimeState.Faulted,
        "The faulted lane must become terminal without changing the healthy lane.")
        .ConfigureAwait(false);

    M3uaHaRuntimeSupervisorSnapshot isolated = supervisor.GetSnapshot();
    Equal(M3uaRuntimeState.Faulted, Lane(isolated, "a").State, "The failed association lane must be faulted.");
    Equal(M3uaRuntimeState.Active, Lane(isolated, "b").State, "A peer lane must remain active after another lane faults.");
    Equal(true, Lane(isolated, "a").FaultEvents >= 1, "The faulting lane must retain a diagnostic fault count.");

    b.Emit(CreateTransfer(sls: 9, routingContext: 100));
    M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync().ConfigureAwait(false);
    Equal("b", inbound.AssociationName, "Healthy inbound traffic must continue after an independent lane fault.");
    Equal((byte)9, inbound.Message.RoutingLabel.SignallingLinkSelection, "Healthy-lane payload metadata must be preserved.");
}

static async Task RuntimeHealthFeedsRouteAdmissionAsync()
{
    M3uaAssociationPool pool = new(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        [
            new M3uaAssociationDefinition(
                "a",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100]),
            new M3uaAssociationDefinition(
                "b",
                "sg-b",
                0,
                M3uaAssociationOperationalState.Active,
                [100])
        ]);
    FakeRuntimeLane a = new("a");
    FakeRuntimeLane b = new("b");
    await using M3uaHaRuntimeSupervisor supervisor = new(
        [a, b],
        inboundCapacity: 2,
        routePool: pool);
    Mtp3TransferMessage transfer = CreateTransfer(sls: 1, routingContext: 100);

    EqualRuntimeState(M3uaRuntimeState.Stopped, Route(pool, "a").RuntimeState,
        "Initial bound health must reflect the stopped runtime lane.");
    Equal(0, pool.SelectTargets(transfer).Count,
        "A bound route pool must fail closed while its runtime lanes are stopped.");
    Equal(M3uaAssociationOperationalState.Active, Route(pool, "a").State,
        "Binding runtime health must not rewrite local node-routing role.");

    await supervisor.StartAsync().ConfigureAwait(false);
    await WaitUntilAsync(
        () => Route(pool, "a").RuntimeState == M3uaRuntimeState.Active
            && Route(pool, "b").RuntimeState == M3uaRuntimeState.Active,
        "Both synthetic runtime lanes did not become route-eligible.")
        .ConfigureAwait(false);

    Equal("b", pool.SelectTargets(transfer).Single().Name,
        "Stable two-member loadshare must preserve deterministic SLS affinity when both runtimes are healthy.");

    b.ObserveFaultWhileActive("synthetic recoverable transport fault");
    await WaitUntilAsync(
        () => Route(pool, "b").RuntimeState == M3uaRuntimeState.Reconnecting,
        "A runtime fault observation must exclude the lane before the runtime publishes its later reconnect state.")
        .ConfigureAwait(false);

    M3uaAssociationRouteSnapshot excluded = Route(pool, "b");
    Equal(M3uaAssociationOperationalState.Active, excluded.State,
        "Recoverable runtime health loss must not silently rewrite the local loadshare role.");
    EqualRuntimeState(M3uaRuntimeState.Reconnecting, excluded.RuntimeState,
        "The route snapshot must expose fail-closed live runtime health independently from role.");
    Equal("a", pool.SelectTargets(transfer).Single().Name,
        "A known-faulting runtime lane must receive no new traffic while the healthy peer remains eligible.");

    b.TransitionTo(
        M3uaRuntimeState.Active,
        M3uaRuntimeEventKind.AspActivated,
        "synthetic reconnect complete");
    await WaitUntilAsync(
        () => Route(pool, "b").RuntimeState == M3uaRuntimeState.Active,
        "The recovered runtime lane did not return to route eligibility.")
        .ConfigureAwait(false);

    Equal("b", pool.SelectTargets(transfer).Single().Name,
        "Runtime recovery must restore the original deterministic SLS affinity without rewriting node policy.");
    Equal(M3uaAssociationOperationalState.Active, Route(pool, "b").State,
        "Runtime recovery must leave local node-routing role unchanged.");
}

static async Task ConcurrentStopWaitersShareShutdownAsync()
{
    FakeRuntimeLane lane = new("a", stallStop: true);
    M3uaHaRuntimeSupervisor supervisor = new([lane], inboundCapacity: 1);
    await supervisor.StartAsync().ConfigureAwait(false);

    Task first = supervisor.StopAsync().AsTask();
    await WaitUntilAsync(
        () => lane.StopCalls == 1,
        "The first shutdown did not reach the association lane.")
        .ConfigureAwait(false);

    Task second = supervisor.StopAsync().AsTask();
    await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
    Equal(false, first.IsCompleted, "The first shutdown waiter must remain pending while lane shutdown is blocked.");
    Equal(false, second.IsCompleted, "A concurrent shutdown waiter must share the same shutdown task instead of returning early.");
    Equal(1, lane.StopCalls, "Concurrent supervisor shutdown waits must not invoke lane shutdown twice.");

    lane.ReleaseStop();
    await WaitAllAsync(first, second).ConfigureAwait(false);
    Equal(1, lane.StopCalls, "The shared shutdown path must call lane shutdown exactly once.");
    Equal(M3uaRuntimeState.Stopped, lane.State, "The lane must be stopped before either supervisor shutdown waiter completes.");

    await supervisor.DisposeAsync().ConfigureAwait(false);
}

static async Task CancelledStopWaiterDoesNotCancelSupervisorAsync()
{
    FakeRuntimeLane lane = new("a", stallStop: true);
    M3uaHaRuntimeSupervisor supervisor = new([lane], inboundCapacity: 1);
    await supervisor.StartAsync().ConfigureAwait(false);

    Task owner = supervisor.StopAsync().AsTask();
    await WaitUntilAsync(
        () => lane.StopCalls == 1,
        "The shared shutdown did not reach the association lane.")
        .ConfigureAwait(false);

    using CancellationTokenSource cancelledWaiter = new();
    Task observer = supervisor.StopAsync(cancelledWaiter.Token).AsTask();
    cancelledWaiter.Cancel();
    await ThrowsAsync<OperationCanceledException>(() => observer).ConfigureAwait(false);

    Equal(false, owner.IsCompleted,
        "Cancelling one shutdown wait must not cancel the shared supervisor shutdown.");
    Equal(1, lane.StopCalls,
        "A cancelled shutdown observer must not invoke lane shutdown again.");

    lane.ReleaseStop();
    await WaitAllAsync(owner).ConfigureAwait(false);
    Equal(1, lane.StopCalls,
        "The shared shutdown must still stop the lane exactly once after another waiter cancels.");
    Equal(M3uaRuntimeState.Stopped, lane.State,
        "The lane must reach Stopped after the shared shutdown completes.");

    await supervisor.DisposeAsync().ConfigureAwait(false);
}

static async Task SynchronousStopFailureDoesNotSkipPeerShutdownAsync()
{
    FakeRuntimeLane failing = new("failing", throwOnStop: true);
    FakeRuntimeLane healthy = new("healthy");
    M3uaHaRuntimeSupervisor supervisor = new([failing, healthy], inboundCapacity: 2);

    await supervisor.StartAsync().ConfigureAwait(false);
    await ThrowsAsync<InvalidOperationException>(() => supervisor.StopAsync().AsTask())
        .ConfigureAwait(false);

    Equal(1, failing.StopCalls,
        "The failing lane must receive exactly one shutdown attempt.");
    Equal(1, healthy.StopCalls,
        "A synchronous peer shutdown failure must not skip shutdown of another lane.");
    Equal(M3uaRuntimeState.Stopped, healthy.State,
        "The healthy peer lane must finish shutdown even when another lane throws synchronously.");

    M3uaHaRuntimeSupervisorSnapshot stopped = supervisor.GetSnapshot();
    Equal(M3uaRuntimeState.Faulted, Lane(stopped, "failing").State,
        "A lane whose shutdown throws must remain attributable as faulted in supervisor diagnostics.");
    Equal(1L, Lane(stopped, "failing").FaultEvents,
        "A shutdown failure without a runtime fault event must still contribute one lane fault diagnostic.");
    Equal(M3uaRuntimeState.Stopped, Lane(stopped, "healthy").State,
        "A successfully stopped peer must be reflected as stopped in supervisor diagnostics.");

    await ThrowsAsync<InvalidOperationException>(() => supervisor.DisposeAsync().AsTask())
        .ConfigureAwait(false);
}

static async Task CapturedLateRuntimeEventIsIgnoredAfterDetachAsync()
{
    FakeRuntimeLane lane = new("a");
    M3uaHaRuntimeSupervisor supervisor = new([lane], inboundCapacity: 1);

    await supervisor.StartAsync().ConfigureAwait(false);
    lane.CaptureRuntimeEventHandler();
    await supervisor.StopAsync().ConfigureAwait(false);

    M3uaHaRuntimeLaneSnapshot stopped = Lane(supervisor.GetSnapshot(), "a");
    Equal(M3uaRuntimeState.Stopped, stopped.State,
        "The lane must be stopped before a captured late callback is replayed.");
    Equal(0L, stopped.FaultEvents,
        "No fault must be recorded before the synthetic late callback.");

    lane.RaiseCapturedRuntimeEvent(
        M3uaRuntimeEventKind.FaultObserved,
        M3uaRuntimeState.Faulted,
        "synthetic late callback after detach");

    M3uaHaRuntimeLaneSnapshot afterLateCallback = Lane(supervisor.GetSnapshot(), "a");
    Equal(M3uaRuntimeState.Stopped, afterLateCallback.State,
        "A callback captured before unsubscription must not mutate supervisor state after lane detach.");
    Equal(0L, afterLateCallback.FaultEvents,
        "A callback captured before unsubscription must not mutate fault diagnostics after lane detach.");

    await supervisor.DisposeAsync().ConfigureAwait(false);
}

static M3uaHaRuntimeLaneSnapshot Lane(
    M3uaHaRuntimeSupervisorSnapshot snapshot,
    string associationName)
{
    M3uaHaRuntimeLaneSnapshot[] matches = snapshot.Lanes
        .Where(lane => string.Equals(
            lane.AssociationName,
            associationName,
            StringComparison.OrdinalIgnoreCase))
        .ToArray();
    Equal(1, matches.Length, $"Expected exactly one snapshot for association '{associationName}'.");
    return matches[0];
}

static M3uaAssociationRouteSnapshot Route(
    M3uaAssociationPool pool,
    string associationName)
{
    M3uaAssociationRouteSnapshot[] matches = pool.GetSnapshot()
        .Where(route => string.Equals(
            route.Name,
            associationName,
            StringComparison.OrdinalIgnoreCase))
        .ToArray();
    Equal(1, matches.Length, $"Expected exactly one route snapshot for association '{associationName}'.");
    return matches[0];
}

static Mtp3TransferMessage CreateTransfer(byte sls, uint? routingContext) =>
    new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01, sls },
        routingContext: routingContext);

static async Task WaitUntilAsync(
    Func<bool> condition,
    string failureMessage)
{
    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
    while (!condition())
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new InvalidOperationException(failureMessage);
        }
    }
}

static async Task WaitAllAsync(params Task[] tasks)
{
    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
    await Task.WhenAll(tasks).WaitAsync(timeout.Token).ConfigureAwait(false);
}

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"{message} Expected={expected}; Actual={actual}.");
    }
}

static void EqualRuntimeState(
    M3uaRuntimeState expected,
    M3uaRuntimeState? actual,
    string message)
{
    if (actual != expected)
    {
        throw new InvalidOperationException(
            $"{message} Expected={expected}; Actual={actual?.ToString() ?? "<unbound>"}.");
    }
}

static async Task ThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action().ConfigureAwait(false);
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(
        $"Expected exception {typeof(TException).Name}.");
}

internal sealed class FakeRuntimeLane : IM3uaAssociationRuntimeLane
{
    private readonly Channel<Mtp3TransferMessage> _inbound =
        Channel.CreateUnbounded<Mtp3TransferMessage>();
    private readonly bool _failStart;
    private readonly bool _stallStart;
    private readonly bool _manualStart;
    private readonly bool _stallStop;
    private readonly bool _throwOnStop;
    private readonly TaskCompletionSource<bool> _activation =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _stopRelease =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile M3uaRuntimeState _state = M3uaRuntimeState.Stopped;
    private EventHandler<M3uaRuntimeEventArgs>? _capturedRuntimeEvent;
    private long _received;
    private int _stopCalls;

    internal FakeRuntimeLane(
        string associationName,
        bool failStart = false,
        bool stallStart = false,
        bool manualStart = false,
        bool stallStop = false,
        bool throwOnStop = false)
    {
        AssociationName = associationName;
        _failStart = failStart;
        _stallStart = stallStart;
        _manualStart = manualStart;
        _stallStop = stallStop;
        _throwOnStop = throwOnStop;
    }

    public string AssociationName { get; }

    public M3uaRuntimeState State => _state;

    internal int StopCalls => Volatile.Read(ref _stopCalls);

    public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (_failStart)
        {
            _state = M3uaRuntimeState.Faulted;
            Raise(M3uaRuntimeEventKind.FaultObserved, "synthetic startup failure");
            throw new InvalidOperationException("Synthetic startup failure.");
        }

        if (_stallStart)
        {
            _state = M3uaRuntimeState.Starting;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic slow startup");
            await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            return;
        }

        if (_manualStart)
        {
            _state = M3uaRuntimeState.Starting;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic gated startup");
            await _activation.Task.WaitAsync(ct).ConfigureAwait(false);
        }

        _state = M3uaRuntimeState.Active;
        Raise(M3uaRuntimeEventKind.AspActivated, "synthetic active");
    }

    public async ValueTask<Mtp3TransferMessage> ReceiveAsync(
        CancellationToken ct = default)
    {
        Mtp3TransferMessage message =
            await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
        Interlocked.Increment(ref _received);
        return message;
    }

    public ValueTask StopAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref _stopCalls);
        if (_throwOnStop)
        {
            throw new InvalidOperationException("Synthetic synchronous stop failure.");
        }

        _state = M3uaRuntimeState.Stopping;
        Raise(M3uaRuntimeEventKind.StateChanged, "synthetic stopping");
        return StopCoreAsync(ct);
    }

    public M3uaRuntimeMetrics GetMetrics() => new(
        _state,
        outboundQueueDepth: 0,
        inboundQueueDepth: 0,
        sentTransfers: 0,
        receivedTransfers: Interlocked.Read(ref _received),
        heartbeatsSent: 0,
        heartbeatsAcknowledged: 0,
        heartbeatTimeouts: 0,
        reconnectAttempts: 0,
        faults: _state == M3uaRuntimeState.Faulted ? 1 : 0);

    internal void Activate() => _activation.TrySetResult(true);

    internal void ReleaseStop() => _stopRelease.TrySetResult(true);

    internal void CaptureRuntimeEventHandler() => _capturedRuntimeEvent = RuntimeEvent;

    internal void RaiseCapturedRuntimeEvent(
        M3uaRuntimeEventKind kind,
        M3uaRuntimeState state,
        string detail)
    {
        _capturedRuntimeEvent?.Invoke(
            this,
            new M3uaRuntimeEventArgs(
                kind,
                state,
                DateTimeOffset.UtcNow,
                AssociationName,
                detail));
    }

    internal void Emit(Mtp3TransferMessage message)
    {
        if (!_inbound.Writer.TryWrite(message))
        {
            throw new InvalidOperationException(
                $"Synthetic lane '{AssociationName}' rejected an inbound transfer.");
        }
    }

    internal void ObserveFaultWhileActive(string detail)
    {
        if (_state != M3uaRuntimeState.Active)
        {
            throw new InvalidOperationException(
                $"Synthetic lane '{AssociationName}' must be Active before observing a recoverable fault.");
        }

        Raise(M3uaRuntimeEventKind.FaultObserved, detail);
    }

    internal void TransitionTo(
        M3uaRuntimeState state,
        M3uaRuntimeEventKind kind,
        string detail)
    {
        _state = state;
        Raise(kind, detail);
    }

    internal void Fault(string detail)
    {
        _state = M3uaRuntimeState.Faulted;
        Raise(M3uaRuntimeEventKind.FaultObserved, detail);
        _inbound.Writer.TryComplete(new InvalidOperationException(detail));
    }

    private async ValueTask StopCoreAsync(CancellationToken ct)
    {
        if (_stallStop)
        {
            await _stopRelease.Task.WaitAsync(ct).ConfigureAwait(false);
        }

        _state = M3uaRuntimeState.Stopped;
        _inbound.Writer.TryComplete();
        Raise(M3uaRuntimeEventKind.ShutdownCompleted, "synthetic stopped");
    }

    private void Raise(M3uaRuntimeEventKind kind, string detail)
    {
        RuntimeEvent?.Invoke(
            this,
            new M3uaRuntimeEventArgs(
                kind,
                _state,
                DateTimeOffset.UtcNow,
                AssociationName,
                detail));
    }
}
