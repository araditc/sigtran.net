# SIGTRAN.NET Roadmap Execution

This document is the durable execution checkpoint for the owner-approved SDK roadmap. It complements the historical phase documents without renumbering completed phases.

## Canonical baseline

- Historical roadmap-activation baseline: `main` at `b60dfdf621aff17ff3fa2816d3d40e5730155c3c`; this SHA is retained only as history and must not be treated as the current branch head.
- Current canonical `main` at this checkpoint: `ee9fda83930db82419174301d76306deafb155c4`; every execution must reconcile the live branch before using this pointer.
- Latest immutable public prerelease: `Sigtran.NET 1.0.0-rc.2`.
- Public tag `v1.0.0-rc.2` resolves to source commit `e2c663460823cd29f073467a79c4f761fb7c1002` and must not be retagged or overwritten.
- Current unpublished source-development package identity: `1.0.0-rc.3-dev`.
- Stable target: `1.0.0`.
- Stable decision: `NO-GO` until the source-controlled evaluator returns `GO` with retained evidence.
- Closed Phase 56 qualification gates: independent M2PA and numeric 20K TPS capacity.
- Open required stable gates: `operator-profile`, `multi-host-soak`, `kubernetes-sctp`, and `trusted-signing`.

The stable manifest in `eng/release/stable-release.json` and `eng/evaluate-stable-release.ps1` remain authoritative when prose and evidence disagree.

## Stable 1.0 scope lock

The 1.0 protocol surface is frozen to:

- native Linux SCTP;
- M3UA;
- M2PA and MTP3 layer boundaries;
- connectionless SCCP;
- TCAP dialogue management;
- the current five-operation MAP SMS service profile;
- production health, telemetry, configuration, packaging, and release controls required by those layers.

The following are explicitly post-1.0 work and must not expand the 1.0 candidate: SCCP classes 2/3 and connection-oriented messages, broader MAP mobility/authentication/interrogation operations, SUA, M2UA, and IUA.

Source changes after the published RC.2 commit must not be republished under the `1.0.0-rc.2` identity. Ordinary source builds use the unpublished `1.0.0-rc.3-dev` identity until a later governed release candidate is explicitly admitted.

## Approved execution milestones

| Milestone | Scope | Exit condition |
| --- | --- | --- |
| 57A — baseline reconciliation | Correct stale release/readiness claims, lock 1.0 scope, move development package identity beyond published RC.2. | Documentation and package metadata agree with the stable manifest and immutable RC.2 evidence. |
| 57B — operator profile framework | Typed fail-closed network/SCCP/TCAP/MAP SMS profile configuration built on existing protocol primitives. | Positive/negative CI tests pass; no operator acceptance claim is made without external evidence. |
| Multi-association M3UA HA runtime | AS/ASP/association pools, route sets, protocol traffic modes, SLS-aware distribution, drain/fencing, health routing, bounded backpressure, diagnostics. | Deterministic fault tests pass and ambiguous send outcomes are explicit. |
| Multi-host qualification | Representative separate-host SCTP traffic with failure, recovery, soak, queue, memory, latency, loss/ambiguity and dialogue checks. | Digest-covered retained evidence closes `multi-host-soak`. |
| Kubernetes SCTP qualification | Representative cluster/CNI or documented host-network profile with SCTP, policy/firewall, probes, drain, restart, update and rollback validation. | Digest-covered retained evidence closes `kubernetes-sctp`. |
| Stable signing and 1.0 | CA-issued organization signing identity, timestamp verification, SBOM/provenance/API evidence and protected publication. | All required manifest gates pass, evaluator returns `GO`, protected approvals succeed, stable public restore verifies. |
| Post-1.0 SCCP CO | SCCP classes 2/3 and connection-oriented state machine/messages. | Standards vectors, state/timer/flow-control tests and independent interoperability evidence. |
| Post-1.0 MAP core | Mobility, authentication and subscriber interrogation operations in a modular MAP package surface. | Standards-based codecs/workflows and synthetic/authorized-lab evidence. |
| Post-1.0 adaptation suite | SUA, then M2UA, then IUA as reviewed modules. | Each module has protocol tests, package boundary review and independent interoperability evidence. |
| Protocol TestKit | Deterministic peer/clock/fault/vector/trace tooling extracted from existing labs. | Reusable consumer-facing testing package and executable scenarios. |
| Defensive robustness | Parser/state/resource fuzzing and bounded-failure invariants. | No crash, unbounded allocation, parser loop, buffer over-read or leaked protocol state in retained campaigns. |
| Low-allocation pipeline | Measured buffer ownership/copy reduction without correctness regressions. | Published benchmark methodology and evidence; any allocation target remains empirical, not a release promise. |

## Evidence rules

Repository simulations and independently compiled in-repository peers prove only the tested repository profile. They do not constitute operator/vendor acceptance. Operator topology, real subscriber data, authentication material, secrets, private keys and unredacted operator traces must not be committed to this public repository.

A stable gate is promoted only when the exact candidate has retained, digest-covered evidence satisfying the manifest. A passing build, an administrative issue closure, or a readiness DTO alone is not evidence of production qualification.

## Current checkpoint

- Parent roadmap tracker: GitHub issue `#12`; Kubernetes qualification tracker: reopened issue `#7`.
- Canonical admitted branch before this package is `main@58da5b80e47bce59ba510de89f02411e6f04df9c`, the merge of PR `#30`.
- 57A baseline/scope-lock and 57B SDK profile-framework implementation remain admitted. The separate stable `operator-profile` gate remains **EXTERNAL-BLOCKED** until genuine authorized operator/vendor acceptance evidence is retained.
- **Milestone C — Multi-association M3UA HA runtime remains VERIFIED-DONE for deterministic repository implementation.**
- **Milestone D — Representative Multi-Host Qualification tooling is admitted through PR `#30`.** The stable `multi-host-soak` gate remains **OPEN / EXTERNAL-BLOCKED** because the required representative separate-host duration/fault matrix has not yet been executed and retained.
- The admitted Milestone D tooling provides bounded-memory timed profiles, protected distinct-host orchestration, source-bound evidence, peer restart/outage/SCTP-partition scenarios, independent rollback, private SSH/evidence handling, authenticated peer endpoint ownership, and a bounded failover/recovery capture handshake. It does not substitute repository simulation for host-loss, delay/loss, route-withdrawal, or release-duration evidence.
- **Active implementation package: Milestone E — Representative Kubernetes SCTP Qualification**, tracked by PR `#31` on branch `kubernetes-sctp-qualification`.
- The Milestone E tooling requires digest-pinned image identity, exact source/run-attempt binding, private kubeconfig/evidence scratch, explicit `cni` versus `hostNetwork` profiles, retained Kubernetes/CNI/node identity, observed SCTP association rows, initial/final liveness/readiness, pod replacement, runtime image digest, and rollout/rollback evidence.
- Sanitized Kubernetes PASS is derived from structured observed evidence rather than hard-coded booleans. Raw topology/cluster evidence remains protected. Failed validator and earlier capture-stage runs are retained through an `always()` finalizer after private scratch creation; public sanitized evidence is eligible only when the qualification job itself succeeds and the complete source-bound matrix is gate-eligible.
- Repository CI for the Kubernetes package is synthetic/offline validation only. It does **not** close `kubernetes-sctp`.
- Required representative Kubernetes matrix rows still include NetworkPolicy/firewall behavior, graceful termination/drain, and node drain/rescheduling. Those results must come from an authorized representative cluster and cannot be inferred from pod restart or rollout tests.
- Trusted stable signing also remains **OPEN / EXTERNAL-BLOCKED** pending an organization-approved CA-issued identity. The rejected self-issued certificate is not accepted as stable author signing.
- The stable evaluator remains **NO-GO** with exactly four required open gates: `operator-profile`, `multi-host-soak`, `kubernetes-sctp`, and `trusted-signing`.
- Stable publication remains prohibited until retained evidence closes every required gate, the machine evaluator returns `GO`, applicable independent/protected approvals are current, and the exact release request is authorized.

## Next dependency-valid action

Complete PR `#31` only after exact-head CI, fresh Codex review, fresh independent review, zero substantive review threads, and mergeability are all current. After guarded merge and post-merge verification, continue the bounded Kubernetes representative-matrix implementation without promoting `kubernetes-sctp` until actual authorized-cluster evidence is retained. Safe repository work may continue around external evidence blockers; stable publication remains blocked.
