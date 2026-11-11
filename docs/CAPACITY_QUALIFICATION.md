# Completed MAP Transaction Capacity

Phase 61 qualifies one Linux SDK process, native SCTP/M3UA, connectionless
SCCP, TCAP dialogue negotiation and the five typed MAP SMS operations.
The fixed target is 20,000 successful **completed** transactions per second
for 1,800 consecutive measured seconds on two declared dedicated hosts.
That is at least 36,000,000 valid correlated results, not queue admissions,
SCTP sends, M3UA echoes or HTTP requests. The workload is an equal round-robin
mix of SRI-SM, MO-ForwardSM, MT-ForwardSM, ReportSMDeliveryStatus and
AlertServiceCentre, using synthetic subscriber data and three-byte TPDUs.

## Ten Units

| Unit | Implementation and acceptance | Status |
| --- | --- | --- |
| 1 | Fixed target, bounded options and diagnostic separation | Implemented; unit verification retained locally |
| 2 | Measured completion ledger and per-second windows | Implemented; boundary and reconciliation tests |
| 3 | Fixed workers, warmup exclusion and complete drain accounting | Implemented; drain/cancel/failure tests |
| 4 | Bounded latency and resource observations | Implemented; quantile and overflow tests |
| 5 | Native full-stack session with generation-fenced sends | Implemented; MAP outcome classification tests |
| 6 | Independent C peer counters and machine-readable summary | Implemented; independent fixtures compiled in CI |
| 7 | Two-host inventory, execution and shared-host safety | Pending |
| 8 | Source-bound report verification and tamper tests | Pending |
| 9 | Real Linux execution and mismatch correction | Pending |
| 10 | Final evidence, CI and remaining-gate report | Pending |

Each unit runs build, SDK/qualification tests, public API compatibility and
pack before its own commit/push. Names in runtime code describe the domain.

## Qualification Boundary

Warmup and drain are outside measured TPS. Only validated MAP results count.
Error, Reject, timeout, cancellation and transport failure remain visible.
No automatic retry is permitted. Every admitted invoke must have exactly one
terminal accounting entry. Late successes must not inflate measured TPS.
Every complete one-second window must meet the target; an average alone does
not hide stalls. Latency percentiles and CPU/memory/allocation report the
declared workload and hardware, not an unconditional performance promise.

Short, local or shared-host runs are diagnostic and cannot close the dedicated
two-host gate. Historical aggregate/single-host capacity remains separate.
Kafka services, configuration and ports must never be changed by this runner.
The measured interval is half-open: a result at exactly 1,800 seconds belongs
to drain. Accounting arrays are fixed by the five operations, eight terminal
outcomes and measured seconds, independent of the number of transactions.
The stable evaluator and release policy are not automatically promoted.

## Execution Model

The new capacity path is separate from historical PerformanceLab stages.
It uses bounded closed-loop concurrency: one Task per worker, one outstanding
MAP invoke per worker, and no retry. This measures saturation capacity rather
than an open-loop arrival SLA; it must not claim coordinated-omission-free
latency under overload. Warmup uses a separate ledger. Admission stops using
the monotonic clock, while the in-flight set drains for at most ten seconds.
External cancellation and drain cancellation remain counted failures.
The capacity path uses 100,001 latency counters, not one retained sample per
transaction. P50/P95/P99 are bucket upper bounds at 0.1 ms resolution up to
10 seconds; overflow quantiles conservatively use the observed maximum.
Only successful results inside measurement enter latency. CPU is process CPU
normalized to available logical processors; memory is sampled working set.
Process-wide allocations and GC counts include the bounded drain, and are not
presented as isolated codec allocations or a managed-heap-only measurement.

## Native Workload

`Sigtran.NET.PerformanceLab capacity` opens one native association: ordered
M3UA DATA stream 1, PPID 3, eight negotiated streams, ITU PCs 1/2, NI 2,
SCCP SI 3 and Routing Context 100. It invokes public `IMapSmsService`
correlated operations directly, not the HTTP ingress or a hand-built TCAP
loop. All TCAP events, unsolicited invokes and SCCP returns have owned readers.
Unexpected work, malformed input, event drops, reconnects and faults prevent
execution PASS. The final stack totals must match every admitted dialogue.
`sdk.json` is created once and retains warmup and measurement separately.
Numeric target and protocol execution are distinct; this file alone cannot
prove dedicated topology, peer counts, wire validity or release qualification.

The independent C executable accepts `IP PORT 0 capacity /private/peer.json`.
It validates the protocol envelopes, MAP arguments, ordered stream 1, network
byte-order PPID 3, Routing Context 100 and declared point codes. It counts
each accepted request/response by operation, without per-request logging.
After orderly SDK closure it creates a new JSON summary with PID, process CPU
seconds and peak RSS. Signal termination, invalid metadata or partial input
cannot emit a passing summary. These counters corroborate SDK totals; peer
send acceptance alone is not proof of SDK application completion.
