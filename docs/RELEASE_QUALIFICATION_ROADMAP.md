# Release Qualification Roadmap

Approved on 2026-11-04. This is the fixed continuation of phases 1-55.
Reconciled with `origin/main@981113c`: the historical Phase 56 M2PA/capacity
closure, admitted profile framework, M3UA HA and external-run tooling are
preserved. Phase numbers below identify this approved continuation; they do
not erase earlier documents using 56/57. Numeric capacity evidence already
exists; phase 61 qualifies the stricter declared workload/duration/hardware.
Completion means meeting acceptance criteria for the declared product profile.
Changing scope requires an explicit recorded decision; failed qualification is
not converted into a passing status by completing documentation.

## Product Scope

Linux and .NET 10; ITU signaling; native SCTP; M3UA and M2PA/MTP3;
connectionless SCCP; TCAP; the five existing MAP SMS operations.
DIAMETER, ANSI, connection-oriented SCCP, and other MAP services are outside v1.
The executable scope contract is `eng/qualification/scope.json`.

| Phase | Objective | Acceptance |
| --- | --- | --- |
| 56 | Qualification baseline | Bounded scope, defect ledger, multi-host tooling, authenticated evidence evaluation, real lab inventory |
| 57 | Protocol wire conformance | Correct SCCP/TCAP/MAP encoding, independent vectors, dialogue negotiation and malformed-input tests |
| 58 | Native SCTP completion | Multi-address connection, kernel notifications, message boundaries, path failover under traffic |
| 59 | MTP3 and M2PA runtime | MTP3 over IMtp2Link, linksets/routesets, SLS, network management, changeover/changeback, independent traffic |
| 60 | External interoperability | Separately maintained peer or vendor stack; positive/negative M3UA and MAP SMS scenarios; mismatches corrected |
| 61 | Capacity qualification | Owner-approved current target: 10K completed transactions/second for 30 minutes on declared hardware/workload; latency/error/resource report; 20K deferred |
| 62 | Long-run resilience | At least 72 hours; bounded memory/queues; network/process failures; every outstanding request accounted for |
| 63 | Deployment qualification | Representative Kubernetes SCTP network, readiness/drain, pod/node failure, upgrade and rollback |
| 64 | Operator acceptance | Documented STP/HLR/SMSC or vendor profile, controlled pilot, accepted operations and support boundaries |
| 65 | Release qualification | Final API compatibility, migration, SBOM, provenance, approved signing, durable artifacts and protected publication |

Each phase has ten reviewable units. Each unit includes implementation,
behavioral tests, standard code documentation, docs/README updates,
build/test/pack, and its own commit/push. Runtime names describe the domain,
never phase numbers or commercial-readiness terminology.

Phase 61's [historical 20K-target measurement](CAPACITY_MEASUREMENT_20261111.md)
is retained: 12,135.85 average completed TPS, minimum one-second TPS 1,209 and
a shared peer did not qualify that run. Its average exceeds the new 10K target,
but does not retroactively qualify the candidate, per-second windows or hosts.
Ten delivered implementation units do not constitute capacity qualification.
On 2026-11-11 the owner [revised the current target to 10K](CAPACITY_TARGET_DECISION.md).
New-candidate evidence is required; the original 20K result above is historical.

The [2026-11-12 rerun](CAPACITY_MEASUREMENT_10K_20261112.md) tested
`324f1932cc7820335599c2e7b70e60ada32de4d9` for the full 1,800 seconds without
a profiler: 25,590,881 successful in-window transactions, 14,217.16 average TPS,
minimum 10,026 and zero sub-target seconds or failures/unresolved work.
The [stall diagnosis](CAPACITY_STALL_DIAGNOSIS_20261112.md) retains the original
29-low-second baseline, interrupted collection and rejected experiments.
That shared-host report closed only its numerical regression, not the dedicated
criterion. A later [first dedicated attempt](CAPACITY_MEASUREMENT_DEDICATED_ATTEMPT_20261112.md)
retained two low seconds (minimum 781) and was not silently promoted.

Phase 61 is now [complete for the tested source/hardware/profile](CAPACITY_QUALIFICATION_EXIT.md).
The [dedicated LAN-profile rerun](CAPACITY_MEASUREMENT_DEDICATED_20261112.md)
tested `947d69abd081449cc0e2c286409eeab69a659ab7`: 26,599,474 successful
in-window transactions, average 14,777.49 TPS, minimum 10,079, zero low
seconds/failures and 3,551 re-decoded MAP pairs. Owner-approved service
quiescence and original-state/hash restoration were verified. The margin is
only 79 TPS and the per-socket profile is explicit; no general SLA is implied.
20K is not a current blocker. Next is Phase 62's actual 72-hour qualification.
Phases 63-65 and final package/source/scope-bound trusted release qualification
remain mandatory; a source-build pass does not close the stable release gates.

The initial estimate is 12-20 engineering person-weeks, excluding external
access and acceptance waiting time; phase 56 records the actual dependencies.
No fixed completion date is implied by that estimate.
