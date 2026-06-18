# SCTP Transport

Phase 2 stabilizes the production SCTP transport boundary. The existing `ISctpSocket` remains the packet-oriented contract used by M3UA. Production transports can additionally implement `ISctpMetadataSocket` when stream id, PPID, and unordered delivery metadata are available.

## Payload Metadata

`SctpPayloadMetadata` captures the SCTP stream id, Payload Protocol Identifier, and unordered flag for one SCTP user message.

```csharp
SctpPayloadMetadata metadata = new(
    streamId: 1,
    payloadProtocolIdentifier: 3,
    unordered: false);
```

`SctpReceiveResult` pairs the received payload byte count with the associated metadata.

## Compatibility

The metadata contract is optional. Existing `ISctpSocket` implementations continue to work for M3UA packet send/receive. A native SCTP transport should implement both interfaces so higher layers can opt into SCTP-specific behavior without breaking the current M3UA session facade.

## Association Lifecycle

`SctpAssociationState` and `SctpAssociationEvent` define the lifecycle vocabulary for production transports. Native implementations should report transitions such as connect start, established, reconnect start, shutdown, closed, and failed using these SDK types.

## Connection Options

`SctpEndpoint` and `SctpConnectionOptions` define the configuration surface for a production transport.

```csharp
SctpConnectionOptions options = new(
    remoteEndpoint: new SctpEndpoint("sg.example.net", 2905),
    localEndpoint: new SctpEndpoint("0.0.0.0", 2905),
    outboundStreams: 8,
    inboundStreams: 8,
    defaultPayloadProtocolIdentifier: 3,
    connectTimeout: TimeSpan.FromSeconds(5));
```

The model validates endpoint host, port range, and positive stream counts before a native transport attempts an association.

## Payload Protocol Identifiers

`SctpPayloadProtocolIdentifiers` defines SIGTRAN PPID constants currently recognized by the SDK:

| Name | Value |
| --- | --- |
| `M3ua` | `3` |
| `M2pa` | `5` |

Use `TryRequireKnown` at transport boundaries when unknown PPIDs should be rejected before M3UA processing.

## Stream Selection

`SctpStreamSelectionPolicy` standardizes outbound stream selection.

```csharp
SctpStreamSelectionPolicy policy = new(
    SctpStreamSelectionMode.RoundRobin,
    streamCount: 8);

ushort streamId = policy.SelectStream(sequence: messageNumber);
```

Fixed mode always uses one stream id. Round-robin mode applies modulo to a caller-provided sequence value so transports can remain stateless if they prefer.

## Reconnect Policy

`SctpReconnectPolicy` defines reconnect attempt count and bounded exponential backoff.

```csharp
SctpReconnectPolicy reconnect = new(
    maxAttempts: 5,
    initialDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(30),
    backoffMultiplier: 2.0);
```

The policy is deterministic and does not sleep by itself. Transport implementations call `GetDelay(attempt)` and decide how to schedule reconnect attempts.

## Health Snapshot

`SctpTransportHealth` is the shared health shape for native and adapter transports. It captures association state, endpoints, negotiated stream counts, default PPID, and sent/received message counters.

```csharp
SctpTransportHealth health = new(
    SctpAssociationState.Established,
    remoteEndpoint,
    localEndpoint,
    outboundStreams: 8,
    inboundStreams: 8,
    defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua,
    sentMessages: 100,
    receivedMessages: 99);
```
