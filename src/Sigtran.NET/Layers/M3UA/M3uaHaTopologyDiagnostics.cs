namespace Sigtran.NET.Layers.M3UA;

internal readonly struct M3uaHaAssociationTopologySnapshot
{
    internal M3uaHaAssociationTopologySnapshot(
        M3uaAssociationRouteSnapshot route,
        M3uaHaRuntimeLaneSnapshot runtime,
        M3uaRuntimeGenerationBindingSnapshot binding,
        M3uaAssociationDispatchDiagnostics dispatch)
    {
        Name = route.Name;
        SignalingGateway = route.SignalingGateway;
        Priority = route.Priority;
        PolicyState = route.State;
        RouteRuntimeState = route.RuntimeState;
        RouteInFlightDispatches = route.InFlightDispatches;
        SelectedTransfers = route.SelectedTransfers;
        RoutingContexts = route.RoutingContexts;
        SupervisorRuntimeState = runtime.State;
        RuntimeReceivedTransfers = runtime.ReceivedTransfers;
        RuntimeFaultEvents = runtime.FaultEvents;
        RuntimeMetrics = runtime.Metrics;
        TransportGeneration = binding.Fence.Generation;
        TransportAcceptingDispatch = binding.Fence.AcceptingDispatch;
        TransportInFlightDispatches = binding.Fence.InFlightDispatches;
        FenceReason = binding.Fence.Reason;
        ActivationDesired = binding.ActivationDesired;
        ActivationEpochAvailable = binding.ActivationEpochAvailable;
        BindingError = binding.BindingError;
        SentOutcomes = dispatch.Sent;
        NotDispatchedOutcomes = dispatch.NotDispatched;
        AmbiguousOutcomes = dispatch.Ambiguous;
    }

    internal string Name { get; }
    internal string SignalingGateway { get; }
    internal int Priority { get; }
    internal M3uaAssociationOperationalState PolicyState { get; }
    internal M3uaRuntimeState? RouteRuntimeState { get; }
    internal int RouteInFlightDispatches { get; }
    internal long SelectedTransfers { get; }
    internal IReadOnlyList<uint> RoutingContexts { get; }
    internal M3uaRuntimeState SupervisorRuntimeState { get; }
    internal long RuntimeReceivedTransfers { get; }
    internal long RuntimeFaultEvents { get; }
    internal M3uaRuntimeMetrics RuntimeMetrics { get; }
    internal long TransportGeneration { get; }
    internal bool TransportAcceptingDispatch { get; }
    internal int TransportInFlightDispatches { get; }
    internal M3uaAssociationFenceReason FenceReason { get; }
    internal bool ActivationDesired { get; }
    internal bool ActivationEpochAvailable { get; }
    internal string? BindingError { get; }
    internal long SentOutcomes { get; }
    internal long NotDispatchedOutcomes { get; }
    internal long AmbiguousOutcomes { get; }
}

internal readonly struct M3uaHaTopologySnapshot
{
    internal M3uaHaTopologySnapshot(
        M3uaNodeRoutingMode nodeRoutingMode,
        M3uaTrafficModeType protocolTrafficMode,
        M3uaHaRuntimeSupervisorSnapshot runtime,
        M3uaHaDispatchCoordinatorSnapshot dispatch,
        IReadOnlyList<M3uaHaAssociationTopologySnapshot> associations)
    {
        NodeRoutingMode = nodeRoutingMode;
        ProtocolTrafficMode = protocolTrafficMode;
        InboundCapacity = runtime.InboundCapacity;
        PendingInboundTransfers = runtime.PendingInboundTransfers;
        PendingDispatches = dispatch.PendingDispatches;
        AdmittedDispatches = dispatch.AdmittedDispatches;
        CompletedDispatches = dispatch.CompletedDispatches;
        CanceledDispatches = dispatch.CanceledDispatches;
        FaultedDispatches = dispatch.FaultedDispatches;
        SentOutcomes = dispatch.SentOutcomes;
        NotDispatchedOutcomes = dispatch.NotDispatchedOutcomes;
        AmbiguousOutcomes = dispatch.AmbiguousOutcomes;
        NoRouteOutcomes = dispatch.NoRouteOutcomes;
        Associations = associations;
    }

    internal M3uaNodeRoutingMode NodeRoutingMode { get; }
    internal M3uaTrafficModeType ProtocolTrafficMode { get; }
    internal int InboundCapacity { get; }
    internal int PendingInboundTransfers { get; }
    internal int PendingDispatches { get; }
    internal long AdmittedDispatches { get; }
    internal long CompletedDispatches { get; }
    internal long CanceledDispatches { get; }
    internal long FaultedDispatches { get; }
    internal long SentOutcomes { get; }
    internal long NotDispatchedOutcomes { get; }
    internal long AmbiguousOutcomes { get; }
    internal long NoRouteOutcomes { get; }
    internal IReadOnlyList<M3uaHaAssociationTopologySnapshot> Associations { get; }
}

/// <summary>
/// Projects routing policy, runtime lifecycle, transport-generation ownership,
/// and dispatch diagnostics into one deterministically ordered HA topology view.
/// </summary>
/// <remarks>
/// The snapshot is operational evidence for the in-process composition only. It
/// is not an operator/vendor, multi-host, Kubernetes, or peer-acceptance claim.
/// Cross-component values are sampled from their existing thread-safe snapshots;
/// no attempt is made to reinterpret queue/sender completion as network acceptance.
/// </remarks>
internal sealed class M3uaHaTopologyDiagnostics
{
    private readonly M3uaAssociationPool _pool;
    private readonly M3uaHaRuntimeSupervisor _runtime;
    private readonly M3uaHaDispatchCoordinator _dispatch;
    private readonly IReadOnlyDictionary<string, M3uaRuntimeGenerationFenceBinding> _bindings;
    private readonly string[] _associationNames;

    internal M3uaHaTopologyDiagnostics(
        M3uaAssociationPool pool,
        M3uaHaRuntimeSupervisor runtime,
        M3uaHaDispatchCoordinator dispatch,
        IEnumerable<KeyValuePair<string, M3uaRuntimeGenerationFenceBinding>> bindings)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        ArgumentNullException.ThrowIfNull(bindings);

        if (_runtime.PublishesRouteHealth)
        {
            throw new ArgumentException(
                "HA topology diagnostics requires a runtime supervisor without an independent route-health binding; generation bindings must be the sole route-health publishers.",
                nameof(runtime));
        }

        if (!ReferenceEquals(_dispatch.RoutePool, _pool))
        {
            throw new ArgumentException(
                "HA topology diagnostics requires the dispatch coordinator to own the exact supplied route pool.",
                nameof(dispatch));
        }

        Dictionary<string, M3uaRuntimeGenerationFenceBinding> byName =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, M3uaRuntimeGenerationFenceBinding> entry in bindings)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new ArgumentException(
                    "Topology binding association name is required.",
                    nameof(bindings));
            }

            ArgumentNullException.ThrowIfNull(entry.Value);
            string name = entry.Key.Trim();
            if (!string.Equals(name, entry.Value.AssociationName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Topology binding key '{name}' does not match binding association '{entry.Value.AssociationName}'.",
                    nameof(bindings));
            }

            if (!ReferenceEquals(entry.Value.RoutePool, _pool))
            {
                throw new ArgumentException(
                    $"Topology binding '{name}' does not own the supplied route pool.",
                    nameof(bindings));
            }

            if (!_runtime.OwnsLane(entry.Value.RuntimeLane))
            {
                throw new ArgumentException(
                    $"Topology binding '{name}' does not own the runtime lane supervised by this topology.",
                    nameof(bindings));
            }

            if (!_dispatch.OwnsSender(name, entry.Value.Sender))
            {
                throw new ArgumentException(
                    $"Topology binding '{name}' does not own the exact sender used by the dispatch coordinator.",
                    nameof(bindings));
            }

            if (!byName.TryAdd(name, entry.Value))
            {
                throw new ArgumentException(
                    $"Duplicate topology binding association '{entry.Key}'.",
                    nameof(bindings));
            }
        }

        string[] routeNames = _pool.GetSnapshot()
            .Select(static route => route.Name)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] runtimeNames = _runtime.GetSnapshot().Lanes
            .Select(static lane => lane.AssociationName)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] bindingNames = byName.Keys
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!routeNames.SequenceEqual(runtimeNames, StringComparer.OrdinalIgnoreCase)
            || !routeNames.SequenceEqual(bindingNames, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "HA topology diagnostics requires exact association membership across route pool, runtime supervisor, and generation bindings.",
                nameof(bindings));
        }

        _bindings = byName;
        _associationNames = routeNames;
    }

    internal M3uaHaTopologySnapshot GetSnapshot()
    {
        M3uaAssociationRouteSnapshot[] routes = _pool.GetSnapshot().ToArray();
        M3uaHaRuntimeSupervisorSnapshot runtime = _runtime.GetSnapshot();
        M3uaHaDispatchCoordinatorSnapshot dispatch = _dispatch.GetSnapshot();

        Dictionary<string, M3uaAssociationRouteSnapshot> routeByName = routes
            .ToDictionary(static route => route.Name, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, M3uaHaRuntimeLaneSnapshot> runtimeByName = runtime.Lanes
            .ToDictionary(static lane => lane.AssociationName, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, M3uaAssociationDispatchDiagnostics> dispatchByName = dispatch.Associations
            .ToDictionary(static item => item.AssociationName, StringComparer.OrdinalIgnoreCase);

        foreach (string dispatchName in dispatchByName.Keys)
        {
            if (!routeByName.ContainsKey(dispatchName))
            {
                throw new InvalidOperationException(
                    $"Dispatch diagnostics reported unknown M3UA association '{dispatchName}'.");
            }
        }

        List<M3uaHaAssociationTopologySnapshot> associations =
            new(_associationNames.Length);
        foreach (string name in _associationNames)
        {
            if (!routeByName.TryGetValue(name, out M3uaAssociationRouteSnapshot route)
                || !runtimeByName.TryGetValue(name, out M3uaHaRuntimeLaneSnapshot runtimeLane)
                || !_bindings.TryGetValue(name, out M3uaRuntimeGenerationFenceBinding? binding))
            {
                throw new InvalidOperationException(
                    $"HA topology association membership changed unexpectedly for '{name}'.");
            }

            M3uaAssociationDispatchDiagnostics dispatchDiagnostics =
                dispatchByName.TryGetValue(name, out M3uaAssociationDispatchDiagnostics value)
                    ? value
                    : new M3uaAssociationDispatchDiagnostics(name, 0, 0, 0);

            associations.Add(new M3uaHaAssociationTopologySnapshot(
                route,
                runtimeLane,
                binding.GetSnapshot(),
                dispatchDiagnostics));
        }

        return new M3uaHaTopologySnapshot(
            _pool.NodeRoutingMode,
            _pool.ProtocolTrafficMode,
            runtime,
            dispatch,
            associations.AsReadOnly());
    }
}