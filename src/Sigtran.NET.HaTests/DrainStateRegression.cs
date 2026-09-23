using System.Runtime.CompilerServices;
using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class DrainStateRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyFailedDrainCannotReactivateBeforeLeaseRelease();
        VerifyFailureFirstCanStillBeginDrain();
        VerifySetStateDrainingCoordinatesOutstandingLease();
    }

    private static void VerifyFailedDrainCannotReactivateBeforeLeaseRelease()
    {
        M3uaAssociationPool pool = CreatePool("guarded");
        Mtp3TransferMessage transfer = CreateTransfer(3);
        M3uaAssociationDispatchLease lease = Acquire(pool, "guarded", transfer);

        pool.BeginDrain("guarded");
        pool.ApplyDispatchFailureState("guarded", ambiguous: true);

        Throws<InvalidOperationException>(() =>
            pool.SetState("guarded", M3uaAssociationOperationalState.Active));

        M3uaAssociationRouteSnapshot pending = pool.GetSnapshot().Single();
        Equal(M3uaAssociationOperationalState.Fenced, pending.State,
            "Rejected reactivation must preserve the fenced state.");
        Equal(1, pending.InFlightDispatches,
            "Rejected reactivation must preserve the outstanding lease.");

        Task drained = pool.WaitForDrainedAsync("guarded").AsTask();
        Equal(false, drained.IsCompleted,
            "The drain waiter must remain pending until the failed in-flight dispatch lease is released.");

        lease.Dispose();
        drained.GetAwaiter().GetResult();

        M3uaAssociationRouteSnapshot settled = pool.GetSnapshot().Single();
        Equal(M3uaAssociationOperationalState.Fenced, settled.State,
            "Drain settlement must retain fenced ownership.");
        Equal(0, settled.InFlightDispatches,
            "Drain settlement must clear the in-flight count.");

        Console.WriteLine("PASS Failed draining route cannot reactivate before lease release");
    }

    private static void VerifyFailureFirstCanStillBeginDrain()
    {
        M3uaAssociationPool pool = CreatePool("failed-first");
        Mtp3TransferMessage transfer = CreateTransfer(4);
        M3uaAssociationDispatchLease lease = Acquire(pool, "failed-first", transfer);

        pool.ApplyDispatchFailureState("failed-first", ambiguous: true);
        Equal(M3uaAssociationOperationalState.Fenced, pool.GetSnapshot().Single().State,
            "The ambiguous send must fence the route before drain begins.");

        pool.BeginDrain("failed-first");
        M3uaAssociationRouteSnapshot draining = pool.GetSnapshot().Single();
        Equal(M3uaAssociationOperationalState.Fenced, draining.State,
            "Beginning drain after failure must preserve the stronger fenced state.");
        Equal(1, draining.InFlightDispatches,
            "Failure-first drain must retain the admitted in-flight lease.");
        Equal(0, pool.SelectTargets(transfer).Count,
            "A fenced route under drain must remain ineligible for new work.");

        Task drained = pool.WaitForDrainedAsync("failed-first").AsTask();
        Equal(false, drained.IsCompleted,
            "Failure-first drain must wait for its outstanding lease.");
        Throws<InvalidOperationException>(() =>
            pool.SetState("failed-first", M3uaAssociationOperationalState.Active));

        lease.Dispose();
        drained.GetAwaiter().GetResult();
        Equal(0, pool.GetSnapshot().Single().InFlightDispatches,
            "Failure-first drain must settle after lease release.");

        Console.WriteLine("PASS Failed route can establish graceful drain for outstanding work");
    }

    private static void VerifySetStateDrainingCoordinatesOutstandingLease()
    {
        M3uaAssociationPool pool = CreatePool("state-drain");
        Mtp3TransferMessage transfer = CreateTransfer(5);
        M3uaAssociationDispatchLease lease = Acquire(pool, "state-drain", transfer);

        pool.SetState("state-drain", M3uaAssociationOperationalState.Draining);
        Equal(M3uaAssociationOperationalState.Draining, pool.GetSnapshot().Single().State,
            "SetState Draining must make the route ineligible.");
        Equal(0, pool.SelectTargets(transfer).Count,
            "SetState Draining must reject new route selections.");

        Task drained = pool.WaitForDrainedAsync("state-drain").AsTask();
        Equal(false, drained.IsCompleted,
            "SetState Draining must create coordination for outstanding work.");

        lease.Dispose();
        drained.GetAwaiter().GetResult();
        pool.SetState("state-drain", M3uaAssociationOperationalState.Active);
        Equal("state-drain", pool.SelectTargets(transfer).Single().Name,
            "A state-driven graceful drain may reactivate only after all leases settle.");

        Console.WriteLine("PASS SetState Draining coordinates outstanding lease completion");
    }

    private static M3uaAssociationPool CreatePool(string name) =>
        new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [new M3uaAssociationDefinition(
                name,
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100])]);

    private static Mtp3TransferMessage CreateTransfer(byte sls) =>
        new(
            new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
            new Mtp3RoutingLabel(
                destinationPointCode: 1234,
                originatingPointCode: 4321,
                signallingLinkSelection: sls),
            new byte[] { 0x01 },
            routingContext: 100);

    private static M3uaAssociationDispatchLease Acquire(
        M3uaAssociationPool pool,
        string name,
        Mtp3TransferMessage transfer) =>
        pool.TryAcquireDispatchLease(name, transfer)
        ?? throw new InvalidOperationException("Expected an active association dispatch lease.");

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
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

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }
}
