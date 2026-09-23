using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class RuntimeShutdownRegressions
{
    internal static async Task ShutdownClosesAdmissionBeforeLaneCallbacksAsync()
    {
        GatedStopLane a = new("a");
        GatedStopLane b = new("b");
        M3uaAssociationPool pool = Pool("a", "b");
        await using M3uaHaRuntimeSupervisor supervisor = new(
            [a, b], inboundCapacity: 1, routePool: pool);
        await supervisor.StartAsync().ConfigureAwait(false);
        using M3uaAssociationDispatchLease? admitted = pool.TryAcquireDispatchLease("a", Transfer());
        Require(admitted is not null, "An active route must admit the pre-shutdown lease.");

        Task stopping = supervisor.StopAsync().AsTask();
        try
        {
            // Neither lane can emit Stopping until released. This deterministically
            // exercises the supervisor-owned boundary, not a race against a timer.
            await Task.WhenAll(a.StopEntered, b.StopEntered)
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Require(a.State == M3uaRuntimeState.Active && b.State == M3uaRuntimeState.Active,
                "The test must hold both underlying lanes before their lifecycle callbacks.");
            Require(pool.GetSnapshot().All(route => route.RuntimeState == M3uaRuntimeState.Stopping),
                "Every bound route must be excluded before lane shutdown callbacks occur.");
            Require(pool.SelectTargets(Transfer()).Count == 0,
                "Stopping the supervisor must close new route selection.");
            using M3uaAssociationDispatchLease? rejected = pool.TryAcquireDispatchLease("b", Transfer());
            Require(rejected is null, "Stopping the supervisor must close new dispatch admission.");
            Require(pool.GetSnapshot().All(route => route.State == M3uaAssociationOperationalState.Active),
                "Shutdown health exclusion must not rewrite local route policy.");
            // Releasing an admitted lease must remain legal after admission closes.
            admitted!.Dispose();
        }
        finally
        {
            a.ReleaseStop();
            b.ReleaseStop();
            await stopping.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }

    internal static async Task ShutdownCannotBeReopenedByActivationAsync()
    {
        GatedStopLane lane = new("a");
        M3uaAssociationPool pool = Pool("a");
        await using M3uaHaRuntimeSupervisor supervisor = new(
            [lane], inboundCapacity: 1, routePool: pool);
        await supervisor.StartAsync().ConfigureAwait(false);

        Task stopping = supervisor.StopAsync().AsTask();
        try
        {
            await lane.StopEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            lane.RaiseActivation();
            Require(pool.GetSnapshot().Single().RuntimeState == M3uaRuntimeState.Stopping,
                "A racing activation must not reopen an association after supervisor shutdown begins.");
            Require(pool.SelectTargets(Transfer()).Count == 0,
                "New work must stay excluded even if the underlying lane reports Active during teardown.");
        }
        finally
        {
            lane.ReleaseStop();
            await stopping.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }

    internal static async Task ConcurrentDisposalWaitsForCleanupAsync()
    {
        GatedStopLane lane = new("a");
        M3uaHaRuntimeSupervisor supervisor = new([lane], inboundCapacity: 1);
        await supervisor.StartAsync().ConfigureAwait(false);
        Task first = supervisor.DisposeAsync().AsTask();
        await lane.StopEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Task second = supervisor.DisposeAsync().AsTask();
        bool firstReturnedEarly = first.IsCompleted;
        bool secondReturnedEarly = second.IsCompleted;

        lane.ReleaseStop();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Require(!firstReturnedEarly && !secondReturnedEarly,
            "Every disposer must wait while the shared lane cleanup is pending.");
        Require(lane.StopCalls == 1, "Concurrent disposal must invoke lane shutdown only once.");
        Require(supervisor.GetSnapshot().Lanes.Single().State == M3uaRuntimeState.Stopped,
            "Disposal completion must follow the settled lane shutdown state.");
        await supervisor.DisposeAsync().ConfigureAwait(false);
        Require(lane.StopCalls == 1, "Disposal after successful cleanup must remain idempotent.");
    }

    internal static async Task ConcurrentDisposalSharesShutdownFailureAsync()
    {
        GatedStopLane lane = new("a", failStop: true);
        M3uaHaRuntimeSupervisor supervisor = new([lane], inboundCapacity: 1);
        await supervisor.StartAsync().ConfigureAwait(false);
        Task first = supervisor.DisposeAsync().AsTask();
        await lane.StopEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Task second = supervisor.DisposeAsync().AsTask();
        bool secondReturnedEarly = second.IsCompleted;

        lane.ReleaseStop();
        Exception? firstFailure = await CaptureFailureAsync(first).ConfigureAwait(false);
        Exception? secondFailure = await CaptureFailureAsync(second).ConfigureAwait(false);
        Exception? lateFailure = await CaptureFailureAsync(supervisor.DisposeAsync().AsTask())
            .ConfigureAwait(false);
        Require(!secondReturnedEarly, "A second disposer must not report success before shared cleanup fails.");
        Require(firstFailure is InvalidOperationException
            && secondFailure is InvalidOperationException
            && lateFailure is InvalidOperationException,
            "Concurrent and later disposers must all observe the stored shutdown failure.");
        Require(firstFailure!.Message == "Synthetic synchronous stop failure."
            && secondFailure!.Message == firstFailure.Message
            && lateFailure!.Message == firstFailure.Message,
            "Shared cleanup must preserve the original failure for all disposal callers.");
        Require(lane.StopCalls == 1, "A failed shared shutdown must not be retried implicitly by disposal.");
        Require(supervisor.GetSnapshot().Lanes.Single().State == M3uaRuntimeState.Faulted,
            "Failed shutdown must remain attributable in lane diagnostics.");
    }

    private static async Task<Exception?> CaptureFailureAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static M3uaAssociationPool Pool(params string[] names) => new(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        names.Select(name => new M3uaAssociationDefinition(
            name, "synthetic-sg", 0, M3uaAssociationOperationalState.Active, [100])));

    private static Mtp3TransferMessage Transfer() => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(1234, 4321, 0),
        new byte[] { 0x01 },
        routingContext: 100);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class GatedStopLane : IM3uaAssociationRuntimeLane
    {
        private readonly FakeRuntimeLane _inner;
        private readonly TaskCompletionSource<bool> _stopEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _stopRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopCalls;

        internal GatedStopLane(string name, bool failStop = false) =>
            _inner = new(name, throwOnStop: failStop);

        public string AssociationName => _inner.AssociationName;
        public M3uaRuntimeState State => _inner.State;
        internal Task StopEntered => _stopEntered.Task;
        internal int StopCalls => Volatile.Read(ref _stopCalls);
        internal void ReleaseStop() => _stopRelease.TrySetResult(true);
        internal void RaiseActivation() => _inner.TransitionTo(
            M3uaRuntimeState.Active, M3uaRuntimeEventKind.AspActivated, "synthetic racing activation");

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent
        {
            add => _inner.RuntimeEvent += value;
            remove => _inner.RuntimeEvent -= value;
        }

        public ValueTask StartAsync(CancellationToken ct = default) => _inner.StartAsync(ct);
        public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default) =>
            _inner.ReceiveAsync(ct);
        public M3uaRuntimeMetrics GetMetrics() => _inner.GetMetrics();

        public async ValueTask StopAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref _stopCalls);
            _stopEntered.TrySetResult(true);
            await _stopRelease.Task.WaitAsync(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            await _inner.StopAsync(ct).ConfigureAwait(false);
        }
    }
}
