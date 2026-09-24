# SIGTRAN.NET SDK Roadmap

This document is the current high-level roadmap for SIGTRAN.NET. Detailed historical phase records remain in [Phase Index](PHASE_INDEX.md) and the linked phase documents; the active execution checkpoint, acceptance boundaries, and dependency order are maintained in [Roadmap Execution](ROADMAP_EXECUTION.md).

The roadmap is grounded in the repository's protocol references, including RFC 4666 for M3UA, RFC 4165 for M2PA, RFC 9260 for SCTP, the applicable ITU-T/ETSI SS7/SIGTRAN material, and the MAP/TCAP profile references already cited by the protocol documents.

## Current Assessment

SIGTRAN.NET has moved beyond proof-of-concept status into a public release-candidate SDK with stateful SCTP/M3UA/M2PA/SCCP/TCAP/MAP SMS runtime surfaces, retained Linux SCTP and independent peer evidence, release governance, public NuGet prerelease publication, and production-oriented operations tooling.

The latest immutable public prerelease is `Sigtran.NET 1.0.0-rc.2`. Source development after that release uses unpublished package identity `1.0.0-rc.3-dev`; source changes must not be republished under the RC.2 identity. Stable `1.0.0` remains governed by `eng/release/stable-release.json` and `eng/evaluate-stable-release.ps1`.

Phase 56 closed two qualification gates:

- independent RFC 4165 M2PA interoperability;
- the numeric 20K TPS capacity target using controlled two-association load-share evidence.

Four required stable gates remain open:

1. `operator-profile` — authorized operator/vendor SCCP/TCAP/MAP SMS acceptance evidence;
2. `multi-host-soak` — representative separate-host long-duration soak/failover evidence;
3. `kubernetes-sctp` — representative Kubernetes SCTP/CNI deployment evidence;
4. `trusted-signing` — organization-approved CA-issued stable package signing identity.

The current machine-evaluated stable decision is therefore `NO-GO`. The closed numeric capacity gate must not be generalized into a broad multi-host or operator capacity claim.

## Stable 1.0 Scope Lock

The stable 1.0 protocol surface is intentionally constrained to the already established stack:

- native Linux SCTP transport;
- M3UA codec/runtime and MTP3 network boundary;
- M2PA runtime and MTP2 link boundary;
- connectionless SCCP service for UDT/XUDT/LUDT and service-return behavior;
- TCAP dialogue/component management;
- the current five-operation MAP SMS profile;
- diagnostics, health, metrics, configuration, packaging, evidence, and release controls required by those layers.

The following capabilities are explicitly post-1.0 and do not expand the current stable candidate:

- SCCP connection-oriented classes 2/3 and CR/CC/CREF/RLSD/RLC/DT1/DT2/AK/IT/ERR/RSR/RSC flows;
- broader MAP mobility, authentication, and subscriber-interrogation operations;
- SUA, M2UA, and IUA adaptation modules;
- a separately consumable protocol TestKit;
- larger fuzzing campaigns and measured zero/low-copy optimization beyond the release-candidate baseline.

## Completed Foundation — Phases 0–56

Phases 0–56 established the SDK foundation and production-readiness machinery. The canonical per-phase status is maintained in [Phase Index](PHASE_INDEX.md), with the individual phase documents retaining design rationale and evidence references.

The completed foundation includes:

- .NET 10 package and protocol primitives;
- M3UA message families, routing, ASP lifecycle, RKM, diagnostics, and production runtime;
- native Linux SCTP transport with stream/PPID metadata, reconnect, queue metrics, graceful shutdown, and retained peer traffic;
- RFC 4165 M2PA codec/runtime with retained independent C/lksctp evidence;
- MTP3 routing boundaries;
- connectionless SCCP UDT/XUDT/LUDT, GT translation, bounded segmentation/reassembly, route policy, and UDTS handling;
- TCAP BER, dialogue state, concurrent transaction/invoke correlation, outcomes, timeouts, and cleanup;
- MAP SMS SRI-SM, MO/MT ForwardSM, ReportSM-DeliveryStatus, AlertServiceCentre, typed client/server workflows, errors, extensions, and operation profiles;
- cross-implementation full-stack MAP SMS traffic through native SCTP/M3UA/SCCP/TCAP;
- production health, telemetry, structured events, configuration validation, container/Kubernetes manifests, and operations runbooks;
- release automation, SBOM/provenance/API-baseline controls, protected publication gates, NuGet Trusted Publishing for RC.2, and stable `GO/NO-GO` evaluation;
- controlled performance/resilience evidence plus the Phase 56 numeric 20K TPS gate closure.

Historical phase text must be interpreted through the latest retained evidence. Older statements saying independent M2PA or the numeric 20K TPS gate are still open are superseded by Phase 56 evidence and the source-controlled stable manifest.

## Phase 57 — Approved Roadmap Execution And 1.0 Scope Lock

Phase 57 is the active roadmap-execution phase. It does not renumber prior history.

### 57A — Baseline Reconciliation

Goals:

- keep README, readiness, phase index, changelog, and source package identity aligned with retained evidence;
- preserve public RC.2 as immutable evidence while using an unpublished development identity for later source changes;
- freeze the 1.0 scope above;
- make the four remaining stable gates explicit and machine-verifiable;
- prevent administrative issue closure or repository-only simulation from being treated as production evidence.

Exit criteria:

- current-status documentation agrees with the stable manifest;
- ordinary development pack output cannot collide with public `1.0.0-rc.2` or stable `1.0.0`;
- exact-head CI verifies the development package identity;
- the roadmap checkpoint names the next dependency-valid work package.

### 57B — Operator Profile Framework

Goals:

- provide typed, validated network/SCCP/TCAP/MAP SMS profile configuration for the currently implemented protocol surface;
- reuse existing SCCP global-title translation and MAP SMS operation-profile primitives rather than creating competing policy models;
- validate the implemented ITU 14-bit point-code boundary, two-bit network indicator, global-title translation rules, MAP SMS operation allowlists, application contexts/timeouts, and strict compatibility behavior;
- fail closed for unsupported variants and ambiguous settings;
- provide deterministic validation findings suitable for configuration tooling and automated tests.

The first implementation slice deliberately supports only the capabilities already implemented by the 1.0 stack. Declaring ANSI-style point-code formats, permissive compatibility behavior, unsupported MAP operations, or conflicting routing rules must fail explicitly rather than silently approximating the peer profile.

A valid SDK profile is not operator acceptance evidence. The `operator-profile` stable gate closes only after authorized traffic is retained against a real operator/vendor profile with sanitized configuration, trace/PCAP comparison, peer observations, deviation classification, and digest coverage.

## Multi-Association M3UA HA Runtime

After the operator-profile foundation is admitted, the next runtime expansion builds carrier topology composition around the existing `M3uaRuntime` and official lower-layer contracts.

Planned capabilities:

- application-server, ASP/association-pool, and route-set composition;
- active/standby and protocol-defined Loadshare/Override/Broadcast behavior;
- multiple signalling gateways and routing-context membership;
- SLS-aware distribution that preserves ordering requirements;
- health-aware association selection, graceful drain, reconnect coordination, and fencing;
- bounded backpressure and per-association telemetry;
- explicit ambiguous-send outcomes rather than an unsupported exactly-once promise;
- deterministic fault tests before external qualification.

Transport or peer failure after dispatch must never cause blind replay of an operation whose acceptance state is unknown.

## Representative Multi-Host Qualification

The separate `multi-host-soak` gate requires genuine separate-host evidence. Multiple associations on one machine are useful controlled capacity evidence but are not multi-host qualification.

The qualification track covers:

- process and peer restart;
- association reset and host loss;
- delay, loss, network partition, and route withdrawal/recovery;
- queue/backpressure recovery and graceful drain;
- duplicate, ambiguous, lost, and orphaned transaction/dialogue observations;
- CPU, memory, allocation, latency, throughput, and failover timing.

The executable evidence ladder is a 15-minute smoke, one-hour stress, six-hour soak, and 24-hour release-grade run. The PerformanceLab uses a time-based, bounded-memory soak path for the long tiers; total operation count is measured rather than preallocated. Exact source SHA, topology, fault scenario and duration are retained with every run. The gate closes only with digest-covered retained artifacts from an authorized representative environment and review of the required fault matrix. See `MULTI_HOST_QUALIFICATION.md`.

## Representative Kubernetes SCTP Qualification

The `kubernetes-sctp` gate validates deployment behavior, not only manifest syntax.

Required qualification areas include:

- Linux kernel SCTP support;
- CNI or explicitly documented `hostNetwork` SCTP behavior;
- NetworkPolicy/firewall/service path behavior;
- startup, liveness, and readiness probes;
- graceful termination and traffic drain;
- rolling update and rollback;
- pod and node restart, node drain, rescheduling, and disruption policy;
- reconnect behavior and retained metrics/logs.

A local manifest test or non-representative development cluster cannot close this stable gate.

## Trusted Stable Signing And `1.0.0`

Trusted Publishing/OIDC authenticates package publication; it is not a substitute for the stable author-signing identity required by repository policy.

The existing Phase 56 preflight retained that the configured signing certificate is self-issued and therefore rejected. Stable publication requires an organization-approved CA-issued code-signing certificate, trusted timestamp verification, complete package/SBOM/provenance/API evidence, a `GO` machine decision, protected environment approval, exact tag/commit binding, publication, and verified public restore.

No stable tag or package may be created while any required manifest gate remains false.

## Post-1.0 — SCCP Connection-Oriented

Add SCCP classes 2/3 and the connection-oriented state machine/messages required for CR, CC, CREF, RLSD, RLC, DT1, DT2, AK, IT, ERR, RSR, and RSC, including local references, sequencing, timers, reset, and flow control.

Exit requires standards vectors, deterministic state/timer tests, bounded resource behavior, and independent interoperability evidence before broad support claims.

## Post-1.0 — MAP Core Expansion

Expand MAP modularly beyond the SMS profile, initially targeting:

- `updateLocation`;
- `cancelLocation`;
- `insertSubscriberData`;
- `deleteSubscriberData`;
- `purgeMS`;
- `sendAuthenticationInfo`;
- `provideSubscriberInfo`;
- `anyTimeInterrogation`.

Encoding, application contexts, errors, and workflow behavior must be based on primary standards and synthetic or explicitly authorized lab evidence. Real subscriber identifiers, authentication vectors, and unauthorized network queries must never be used as convenience test data.

## Post-1.0 — Modular SIGTRAN Adaptations

Add independent modules in dependency order:

1. SUA — RFC 3868;
2. M2UA — RFC 3331;
3. IUA — RFC 4233.

Each module requires reviewed package boundaries, protocol/vector tests, interoperability evidence, and compatibility/migration documentation. Partial support must not be described as a complete SIGTRAN suite.

## Post-1.0 — Protocol TestKit

Extract reusable deterministic test capabilities from the existing lab tooling:

- deterministic transport and clock;
- SG/ASP/MAP simulations;
- fault injection;
- golden protocol vectors;
- sanitized PCAP replay and trace comparison;
- executable protocol scenarios.

Small deterministic helpers may be introduced earlier when they are required to prove an active work package, but the consumer-facing TestKit remains a separate post-1.0 deliverable.

## Post-1.0 — Defensive Robustness And Fuzzing

Exercise malformed lengths/TLVs, SCCP pointers and segmentation, BER nesting and length bounds, dialogue/invoke exhaustion, queue pressure, and replay/duplicate behavior in isolated owned test environments.

Acceptance invariants include:

- no process crash;
- no unbounded CPU or memory growth;
- no parser loop or buffer over-read;
- bounded queues and state;
- no leaked dialogue, invoke, or reassembly context after failure cleanup.

Targeted negative tests remain part of normal feature work and are not postponed until the large fuzzing campaign.

## Post-1.0 — Measured Low-Allocation Pipeline

Optimize only after measuring the active workload. Candidate techniques include `ReadOnlySequence<byte>`, `IBufferWriter<byte>`, pooled buffers, `MemoryPool<byte>`, reduced intermediate arrays, and tighter buffer-lifetime ownership across SCCP/TCAP/MAP paths.

The previously discussed sub-4-KB allocation figure is an optimization objective, not a release promise. Every optimization report must publish workload, runtime/hardware/topology, allocation, throughput, and latency together, and must not trade protocol correctness or maintainability for one synthetic number.

## Evidence And Claim Discipline

For all roadmap stages:

- source-head changes invalidate earlier exact-head CI/review evidence;
- external qualification claims require the exact tested candidate and retained digest-covered artifacts;
- in-repository or independently compiled local peers prove only the tested profile, not broad vendor certification;
- raw subscriber data, operator topology, secrets, private keys, authentication vectors, and unredacted operator traces do not belong in this public repository;
- a passing build, readiness DTO, issue closure, or generated manifest is not production qualification by itself;
- stable gate changes are made only from real evidence and the machine evaluator.

## Current Execution Pointer

Use [Roadmap Execution](ROADMAP_EXECUTION.md) for the current work package, dependency state, and acceptance checkpoint. Use [Phase Index](PHASE_INDEX.md) to navigate historical phase documents and retained evidence.
