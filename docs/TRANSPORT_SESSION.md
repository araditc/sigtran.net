# M3UA Transport Session

`M3uaTransportSession` connects the M3UA processors to an `ISctpSocket`. It is the first async session facade over the transport abstraction.

## Responsibilities

- Receive one complete M3UA PDU from `ISctpSocket`.
- Process inbound packets through `M3uaInboundProcessor`.
- Build outbound ASP lifecycle and DATA messages through `M3uaOutboundProcessor`.
- Send Heartbeat and Heartbeat Ack messages.
- Send RKM Registration and Deregistration Request messages.
- Send encoded packets through `ISctpSocket`.
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

Network Appearance and Routing Context defaults are applied by the configured `M3uaOutboundProcessor`.

## Receiving

`ReceiveAsync` returns null when the transport reports a clean close by returning zero bytes. Decode, typed parsing, ASP acknowledgement updates, DATA active-state policy, and route resolution are delegated to the inbound processor.

## Current Scope

The session expects `ISctpSocket.ReceiveAsync` to return exactly one complete M3UA PDU per call. This matches the existing transport contract and the TCP development adapter. Native SCTP stream and PPID handling will be added in the transport phase.
