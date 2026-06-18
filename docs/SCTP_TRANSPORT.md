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
