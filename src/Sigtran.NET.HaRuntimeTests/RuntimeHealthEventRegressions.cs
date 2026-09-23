using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

// Executed from the normal async test entry point, never a module initializer.
internal static class RuntimeHealthEventRegressions
{
    private static readonly M3uaRuntimeEventKind[] DiagnosticKinds =
    [
        M3uaRuntimeEventKind.TransferSent,
        M3uaRuntimeEventKind.TransferReceived,
        M3uaRuntimeEventKind.HeartbeatAcknowledged,
        (M3uaRuntimeEventKind)int.MaxValue
    ];

    internal static async Task DiagnosticsCannotRecoverFaultedRouteAsync()
    {
        FakeRuntimeLane lane = new("a");
        M3uaAssociationPool pool = Pool("a");
        await using M3uaHaRuntimeSupervisor supervisor = new(
            [lane], inboundCapacity: 1, routePool: pool);
        await supervisor.StartAsync().ConfigureAwait(false);
        lane.CaptureRuntimeEventHandler();
        lane.ObserveFaultWhileActive("synthetic fault before reconnect transition");

        foreach (M3uaRuntimeEventKind kind in DiagnosticKinds)
        {
            lane.RaiseCapturedRuntimeEvent(kind, M3uaRuntimeState.Active, "stale diagnostic state");
            Require(Route(pool, "a").RuntimeState == M3uaRuntimeState.Reconnecting,
                $"Diagnostic {kind} must not clear the observed recoverable fault.");
            Require(pool.SelectTargets(Transfer()).Count == 0,
                $"Diagnostic {kind} must not reopen route admission.");
        }

        Require(Route(pool, "a").State == M3uaAssociationOperationalState.Active,
            "Health exclusion must not rewrite the node role.");
        Require(supervisor.GetSnapshot().Lanes.Single().FaultEvents == 1,
            "Stale diagnostic events must not create or erase fault observations.");

        lane.TransitionTo(M3uaRuntimeState.Active, M3uaRuntimeEventKind.AspActivated,
            "synthetic explicit reconnect completion");
        Require(Route(pool, "a").RuntimeState == M3uaRuntimeState.Active,
            "Explicit ASP activation must restore live health.");
        Require(pool.SelectTargets(Transfer()).Single().Name == "a",
            "Explicit recovery must restore route admission.");
    }

    internal static async Task DiagnosticsCannotClearTerminalFaultAsync()
    {
        FakeRuntimeLane failed = new("a");
        FakeRuntimeLane healthy = new("b");
        M3uaAssociationPool pool = Pool("a", "b");
        await using M3uaHaRuntimeSupervisor supervisor = new(
            [failed, healthy], inboundCapacity: 1, routePool: pool);
        await supervisor.StartAsync().ConfigureAwait(false);
        // Fake startup is synchronous: both pumps subscribe and activate before
        // supervisor StartAsync returns. No timing sleeps are needed here.
        failed.CaptureRuntimeEventHandler();
        failed.Fault("synthetic terminal association failure");

        foreach (M3uaRuntimeEventKind kind in DiagnosticKinds)
        {
            failed.RaiseCapturedRuntimeEvent(kind, M3uaRuntimeState.Active, "late pre-fault diagnostic");
            Require(Route(pool, "a").RuntimeState == M3uaRuntimeState.Faulted,
                $"Diagnostic {kind} must not clear terminal route health.");
            Require(supervisor.GetSnapshot().Lanes.Single(x => x.AssociationName == "a").State
                == M3uaRuntimeState.Faulted,
                "The terminal lane must remain attributable as faulted.");
            Require(pool.SelectTargets(Transfer()).Single().Name == "b",
                "Only the healthy peer may carry new work after a terminal fault.");
        }
    }

    internal static async Task StartupCompletionCannotClearObservedFaultAsync()
    {
        FaultBeforeStartupReturnLane lane = new("a");
        M3uaAssociationPool pool = Pool("a");
        await using M3uaHaRuntimeSupervisor supervisor = new(
            [lane], inboundCapacity: 1, routePool: pool);

        await supervisor.StartAsync().ConfigureAwait(false);

        Require(lane.State == M3uaRuntimeState.Active,
            "The synthetic underlying lane must expose the real fault-before-state-transition gap.");
        Require(Route(pool, "a").RuntimeState == M3uaRuntimeState.Reconnecting,
            "A first-activation completion must not overwrite a newer fault observation.");
        Require(pool.SelectTargets(Transfer()).Count == 0,
            "A reported fault before startup continuation must leave admission closed.");
        Require(supervisor.GetSnapshot().Lanes.Single().FaultEvents == 1,
            "The startup/fault ordering must retain its fault observation.");
    }

    private static M3uaAssociationPool Pool(params string[] names) => new(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        names.Select(name => new M3uaAssociationDefinition(
            name, "synthetic-sg", 0, M3uaAssociationOperationalState.Active, [100])));

    private static M3uaAssociationRouteSnapshot Route(M3uaAssociationPool pool, string name) =>
        pool.GetSnapshot().Single(route => route.Name == name);

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

    private sealed class FaultBeforeStartupReturnLane : IM3uaAssociationRuntimeLane
    {
        private readonly FakeRuntimeLane _inner;

        internal FaultBeforeStartupReturnLane(string name) => _inner = new(name);

        public string AssociationName => _inner.AssociationName;

        public M3uaRuntimeState State => _inner.State;

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent
        {
            add => _inner.RuntimeEvent += value;
            remove => _inner.RuntimeEvent -= value;
        }

        public async ValueTask StartAsync(CancellationToken ct = default)
        {
            await _inner.StartAsync(ct).ConfigureAwait(false);
            _inner.ObserveFaultWhileActive("synthetic fault before startup completion returns");
        }

        public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default) =>
            _inner.ReceiveAsync(ct);

        public ValueTask StopAsync(CancellationToken ct = default) => _inner.StopAsync(ct);

        public M3uaRuntimeMetrics GetMetrics() => _inner.GetMetrics();
    }
}
