namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Validation snapshot for an M3UA Payload Data route table.
/// </summary>
public readonly struct M3uaPayloadRouteTableValidation
{
    /// <summary>Creates a route-table validation snapshot.</summary>
    /// <param name="routeCount">The number of registered routes.</param>
    /// <param name="duplicateNameCount">The number of route entries whose name duplicates an earlier route.</param>
    public M3uaPayloadRouteTableValidation(int routeCount, int duplicateNameCount)
    {
        RouteCount = routeCount;
        DuplicateNameCount = duplicateNameCount;
    }

    /// <summary>The number of registered routes.</summary>
    public int RouteCount { get; }

    /// <summary>The number of route entries whose name duplicates an earlier route.</summary>
    public int DuplicateNameCount { get; }

    /// <summary>Whether the route table has no registered routes.</summary>
    public bool IsEmpty => RouteCount == 0;

    /// <summary>Whether any route name appears more than once.</summary>
    public bool HasDuplicateNames => DuplicateNameCount > 0;

    /// <summary>Whether the route table passes the default alpha validation gate.</summary>
    public bool IsValid => !IsEmpty && !HasDuplicateNames;
}

/// <summary>
/// Describes an application route for incoming M3UA Payload Data messages.
/// </summary>
public sealed class M3uaPayloadRoute
{
    /// <summary>Creates a route for matching incoming Payload Data messages.</summary>
    /// <param name="name">The route name, usually an Application Server or handler name.</param>
    /// <param name="networkAppearance">The optional Network Appearance selector.</param>
    /// <param name="routingContext">The optional Routing Context selector.</param>
    /// <param name="destinationPointCode">The optional Destination Point Code selector.</param>
    /// <param name="serviceIndicator">The optional Service Indicator selector.</param>
    public M3uaPayloadRoute(
        string name,
        uint? networkAppearance,
        uint? routingContext,
        uint? destinationPointCode,
        byte? serviceIndicator)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Route name must not be empty.", nameof(name));
        }

        Name = name;
        NetworkAppearance = networkAppearance;
        RoutingContext = routingContext;
        DestinationPointCode = destinationPointCode;
        ServiceIndicator = serviceIndicator;
    }

    /// <summary>The route name, usually an Application Server or handler name.</summary>
    public string Name { get; }

    /// <summary>The optional Network Appearance selector.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Routing Context selector.</summary>
    public uint? RoutingContext { get; }

    /// <summary>The optional Destination Point Code selector.</summary>
    public uint? DestinationPointCode { get; }

    /// <summary>The optional Service Indicator selector.</summary>
    public byte? ServiceIndicator { get; }

    /// <summary>The number of concrete selectors on this route.</summary>
    public int Specificity =>
        (NetworkAppearance.HasValue ? 1 : 0)
        + (RoutingContext.HasValue ? 1 : 0)
        + (DestinationPointCode.HasValue ? 1 : 0)
        + (ServiceIndicator.HasValue ? 1 : 0);

    /// <summary>
    /// Determines whether this route matches the supplied Payload Data message.
    /// </summary>
    /// <param name="message">The typed Payload Data message to test.</param>
    /// <returns>True if the route matches; otherwise false.</returns>
    public bool Matches(M3uaPayloadDataMessage message)
    {
        return MatchesOptional(NetworkAppearance, message.NetworkAppearance)
               && MatchesOptional(RoutingContext, message.RoutingContext)
               && MatchesOptional(DestinationPointCode, message.DestinationPointCode)
               && MatchesOptional(ServiceIndicator, message.ServiceIndicator);
    }

    /// <summary>
    /// Determines whether this route has the same selectors as another route.
    /// </summary>
    /// <param name="other">The route to compare with this route.</param>
    /// <returns>True if all selectors are identical; otherwise false.</returns>
    public bool HasSameSelectors(M3uaPayloadRoute other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return NetworkAppearance == other.NetworkAppearance
               && RoutingContext == other.RoutingContext
               && DestinationPointCode == other.DestinationPointCode
               && ServiceIndicator == other.ServiceIndicator;
    }

    /// <summary>
    /// Formats the route selectors in a deterministic diagnostic form.
    /// </summary>
    /// <returns>A compact selector description.</returns>
    public string DescribeSelectors()
    {
        return $"NA={FormatSelector(NetworkAppearance)} RC={FormatSelector(RoutingContext)} DPC={FormatSelector(DestinationPointCode)} SI={FormatSelector(ServiceIndicator)}";
    }

    private static bool MatchesOptional<T>(T? selector, T? value)
        where T : struct
    {
        return !selector.HasValue || (value.HasValue && EqualityComparer<T>.Default.Equals(selector.Value, value.Value));
    }

    private static string FormatSelector<T>(T? selector)
        where T : struct
    {
        return selector.HasValue ? selector.Value.ToString() ?? string.Empty : "*";
    }
}

/// <summary>
/// Resolves incoming Payload Data messages to registered M3UA application routes.
/// </summary>
public sealed class M3uaPayloadRouteTable
{
    private readonly List<M3uaPayloadRoute> _routes = new();

    /// <summary>The registered routes in insertion order.</summary>
    public IReadOnlyList<M3uaPayloadRoute> Routes => _routes;

    /// <summary>The number of registered routes.</summary>
    public int Count => _routes.Count;

    /// <summary>Whether the route table has no registered routes.</summary>
    public bool IsEmpty => _routes.Count == 0;

    /// <summary>
    /// Adds a route if no existing route has the same selectors.
    /// </summary>
    /// <param name="route">The route to add.</param>
    /// <param name="error">An error message if the route cannot be added.</param>
    /// <returns>True if the route was added; otherwise false.</returns>
    public bool TryAdd(M3uaPayloadRoute route, out string? error)
    {
        ArgumentNullException.ThrowIfNull(route);
        error = null;

        for (int i = 0; i < _routes.Count; i++)
        {
            if (_routes[i].HasSameSelectors(route))
            {
                error = $"A route with the same selectors already exists: {_routes[i].Name}";
                return false;
            }
        }

        _routes.Add(route);
        return true;
    }

    /// <summary>
    /// Removes the first route with the same selectors as the supplied route.
    /// </summary>
    /// <param name="route">The route selectors to remove.</param>
    /// <param name="error">An error message if no matching route exists.</param>
    /// <returns>True if a route was removed; otherwise false.</returns>
    public bool TryRemove(M3uaPayloadRoute route, out string? error)
    {
        ArgumentNullException.ThrowIfNull(route);
        error = null;

        for (int i = 0; i < _routes.Count; i++)
        {
            if (_routes[i].HasSameSelectors(route))
            {
                _routes.RemoveAt(i);
                return true;
            }
        }

        error = "No route with the same selectors exists.";
        return false;
    }

    /// <summary>
    /// Replaces the first route with the same selectors as the supplied route.
    /// </summary>
    /// <param name="route">The replacement route.</param>
    /// <param name="error">An error message if no matching route exists.</param>
    /// <returns>True if a route was replaced; otherwise false.</returns>
    public bool TryReplace(M3uaPayloadRoute route, out string? error)
    {
        ArgumentNullException.ThrowIfNull(route);
        error = null;

        for (int i = 0; i < _routes.Count; i++)
        {
            if (_routes[i].HasSameSelectors(route))
            {
                _routes[i] = route;
                return true;
            }
        }

        error = "No route with the same selectors exists.";
        return false;
    }

    /// <summary>
    /// Adds the supplied route or replaces an existing route with the same selectors.
    /// </summary>
    /// <param name="route">The route to add or use as a replacement.</param>
    /// <param name="replaced">True when an existing route was replaced; false when a new route was added.</param>
    public void AddOrReplace(M3uaPayloadRoute route, out bool replaced)
    {
        ArgumentNullException.ThrowIfNull(route);
        for (int i = 0; i < _routes.Count; i++)
        {
            if (_routes[i].HasSameSelectors(route))
            {
                _routes[i] = route;
                replaced = true;
                return;
            }
        }

        _routes.Add(route);
        replaced = false;
    }

    /// <summary>
    /// Removes all registered routes.
    /// </summary>
    public void Clear()
    {
        _routes.Clear();
    }

    /// <summary>
    /// Copies registered routes into a stable snapshot array.
    /// </summary>
    /// <returns>A snapshot of the registered routes in insertion order.</returns>
    public M3uaPayloadRoute[] Snapshot()
    {
        return _routes.ToArray();
    }

    /// <summary>
    /// Builds a validation snapshot for the registered routes.
    /// </summary>
    /// <returns>A route-table validation snapshot.</returns>
    public M3uaPayloadRouteTableValidation Validate()
    {
        HashSet<string> seenNames = new(StringComparer.Ordinal);
        int duplicateNameCount = 0;
        for (int i = 0; i < _routes.Count; i++)
        {
            if (!seenNames.Add(_routes[i].Name))
            {
                duplicateNameCount++;
            }
        }

        return new(_routes.Count, duplicateNameCount);
    }

    /// <summary>
    /// Finds the first registered route with the supplied name.
    /// </summary>
    /// <param name="name">The route name to find.</param>
    /// <param name="route">The matching route on success.</param>
    /// <returns>True if a matching route was found; otherwise false.</returns>
    public bool TryFindByName(string name, out M3uaPayloadRoute? route)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Route name must not be empty.", nameof(name));
        }

        for (int i = 0; i < _routes.Count; i++)
        {
            if (string.Equals(_routes[i].Name, name, StringComparison.Ordinal))
            {
                route = _routes[i];
                return true;
            }
        }

        route = null;
        return false;
    }

    /// <summary>
    /// Resolves a Payload Data message to the most specific matching route.
    /// </summary>
    /// <param name="message">The typed Payload Data message to resolve.</param>
    /// <param name="route">The selected route on success.</param>
    /// <param name="error">An error message if no route matches or the match is ambiguous.</param>
    /// <returns>True if exactly one best route was found; otherwise false.</returns>
    public bool TryResolve(
        M3uaPayloadDataMessage message,
        out M3uaPayloadRoute? route,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(message);
        route = null;
        error = null;
        int bestSpecificity = -1;
        bool ambiguous = false;

        for (int i = 0; i < _routes.Count; i++)
        {
            M3uaPayloadRoute candidate = _routes[i];
            if (!candidate.Matches(message))
            {
                continue;
            }

            if (candidate.Specificity > bestSpecificity)
            {
                route = candidate;
                bestSpecificity = candidate.Specificity;
                ambiguous = false;
                continue;
            }

            if (candidate.Specificity == bestSpecificity)
            {
                ambiguous = true;
            }
        }

        if (route is null)
        {
            error = "No Payload Data route matched the message.";
            return false;
        }

        if (ambiguous)
        {
            error = "Multiple Payload Data routes matched with the same specificity.";
            route = null;
            return false;
        }

        return true;
    }
}
