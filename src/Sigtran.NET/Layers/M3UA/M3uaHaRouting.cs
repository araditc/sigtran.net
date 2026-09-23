using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaNodeRoutingMode
{
    ActiveStandby,
    Override,
    Loadshare,
    Broadcast
}

internal enum M3uaAssociationOperationalState
{
    Active,
    Standby,
    Draining,
    Reconnecting,
    Fenced,
    Faulted
}

internal sealed class M3uaAssociationDefinition
{
    private readonly uint[] _routingContexts;

    internal M3uaAssociationDefinition(
        string name,
        string signalingGateway,
        int priority,
        M3uaAssociationOperationalState initialState,
        IEnumerable<uint>? routingContexts = null)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Association name is required.", nameof(name))
            : name.Trim();
        SignalingGateway = string.IsNullOrWhiteSpace(signalingGateway)
            ? throw new ArgumentException("Signaling gateway name is required.", nameof(signalingGateway))
            : signalingGateway.Trim();

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "Association priority must not be negative.");
        }

        if (!Enum.IsDefined(initialState))
        {
            throw new ArgumentOutOfRangeException(nameof(initialState));
        }

        Priority = priority;
        InitialState = initialState;

        uint[] values = routingContexts?.ToArray() ?? Array.Empty<uint>();
        if (values.Length != values.Distinct().Count())
        {
            throw new ArgumentException(
                "Routing context membership must not contain duplicates.",
                nameof(routingContexts));
        }

        Array.Sort(values);
        _routingContexts = values;
    }

    internal string Name { get; }

    internal string SignalingGateway { get; }

    internal int Priority { get; }

    internal M3uaAssociationOperationalState InitialState { get; }

    internal IReadOnlyList<uint> RoutingContexts => _routingContexts;

    internal bool MatchesRoutingContext(uint? routingContext)
    {
        if (_routingContexts.Length == 0)
        {
            return true;
        }

        return routingContext.HasValue
            && Array.BinarySearch(_routingContexts, routingContext.Value) >= 0;
    }
}

internal readonly struct M3uaAssociationRouteSnapshot
{
    internal M3uaAssociationRouteSnapshot(
        string name,
        string signalingGateway,
        int priority,
        M3uaAssociationOperationalState state,
        long selectedTransfers,
        IReadOnlyList<uint> routingContexts)
    {
        Name = name;
        SignalingGateway = signalingGateway;
        Priority = priority;
        State = state;
        SelectedTransfers = selectedTransfers;
        RoutingContexts = routingContexts;
    }

    internal string Name { get; }

    internal string SignalingGateway { get; }

    internal int Priority { get; }

    internal M3uaAssociationOperationalState State { get; }

    internal long SelectedTransfers { get; }

    internal IReadOnlyList<uint> RoutingContexts { get; }
}

internal sealed class M3uaAssociationPool
{
    private sealed class Slot
    {
        internal Slot(M3uaAssociationDefinition definition)
        {
            Definition = definition;
            State = definition.InitialState;
        }

        internal M3uaAssociationDefinition Definition { get; }

        internal M3uaAssociationOperationalState State { get; set; }

        internal long SelectedTransfers { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, Slot> _slots;

    internal M3uaAssociationPool(
        M3uaNodeRoutingMode nodeRoutingMode,
        M3uaTrafficModeType protocolTrafficMode,
        IEnumerable<M3uaAssociationDefinition> associations)
    {
        if (!Enum.IsDefined(nodeRoutingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(nodeRoutingMode));
        }

        if (!Enum.IsDefined(protocolTrafficMode))
        {
            throw new ArgumentOutOfRangeException(nameof(protocolTrafficMode));
        }

        ArgumentNullException.ThrowIfNull(associations);

        M3uaAssociationDefinition[] definitions = associations.ToArray();
        if (definitions.Length == 0)
        {
            throw new ArgumentException("At least one M3UA association is required.", nameof(associations));
        }

        _slots = new Dictionary<string, Slot>(StringComparer.OrdinalIgnoreCase);
        foreach (M3uaAssociationDefinition definition in definitions)
        {
            if (!_slots.TryAdd(definition.Name, new Slot(definition)))
            {
                throw new ArgumentException(
                    $"Duplicate M3UA association name '{definition.Name}'.",
                    nameof(associations));
            }
        }

        NodeRoutingMode = nodeRoutingMode;
        ProtocolTrafficMode = protocolTrafficMode;
    }

    internal M3uaNodeRoutingMode NodeRoutingMode { get; }

    internal M3uaTrafficModeType ProtocolTrafficMode { get; }

    internal void SetState(string associationName, M3uaAssociationOperationalState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        lock (_sync)
        {
            GetSlot(associationName).State = state;
        }
    }

    internal void PromoteStandby(string associationName)
    {
        if (NodeRoutingMode != M3uaNodeRoutingMode.ActiveStandby)
        {
            throw new InvalidOperationException("Explicit standby promotion is only valid for active/standby node routing.");
        }

        lock (_sync)
        {
            Slot promoted = GetSlot(associationName);
            if (promoted.State != M3uaAssociationOperationalState.Standby)
            {
                throw new InvalidOperationException(
                    $"Association '{associationName}' is not in Standby state.");
            }

            foreach (Slot slot in _slots.Values)
            {
                if (slot.State == M3uaAssociationOperationalState.Active)
                {
                    slot.State = M3uaAssociationOperationalState.Standby;
                }
            }

            promoted.State = M3uaAssociationOperationalState.Active;
        }
    }

    internal IReadOnlyList<M3uaAssociationDefinition> SelectTargets(Mtp3TransferMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_sync)
        {
            Slot[] active = _slots.Values
                .Where(slot =>
                    slot.State == M3uaAssociationOperationalState.Active
                    && slot.Definition.MatchesRoutingContext(message.RoutingContext))
                .OrderBy(slot => slot.Definition.Priority)
                .ThenBy(slot => slot.Definition.Name, StringComparer.Ordinal)
                .ToArray();

            if (active.Length == 0)
            {
                return Array.Empty<M3uaAssociationDefinition>();
            }

            Slot[] selected = NodeRoutingMode switch
            {
                M3uaNodeRoutingMode.ActiveStandby => [active[0]],
                M3uaNodeRoutingMode.Override => [active[0]],
                M3uaNodeRoutingMode.Loadshare =>
                    [active[message.RoutingLabel.SignallingLinkSelection % active.Length]],
                M3uaNodeRoutingMode.Broadcast => active,
                _ => throw new InvalidOperationException(
                    $"Unsupported M3UA node routing mode '{NodeRoutingMode}'.")
            };

            foreach (Slot slot in selected)
            {
                checked
                {
                    slot.SelectedTransfers++;
                }
            }

            return selected.Select(slot => slot.Definition).ToArray();
        }
    }

    internal IReadOnlyList<M3uaAssociationRouteSnapshot> GetSnapshot()
    {
        lock (_sync)
        {
            return _slots.Values
                .OrderBy(slot => slot.Definition.Priority)
                .ThenBy(slot => slot.Definition.Name, StringComparer.Ordinal)
                .Select(slot => new M3uaAssociationRouteSnapshot(
                    slot.Definition.Name,
                    slot.Definition.SignalingGateway,
                    slot.Definition.Priority,
                    slot.State,
                    slot.SelectedTransfers,
                    slot.Definition.RoutingContexts.ToArray()))
                .ToArray();
        }
    }

    private Slot GetSlot(string associationName)
    {
        if (string.IsNullOrWhiteSpace(associationName))
        {
            throw new ArgumentException("Association name is required.", nameof(associationName));
        }

        return _slots.TryGetValue(associationName, out Slot? slot)
            ? slot
            : throw new KeyNotFoundException($"Unknown M3UA association '{associationName}'.");
    }
}
