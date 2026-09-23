using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

Run("Loadshare preserves deterministic SLS affinity", LoadsharePreservesDeterministicSlsAffinity);
Run("Active standby requires explicit promotion", ActiveStandbyRequiresExplicitPromotion);
Run("Draining and fenced associations receive no new traffic", DrainingAndFencedAssociationsReceiveNoNewTraffic);
Run("Graceful drain waits for in-flight dispatch leases", GracefulDrainWaitsForInFlightDispatchLeases);
Run("Drain reactivation waits for in-flight dispatches", DrainReactivationWaitsForInFlightDispatches);
Run("Dispatch failure does not strand a drain waiter", DispatchFailureDoesNotStrandDrainWaiter);
Run("Drain without in-flight dispatch completes immediately", DrainWithoutInFlightDispatchCompletesImmediately);
Run("Broadcast selects all eligible active associations", BroadcastSelectsAllEligibleActiveAssociations);
Run("Routing context membership fails closed", RoutingContextMembershipFailsClosed);
Run("Duplicate association names fail closed", DuplicateAssociationNamesFailClosed);
Run("Duplicate routing context membership fails closed", DuplicateRoutingContextMembershipFailsClosed);
Run("Node policy remains separate from negotiated traffic mode", NodePolicyRemainsSeparateFromNegotiatedTrafficMode);
Run("Active standby state updates cannot activate any path", ActiveStandbyStateUpdatesCannotActivateAnyPath);
Run("Active standby rejects multiple initially active paths", ActiveStandbyRejectsMultipleInitiallyActivePaths);
Run("Active standby admits an all-standby topology", ActiveStandbyAdmitsAllStandbyTopology);
Run("Standby promotion demotes the previous active path", StandbyPromotionDemotesPreviousActivePath);
Run("Standby promotion rejects non-standby states atomically", StandbyPromotionRejectsNonStandbyStatesAtomically);
Run("Loadshare rejects seventeen targets in one routing context", LoadshareRejectsOversizedMembership);
Run("Loadshare capacity includes inactive membership", LoadshareCapacityIncludesInactiveMembership);
Run("Loadshare capacity includes wildcard and overlapping contexts", LoadshareCapacityIncludesWildcardAndOverlappingContexts);
Run("Loadshare reaches all sixteen admitted targets", LoadshareReachesAllSixteenTargets);
Run("Loadshare allows larger disjoint context memberships", LoadshareAllowsDisjointContextMemberships);
Run("Routing context membership cannot be mutated after admission", RoutingContextMembershipCannotBeMutatedAfterAdmission);
Run("Other node modes retain explicit state activation", OtherNodeModesRetainExplicitStateActivation);

static void LoadsharePreservesDeterministicSlsAffinity()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));

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
        new M3uaAssociationDefinition("primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("standby", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));

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
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 0, M3uaAssociationOperationalState.Active, [100]));

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

static void GracefulDrainWaitsForInFlightDispatchLeases()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));
    Mtp3TransferMessage transfer = CreateTransfer(0, 100);
    M3uaAssociationDispatchLease lease = pool.TryAcquireDispatchLease("a", transfer)
        ?? throw new InvalidOperationException("Expected an active association dispatch lease.");

    pool.BeginDrain("a");
    M3uaAssociationRouteSnapshot draining = Single(pool.GetSnapshot());
    Equal(M3uaAssociationOperationalState.Draining, draining.State, "BeginDrain must make the route immediately ineligible for new work.");
    Equal(1, draining.InFlightDispatches, "The in-flight lease must remain visible while drain is pending.");
    Equal(0, pool.SelectTargets(transfer).Count, "A draining route must reject new selections before existing work completes.");

    Task drained = pool.WaitForDrainedAsync("a").AsTask();
    Equal(false, drained.IsCompleted, "Drain completion must wait for the in-flight dispatch lease.");

    lease.Dispose();
    drained.GetAwaiter().GetResult();
    M3uaAssociationRouteSnapshot completed = Single(pool.GetSnapshot());
    Equal(0, completed.InFlightDispatches, "Drain completion must observe zero in-flight dispatch leases.");
    Equal(M3uaAssociationOperationalState.Draining, completed.State, "Completing the drain must not silently reactivate the route.");
}

static void DrainReactivationWaitsForInFlightDispatches()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));
    Mtp3TransferMessage transfer = CreateTransfer(1, 100);
    M3uaAssociationDispatchLease lease = pool.TryAcquireDispatchLease("a", transfer)
        ?? throw new InvalidOperationException("Expected an active association dispatch lease.");

    pool.BeginDrain("a");
    Throws<InvalidOperationException>(() =>
        pool.SetState("a", M3uaAssociationOperationalState.Active));
    Equal(M3uaAssociationOperationalState.Draining, Single(pool.GetSnapshot()).State, "Rejected reactivation must preserve Draining state.");

    lease.Dispose();
    pool.WaitForDrainedAsync("a").AsTask().GetAwaiter().GetResult();
    pool.SetState("a", M3uaAssociationOperationalState.Active);
    Equal("a", Single(pool.SelectTargets(transfer)).Name, "A fully drained route may be explicitly reactivated in non-active-standby modes.");
}

static void DispatchFailureDoesNotStrandDrainWaiter()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));
    Mtp3TransferMessage transfer = CreateTransfer(2, 100);
    M3uaAssociationDispatchLease lease = pool.TryAcquireDispatchLease("a", transfer)
        ?? throw new InvalidOperationException("Expected an active association dispatch lease.");

    pool.BeginDrain("a");
    pool.ApplyDispatchFailureState("a", ambiguous: true);
    Equal(M3uaAssociationOperationalState.Fenced, Single(pool.GetSnapshot()).State, "An ambiguous in-flight failure must retain fenced precedence over Draining.");

    Task drained = pool.WaitForDrainedAsync("a").AsTask();
    Equal(false, drained.IsCompleted, "Failure state must not complete the drain while a lease remains held.");
    lease.Dispose();
    drained.GetAwaiter().GetResult();

    M3uaAssociationRouteSnapshot snapshot = Single(pool.GetSnapshot());
    Equal(M3uaAssociationOperationalState.Fenced, snapshot.State, "Drain completion must not erase the stronger failure state.");
    Equal(0, snapshot.InFlightDispatches, "The failed draining route must settle all in-flight leases.");
    Equal(0, pool.SelectTargets(transfer).Count, "A fenced route must remain ineligible after its drain settles.");
}

static void DrainWithoutInFlightDispatchCompletesImmediately()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));

    pool.BeginDrain("a");
    Task drained = pool.WaitForDrainedAsync("a").AsTask();
    Equal(true, drained.IsCompletedSuccessfully, "A route with no in-flight dispatches should already be drained.");
    Equal(0, Single(pool.GetSnapshot()).InFlightDispatches, "Immediate drain must report no in-flight work.");
}

static void BroadcastSelectsAllEligibleActiveAssociations()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Broadcast,
        M3uaTrafficModeType.Broadcast,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 1, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("c", "sg-c", 2, M3uaAssociationOperationalState.Standby, [100]));

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
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]));

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
        new M3uaAssociationDefinition("wildcard", "sg-w", 0, M3uaAssociationOperationalState.Active));

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
        new M3uaAssociationDefinition("dup", "sg-a", 0, M3uaAssociationOperationalState.Active),
        new M3uaAssociationDefinition("DUP", "sg-b", 1, M3uaAssociationOperationalState.Active)));
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
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active));

    Equal(
        M3uaNodeRoutingMode.ActiveStandby,
        pool.NodeRoutingMode,
        "Local node policy must remain explicit.");
    Equal(
        M3uaTrafficModeType.Loadshare,
        pool.ProtocolTrafficMode,
        "Negotiated protocol traffic mode must be represented independently.");
}

static void ActiveStandbyStateUpdatesCannotActivateAnyPath()
{
    // Include the already-active case: the state setter is never an activation API.
    foreach (M3uaAssociationOperationalState initial in Enum.GetValues<M3uaAssociationOperationalState>())
    {
        M3uaAssociationPool pool = CreatePool(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            new M3uaAssociationDefinition("candidate", "sg-a", 0, initial, [100]));

        Throws<InvalidOperationException>(() => pool.SetState("candidate", M3uaAssociationOperationalState.Active));
        M3uaAssociationRouteSnapshot snapshot = Single(pool.GetSnapshot());
        Equal(initial, snapshot.State, "Rejected activation must leave the prior state unchanged.");
        Equal(0L, snapshot.SelectedTransfers, "Rejected activation must not mutate counters.");
        Equal(
            initial == M3uaAssociationOperationalState.Active ? 1 : 0,
            pool.SelectTargets(CreateTransfer(0, 100)).Count,
            "A state update must not make a previously ineligible path selectable.");
    }
}

static void ActiveStandbyRejectsMultipleInitiallyActivePaths()
{
    foreach (uint secondContext in new uint[] { 100, 200 })
    {
        Throws<ArgumentException>(() => CreatePool(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
            new M3uaAssociationDefinition("b", "sg-b", 1, M3uaAssociationOperationalState.Active, [secondContext])));
    }
}

static void ActiveStandbyAdmitsAllStandbyTopology()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Standby, [100]),
        new M3uaAssociationDefinition("b", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));

    Equal(0, pool.SelectTargets(CreateTransfer(0, 100)).Count, "All-standby startup must select no traffic.");
    pool.PromoteStandby("b");
    Equal("b", Single(pool.SelectTargets(CreateTransfer(0, 100))).Name, "Explicit startup promotion must select exactly one path.");
}

static void StandbyPromotionDemotesPreviousActivePath()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.ActiveStandby,
        M3uaTrafficModeType.Override,
        new M3uaAssociationDefinition("primary", "sg-a", 0, M3uaAssociationOperationalState.Active, [100]),
        new M3uaAssociationDefinition("backup", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));

    pool.PromoteStandby("backup");
    IReadOnlyList<M3uaAssociationRouteSnapshot> snapshots = pool.GetSnapshot();
    Equal(1, snapshots.Count(snapshot => snapshot.State == M3uaAssociationOperationalState.Active), "Promotion must retain one active path.");
    Equal(M3uaAssociationOperationalState.Standby, snapshots.Single(snapshot => snapshot.Name == "primary").State, "The previous active must be demoted.");
    Equal("backup", Single(pool.SelectTargets(CreateTransfer(0, 100))).Name, "Lower priority must not override explicit promotion.");

    pool.PromoteStandby("primary");
    Equal("primary", Single(pool.SelectTargets(CreateTransfer(0, 100))).Name, "Explicit failback must be supported.");
    Equal(1, pool.GetSnapshot().Count(snapshot => snapshot.State == M3uaAssociationOperationalState.Active), "Failback must not create two active paths.");
}

static void StandbyPromotionRejectsNonStandbyStatesAtomically()
{
    foreach (M3uaAssociationOperationalState state in Enum.GetValues<M3uaAssociationOperationalState>())
    {
        if (state == M3uaAssociationOperationalState.Standby)
        {
            continue;
        }

        M3uaAssociationPool pool = CreatePool(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            new M3uaAssociationDefinition("candidate", "sg-a", 0, state, [100]),
            new M3uaAssociationDefinition("other", "sg-b", 1, M3uaAssociationOperationalState.Standby, [100]));

        Throws<InvalidOperationException>(() => pool.PromoteStandby("candidate"));
        Equal(state, pool.GetSnapshot().Single(snapshot => snapshot.Name == "candidate").State, "Invalid promotion must not change the target state.");
        Equal(M3uaAssociationOperationalState.Standby, pool.GetSnapshot().Single(snapshot => snapshot.Name == "other").State, "Invalid promotion must not change another path.");
    }
}

static void LoadshareRejectsOversizedMembership()
{
    Throws<ArgumentException>(() => CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("lane", 17, 100)));
}

static void LoadshareCapacityIncludesInactiveMembership()
{
    foreach (M3uaAssociationOperationalState state in Enum.GetValues<M3uaAssociationOperationalState>())
    {
        M3uaAssociationDefinition inactive = new("extra", "sg-extra", 0, state, [100]);
        Throws<ArgumentException>(() => CreatePool(
            M3uaNodeRoutingMode.Loadshare,
            M3uaTrafficModeType.Loadshare,
            Members("lane", 16, 100).Append(inactive).ToArray()));
    }
}

static void LoadshareCapacityIncludesWildcardAndOverlappingContexts()
{
    Throws<ArgumentException>(() => CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("wildcard", 17)));

    Throws<ArgumentException>(() => CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("lane", 16, 100).Concat(Members("wildcard", 1)).ToArray()));

    Throws<ArgumentException>(() => CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("multi", 1, 100, 200).Concat(Members("lane", 16, 200)).ToArray()));

    // A wildcard participates in each explicit context and alone in null/unknown contexts.
    M3uaAssociationPool valid = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("a", 15, 100).Concat(Members("b", 15, 200)).Concat(Members("wildcard", 1)).ToArray());
    foreach (uint context in new uint[] { 100, 200 })
    {
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (byte sls = 0; sls < 16; sls++)
        {
            selected.Add(Single(valid.SelectTargets(CreateTransfer(sls, context))).Name);
        }
        Equal(16, selected.Count, "The valid boundary must include the wildcard and all context-specific paths.");
        Equal(true, selected.Contains("wildcard-00"), "The wildcard must remain reachable in each explicit context.");
    }
    Equal("wildcard-00", Single(valid.SelectTargets(CreateTransfer(0, null))).Name, "Null context must select only the wildcard.");
    Equal("wildcard-00", Single(valid.SelectTargets(CreateTransfer(0, 999))).Name, "Unknown context must select only an explicitly unscoped path.");
}

static void LoadshareReachesAllSixteenTargets()
{
    foreach (uint? context in new uint?[] { 100, null })
    {
        M3uaAssociationPool pool = CreatePool(
            M3uaNodeRoutingMode.Loadshare,
            M3uaTrafficModeType.Loadshare,
            context.HasValue ? Members("lane", 16, context.Value) : Members("lane", 16));
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (byte sls = 0; sls < 16; sls++)
        {
            selected.Add(Single(pool.SelectTargets(CreateTransfer(sls, context))).Name);
        }
        Equal(16, selected.Count, "Every configured target at the four-bit SLS limit must be reachable.");
        foreach (M3uaAssociationRouteSnapshot snapshot in pool.GetSnapshot())
        {
            Equal(1L, snapshot.SelectedTransfers, "Every lane must receive exactly one selection over the full SLS space.");
        }
    }
}

static void LoadshareAllowsDisjointContextMemberships()
{
    M3uaAssociationPool pool = CreatePool(
        M3uaNodeRoutingMode.Loadshare,
        M3uaTrafficModeType.Loadshare,
        Members("a", 16, 100).Concat(Members("b", 16, 200)).ToArray());

    Equal(32, pool.GetSnapshot().Count, "The bound is per overlapping membership, not global pool size.");
    foreach (uint context in new uint[] { 100, 200 })
    {
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (byte sls = 0; sls < 16; sls++)
        {
            M3uaAssociationDefinition target = Single(pool.SelectTargets(CreateTransfer(sls, context)));
            Equal(true, target.MatchesRoutingContext(context), "No selection may cross context ownership.");
            selected.Add(target.Name);
        }
        Equal(16, selected.Count, "All members of each disjoint context must be reachable.");
    }
    Equal(32L, pool.GetSnapshot().Sum(snapshot => snapshot.SelectedTransfers), "Aggregate counters must reconcile across contexts.");
}

static void RoutingContextMembershipCannotBeMutatedAfterAdmission()
{
    uint[] input = [100];
    M3uaAssociationDefinition definition = new("guarded", "sg-a", 0, M3uaAssociationOperationalState.Active, input);
    M3uaAssociationPool pool = CreatePool(M3uaNodeRoutingMode.Loadshare, M3uaTrafficModeType.Loadshare, definition);

    input[0] = 200;
    IList<uint> exposed = (IList<uint>)definition.RoutingContexts;
    Throws<NotSupportedException>(() => exposed[0] = 200);
    Equal(true, definition.MatchesRoutingContext(100), "Membership must preserve its admitted context.");
    Equal(false, definition.MatchesRoutingContext(200), "Neither caller input nor the exposed collection may change ownership.");
    Equal(1, pool.SelectTargets(CreateTransfer(0, 100)).Count, "The admitted route must remain usable.");
    Equal(0, pool.SelectTargets(CreateTransfer(0, 200)).Count, "Mutating a view must not admit an unauthorized route.");
}

static void OtherNodeModesRetainExplicitStateActivation()
{
    foreach (M3uaNodeRoutingMode mode in new[] { M3uaNodeRoutingMode.Override, M3uaNodeRoutingMode.Loadshare, M3uaNodeRoutingMode.Broadcast })
    {
        M3uaAssociationPool pool = CreatePool(
            mode,
            M3uaTrafficModeType.Loadshare,
            new M3uaAssociationDefinition("a", "sg-a", 0, M3uaAssociationOperationalState.Faulted, [100]));
        pool.SetState("a", M3uaAssociationOperationalState.Active);
        Equal("a", Single(pool.SelectTargets(CreateTransfer(0, 100))).Name, "The explicit-promotion guard must be scoped to ActiveStandby.");
    }

    M3uaAssociationPool broadcast = CreatePool(M3uaNodeRoutingMode.Broadcast, M3uaTrafficModeType.Broadcast, Members("lane", 17, 100));
    Equal(17, broadcast.SelectTargets(CreateTransfer(0, 100)).Count, "The SLS-affinity limit must not restrict Broadcast fanout.");
}

static M3uaAssociationDefinition[] Members(string prefix, int count, params uint[] contexts) =>
    Enumerable.Range(0, count)
        .Select(index => new M3uaAssociationDefinition(
            $"{prefix}-{index:D2}",
            "sg-test",
            0,
            M3uaAssociationOperationalState.Active,
            contexts))
        .ToArray();

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
