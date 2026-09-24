using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class TopologyDiagnosticsRegression
{
    internal static async Task SnapshotReconcilesRuntimeGenerationAndDispatchAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Loadshare,
            M3uaTrafficModeType.Loadshare,
            [
                new M3uaAssociationDefinition(
                    "b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]),
                new M3uaAssociationDefinition(
                    "a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100])
            ]);
        TopologyRuntimeLane laneB = new("b");
        TopologyRuntimeLane laneA = new("a");
        M3uaHaRuntimeSupervisor runtime = new([laneB, laneA], inboundCapacity: 4);
        TopologySender transportB = new("b");
        TopologySender transportA = new("a");
        M3uaReconnectFencedAssociationSender senderB = new(transportB);
        M3uaReconnectFencedAssociationSender senderA = new(transportA);
        M3uaRuntimeGenerationFenceBinding bindingB = new(laneB, senderB, pool);
        M3uaRuntimeGenerationFenceBinding bindingA = new(laneA, senderA, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [senderB, senderA]);
        M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 2);
        M3uaHaTopologyDiagnostics diagnostics = new(
            pool,
            runtime,
            coordinator,
            [
                new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("b", bindingB),
                new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", bindingA)
            ]);

        try
        {
            M3uaHaTopologySnapshot initial = diagnostics.GetSnapshot();
            Require(initial.Associations.Select(static item => item.Name).SequenceEqual(["a", "b"]),
                "Topology associations must be deterministically ordered by association name.");
            Require(initial.Associations.All(static item =>
                    item.RouteRuntimeState == M3uaRuntimeState.Stopped
                    && !item.TransportAcceptingDispatch
                    && item.TransportGeneration == 0),
                "A stopped topology must remain fail-closed before runtime activation.");

            await runtime.StartAsync().ConfigureAwait(false);
            await WaitUntilAsync(
                () => senderA.GetSnapshot().AcceptingDispatch
                    && senderB.GetSnapshot().AcceptingDispatch,
                "Both synthetic runtime generations did not become dispatch-ready.")
                .ConfigureAwait(false);

            IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
                await coordinator.DispatchAsync(Transfer()).ConfigureAwait(false);
            Require(outcomes.Count == 1
                && outcomes[0].Disposition == M3uaDispatchDisposition.Sent
                && !string.IsNullOrWhiteSpace(outcomes[0].AssociationName),
                "One healthy loadshare association must own the synthetic dispatch.");

            M3uaHaTopologySnapshot active = diagnostics.GetSnapshot();
            Require(active.NodeRoutingMode == M3uaNodeRoutingMode.Loadshare
                && active.ProtocolTrafficMode == M3uaTrafficModeType.Loadshare,
                "Topology snapshot must preserve node policy separately from protocol traffic mode.");
            Require(active.PendingDispatches == 0
                && active.AdmittedDispatches == 1
                && active.CompletedDispatches == 1
                && active.SentOutcomes == 1
                && active.NotDispatchedOutcomes == 0
                && active.AmbiguousOutcomes == 0
                && active.NoRouteOutcomes == 0,
                "Aggregate dispatch counters must reconcile after one successful dispatch.");
            Require(active.Associations.All(static item =>
                    item.SupervisorRuntimeState == M3uaRuntimeState.Active
                    && item.RouteRuntimeState == M3uaRuntimeState.Active
                    && item.TransportGeneration == 1
                    && item.TransportAcceptingDispatch
                    && item.FenceReason == M3uaAssociationFenceReason.None),
                "Healthy lanes must expose aligned runtime, route-health, and transport-generation state.");
            Require(active.Associations.Sum(static item => item.SentOutcomes) == 1,
                "Per-association sent counters must reconcile with the aggregate sent counter.");
            Require(active.Associations.Sum(static item => item.SelectedTransfers) == 1,
                "Per-association route selection counters must reconcile with one loadshare selection.");

            laneB.Fault("synthetic-b-loss");
            await WaitUntilAsync(
                () => !senderB.GetSnapshot().AcceptingDispatch,
                "Faulted lane b did not close transport-generation admission.")
                .ConfigureAwait(false);

            M3uaHaTopologySnapshot isolated = diagnostics.GetSnapshot();
            M3uaHaAssociationTopologySnapshot b = Association(isolated, "b");
            M3uaHaAssociationTopologySnapshot a = Association(isolated, "a");
            Require(b.SupervisorRuntimeState == M3uaRuntimeState.Reconnecting
                && b.RouteRuntimeState == M3uaRuntimeState.Reconnecting
                && !b.TransportAcceptingDispatch
                && b.FenceReason == M3uaAssociationFenceReason.RuntimeUnavailable
                && b.RuntimeFaultEvents >= 1,
                "A faulted lane must remain attributable and non-eligible across runtime, route, and generation diagnostics.");
            Require(a.SupervisorRuntimeState == M3uaRuntimeState.Active
                && a.RouteRuntimeState == M3uaRuntimeState.Active
                && a.TransportAcceptingDispatch,
                "A peer lane must remain independently healthy after another association faults.");
        }
        finally
        {
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await bindingA.DisposeAsync().ConfigureAwait(false);
            await bindingB.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task MembershipMismatchFailsClosedAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
                new M3uaAssociationDefinition(
                    "b", "sg-b", 1, M3uaAssociationOperationalState.Active, [100])
            ]);
        TopologyRuntimeLane laneA = new("a");
        TopologyRuntimeLane laneB = new("b");
        M3uaHaRuntimeSupervisor runtime = new([laneA, laneB], inboundCapacity: 2);
        M3uaReconnectFencedAssociationSender senderA = new(new TopologySender("a"));
        M3uaReconnectFencedAssociationSender senderB = new(new TopologySender("b"));
        M3uaRuntimeGenerationFenceBinding bindingA = new(laneA, senderA, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [senderA, senderB]);
        M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

        try
        {
            Throws<ArgumentException>(() =>
                _ = new M3uaHaTopologyDiagnostics(
                    pool,
                    runtime,
                    coordinator,
                    [new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", bindingA)]));
        }
        finally
        {
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await bindingA.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task BindingRuntimeOwnershipMismatchFailsClosedAsync()
    {
        M3uaAssociationPool pool = SingleAssociationPool();
        TopologyRuntimeLane supervisedLane = new("a");
        TopologyRuntimeLane shadowLaneWithSameName = new("a");
        M3uaHaRuntimeSupervisor runtime = new([supervisedLane], inboundCapacity: 1);
        M3uaReconnectFencedAssociationSender supervisedSender =
            new(new TopologySender("a"));
        M3uaReconnectFencedAssociationSender shadowSender =
            new(new TopologySender("a"));
        M3uaRuntimeGenerationFenceBinding shadowBinding =
            new(shadowLaneWithSameName, shadowSender, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [supervisedSender]);
        M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

        try
        {
            Throws<ArgumentException>(() =>
                _ = new M3uaHaTopologyDiagnostics(
                    pool,
                    runtime,
                    coordinator,
                    [new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", shadowBinding)]));
        }
        finally
        {
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await shadowBinding.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task CoordinatorPoolOwnershipMismatchFailsClosedAsync()
    {
        M3uaAssociationPool pool = SingleAssociationPool();
        M3uaAssociationPool foreignPool = SingleAssociationPool();
        TopologyRuntimeLane lane = new("a");
        M3uaHaRuntimeSupervisor runtime = new([lane], inboundCapacity: 1);
        M3uaReconnectFencedAssociationSender sender = new(new TopologySender("a"));
        M3uaReconnectFencedAssociationSender foreignSender = new(new TopologySender("a"));
        M3uaRuntimeGenerationFenceBinding binding = new(lane, sender, pool);
        M3uaAssociationDispatcher foreignDispatcher = new(foreignPool, [foreignSender]);
        M3uaHaDispatchCoordinator foreignCoordinator =
            new(foreignDispatcher, perSlsQueueCapacity: 1);

        try
        {
            Throws<ArgumentException>(() =>
                _ = new M3uaHaTopologyDiagnostics(
                    pool,
                    runtime,
                    foreignCoordinator,
                    [new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", binding)]));
        }
        finally
        {
            await foreignCoordinator.DisposeAsync().ConfigureAwait(false);
            await binding.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task DispatchSenderOwnershipMismatchFailsClosedAsync()
    {
        M3uaAssociationPool pool = SingleAssociationPool();
        TopologyRuntimeLane lane = new("a");
        M3uaHaRuntimeSupervisor runtime = new([lane], inboundCapacity: 1);
        M3uaReconnectFencedAssociationSender bindingSender =
            new(new TopologySender("a"));
        M3uaReconnectFencedAssociationSender dispatchSender =
            new(new TopologySender("a"));
        M3uaRuntimeGenerationFenceBinding binding =
            new(lane, bindingSender, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [dispatchSender]);
        M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

        try
        {
            Throws<ArgumentException>(() =>
                _ = new M3uaHaTopologyDiagnostics(
                    pool,
                    runtime,
                    coordinator,
                    [new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", binding)]));
        }
        finally
        {
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await binding.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task SupervisorRouteHealthPublisherMismatchFailsClosedAsync()
    {
        M3uaAssociationPool pool = SingleAssociationPool();
        TopologyRuntimeLane lane = new("a");
        M3uaHaRuntimeSupervisor runtime = new(
            [lane],
            inboundCapacity: 1,
            routePool: pool);
        M3uaReconnectFencedAssociationSender sender =
            new(new TopologySender("a"));
        M3uaRuntimeGenerationFenceBinding binding =
            new(lane, sender, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [sender]);
        M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

        try
        {
            Throws<ArgumentException>(() =>
                _ = new M3uaHaTopologyDiagnostics(
                    pool,
                    runtime,
                    coordinator,
                    [new KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>("a", binding)]));
        }
        finally
        {
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await binding.DisposeAsync().ConfigureAwait(false);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task DuplicateProductionRuntimeAliasFailsClosedAsync()
    {
        M3uaRuntime sharedRuntime = CreateStoppedRuntime();
        try
        {
            M3uaRuntimeAssociationLane laneA = new("a", sharedRuntime);
            M3uaRuntimeAssociationLane laneB = new("b", sharedRuntime);

            Throws<ArgumentException>(() =>
                _ = new M3uaHaRuntimeSupervisor([laneA, laneB], inboundCapacity: 1));
            Require(sharedRuntime.State == M3uaRuntimeState.Stopped,
                "Aliased production runtime composition must fail before either lane starts the shared runtime.");
        }
        finally
        {
            await sharedRuntime.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task DistinctProductionRuntimesAreAcceptedAsync()
    {
        M3uaRuntime runtimeA = CreateStoppedRuntime();
        M3uaRuntime runtimeB = CreateStoppedRuntime();
        M3uaHaRuntimeSupervisor? supervisor = null;

        try
        {
            M3uaRuntimeAssociationLane laneA = new("a", runtimeA);
            M3uaRuntimeAssociationLane laneB = new("b", runtimeB);
            supervisor = new M3uaHaRuntimeSupervisor([laneA, laneB], inboundCapacity: 1);

            M3uaHaRuntimeSupervisorSnapshot snapshot = supervisor.GetSnapshot();
            Require(snapshot.Lanes.Count == 2
                && snapshot.Lanes.All(static lane => lane.State == M3uaRuntimeState.Stopped),
                "Distinct production runtimes must remain independently admitted and stopped before explicit start.");
            Require(runtimeA.State == M3uaRuntimeState.Stopped
                && runtimeB.State == M3uaRuntimeState.Stopped,
                "Composition validation must not start either distinct production runtime.");
        }
        finally
        {
            if (supervisor is not null)
            {
                await supervisor.DisposeAsync().ConfigureAwait(false);
            }

            await runtimeA.DisposeAsync().ConfigureAwait(false);
            await runtimeB.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static M3uaRuntime CreateStoppedRuntime() => new(
        new M3uaDelegateRuntimeSessionFactory(_ =>
            new ValueTask<M3uaRuntimeSessionLease>(
                Task.FromException<M3uaRuntimeSessionLease>(
                    new InvalidOperationException(
                        "Production runtime uniqueness regression must not open transport.")))));

    private static M3uaAssociationPool SingleAssociationPool() => new(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        [new M3uaAssociationDefinition(
            "a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100])]);

    private static M3uaHaAssociationTopologySnapshot Association(
        M3uaHaTopologySnapshot snapshot,
        string name) => snapshot.Associations.Single(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

    private static Mtp3TransferMessage Transfer() => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(1234, 4321, 1),
        new byte[] { 0x01 },
        routingContext: 100);

    private static async Task WaitUntilAsync(Func<bool> condition, string failureMessage)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
    }

    private sealed class TopologyRuntimeLane : IM3uaAssociationRuntimeLane
    {
        private M3uaRuntimeState _state = M3uaRuntimeState.Stopped;
        private long _faults;

        internal TopologyRuntimeLane(string associationName) =>
            AssociationName = associationName;

        public string AssociationName { get; }
        public M3uaRuntimeState State => _state;
        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

        public ValueTask StartAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _state = M3uaRuntimeState.Starting;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic-starting");
            _state = M3uaRuntimeState.Active;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic-active");
            Raise(M3uaRuntimeEventKind.AspActivated, "synthetic-asp-active");
            return ValueTask.CompletedTask;
        }

        public async ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            throw new InvalidOperationException("Synthetic topology lane receive should only end by cancellation.");
        }

        public ValueTask StopAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _state = M3uaRuntimeState.Stopping;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic-stopping");
            _state = M3uaRuntimeState.Stopped;
            Raise(M3uaRuntimeEventKind.ShutdownCompleted, "synthetic-stopped");
            return ValueTask.CompletedTask;
        }

        public M3uaRuntimeMetrics GetMetrics() => new(
            _state,
            outboundQueueDepth: 0,
            inboundQueueDepth: 0,
            sentTransfers: 0,
            receivedTransfers: 0,
            heartbeatsSent: 0,
            heartbeatsAcknowledged: 0,
            heartbeatTimeouts: 0,
            reconnectAttempts: _state == M3uaRuntimeState.Reconnecting ? 1 : 0,
            faults: Interlocked.Read(ref _faults));

        internal void Fault(string detail)
        {
            Interlocked.Increment(ref _faults);
            Raise(M3uaRuntimeEventKind.FaultObserved, detail);
            _state = M3uaRuntimeState.Reconnecting;
            Raise(M3uaRuntimeEventKind.StateChanged, "synthetic-reconnecting");
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

    private sealed class TopologySender : IM3uaAssociationSender
    {
        private int _calls;

        internal TopologySender(string associationName) =>
            AssociationName = associationName;

        public string AssociationName { get; }
        internal int Calls => Volatile.Read(ref _calls);

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            ct.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _calls);
            return ValueTask.CompletedTask;
        }
    }
}
