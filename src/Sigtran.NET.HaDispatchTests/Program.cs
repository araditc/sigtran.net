using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

await RunAsync("Dispatcher rejects missing association senders", DispatcherRejectsMissingAssociationSenders);
await RunAsync("Dispatcher rejects unknown association senders", DispatcherRejectsUnknownAssociationSenders);
await RunAsync("No eligible route fails closed without transport send", NoEligibleRouteFailsClosedWithoutTransportSend);
await RunAsync("Active standby retries only proven pre-dispatch failure", ActiveStandbyRetriesOnlyProvenPreDispatchFailure);
await RunAsync("Ambiguous active-standby send is fenced without replay", AmbiguousActiveStandbySendIsFencedWithoutReplay);
await RunAsync("Unknown transport failure is ambiguous and not replayed", UnknownTransportFailureIsAmbiguousAndNotReplayed);
await RunAsync("Loadshare reroutes after proven pre-dispatch failure", LoadshareReroutesAfterProvenPreDispatchFailure);
await RunAsync("Broadcast attempts each selected association exactly once", BroadcastAttemptsEachSelectedAssociationExactlyOnce);
await RunAsync("Cancellation before selection sends nothing", CancellationBeforeSelectionSendsNothing);

static Task DispatcherRejectsMissingAssociationSenders()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));

    Throws<ArgumentException>(() =>
        _ = new M3uaAssociationDispatcher(pool, [new FakeSender("a")]));
    return Task.CompletedTask;
}

static Task DispatcherRejectsUnknownAssociationSenders()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));

    Throws<ArgumentException>(() =>
        _ = new M3uaAssociationDispatcher(
            pool,
            [new FakeSender("a"), new FakeSender("unexpected")]));
    return Task.CompletedTask;
}

static async Task NoEligibleRouteFailsClosedWithoutTransportSend()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));
    FakeSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = new(pool, [sender]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(0, 200));

    Equal(1, outcomes.Count, "A no-route result must be explicit.");
    Equal(M3uaDispatchDisposition.NoRoute, outcomes[0].Disposition, "No-route disposition mismatch.");
    Equal<string?>(null, outcomes[0].AssociationName, "A no-route result must not invent an association.");
    Equal(0, sender.Calls, "No transport send may occur when no route matches.");
}

static async Task ActiveStandbyRetriesOnlyProvenPreDispatchFailure()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("backup", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));
    FakeSender primary = new("primary", FakeBehavior.PreDispatchFailure);
    FakeSender backup = new("backup", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(3, 100));

    Equal(2, outcomes.Count, "A proven pre-dispatch failure should allow one deterministic failover attempt.");
    Equal("primary", outcomes[0].AssociationName, "Primary attempt order mismatch.");
    Equal(M3uaDispatchDisposition.NotDispatched, outcomes[0].Disposition, "Primary failure must remain pre-dispatch.");
    Equal("backup", outcomes[1].AssociationName, "Backup attempt order mismatch.");
    Equal(M3uaDispatchDisposition.Sent, outcomes[1].Disposition, "Backup should complete the transfer.");
    Equal(1, primary.Calls, "Primary must be attempted once.");
    Equal(1, backup.Calls, "Backup must be attempted once.");

    IReadOnlyList<M3uaAssociationRouteSnapshot> snapshot = pool.GetSnapshot();
    Equal(
        M3uaAssociationOperationalState.Faulted,
        snapshot.Single(route => route.Name == "primary").State,
        "The proven failed primary must be fenced from new traffic.");
    Equal(
        M3uaAssociationOperationalState.Active,
        snapshot.Single(route => route.Name == "backup").State,
        "The standby must be explicitly promoted before retry.");
}

static async Task AmbiguousActiveStandbySendIsFencedWithoutReplay()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("backup", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));
    FakeSender primary = new("primary", FakeBehavior.AmbiguousFailure);
    FakeSender backup = new("backup", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [primary, backup]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(4, 100));

    Equal(1, outcomes.Count, "Ambiguous transport acceptance must stop retry immediately.");
    Equal(M3uaDispatchDisposition.Ambiguous, outcomes[0].Disposition, "Ambiguous disposition mismatch.");
    Equal(1, primary.Calls, "Primary must be attempted exactly once.");
    Equal(0, backup.Calls, "Standby must not receive a blind replay after ambiguous acceptance.");

    IReadOnlyList<M3uaAssociationRouteSnapshot> snapshot = pool.GetSnapshot();
    Equal(
        M3uaAssociationOperationalState.Faulted,
        snapshot.Single(route => route.Name == "primary").State,
        "The uncertain association must be fenced from later traffic.");
    Equal(
        M3uaAssociationOperationalState.Standby,
        snapshot.Single(route => route.Name == "backup").State,
        "Ambiguous acceptance must not implicitly promote standby and replay.");
}

static async Task UnknownTransportFailureIsAmbiguousAndNotReplayed()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 1, M3uaAssociationOperationalState.Active, [100]));
    FakeSender first = new("a", FakeBehavior.UnexpectedFailure);
    FakeSender second = new("b", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [first, second]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(0, 100));

    Equal(1, outcomes.Count, "Unknown transport faults must fail closed as ambiguous.");
    Equal(M3uaDispatchDisposition.Ambiguous, outcomes[0].Disposition, "Unknown exception must be ambiguous.");
    Equal(1, first.Calls, "First association call count mismatch.");
    Equal(0, second.Calls, "Unknown exception must not trigger replay on another path.");
}

static async Task LoadshareReroutesAfterProvenPreDispatchFailure()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));
    FakeSender first = new("a", FakeBehavior.PreDispatchFailure);
    FakeSender second = new("b", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [first, second]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(0, 100));

    Equal(2, outcomes.Count, "Proven pre-dispatch loadshare failure should reselect once.");
    Equal("a", outcomes[0].AssociationName, "SLS zero should initially select deterministic lane a.");
    Equal(M3uaDispatchDisposition.NotDispatched, outcomes[0].Disposition, "First lane must be classified pre-dispatch.");
    Equal("b", outcomes[1].AssociationName, "The remaining healthy lane should be selected.");
    Equal(M3uaDispatchDisposition.Sent, outcomes[1].Disposition, "Healthy lane should send successfully.");
    Equal(1, first.Calls, "Failed loadshare lane must be attempted once.");
    Equal(1, second.Calls, "Healthy loadshare lane must be attempted once.");
}

static async Task BroadcastAttemptsEachSelectedAssociationExactlyOnce()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Broadcast,
        M3uaTrafficModeType.Broadcast,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 1, M3uaAssociationOperationalState.Active, [100]));
    FakeSender first = new("a", FakeBehavior.AmbiguousFailure);
    FakeSender second = new("b", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [first, second]);

    IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
        await dispatcher.DispatchAsync(CreateTransfer(7, 100));

    Equal(2, outcomes.Count, "Broadcast must report each selected fanout leg.");
    Equal(M3uaDispatchDisposition.Ambiguous, outcomes[0].Disposition, "First fanout leg mismatch.");
    Equal(M3uaDispatchDisposition.Sent, outcomes[1].Disposition, "Second fanout leg mismatch.");
    Equal(1, first.Calls, "Ambiguous broadcast leg must not be replayed.");
    Equal(1, second.Calls, "Healthy broadcast leg must be sent exactly once.");

    IReadOnlyList<M3uaAssociationRouteSnapshot> snapshot = pool.GetSnapshot();
    Equal(M3uaAssociationOperationalState.Faulted, snapshot.Single(route => route.Name == "a").State, "Failed fanout association must be fenced.");
    Equal(M3uaAssociationOperationalState.Active, snapshot.Single(route => route.Name == "b").State, "Healthy fanout association must remain active.");
}

static async Task CancellationBeforeSelectionSendsNothing()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));
    FakeSender sender = new("a", FakeBehavior.Success);
    M3uaAssociationDispatcher dispatcher = new(pool, [sender]);
    using CancellationTokenSource cts = new();
    cts.Cancel();

    await ThrowsAsync<OperationCanceledException>(
        () => dispatcher.DispatchAsync(CreateTransfer(0, 100), cts.Token).AsTask());
    Equal(0, sender.Calls, "A pre-cancelled dispatch must not touch transport.");
}

static M3uaAssociationPool CreatePool(
    M3uaNodeRoutingMode mode,
    M3uaTrafficModeType trafficMode,
    params M3uaAssociationDefinition[] associations) =>
    new(mode, trafficMode, associations);

static Mtp3TransferMessage CreateTransfer(byte sls, uint? routingContext) =>
    new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01 },
        routingContext: routingContext);

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
        throw new InvalidOperationException($"{message} Expected={expected}; Actual={actual}.");
    }
}

static void Throws<TException>(Action action)
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

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

internal enum FakeBehavior
{
    Success,
    PreDispatchFailure,
    AmbiguousFailure,
    UnexpectedFailure
}

internal sealed class FakeSender : IM3uaAssociationSender
{
    private readonly Queue<FakeBehavior> _behaviors;

    internal FakeSender(string associationName, params FakeBehavior[] behaviors)
    {
        AssociationName = associationName;
        _behaviors = new Queue<FakeBehavior>(behaviors);
    }

    public string AssociationName { get; }

    internal int Calls { get; private set; }

    public ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        Calls++;

        FakeBehavior behavior = _behaviors.Count == 0
            ? FakeBehavior.Success
            : _behaviors.Dequeue();
        return behavior switch
        {
            FakeBehavior.Success => ValueTask.CompletedTask,
            FakeBehavior.PreDispatchFailure => ValueTask.FromException(
                new M3uaAssociationSendException(
                    "Synthetic failure before transport accepted the transfer.",
                    dispatchMayHaveOccurred: false)),
            FakeBehavior.AmbiguousFailure => ValueTask.FromException(
                new M3uaAssociationSendException(
                    "Synthetic failure after transport acceptance became uncertain.",
                    dispatchMayHaveOccurred: true)),
            FakeBehavior.UnexpectedFailure => ValueTask.FromException(
                new InvalidOperationException("Synthetic unclassified transport fault.")),
            _ => throw new InvalidOperationException($"Unsupported behavior {behavior}.")
        };
    }
}
