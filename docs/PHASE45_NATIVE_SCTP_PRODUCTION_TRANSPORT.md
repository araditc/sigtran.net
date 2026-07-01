# Phase 45 - Native SCTP Production Transport

Phase 45 hardens the Linux native SCTP transport path so SDK traffic can carry real stream id and PPID metadata through the kernel SCTP stack and retain evidence from a repeatable Linux sample.

## Goals

- Use Linux SCTP metadata APIs for send and receive.
- Preserve stream id, PPID, and unordered metadata in `ISctpTransport`.
- Apply reconnect policy during native SCTP connect.
- Enforce send backpressure before kernel send.
- Track cancellation, operation timeout, lifecycle, recovery, queue metrics, and graceful shutdown.
- Provide a repeatable Linux sample that captures PCAP, SDK trace, logs, reconnect attempts, metadata validation, comparison output, report, and digests.

## Completed Units

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | lksctp-backed metadata send and receive | Complete |
| 2 | `NativeSctpTransportOptions` for metadata, timeout, reconnect, and backpressure policy | Complete |
| 3 | send queue and receive operation metrics | Complete |
| 4 | native adapter timeout, cancellation, fault recovery, and diagnostics snapshots | Complete |
| 5 | graceful shutdown lifecycle events | Complete |
| 6 | connector retry trace for reconnect validation | Complete |
| 7 | lab runner metadata validation for stream id and PPID | Complete |
| 8 | Linux evidence script with PCAP, logs, trace, comparison, report, and digests | Complete |
| 9 | unit tests for production options and reconnect attempt records | Complete |
| 10 | README, SCTP docs, roadmap, and phase index alignment | Complete |

## Production Transport Behavior

`NativeSctpSocketAdapter` now sends `SctpOutboundMessage` values through lksctp `sctp_sendmsg`, preserving:

- stream id
- PPID
- unordered flag

Receive uses lksctp `sctp_recvmsg`, so `ISctpTransport.ReceiveAsync` returns `SctpReceiveResult` with kernel-provided metadata instead of a synthetic default.

The adapter also exposes:

- `GetQueueMetrics()`
- `GetDiagnosticsSnapshot()`
- `ShutdownAsync(...)`
- latest backpressure and recovery decisions
- association lifecycle entries

## Linux Sample

Run this from the repository root on a Linux host with kernel SCTP, `libsctp.so.1`, `tcpdump`, `tshark`, and .NET installed:

```bash
SIGTRAN_ARTIFACT_ROOT="$HOME/sigtran-lab/artifacts/phase45" \
SIGTRAN_STREAM_ID=1 \
SIGTRAN_PPID=3 \
bash scripts/run-phase45-native-sctp-lab.sh
```

The script publishes the lab binary, starts `tcpdump`, runs a loopback M3UA exchange over real SCTP, intentionally starts the client before the server to validate reconnect, validates stream/PPID metadata from `sctp_recvmsg`, performs graceful shutdown, generates a TShark decode, writes a Markdown report, and computes SHA-256 digests.

## Completion Criteria

Phase 45 is complete when the code path builds, tests, packs, and the Linux sample produces:

- PCAP under `pcap/`
- SDK JSONL trace under `trace/`
- lab and capture logs under `logs/`
- TShark comparison output under `comparison/`
- readiness report under `reports/`
- digest manifest under `digests/`

The report must show metadata validation, stream validation, PPID validation, at least one failed reconnect attempt, one successful reconnect attempt, metrics snapshots, and client/server graceful shutdown events.

## Passing Evidence

Run `phase45-native-sctp-20260701T103951Z` passed on the Linux VM `sigtrannet` with kernel `5.15.0-181-generic`.

- Evidence report: `docs/evidence/PHASE45_NATIVE_SCTP_20260701T103951Z.md`
- Evidence manifest: `docs/evidence/PHASE45_NATIVE_SCTP_20260701T103951Z.json`
- Retained artifact root: `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z`
- PCAP SHA-256: `33c7708b66fba17b7f5e72ed30e06be3e1f8e01d27e60c25d8c7165fe5663f35`
- SDK trace SHA-256: `abaa2a9153b10db6abd73050c85d26c820d16d892594e781e4ade9559e863c90`

This evidence closes the native Linux SCTP loopback gate for Phase 45. Independent external peer interoperability remains a separate production gate.
