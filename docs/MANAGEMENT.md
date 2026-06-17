# M3UA Management Messages

M3UA Management messages report protocol errors and application-server status changes. SIGTRAN.NET supports Error and Notify through low-level builders, typed parsers, outbound processing, and transport send helpers.

## Error

`SendErrorAsync` builds and sends a Management Error message. Use it when a peer sends invalid traffic, unknown routing context, unsupported message type, or other RFC 4666 error conditions.

```csharp
await transport.SendErrorAsync(
    M3uaErrorCode.InvalidRoutingContext,
    routingContexts: new uint[] { 100 },
    networkAppearance: 7,
    diagnosticInformation: offendingPacketBytes,
    ct: ct);
```

## Notify

`SendNotifyAsync` builds and sends a Management Notify message for Application Server state changes or other ASP status events.

```csharp
await transport.SendNotifyAsync(
    M3uaNotifyStatusType.ApplicationServerStateChange,
    (ushort)M3uaApplicationServerState.Active,
    aspIdentifier: 42,
    routingContexts: new uint[] { 100 },
    infoString: "as-active"u8.ToArray(),
    ct: ct);
```

## Validation

Typed parsers validate Error Code, Status Type, and Status Information values. Unknown Notify status information is rejected before an application consumes the message.
