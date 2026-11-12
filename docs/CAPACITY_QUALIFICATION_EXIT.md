# Capacity Qualification Exit

Status: **Phase 61 complete for the declared source/hardware/profile**.
Date: 2026-11-12 UTC. Policy: `completed-map-10k-v1`, unchanged.
Tested source: `947d69abd081449cc0e2c286409eeab69a659ab7`.

## Verified Result

- Two distinct Linux VMs, dedicated workload window, one SDK and one independent C peer.
- 1,800 measured seconds, 26,599,474 correlated successful MAP results.
- Average 14,777.49 TPS; minimum/maximum one-second TPS 10,079/20,485.
- Zero low seconds, failed/unresolved work, reconnects, malformed input or event drops.
- P50/P95/P99 17.0/23.4/30.9 ms; maximum 66.255 ms.
- 256 late successes excluded from TPS; warmup and all per-operation totals reconcile.
- 3,551 independently decoded complete wire pairs across all five operations.
- Original SHA256s, archive source ID, four zero exit receipts and owned-process cleanup verified.
- Approved service quiescence, fallback timer and original-state/hash restoration verified.

The [measurement](CAPACITY_MEASUREMENT_DEDICATED_20261112.md) and
[sanitized evidence](evidence/COMPLETED_MAP_CAPACITY_DEDICATED_20261112.json)
retain hashes of the protected original PCAP, configurations/inventories, SDK
runtime observations, peer counts and maintenance audit. Capture is a bounded
prefix, not a capture of every transaction or an extrapolated TPS calculation.

## Declared Profile

SDK VM: four logical CPUs, 15,988 MiB RAM, kernel 5.15.0-191-generic.
Peer VM: four logical CPUs, 5,924 MiB RAM, kernel 6.8.0-149-generic.
.NET 10.0.12/server GC; four minimum/maximum application workers; concurrency
256; one-CPU C peer at nice 0 and owned-peer observer at nice 19.
Native ordered SCTP DATA stream 1, PPID 3, RC 100, ITU PCs 1/2, NI 2 and SI 3;
equal SRI-SM/MO/MT/ReportSMDeliveryStatus/AlertServiceCentre mix; no MAP retry.

The explicit `lan-bounded-v1` [socket profile](CAPACITY_SOCKET_PROFILE.md)
observed 425,984-byte send/receive buffers at both endpoints, and SCTP RTO
initial/minimum/maximum 300/200/1,000 ms. No SDK default, host sysctl, firewall,
Kafka configuration or topic-data file was manually changed. The operator
approved two temporary service windows; services were restored between attempts
and after the final run. A subsequent built-in Kafka health check returned success.
Dedicated VMs do not establish exclusive physical hypervisor resources.

## Negative Evidence And Validation Correction

The [first dedicated attempt](CAPACITY_MEASUREMENT_DEDICATED_ATTEMPT_20261112.md)
retains two low seconds and a 781-TPS minimum. The earlier
[shared-host pass](CAPACITY_MEASUREMENT_10K_20261112.md) remains non-qualifying.
Neither was overwritten or regraded. The combined LAN-profile rerun had no
measured SDK namespace T3 increase; this is not an association-only causal trace
or proof that any individual setting uniquely caused the improvement.

A startup ASP Active Ack retransmission at about 201.7 ms was byte-identical
in original frames 10/11. TShark's default TSN analysis suppressed its duplicate
payload, triggering a false changed-bytes rejection. Extraction now preserves
payloads and reads absolute TSNs; independent comparison still rejects changed
bytes, stream/PPID, empty payloads and malformed envelopes. Original PCAP bytes
and digests were not changed. This duplicate is retained, not filtered out.
Short recovery bounds can cause spurious retransmissions and are not a general
public-network recommendation.

## Qualification Boundary

The minimum margin is only 79 TPS (0.79%). This is a measured closed-loop capacity
result for this exact profile, not an open-loop HTTP SLA, leak-freedom claim or
guarantee after changing runtime, network, kernel, resources or candidate code.

`qualificationPassed=true` in this source/profile verifier is not release `GO`.
The stable capacity flag remains open because a deployed packed candidate and
authenticated package/source/scope-bound schema-2 envelope are still required.
The frozen defect ledger likewise remains conservative about final release
binding. Phase 65 owns that exact-candidate requalification. No NuGet publication,
tag or stable promotion is authorized by this result.

Next: Phase 62's real 72-hour multi-host resilience and fault-accounting evidence,
then representative cluster qualification (63), operator acceptance (64) and
final candidate release qualification (65). None is replaced by this 30-minute run.
