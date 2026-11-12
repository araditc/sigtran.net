# Long-Run MAP Resilience Qualification

Phase 62 is **in progress, not qualified**. Policy: `completed-map-endurance-v1`.
The [Phase 61 capacity pass](CAPACITY_QUALIFICATION_EXIT.md) is preserved and
does not substitute for three real days of continuous execution.

## Fixed Exit Criteria

One SDK process runs the five-operation ITU MAP SMS mix over native SCTP/M3UA/
SCCP/TCAP for at least 259,200 measured seconds on two identified Linux hosts.
Short diagnostics, stitched attempts, process restarts, missing final receipts
or documentation alone cannot satisfy this duration. Source/package-bound
stable release qualification remains separate and cannot be promoted here.

The versioned [policy](../eng/qualification/endurance-policy.json) fixes a
60-second durable checkpoint cadence, memory and artifact bounds, steady-load
target, nine incidents spanning three fault families, a 90-second recovery
budget, and zero unexpected failures or unresolved requests. A known injected
failure may produce explicitly counted terminal timeout/transport outcomes;
these are never silently discarded, retried or described as successful MAP work.
Unexpected errors/rejects/malformed results remain failures even during a fault.

The planned incidents are peer crash/restart, a held private peer outage, and
an SCTP-only partition restricted to the owned lab flow. Kafka TCP ports,
configuration and topic data are outside fault-injection scope. A 72-hour
Kafka maintenance window requires separate explicit owner approval; the prior
30-minute permission is not assumed to authorize three days of service outage.

## Ten Units

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Versioned duration/resource/fault contract and validated options | Complete; build/test/API/pack passed |
| 2 | 64-bit, bounded, exact admission-to-terminal ledger | Planned |
| 3 | Continuous runner, durable checkpoints and deadline drain | Planned |
| 4 | Independent long-running peer and crash-surviving counter evidence | Planned |
| 5 | Scoped incident control, recovery probes and rollback | Planned |
| 6 | Memory/heap/queue progression and bounded capture/log storage | Planned |
| 7 | Independent verifier, tamper rejection and release-boundary tests | Planned |
| 8 | Durable execution, watchdogs and authenticated host control | Planned |
| 9 | Real two-host short diagnostic of all incident families | Planned |
| 10 | Actual 72-hour run, restoration, verification and exit report | Pending real execution; cannot be claimed early |

Each unit includes code, behavioral tests, standard code documentation,
docs/README updates, build/test/API/pack and its own commit/push. Runtime
names describe the domain, never a phase number. Raw evidence is private;
only validated sanitized summaries and hashes are published.
