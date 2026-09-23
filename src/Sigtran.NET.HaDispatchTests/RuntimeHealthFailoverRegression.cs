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
        Equal(M3uaAssociationOperationalState.Active, backupRoute.State,
            "The healthy standby must become the single policy-active route before sender invocation.");
        Equal(M3uaRuntimeState.Active, backupRoute.RuntimeState,
            "Promotion must not rewrite the standby's live runtime health.");

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

        Console.WriteLine("PASS Active standby fails closed when no runtime is healthy");
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

    private static Mtp3TransferMessage CreateTransfer(byte sls) => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01, sls },
        routingContext: 100);

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
        private int _calls;

        internal RecordingSender(string associationName) =>
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
