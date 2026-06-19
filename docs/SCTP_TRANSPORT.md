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

The development `TcpSctpAdapter` now implements `ISctpMetadataSocket` with default M3UA PPID metadata and exposes a health snapshot. It still uses TCP length-prefix framing and must not be treated as production SCTP.

`SigtranTransportSamples.CreateLocalM3uaAspToSg()` provides a documented local TCP sample scenario that maps an ASP endpoint to an SG endpoint with M3UA PPID metadata. It is intended for demos and deterministic tooling only.

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

## Readiness Report

`SctpTransportReadiness.GetReport()` reports the current phase status. The foundation is ready when metadata, lifecycle, options, reconnect policy, and the development adapter are present. Production readiness remains false until a native SCTP implementation is added and verified.

```csharp
SctpTransportReadinessReport report = SctpTransportReadiness.GetReport();
bool canShipNativeSctp = report.IsProductionReady;
```

The report exposes `FoundationCapabilityCount` and `RequiredFoundationCapabilityCount` so release tooling can verify that all foundation capabilities are present before native SCTP work starts.

## Production Boundary

Phase 2 completes the SDK-level SCTP foundation: metadata contracts, association lifecycle vocabulary, connection options, reconnect policy, stream selection, PPID helpers, health snapshots, readiness reporting, and a metadata-aware TCP development adapter.

The remaining production gate is explicit: native SCTP implementation and interoperability verification are required before this SDK can be used as a production SCTP stack.

## Phase 7 Native SCTP Matrix

`SigtranNativeSctpSupport.GetSupportMatrix()` records native SCTP support claims for commercial release planning. Linux is marked as verification required, while Windows and macOS are contract-only until a production provider is selected and verified.

## Phase 8 Native SCTP Probe

`NativeSctpPlatform.Probe()` checks whether the current runtime can create a Linux SCTP socket using `SocketType.Seqpacket` and IP protocol number `132`.

The probe does not mark the transport production-ready by itself. It is the first native SCTP implementation gate.

`NativeSctpSocketFactory` centralizes socket creation and throws `NativeSctpUnavailableException` when the current platform cannot create native SCTP sockets.

`NativeSctpConnectionPlanner` resolves configured SCTP endpoints to `IPEndPoint` values before native bind/connect attempts.
