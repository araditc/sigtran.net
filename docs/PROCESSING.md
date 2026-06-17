# M3UA Inbound Processing

`M3uaInboundProcessor` is the first high-level orchestration API for receiving M3UA packets. It combines the lower-level pieces that applications otherwise need to wire by hand.

## Responsibilities

The processor performs these steps for one encoded M3UA packet:

1. Decode the packet into `M3uaMessage`.
2. Dispatch it through `M3uaTypedMessageParser.TryParseKnown`.
3. Apply ASPSM and ASPTM acknowledgement messages to `M3uaAspSession`.
4. Optionally reject DATA unless the ASP session is active.
5. Resolve typed DATA messages through `M3uaPayloadRouteTable` when routes are configured.

## Example

```csharp
M3uaPayloadRouteTable routes = new();
routes.TryAdd(
    new M3uaPayloadRoute(
        name: "map-home",
        networkAppearance: 7,
        routingContext: 100,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3),
    out string? error);

M3uaInboundProcessor processor = new(
    payloadRoutes: routes,
    requireActiveAspForPayload: true);

if (!processor.TryProcess(packet, out M3uaInboundProcessingResult? result, out error))
{
    throw new InvalidOperationException(error);
}
```

## Result Model

`M3uaInboundProcessingResult` exposes:

| Property | Meaning |
| --- | --- |
| `Message` | The decoded generic M3UA message |
| `TypedMessage` | The typed dispatcher result |
| `PayloadRoute` | The resolved DATA route, when applicable |
| `StateTransition` | The ASP state transition accepted from an acknowledgement |

## DATA Policy

When `requireActiveAspForPayload` is `true`, DATA is rejected unless the ASP session is `Active`. This is useful for production receive loops that should not pass MTP3-user payloads upward before the ASP lifecycle is ready.

If the route table is empty, DATA can still be accepted without a route. If routes exist, DATA must match exactly one best route.

Routes can be added, removed, cleared, snapshotted, or found by name on the `M3uaPayloadRouteTable`. Applications that mutate routes while processing traffic should protect the table with their own synchronization until a later Application Server model provides coordinated reconfiguration.

## Current Scope

This processor is intentionally synchronous and packet-oriented. Async transport receive loops should call it after reading one complete M3UA packet from SCTP or a test transport. Future transport work can layer channel-based receive APIs on top of this processor.
