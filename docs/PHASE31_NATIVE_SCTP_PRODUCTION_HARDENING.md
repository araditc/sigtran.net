# Phase 31 Native SCTP Production Hardening

Phase 31 hardens the native SCTP production boundary around stream and PPID correctness, reconnect behavior, backpressure, cancellation, multi-homing readiness, association lifecycle, and fault recovery. The phase does not claim production SCTP readiness without retained Linux SCTP and external peer evidence.

## Unit 1 - Stream And PPID Framing

`SctpOutboundMessage` and `SctpOutboundMessageBuilder` now provide a validated outbound user-message contract. They cover:

- Non-empty SCTP user message payloads.
- Default or caller-provided PPID selection.
- Known SIGTRAN PPID validation.
- Stream selection through `SctpStreamSelectionPolicy`.
- Negotiated outbound stream count validation.
- Compact diagnostic summaries.

This gives native transports a hardened send boundary before payloads are passed to kernel SCTP send APIs.

## Unit 2 - Association Lifecycle Journal

`SctpAssociationJournal` now records timestamped association lifecycle history. It provides:

- Timestamped lifecycle entries.
- Current state inference.
- Failure detection.
- Latest failure reason lookup.
- Snapshot export for diagnostics.
- Native adapter lifecycle recording for established, failed, and closed events.

This makes association behavior auditable across connect, reconnect, shutdown, and failure paths.

## Unit 3 - Reconnect Schedule

`SctpReconnectSchedule` now turns a reconnect policy into deterministic retry entries. It provides:

- Attempt numbers.
- Bounded reconnect delays.
- Scheduled UTC attempt times.
- Next-attempt lookup.
- Exhaustion checks.
- Disabled reconnect handling.

This keeps reconnect orchestration deterministic while leaving actual sleeping, socket recreation, and peer-specific recovery to the transport implementation.

## Unit 4 - Send Backpressure Policy

`SctpBackpressurePolicy` now defines send queue pressure decisions. It provides:

- Queue message and byte snapshots.
- Maximum queued message limits.
- Maximum queued byte limits.
- Drain thresholds.
- Enqueue, drain, and reject decisions.
- Compact diagnostic summaries.

This gives native transports a deterministic pressure gate before accepting additional outbound user messages.

## Unit 5 - Cancellation And Timeout Policy

`SctpOperationTimeoutPolicy` now defines operation-specific cancellation budgets. It provides:

- Connect timeout.
- Send timeout.
- Receive timeout.
- Reconnect timeout.
- Shutdown timeout.
- UTC operation deadlines.
- Caller cancellation visibility.

This gives native transports a single timeout contract for async socket operations and graceful shutdown handling.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
