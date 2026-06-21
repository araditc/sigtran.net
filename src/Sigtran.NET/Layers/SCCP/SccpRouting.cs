namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Selects SCCP traffic by SSN/point-code or global-title prefix.
/// </summary>
public sealed class SccpRouteSelector
{
    private SccpRouteSelector(
        SccpRoutingIndicator routingIndicator,
        SubsystemNumber? subsystemNumber,
        ushort? pointCode,
        string? globalTitlePrefix)
    {
        RoutingIndicator = routingIndicator;
        SubsystemNumber = subsystemNumber;
        PointCode = pointCode;
        GlobalTitlePrefix = globalTitlePrefix;
    }

    /// <summary>The routing indicator matched by this selector.</summary>
    public SccpRoutingIndicator RoutingIndicator { get; }

    /// <summary>The optional subsystem number.</summary>
    public SubsystemNumber? SubsystemNumber { get; }

    /// <summary>The optional point code.</summary>
    public ushort? PointCode { get; }

    /// <summary>The optional global title prefix.</summary>
    public string? GlobalTitlePrefix { get; }

    /// <summary>Creates a route-on-SSN selector.</summary>
    /// <param name="subsystemNumber">The subsystem number.</param>
    /// <param name="pointCode">The optional point code.</param>
    /// <returns>The route selector.</returns>
    public static SccpRouteSelector ForSubsystem(SubsystemNumber subsystemNumber, ushort? pointCode = null)
    {
        if (subsystemNumber == global::Sigtran.NET.Layers.SCCP.SubsystemNumber.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(subsystemNumber), "A route selector requires a concrete subsystem number.");
        }

        if (pointCode > 0x3FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(pointCode), "SCCP point code must fit in 14 bits.");
        }

        return new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber, pointCode, null);
    }

    /// <summary>Creates a route-on-global-title selector.</summary>
    /// <param name="globalTitlePrefix">The numeric global title prefix.</param>
    /// <returns>The route selector.</returns>
    public static SccpRouteSelector ForGlobalTitlePrefix(string globalTitlePrefix)
    {
        if (string.IsNullOrWhiteSpace(globalTitlePrefix))
        {
            throw new ArgumentException("Global title prefix is required.", nameof(globalTitlePrefix));
        }

        string normalized = globalTitlePrefix.Trim().TrimStart('+');
        if (!normalized.All(char.IsDigit))
        {
            throw new ArgumentException("Global title prefix must be numeric.", nameof(globalTitlePrefix));
        }

        return new(SccpRoutingIndicator.RouteOnGlobalTitle, null, null, normalized);
    }

    /// <summary>Returns true when this selector matches an SCCP party address.</summary>
    /// <param name="address">The SCCP party address.</param>
    /// <returns>True when the selector matches; otherwise false.</returns>
    public bool Matches(SccpPartyAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (RoutingIndicator == SccpRoutingIndicator.RouteOnSubsystemNumber)
        {
            return address.SubsystemNumber == SubsystemNumber
                && (!PointCode.HasValue || address.PointCode == PointCode);
        }

        return address.GlobalTitle?.Digits.StartsWith(GlobalTitlePrefix!, StringComparison.Ordinal) == true;
    }

    /// <summary>Computes route specificity for deterministic resolution.</summary>
    /// <returns>The specificity score.</returns>
    public int Specificity()
    {
        if (RoutingIndicator == SccpRoutingIndicator.RouteOnGlobalTitle)
        {
            return GlobalTitlePrefix?.Length ?? 0;
        }

        return PointCode.HasValue ? 2 : 1;
    }
}

/// <summary>
/// Represents an SCCP application route.
/// </summary>
public sealed class SccpRoute
{
    /// <summary>Creates an SCCP route.</summary>
    /// <param name="name">The route name.</param>
    /// <param name="selector">The route selector.</param>
    public SccpRoute(string name, SccpRouteSelector selector)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Route name is required.", nameof(name));
        }

        Name = name;
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <summary>The route name.</summary>
    public string Name { get; }

    /// <summary>The route selector.</summary>
    public SccpRouteSelector Selector { get; }
}

/// <summary>
/// Resolves SCCP party addresses to configured application routes.
/// </summary>
public sealed class SccpRouteTable
{
    private readonly List<SccpRoute> _routes = [];

    /// <summary>Adds a route to the table.</summary>
    /// <param name="route">The route to add.</param>
    public void Add(SccpRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (_routes.Any(existing => string.Equals(existing.Name, route.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"SCCP route '{route.Name}' already exists.");
        }

        _routes.Add(route);
    }

    /// <summary>Returns a snapshot of the configured routes.</summary>
    /// <returns>The configured routes.</returns>
    public IReadOnlyList<SccpRoute> Snapshot()
    {
        return _routes.ToArray();
    }

    /// <summary>Resolves the best route for the supplied SCCP party address.</summary>
    /// <param name="address">The SCCP party address.</param>
    /// <param name="route">The resolved route on success.</param>
    /// <returns>True when a route matched; otherwise false.</returns>
    public bool TryResolve(SccpPartyAddress address, out SccpRoute? route)
    {
        route = _routes
            .Where(candidate => candidate.Selector.Matches(address))
            .OrderByDescending(candidate => candidate.Selector.Specificity())
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        return route is not null;
    }
}
