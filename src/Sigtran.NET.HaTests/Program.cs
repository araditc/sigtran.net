using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

Run("Loadshare preserves deterministic SLS affinity", LoadsharePreservesDeterministicSlsAffinity);
Run("Active standby requires explicit promotion", ActiveStandbyRequiresExplicitPromotion);
Run("Draining and fenced associations receive no new traffic", DrainingAndFencedAssociationsReceiveNoNewTraffic);
Run("Broadcast selects all eligible active associations", BroadcastSelectsAllEligibleActiveAssociations);
Run("Routing context membership fails closed", RoutingContextMembershipFailsClosed);
Run("Duplicate association names fail closed", DuplicateAssociationNamesFailClosed);
Run("Duplicate routing context membership fails closed", DuplicateRoutingContextMembershipFailsClosed);
Run("Node policy remains separate from negotiated traffic mode", NodePolicyRemainsSeparateFromNegotiatedTrafficMode);

static void LoadsharePreservesDeterministicSlsAffinity()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));

    string first = Single(pool.SelectTargets(CreateTransfer(sls: 3, routingContext: 100))).Name;
    string second = Single(pool.SelectTargets(CreateTransfer(sls: 3, routingContext: 100))).Name;
    string alternate = Single(pool.SelectTargets(CreateTransfer(sls: 4, routingContext: 100))).Name;

    Equal(first, second, "The same SLS must remain pinned to the same association while membership is stable.");
    NotEqual(first, alternate, "Adjacent SLS values should distribute across the two active associations.");

    long selected = pool.GetSnapshot().Sum(snapshot => snapshot.SelectedTransfers);
    Equal(3L, selected, "Per-association selection counters must reconcile with aggregate selections.");
}

static void ActiveStandbyRequiresExplicitPromotion()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        new("primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new("standby", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));

    Equal(
        "primary",
        Single(pool.SelectTargets(CreateTransfer(sls: 1, routingContext: 100))).Name,
        "The active association must carry traffic.");

    pool.SetState("primary", M3uaAssociationOperationalState.Faulted);
    Equal(
        0,
        pool.SelectTargets(CreateTransfer(sls: 1, routingContext: 100)).Count,
        "A standby path must not be selected implicitly after the active path faults.");

    pool.PromoteStandby("standby");
    Equal(
        "standby",
        Single(pool.SelectTargets(CreateTransfer(sls: 1, routingContext: 100))).Name,
        "An explicitly promoted standby must become eligible.");
}

static void DrainingAndFencedAssociationsReceiveNoNewTraffic()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Broadcast,
        M3uaTrafficModeType.Broadcast,
        new("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));

    pool.SetState("a", M3uaAssociationOperationalState.Draining);
    IReadOnlyList<M3uaAssociationDefinition> afterDrain =
        pool.SelectTargets(CreateTransfer(sls: 0, routingContext: 100));
    Equal(1, afterDrain.Count, "A draining association must receive no new transfers.");
    Equal("b", afterDrain[0].Name, "The remaining active path must stay eligible.");

    pool.SetState("b", M3uaAssociationOperationalState.Fenced);
    Equal(
        0,
        pool.SelectTargets(CreateTransfer(sls: 0, routingContext: 100)).Count,
        "A fenced association must receive no new transfers.");
}

static void BroadcastSelectsAllEligibleActiveAssociations()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Broadcast,
        M3uaTrafficModeType.Broadcast,
        new("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new("b", "sg-b", 1, M3uaAssociationOperationalState.Active, [100]),
        new("c", "sg-c", 2, M3uaAssociationOperationalState.Standby, [100]));

    IReadOnlyList<M3uaAssociationDefinition> selected =
        pool.SelectTargets(CreateTransfer(sls: 7, routingContext: 100));

    Equal(2, selected.Count, "Broadcast must include each eligible active association exactly once.");
    Equal("a", selected[0].Name, "Broadcast ordering must be deterministic.");
    Equal("b", selected[1].Name, "Broadcast ordering must be deterministic.");
}

static void RoutingContextMembershipFailsClosed()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));

    Equal(
        0,
        pool.SelectTargets(CreateTransfer(sls: 0, routingContext: 200)).Count,
        "An association must not receive a routing context it does not own.");
    Equal(
        0,
        pool.SelectTargets(CreateTransfer(sls: 0, routingContext: null)).Count,
        "Configured routing-context membership must not match a missing routing context.");

    M3uaAssociationPool wildcard = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new("wildcard", "sg-w", 0, M3uaAssociationOperationalState.Active));

    Equal(
        "wildcard",
        Single(wildcard.SelectTargets(CreateTransfer(sls: 0, routingContext: null))).Name,
        "An explicitly unscoped association may carry transfers without a routing context.");
}

static void DuplicateAssociationNamesFailClosed()
{
    Throws<ArgumentException>(() => CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new("dup", "sg-a", 0, M3uaAssociationOperationalState.Active),
        new("DUP", "sg-b", 1, M3uaAssociationOperationalState.Active)));
}

static void DuplicateRoutingContextMembershipFailsClosed()
{
    Throws<ArgumentException>(() =>
        new M3uaAssociationDefinition(
            "a",
            "sg-a",
            0,
            M3uaAssociationOperationalState.Active,
            [100, 100]));
}

static void NodePolicyRemainsSeparateFromNegotiatedTrafficMode()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Loadshare,
        new("a", "sg-a", 0, M3uaAssociationOperationalState.Active));

    Equal(
        M3uaNodeRoutingMode.ActiveStandby,
        pool.NodeRoutingMode,
        "Local node policy must remain explicit.");
    Equal(
        M3uaTrafficModeType.Loadshare,
        pool.ProtocolTrafficMode,
        "Negotiated protocol traffic mode must be represented independently.");
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

static T Single<T>(IReadOnlyList<T> values)
{
    Equal(1, values.Count, "Expected exactly one selected association.");
    return values[0];
}

static void Run(string name, Action test)
{
    try
    {
        test();
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

static void NotEqual<T>(T left, T right, string message)
{
    if (EqualityComparer<T>.Default.Equals(left, right))
    {
        throw new InvalidOperationException($"{message} Both={left}.");
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
