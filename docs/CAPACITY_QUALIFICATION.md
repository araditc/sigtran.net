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
| 3 | Fixed workers, warmup exclusion and complete drain accounting | Pending |
| 4 | Bounded latency and resource observations | Pending |
| 5 | Native full-stack session with generation-fenced sends | Pending |
| 6 | Independent C peer counters and machine-readable summary | Pending |
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
