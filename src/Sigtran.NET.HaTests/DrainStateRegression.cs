using System.Runtime.CompilerServices;
using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class DrainStateRegression
{
    [ModuleInitializer]
    internal static void VerifyFailedDrainCannotReactivateBeforeLeaseRelease()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [new M3uaAssociationDefinition(
                "guarded",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100])]);
        Mtp3TransferMessage transfer = new(
            new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
            new Mtp3RoutingLabel(
                destinationPointCode: 1234,
                originatingPointCode: 4321,
                signallingLinkSelection: 3),
            new byte[] { 0x01 },
            routingContext: 100);

        M3uaAssociationDispatchLease lease = pool.TryAcquireDispatchLease("guarded", transfer)
            ?? throw new InvalidOperationException("Expected an active association dispatch lease.");

        pool.BeginDrain("guarded");
        pool.ApplyDispatchFailureState("guarded", ambiguous: true);

        bool rejected = false;
        try
        {
            pool.SetState("guarded", M3uaAssociationOperationalState.Active);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        if (!rejected)
        {
            throw new InvalidOperationException(
                "A failed graceful drain must not reactivate while an admitted dispatch lease remains in flight.");
        }

        M3uaAssociationRouteSnapshot pending = pool.GetSnapshot().Single();
        if (pending.State != M3uaAssociationOperationalState.Fenced
            || pending.InFlightDispatches != 1)
        {
            throw new InvalidOperationException(
                "Rejected reactivation must preserve the fenced state and outstanding lease.");
        }

        Task drained = pool.WaitForDrainedAsync("guarded").AsTask();
        if (drained.IsCompleted)
        {
            throw new InvalidOperationException(
                "The drain waiter must remain pending until the failed in-flight dispatch lease is released.");
        }

        lease.Dispose();
        drained.GetAwaiter().GetResult();

        M3uaAssociationRouteSnapshot settled = pool.GetSnapshot().Single();
        if (settled.State != M3uaAssociationOperationalState.Fenced
            || settled.InFlightDispatches != 0)
        {
            throw new InvalidOperationException(
                "Drain settlement must retain fenced ownership and clear the in-flight count.");
        }

        Console.WriteLine("PASS Failed draining route cannot reactivate before lease release");
    }
}
