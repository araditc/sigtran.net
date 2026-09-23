using System.Runtime.CompilerServices;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class RuntimeHealthRoutingRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyReconnectingLoadshareMemberFailsClosedAndRecovers();
        VerifyFaultedOverridePrimaryFailsOverAndRecovers();
    }

    private static void VerifyReconnectingLoadshareMemberFailsClosedAndRecovers()
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

        Mtp3TransferMessage transfer = CreateTransfer(sls: 1, routingContext: 100);
        Equal(
            "b",
            pool.SelectTargets(transfer).Single().Name,
            "Stable two-member loadshare must route SLS=1 to the second deterministic member.");

        pool.SetState("b", M3uaAssociationOperationalState.Reconnecting);
        Equal(
            "a",
            pool.SelectTargets(transfer).Single().Name,
            "A reconnecting loadshare member must receive no new traffic and the healthy member must remain eligible.");
        Equal(
            M3uaAssociationOperationalState.Reconnecting,
            Snapshot(pool, "b").State,
            "Route diagnostics must retain the reconnecting state while the member is excluded.");

        pool.SetState("b", M3uaAssociationOperationalState.Active);
        Equal(
            "b",
            pool.SelectTargets(transfer).Single().Name,
            "Explicit recovery must restore the original deterministic SLS affinity when membership is stable again.");

        Equal(1L, Snapshot(pool, "a").SelectedTransfers,
            "The healthy peer must account only for the selection made while its partner was reconnecting.");
        Equal(2L, Snapshot(pool, "b").SelectedTransfers,
            "The recovered member must retain selections made before and after recovery without counting excluded traffic.");

        Console.WriteLine("PASS Reconnecting loadshare member is excluded and deterministic affinity recovers");
    }

    private static void VerifyFaultedOverridePrimaryFailsOverAndRecovers()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "primary",
                    "sg-a",
                    0,
                    M3uaAssociationOperationalState.Active,
                    [100]),
                new M3uaAssociationDefinition(
                    "secondary",
                    "sg-b",
                    1,
                    M3uaAssociationOperationalState.Active,
                    [100])
            ]);

        Mtp3TransferMessage transfer = CreateTransfer(sls: 7, routingContext: 100);
        Equal(
            "primary",
            pool.SelectTargets(transfer).Single().Name,
            "Override routing must prefer the highest-priority active association.");

        pool.ApplyDispatchFailureState("primary", ambiguous: false);
        Equal(
            M3uaAssociationOperationalState.Faulted,
            Snapshot(pool, "primary").State,
            "A proven dispatch failure must make the primary route faulted and ineligible.");
        Equal(
            "secondary",
            pool.SelectTargets(transfer).Single().Name,
            "Override routing must use the healthy lower-priority association while the primary is faulted.");

        pool.SetState("primary", M3uaAssociationOperationalState.Active);
        Equal(
            "primary",
            pool.SelectTargets(transfer).Single().Name,
            "Recovery must be explicit; once reactivated, the higher-priority primary must regain precedence.");

        Console.WriteLine("PASS Faulted override primary fails over and requires explicit recovery");
    }

    private static M3uaAssociationRouteSnapshot Snapshot(
        M3uaAssociationPool pool,
        string associationName) =>
        pool.GetSnapshot().Single(snapshot => string.Equals(
            snapshot.Name,
            associationName,
            StringComparison.OrdinalIgnoreCase));

    private static Mtp3TransferMessage CreateTransfer(byte sls, uint? routingContext) =>
        new(
            new Mtp3ServiceInformationOctet(
                Mtp3ServiceIndicator.Sccp,
                networkIndicator: 2),
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
}
