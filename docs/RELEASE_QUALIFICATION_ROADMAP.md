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
| 61 | Capacity qualification | 20K completed transactions/second for 30 minutes on declared hardware/workload, latency/error/resource report |
| 62 | Long-run resilience | At least 72 hours; bounded memory/queues; network/process failures; every outstanding request accounted for |
| 63 | Deployment qualification | Representative Kubernetes SCTP network, readiness/drain, pod/node failure, upgrade and rollback |
| 64 | Operator acceptance | Documented STP/HLR/SMSC or vendor profile, controlled pilot, accepted operations and support boundaries |
| 65 | Release qualification | Final API compatibility, migration, SBOM, provenance, approved signing, durable artifacts and protected publication |

Each phase has ten reviewable units. Each unit includes implementation,
behavioral tests, standard code documentation, docs/README updates,
build/test/pack, and its own commit/push. Runtime names describe the domain,
never phase numbers or commercial-readiness terminology.

Phase 61's [real 30-minute measurement](CAPACITY_MEASUREMENT_20261111.md)
is retained, but the capacity gate is **not qualified**: 12,135.85 completed TPS
and a shared peer do not meet the fixed target. Ten delivered implementation
units must not be interpreted as an accepted capacity qualification.

The initial estimate is 12-20 engineering person-weeks, excluding external
access and acceptance waiting time; phase 56 records the actual dependencies.
No fixed completion date is implied by that estimate.
