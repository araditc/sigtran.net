# SIGTRAN.NET Roadmap Execution

This document is the durable execution checkpoint for the owner-approved SDK roadmap. It complements the historical phase documents without renumbering completed phases.

## Canonical baseline

- Activation baseline: `main` at `b60dfdf621aff17ff3fa2816d3d40e5730155c3c`.
- Latest immutable public prerelease: `Sigtran.NET 1.0.0-rc.2`.
- Public tag `v1.0.0-rc.2` resolves to source commit `e2c663460823cd29f073467a79c4f761fb7c1002`.
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

Source changes after the published RC.2 commit must not be republished under the `1.0.0-rc.2` identity. Ordinary source builds move to an unpublished development prerelease identity until a later governed release candidate is explicitly admitted.

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

- Parent tracker: GitHub issue `#12`.
- Active work package: GitHub issue `#13` — 57A/57B baseline reconciliation and operator profile foundation.
- Next dependency after 57B: multi-association M3UA HA runtime.
- External prerequisites are allowed to block only their dependent qualification step; safe independent SDK development continues.
