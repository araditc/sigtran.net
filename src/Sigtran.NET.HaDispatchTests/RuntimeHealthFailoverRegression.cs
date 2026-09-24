using System.Runtime.CompilerServices;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class RuntimeHealthFailoverRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        KnownUnhealthyPrimaryPromotesHealthyStandby();
        RecoveredFormerPrimaryDoesNotPreemptCurrentActive();
        NoHealthyRuntimeReturnsExplicitNoRoute();
        AllStandbyTopologyRequiresExplicitPromotion();
        RoutingContextMismatchDoesNotAutoFailback();
        HealthyPrimarySelectionIsCountedOnce();
        ProvenPreDispatchFailureStillPromotesAndAccountsStandby();
    }

    private static void KnownUnhealthyPrimaryPromotesHealthyStandby()
    {
        M3uaAssociationPool pool = CreatePool();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Reconnecting);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Active);
        RecordingSender primary = new("primary");
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes = dispatcher
            .DispatchAsync(CreateTransfer(sls: 3))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(1, outcomes.Count,
            "Known runtime health should select one healthy association without manufacturing a failed transport attempt.");
        Equal("backup", outcomes[0].AssociationName,
            "The healthy standby must own the first dispatch when the policy-active runtime is already unhealthy.");
        Equal(M3uaDispatchDisposition.Sent, outcomes[0].Disposition,
            "The healthy standby dispatch should complete normally.");
        Equal(0, primary.Calls,
            "A runtime-ineligible primary must not be invoked merely to trigger failover.");
        Equal(1, backup.Calls,
            "The promoted standby must be invoked exactly once.");

        M3uaAssociationRouteSnapshot primaryRoute = Route(pool, "primary");
        M3uaAssociationRouteSnapshot backupRoute = Route(pool, "backup");
        Equal(M3uaAssociationOperationalState.Standby, primaryRoute.State,
            "Health-driven failover must demote the old policy-active route atomically.");
        Equal(M3uaRuntimeState.Reconnecting, primaryRoute.RuntimeState,
            "Failover must retain the old primary's live reconnecting health.");
        Equal(0L, primaryRoute.SelectedTransfers,
            "The runtime-ineligible primary must not be counted as selected when no sender is invoked.");
        Equal(M3uaAssociationOperationalState.Active, backupRoute.State,
            "The healthy standby must become the single policy-active route before sender invocation.");
        Equal(M3uaRuntimeState.Active, backupRoute.RuntimeState,
            "Promotion must not rewrite the standby's live runtime health.");
        Equal(1L, backupRoute.SelectedTransfers,
            "The atomically promoted standby must account exactly one selected transfer.");

        Console.WriteLine("PASS Active standby bypasses known-unhealthy primary before sender invocation");
    }

    private static void RecoveredFormerPrimaryDoesNotPreemptCurrentActive()
    {
        M3uaAssociationPool pool = CreatePool();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Reconnecting);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Active);
        RecordingSender primary = new("primary");
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        dispatcher.DispatchAsync(CreateTransfer(sls: 4))
            .AsTask()
            .GetAwaiter()
            .GetResult();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Active);

        IReadOnlyList<M3uaAssociationDispatchOutcome> recovered = dispatcher
            .DispatchAsync(CreateTransfer(sls: 5))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(1, recovered.Count,
            "A later transaction should still have one selected active owner.");
        Equal("backup", recovered[0].AssociationName,
            "Runtime recovery alone must not automatically fail back policy ownership.");
        Equal(0, primary.Calls,
            "A recovered standby must not preempt the current active route without explicit policy promotion.");
        Equal(2, backup.Calls,
            "The currently active route should carry both independent transactions.");
        Equal(M3uaAssociationOperationalState.Standby, Route(pool, "primary").State,
            "Former primary must remain policy standby after runtime recovery.");
        Equal(M3uaAssociationOperationalState.Active, Route(pool, "backup").State,
            "Current active ownership must remain stable after peer recovery.");
        Equal(2L, Route(pool, "backup").SelectedTransfers,
            "Each independent transaction selected on the current active route must be counted once.");

        Console.WriteLine("PASS Runtime recovery does not trigger automatic active-standby failback");
    }

    private static void NoHealthyRuntimeReturnsExplicitNoRoute()
    {
        M3uaAssociationPool pool = CreatePool();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Reconnecting);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Starting);
        RecordingSender primary = new("primary");
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes = dispatcher
            .DispatchAsync(CreateTransfer(sls: 6))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(1, outcomes.Count,
            "Missing healthy active/standby capacity must produce one structured outcome.");
        Equal(M3uaDispatchDisposition.NoRoute, outcomes[0].Disposition,
            "Known-unhealthy active/standby capacity must fail closed as NoRoute before sender invocation.");
        Equal<string?>(null, outcomes[0].AssociationName,
            "A no-route result must not invent dispatch ownership.");
        Equal(0, primary.Calls,
            "No sender may be invoked while the primary runtime is ineligible.");
        Equal(0, backup.Calls,
            "No sender may be invoked while the standby runtime is ineligible.");
        Equal(M3uaAssociationOperationalState.Active, Route(pool, "primary").State,
            "No-route evaluation must not mutate policy ownership when no standby is eligible.");
        Equal(M3uaAssociationOperationalState.Standby, Route(pool, "backup").State,
            "No-route evaluation must preserve standby policy when promotion is impossible.");
        Equal(0L, Route(pool, "primary").SelectedTransfers,
            "No route must not create selection accounting on the unhealthy primary.");
        Equal(0L, Route(pool, "backup").SelectedTransfers,
            "No route must not create selection accounting on an unhealthy standby.");

        Console.WriteLine("PASS Active standby fails closed when no runtime is healthy");
    }

    private static void AllStandbyTopologyRequiresExplicitPromotion()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "a", "sg-a", 0, M3uaAssociationOperationalState.Standby, [100]),
                new M3uaAssociationDefinition(
                    "b", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100])
            ]);
        pool.SetRuntimeState("a", M3uaRuntimeState.Active);
        pool.SetRuntimeState("b", M3uaRuntimeState.Active);
        RecordingSender a = new("a");
        RecordingSender b = new("b");
        M3uaAssociationDispatcher dispatcher = new(pool, [a, b]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes = dispatcher
            .DispatchAsync(CreateTransfer(sls: 7))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(1, outcomes.Count, "An all-standby policy must produce one explicit result.");
        Equal(M3uaDispatchDisposition.NoRoute, outcomes[0].Disposition,
            "Healthy runtime alone must not create active policy ownership.");
        Equal(0, a.Calls, "Standby a must not be invoked before explicit promotion.");
        Equal(0, b.Calls, "Standby b must not be invoked before explicit promotion.");
        Equal(M3uaAssociationOperationalState.Standby, Route(pool, "a").State,
            "All-standby policy must remain unchanged after no-route evaluation.");
        Equal(M3uaAssociationOperationalState.Standby, Route(pool, "b").State,
            "All-standby policy must remain unchanged after no-route evaluation.");
        Equal(0L, Route(pool, "a").SelectedTransfers,
            "Rejected all-standby admission must not mutate selection metrics.");
        Equal(0L, Route(pool, "b").SelectedTransfers,
            "Rejected all-standby admission must not mutate selection metrics.");

        Console.WriteLine("PASS All-standby topology requires explicit policy promotion");
    }

    private static void RoutingContextMismatchDoesNotAutoFailback()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100, 200]),
                new M3uaAssociationDefinition(
                    "backup", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100])
            ]);
        pool.SetRuntimeState("primary", M3uaRuntimeState.Reconnecting);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Active);
        RecordingSender primary = new("primary");
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> first = dispatcher
            .DispatchAsync(CreateTransfer(sls: 8, routingContext: 100))
            .AsTask()
            .GetAwaiter()
            .GetResult();
        Equal("backup", first.Single().AssociationName,
            "RC100 must fail over to the compatible healthy standby.");
        pool.SetRuntimeState("primary", M3uaRuntimeState.Active);

        IReadOnlyList<M3uaAssociationDispatchOutcome> rc200 = dispatcher
            .DispatchAsync(CreateTransfer(sls: 9, routingContext: 200))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(1, rc200.Count, "Unsupported current-active routing context must return one result.");
        Equal(M3uaDispatchDisposition.NoRoute, rc200[0].Disposition,
            "A recovered standby must not be automatically promoted merely because it matches another routing context.");
        Equal<string?>(null, rc200[0].AssociationName,
            "Routing-context no-route must not invent transaction ownership.");
        Equal(0, primary.Calls,
            "Recovered former primary must not receive automatic failback traffic for RC200.");
        Equal(1, backup.Calls,
            "Backup should only have carried the original RC100 transaction.");
        Equal(M3uaAssociationOperationalState.Standby, Route(pool, "primary").State,
            "Former primary must remain standby after runtime recovery and RC mismatch.");
        Equal(M3uaAssociationOperationalState.Active, Route(pool, "backup").State,
            "Current active route must remain policy owner after RC mismatch.");
        Equal(0L, Route(pool, "primary").SelectedTransfers,
            "RC mismatch must not count the recovered standby as selected.");
        Equal(1L, Route(pool, "backup").SelectedTransfers,
            "Only the successful RC100 selection should be counted on the backup.");

        Console.WriteLine("PASS Routing-context mismatch cannot trigger automatic failback");
    }

    private static void HealthyPrimarySelectionIsCountedOnce()
    {
        M3uaAssociationPool pool = CreatePool();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Active);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Active);
        RecordingSender primary = new("primary");
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes = dispatcher
            .DispatchAsync(CreateTransfer(sls: 10))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(M3uaDispatchDisposition.Sent, outcomes.Single().Disposition,
            "Healthy active route should send normally.");
        Equal("primary", outcomes.Single().AssociationName,
            "Healthy policy-active route should retain ownership.");
        Equal(1, primary.Calls, "Healthy primary must be invoked exactly once.");
        Equal(0, backup.Calls, "Healthy standby must not be invoked.");
        Equal(1L, Route(pool, "primary").SelectedTransfers,
            "Atomic initial active selection must increment selection accounting exactly once.");
        Equal(0L, Route(pool, "backup").SelectedTransfers,
            "Unselected standby must retain zero selection count.");

        Console.WriteLine("PASS Healthy active-standby selection accounting is exact");
    }

    private static void ProvenPreDispatchFailureStillPromotesAndAccountsStandby()
    {
        M3uaAssociationPool pool = CreatePool();
        pool.SetRuntimeState("primary", M3uaRuntimeState.Active);
        pool.SetRuntimeState("backup", M3uaRuntimeState.Active);
        RecordingSender primary = new("primary", failBeforeDispatch: true);
        RecordingSender backup = new("backup");
        M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes = dispatcher
            .DispatchAsync(CreateTransfer(sls: 11))
            .AsTask()
            .GetAwaiter()
            .GetResult();

        Equal(2, outcomes.Count,
            "A proven pre-dispatch primary failure must retain one safe standby retry.");
        Equal(M3uaDispatchDisposition.NotDispatched, outcomes[0].Disposition,
            "First attempt must remain classified as definitely not dispatched.");
        Equal(M3uaDispatchDisposition.Sent, outcomes[1].Disposition,
            "Promoted standby must complete the safe retry.");
        Equal("backup", outcomes[1].AssociationName,
            "Safe retry must use the compatible promoted standby.");
        Equal(1, primary.Calls, "Primary must be invoked exactly once.");
        Equal(1, backup.Calls, "Backup must be invoked exactly once after proven-safe failure.");
        Equal(1L, Route(pool, "primary").SelectedTransfers,
            "The failed but definitely invoked primary selection must be counted once.");
        Equal(1L, Route(pool, "backup").SelectedTransfers,
            "The safe standby retry selection must be counted once.");
        Equal(M3uaAssociationOperationalState.Faulted, Route(pool, "primary").State,
            "Proven pre-dispatch failure must remain distinguishable from ambiguous fencing.");
        Equal(M3uaAssociationOperationalState.Active, Route(pool, "backup").State,
            "Safe retry must leave the promoted standby as current active policy owner.");

        Console.WriteLine("PASS Proven pre-dispatch failure retains safe failover and exact accounting");
    }

    private static M3uaAssociationPool CreatePool() => new(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        [
            new M3uaAssociationDefinition(
                "primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
            new M3uaAssociationDefinition(
                "backup", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100])
        ]);

    private static M3uaAssociationRouteSnapshot Route(
        M3uaAssociationPool pool,
        string associationName) => pool.GetSnapshot().Single(route =>
            string.Equals(route.Name, associationName, StringComparison.OrdinalIgnoreCase));

    private static Mtp3TransferMessage CreateTransfer(
        byte sls,
        uint routingContext = 100) => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01, sls },
        routingContext: routingContext);

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private sealed class RecordingSender : IM3uaAssociationSender
    {
        private readonly bool _failBeforeDispatch;
        private int _calls;

        internal RecordingSender(
            string associationName,
            bool failBeforeDispatch = false)
        {
            AssociationName = associationName;
            _failBeforeDispatch = failBeforeDispatch;
        }

        public string AssociationName { get; }

        internal int Calls => Volatile.Read(ref _calls);

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            ct.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _calls);
            if (_failBeforeDispatch)
            {
                throw new M3uaAssociationSendException(
                    "Synthetic failure before transport dispatch.",
                    dispatchMayHaveOccurred: false);
            }

            return ValueTask.CompletedTask;
        }
    }
}
