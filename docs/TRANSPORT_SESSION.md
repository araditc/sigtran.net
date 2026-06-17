# M3UA Transport Session

`M3uaTransportSession` connects the M3UA processors to an `ISctpSocket`. It is the first async session facade over the transport abstraction.

## Responsibilities

- Receive one complete M3UA PDU from `ISctpSocket`.
- Automatically acknowledge inbound Heartbeat messages when requested.
- Wait for a specific typed message kind across multiple inbound PDUs.
- Wait for a specific ASP acknowledgement state transition across multiple inbound PDUs.
- Process inbound packets through `M3uaInboundProcessor`.
- Build outbound ASP lifecycle and DATA messages through `M3uaOutboundProcessor`.
- Send Heartbeat and Heartbeat Ack messages.
- Send RKM Registration and Deregistration Request messages.
- Send Management Error and Notify messages.
- Send SSNM DUNA, DAVA, DAUD, DRST, DUPU, and SCON messages.
- Send encoded packets through `ISctpSocket`.
- Expose session-local send, receive, and failure counters.
- Manage rented buffers with `ArrayPool<byte>`.

## Example

```csharp
await using M3uaTransportSession session = new(
    socket,
    inboundProcessor,
    outboundProcessor);

await session.SendAspUpAsync(
    aspIdentifier: 42,
    infoString: ReadOnlyMemory<byte>.Empty,
    ct);

M3uaInboundProcessingResult? result = await session.ReceiveAsync(ct);
```

## Sending DATA

```csharp
await session.SendPayloadDataAsync(
    userPayload: payload,
    originatingPointCode: 0x00010203,
    destinationPointCode: 0x00040506,
    serviceIndicator: 3,
    networkIndicator: 2,
    messagePriority: 0,
    signallingLinkSelection: 7,
    correlationId: 42,
    ct: ct);
```

When a typed DATA model is already available, the session can send it directly.

```csharp
await session.SendPayloadDataAsync(payloadDataMessage, ct);
```

Network Appearance and Routing Context defaults are applied by the configured `M3uaOutboundProcessor`.

## Receiving

`ReceiveAsync` returns null when the transport reports a clean close by returning zero bytes. Decode, typed parsing, ASP acknowledgement updates, DATA active-state policy, and route resolution are delegated to the inbound processor.

`ReceiveAndAcknowledgeHeartbeatAsync` behaves like `ReceiveAsync`, but if the inbound message is ASPSM Heartbeat it sends Heartbeat Ack with the same Heartbeat Data before returning.

```csharp
M3uaInboundProcessingResult? result =
    await session.ReceiveAndAcknowledgeHeartbeatAsync(ct);
```

`ReceiveUntilAsync` keeps receiving processed messages until a requested `M3uaTypedMessageKind` is accepted or the configured message limit is reached.

```csharp
M3uaInboundProcessingResult result = await session.ReceiveUntilAsync(
    M3uaTypedMessageKind.RegistrationResponse,
    maxMessages: 8,
    ct);
```

`ReceiveUntilTransitionAsync` is the equivalent helper for ASP acknowledgement events. It is used by `M3uaAspClient` for startup, Heartbeat, and shutdown handshakes.

```csharp
M3uaInboundProcessingResult activeAck = await session.ReceiveUntilTransitionAsync(
    M3uaAspEvent.AspActiveAcknowledged,
    maxMessages: 8,
    ct);
```

## Counters

`Counters` returns a snapshot of session-local packet counters.

```csharp
M3uaTransportSessionCounters counters = session.Counters;
long sent = counters.SentPdus;
long received = counters.ReceivedPdus;
```

`ResetCounters` starts a new local measurement window without rebuilding the transport session.

```csharp
session.ResetCounters();
```

The counters are intentionally framework-neutral. Applications can export them through OpenTelemetry, Prometheus, logs, or their own health endpoints.

## Transport Loss

`TryNotifyTransportLost` applies the ASP `TransportLost` event through the session facade. Use it when the socket, SCTP association, or hosting layer detects a connection failure outside `ReceiveAsync`.

```csharp
if (session.TryNotifyTransportLost(out M3uaAspStateTransition transition, out string? error))
{
    logger.LogWarning("ASP moved from {From} to {To}", transition.From, transition.To);
}
```

## Current Scope

The session expects `ISctpSocket.ReceiveAsync` to return exactly one complete M3UA PDU per call. This matches the existing transport contract and the TCP development adapter. Native SCTP stream and PPID handling will be added in the transport phase.
