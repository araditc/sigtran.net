# SCTP Transport

Phase 2 stabilizes the production SCTP transport boundary. The official transport contract is now `ISctpTransport`, which carries complete SCTP user messages with stream id, PPID, unordered delivery metadata, and association lifecycle visibility through `ISctpAssociation`. The older `ISctpSocket` remains as a compatibility contract and can be adapted with `SctpSocketTransportAdapter`.

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

The metadata contract is no longer limited to optional helpers. Existing `ISctpSocket` implementations continue to work for M3UA packet send/receive through `SctpSocketTransportAdapter`, while production transports should implement `ISctpTransport` directly so higher layers can opt into SCTP-specific behavior without binding to a concrete socket.

On Linux, `NativeSctpSocketAdapter` now uses lksctp `sctp_sendmsg` and `sctp_recvmsg` for `ISctpTransport` operations. That path preserves outbound stream id, PPID, and unordered metadata and returns kernel-provided receive metadata through `SctpReceiveResult`.

The development `TcpSctpAdapter` now implements `ISctpMetadataSocket`, `ISctpTransport`, and `ISctpAssociation` with default M3UA PPID metadata and exposes a health snapshot. It still uses TCP length-prefix framing and must not be treated as production SCTP.

`SigtranTransportSamples.CreateLocalM3uaAspToSg()` provides a documented local TCP sample scenario that maps an ASP endpoint to an SG endpoint with M3UA PPID metadata. It is intended for demos and deterministic tooling only.

## Association Lifecycle

`SctpAssociationState` and `SctpAssociationEvent` define the lifecycle vocabulary for production transports. Native implementations should report transitions such as connect start, established, reconnect start, shutdown, closed, and failed using these SDK types.

`SctpAssociationJournal` records timestamped lifecycle entries and exposes current state, failure detection, latest failure reason, and snapshots for diagnostics. `NativeSctpSocketAdapter` records established, failed, and closed events so higher layers can inspect lifecycle history in addition to the current health snapshot.

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

## Outbound Message Contract

`SctpOutboundMessage` pairs a non-empty user message payload with validated stream and PPID metadata. `SctpOutboundMessageBuilder.TryCreate(...)` applies the connection options, stream selection policy, sequence number, optional PPID override, and unordered flag before a native transport attempts a send.

The builder rejects unknown SIGTRAN PPIDs and stream ids outside the negotiated outbound stream count. This keeps the native send boundary deterministic and reviewable before platform-specific SCTP APIs are used.

## Send Backpressure

`SctpBackpressurePolicy` evaluates send queue pressure from `SctpSendQueueSnapshot` and an outbound message. It returns enqueue, drain, or reject decisions based on queued message count, queued bytes, and configured drain thresholds.

Native transports can use this contract before adding user messages to a channel, socket writer, or platform-specific send queue.

## Cancellation And Timeouts

`SctpOperationTimeoutPolicy` defines operation-specific timeouts for connect, send, receive, reconnect, and shutdown operations. It creates `SctpOperationCancellationBudget` values that expose UTC deadlines, timeout checks, and caller cancellation state.

Native transports should use these budgets consistently around async socket calls so timeout behavior is deterministic and observable.

## Multi-Homing Readiness

`SctpMultiHomingEndpointSet` models ordered remote endpoints and optional ordered local endpoints for native SCTP associations. The first remote endpoint is treated as the primary remote association target.

`SctpMultiHomingReadiness.Evaluate(...)` reports whether the endpoint set is ready for multi-homing, whether a single-homed fallback remains possible, and whether warnings such as duplicate endpoints or single-homed configuration should block production promotion.

This contract is intentionally package-neutral. Platform-specific transports can map the endpoint set to their own bind/connect APIs after the SDK has validated that the configuration is reviewable.

## Fault Recovery

`SctpTransportFaultKind` classifies transport-level failures such as connect timeout, send timeout, receive timeout, peer reset, socket error, backpressure rejection, caller cancellation, and protocol error.

`SctpFaultRecovery.Decide(...)` combines a fault with an `SctpReconnectSchedule` and returns a `SctpRecoveryDecision`. Reconnectable faults schedule the next reconnect attempt while the schedule has capacity; exhausted reconnect schedules fail fast. Backpressure rejection asks the caller to retry after drain, caller cancellation closes the association, and protocol errors fail fast for operator attention.

Native transports should record the decision in diagnostics before reconnecting or closing the association.

## Production Native SCTP Options

`NativeSctpTransportOptions` binds the production behavior used by native SCTP transports:

- `SctpBackpressurePolicy`
- `SctpOperationTimeoutPolicy`
- `SctpReconnectPolicy`
- kernel metadata requirement

`NativeSctpConnector` records `NativeSctpConnectionAttempt` entries for initial connect and reconnect attempts. The Phase 45 Linux sample uses those records as reconnect evidence.

`NativeSctpSocketAdapter.GetQueueMetrics()` returns `SctpTransportQueueMetrics` for queued send messages, queued bytes, pending receives, sent/received counters, backpressure rejections, and graceful shutdown count.

`NativeSctpSocketAdapter.ShutdownAsync(...)` moves the association through `ShuttingDown` and `Closed` lifecycle events and records graceful shutdown metrics.

## Transport Diagnostics

`SctpTransportDiagnostics.CreateSnapshot(...)` creates an immutable diagnostic envelope from transport health, association lifecycle history, optional multi-homing readiness, optional backpressure decision, optional recovery decision, and optional active operation timeout budget.

The snapshot summarizes transport state as healthy, degraded, or faulted. Healthy requires an established association with no warnings or recovery pressure. Degraded captures usable but attention-worthy states such as single-homed fallback, drain pressure, reconnect scheduling, or non-established lifecycle state. Faulted captures failed association state, recorded failure events, or fail-fast recovery decisions.

Transport implementations should emit this snapshot into structured logs and retained lab traces when diagnosing SCTP behavior.

## Production Hardening Readiness

`SctpProductionHardeningReadiness.GetReport(...)` reports whether the SCTP hardening foundation is complete and whether retained production evidence is available.

The foundation gate covers stream and PPID framing, association lifecycle journal, reconnect schedule, send backpressure, operation timeout policy, multi-homing readiness, fault recovery, and transport diagnostics. The production gate additionally requires retained Linux SCTP evidence and retained external peer traffic evidence.

This keeps source-level SDK readiness separate from production claims backed by lab artifacts.

`SctpProductionHardeningStatus.GetStatus()` summarizes the completed hardening foundation, completed capabilities, and remaining evidence blockers for release reporting.

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

`SctpReconnectSchedule` converts a policy and failure timestamp into deterministic scheduled attempts. It exposes attempt entries, next-attempt lookup, and exhaustion checks so native transports can separate retry planning from socket recreation and timers.

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
SctpTransportReadinessSnapshot report = SctpTransportReadiness.GetReport();
bool canShipNativeSctp = report.IsProductionReady;
```

The report exposes `FoundationCapabilityCount` and `RequiredFoundationCapabilityCount` so release tooling can verify that all foundation capabilities are present before native SCTP work starts.

## Production Boundary

Phase 2 completes the SDK-level SCTP foundation: metadata contracts, association lifecycle vocabulary, connection options, reconnect policy, stream selection, PPID helpers, health snapshots, readiness reporting, and a metadata-aware TCP development adapter.

The remaining production gate is explicit: native SCTP implementation and interoperability verification are required before this SDK can be used as a production SCTP stack.

## Phase 7 Native SCTP Matrix

`SigtranNativeSctpSupport.GetSupportMatrix()` records native SCTP support claims for commercial release planning. Linux is marked as verification required, while Windows and macOS are contract-only until a production provider is selected and verified.

## Phase 8 Native SCTP Probe

`NativeSctpPlatform.Probe()` checks whether the current runtime can create a Linux one-to-one SCTP socket using `SocketType.Stream` and IP protocol number `132`.

Some .NET runtimes can reject the managed `Socket(AddressFamily.InterNetwork, SocketType.Stream, (ProtocolType)132)` constructor even when the Linux kernel SCTP module is loaded and libc can create the socket. `NativeSctpSocketFactory` therefore keeps a Linux libc `socket(AF_INET, SOCK_STREAM, IPPROTO_SCTP)` fallback and wraps the resulting handle in `System.Net.Sockets.Socket`.

When .NET wraps a libc-created SCTP handle, `Socket.ProtocolType` can report `ProtocolType.Unknown` even though the kernel socket was created with `IPPROTO_SCTP`. Treat `NativeSctpPlatform.Probe()` and retained Linux lab evidence as the SCTP capability source of truth instead of relying on that diagnostic property alone.

The current retained VM evidence run is `commercial-native-sctp-20260627T073300Z` on Ubuntu 22.04.1 LTS with kernel `5.15.0-181-generic`. It captured M3UA ASPUP, ASPACTIVE, Payload DATA, HEARTBEAT, and HEARTBEAT ACK over native SCTP loopback with retained PCAP, SDK trace, TShark comparison, run report, and SHA-256 digests.

The probe does not mark the transport production-ready by itself. It is the first native SCTP implementation gate.

`NativeSctpSocketFactory` centralizes socket creation and throws `NativeSctpUnavailableException` when the current platform cannot create native SCTP sockets.

`NativeSctpConnectionPlanner` resolves configured SCTP endpoints to `IPEndPoint` values before native bind/connect attempts.

`NativeSctpSocketAdapter` wraps an SCTP socket as `ISctpSocket` and reports `SctpTransportHealth` snapshots for native associations.

`NativeSctpConnector` performs the client-side bind/connect path and returns an established native adapter.

`NativeSctpListener` provides the server-side bind/listen/accept path for Linux native SCTP lab scenarios.

`scripts/run-phase45-native-sctp-lab.sh` runs the production transport sample on Linux. It publishes the lab binary, captures PCAP with `tcpdump`, validates stream/PPID metadata from `sctp_recvmsg`, records reconnect attempts, writes queue metrics, performs graceful shutdown, decodes SCTP with TShark, generates a report, and computes SHA-256 digests.

Run `phase45-native-sctp-20260701T103951Z` passed this script on a Linux VM and retained PCAP, trace, TShark decode, report, and digest evidence in `docs/evidence/PHASE45_NATIVE_SCTP_20260701T103951Z.md`.

`NativeSctpLab.CreateFromEnvironment()` keeps native SCTP integration verification opt-in through `SIGTRAN_NATIVE_SCTP_LAB=1`.

`NativeSctpReadiness.GetReport()` marks the Phase 8 native SCTP foundation complete while keeping production readiness blocked until Linux verification passes.
