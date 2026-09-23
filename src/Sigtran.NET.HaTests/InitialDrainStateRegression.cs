using System.Runtime.CompilerServices;
using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class InitialDrainStateRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [new M3uaAssociationDefinition(
                "initial-drain",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Draining,
                [100])]);

        Mtp3TransferMessage transfer = new(
            new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
            new Mtp3RoutingLabel(
                destinationPointCode: 1234,
                originatingPointCode: 4321,
                signallingLinkSelection: 6),
            new byte[] { 0x01 },
            routingContext: 100);

        M3uaAssociationRouteSnapshot snapshot = pool.GetSnapshot().Single();
        Equal(M3uaAssociationOperationalState.Draining, snapshot.State,
            "An initially draining association must report Draining state.");
        Equal(0, snapshot.InFlightDispatches,
            "An initially draining association must begin with no admitted dispatches.");
        Equal(0, pool.SelectTargets(transfer).Count,
            "An initially draining association must remain ineligible for route selection.");
        Equal<M3uaAssociationDispatchLease?>(null, pool.TryAcquireDispatchLease("initial-drain", transfer),
            "An initially draining association must reject dispatch admission.");

        ValueTask drained = pool.WaitForDrainedAsync("initial-drain");
        Equal(true, drained.IsCompletedSuccessfully,
            "An initially draining association with no in-flight work must expose a completed drain waiter.");

        Console.WriteLine("PASS Initially draining association has settled drain tracking");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }
}
