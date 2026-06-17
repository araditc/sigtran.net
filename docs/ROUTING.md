# M3UA Payload Routing

`M3uaPayloadRouteTable` maps typed DATA messages to application routes. This keeps route selection in the SDK instead of duplicating selector logic in every application.

## Route Selectors

`M3uaPayloadRoute` supports these selectors:

| Selector | Source field |
| --- | --- |
| Network Appearance | `M3uaPayloadDataMessage.NetworkAppearance` |
| Routing Context | `M3uaPayloadDataMessage.RoutingContext` |
| Destination Point Code | `M3uaPayloadDataMessage.DestinationPointCode` |
| Service Indicator | `M3uaPayloadDataMessage.ServiceIndicator` |

Any selector can be omitted. Omitted selectors act as wildcards.

`DescribeSelectors` formats selectors deterministically for diagnostics and admin surfaces.

```csharp
string selectors = route.DescribeSelectors(); // NA=7 RC=100 DPC=263430 SI=3
```

## Resolution

The route table finds all matching routes and selects the most specific route. Specificity is the number of concrete selectors on the route.

If two or more matching routes have the same best specificity, resolution fails with an ambiguity error. This is intentional: production systems should not silently route SS7 traffic to a random handler.

## Example

```csharp
M3uaPayloadRouteTable routes = new();

routes.TryAdd(
    new M3uaPayloadRoute(
        name: "sccp-default",
        networkAppearance: null,
        routingContext: 100,
        destinationPointCode: null,
        serviceIndicator: 3),
    out string? error);

routes.TryAdd(
    new M3uaPayloadRoute(
        name: "map-home",
        networkAppearance: 7,
        routingContext: 100,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3),
    out error);

if (!routes.TryResolve(data, out M3uaPayloadRoute? route, out error))
{
    throw new InvalidOperationException(error);
}
```

## Duplicate Protection

`TryAdd` rejects a route when an existing route has the exact same selectors. Route names can differ, but selector duplicates are not allowed because they make traffic ownership unclear.

## Route Updates

`TryReplace` replaces the first route with the same selectors as the supplied route. This is useful for renaming a handler or refreshing route metadata while preserving selector ownership.

`AddOrReplace` is the reload-friendly helper: it adds a new route when selectors are new, or replaces the existing owner when selectors already exist.

`TryRemove` removes the first route with the same selectors as the supplied route. The route name does not need to match. `Clear` removes all routes, which is useful when an application reloads routing configuration from a trusted source.

```csharp
routes.AddOrReplace(
    new M3uaPayloadRoute(
        name: "map-home-current",
        networkAppearance: 7,
        routingContext: 100,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3),
    out bool replaced);

routes.TryReplace(
    new M3uaPayloadRoute(
        name: "map-home-v2",
        networkAppearance: 7,
        routingContext: 100,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3),
    out error);

routes.TryRemove(
    new M3uaPayloadRoute(
        name: "old-name-is-ok",
        networkAppearance: 7,
        routingContext: 100,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3),
    out error);

routes.Clear();
```

## Inspection

`Count` and `IsEmpty` expose lightweight table status for health checks and configuration validation.

`Snapshot` returns a stable array copy of routes in insertion order. `TryFindByName` returns the first route with the requested name, which is useful for diagnostics, admin APIs, and configuration validation.

```csharp
if (routes.IsEmpty)
{
    throw new InvalidOperationException("No M3UA DATA routes are configured.");
}

M3uaPayloadRoute[] snapshot = routes.Snapshot();

if (routes.TryFindByName("map-home", out M3uaPayloadRoute? route))
{
    Console.WriteLine(route.RoutingContext);
}
```

## Current Scope

This table resolves already-decoded DATA messages. It does not yet enforce ASP active state, traffic mode policy, multi-ASP load-sharing decisions, or cross-thread synchronization. Those belong in a later Application Server model.
