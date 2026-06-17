# M3UA Outbound Processing

`M3uaOutboundProcessor` is a high-level outbound helper for building common M3UA messages with association defaults.

## Responsibilities

- Build ASP Up and ASP Down messages.
- Build ASP Active and ASP Inactive using a configured default Routing Context.
- Build Heartbeat and Heartbeat Ack messages.
- Build RKM Registration and Deregistration Request messages.
- Build Management Error and Notify messages.
- Build SSNM DUNA, DAVA, DAUD, DRST, DUPU, and SCON messages.
- Build DATA using configured default Network Appearance and Routing Context.
- Optionally reject DATA while the associated ASP session is not `Active`.

## Example

```csharp
M3uaOutboundProcessor outbound = new(
    networkAppearance: 7,
    routingContext: 100,
    requireActiveAspForPayload: true);

Span<byte> buffer = stackalloc byte[1024];

bool ok = outbound.TryBuildPayloadData(
    buffer,
    userPayload: [0x01, 0x02],
    originatingPointCode: 0x00010203,
    destinationPointCode: 0x00040506,
    serviceIndicator: 3,
    networkIndicator: 2,
    messagePriority: 0,
    signallingLinkSelection: 7,
    networkAppearance: null,
    routingContext: null,
    correlationId: 42,
    out int written,
    out string? error);
```

Passing `null` for Network Appearance or Routing Context lets the processor use its configured defaults. Explicit values override defaults for that single DATA message.

If a typed DATA model is already available, use the typed overload.

```csharp
bool typedOk = outbound.TryBuildPayloadData(
    buffer,
    payloadDataMessage,
    out written,
    out error);
```

## State Policy

When `requireActiveAspForPayload` is `true`, DATA building fails unless `AspSession.State` is `Active`. This mirrors the inbound processor option and helps applications avoid sending user payloads before ASP activation is complete.

## Current Scope

The outbound processor builds packets only. It does not send them over a transport yet. A future transport session API can combine this processor with `ISctpSocket`.
