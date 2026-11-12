# Capacity Target Decision

Approved by the product owner on **2026-11-11** in the project conversation.
Current policy: `completed-map-10k-v1`, recorded in
[`capacity-policy.json`](../eng/qualification/capacity-policy.json).

The current v1 target is **10,000 successful complete MAP transactions per
second for 1,800 measured seconds**, at least 18,000,000 complete transactions.
The five-operation mix, one-second window accounting, native SCTP, zero failures,
zero unresolved work and dedicated two-host qualification are unchanged.
An invoke plus its validated correlated Result counts once; queued sends, raw
protocol messages, warmup and drain do not count as additional transactions.

20,000 TPS is future optimization, not the current v1 release blocker. No old
20K report, trace, source binding or failed decision is rewritten or silently
regraded. New evidence must declare this policy and the exact new candidate.
The change does not waive operator acceptance, resilience, Kubernetes, signing,
provenance or other mandatory release gates. Shared-peer measurements remain
explicitly diagnostic, even when numeric throughput meets 10K.

The legacy stable manifest also reopens capacity instead of inheriting its old
aggregate pass. CI checks the actual named failed gates and their blockers,
not the former hard-coded count of four historical gaps. Missing evidence still
produces NO-GO; changing the threshold never substitutes for a measured run.

The [first new-policy 30-minute run](CAPACITY_MEASUREMENT_10K_20261111.md)
exceeded the average target at 12,356.65 completed TPS with zero failures, but
did not qualify: 29 one-second windows were below 10K and the peer was shared.
Reducing the target does not silently replace the per-second criterion with
an average-only criterion. The latter would require a separate owner decision.

The [2026-11-12 rerun](CAPACITY_MEASUREMENT_10K_20261112.md) met the original
per-second criterion without relaxing it: 1,800 measured seconds, minimum
10,026 TPS, zero low seconds and zero failed/unresolved work. Source/profile
conditions and the shared-peer restriction are explicit. That report alone
does not qualify dedicated hosts or authorize stable release.

The later [dedicated-window rerun](CAPACITY_MEASUREMENT_DEDICATED_20261112.md)
meets the same unrelaxed criterion: minimum 10,079 TPS across all 1,800 seconds,
zero failed/unresolved work, and approved quiescence/restoration verified.
The [Phase 61 exit](CAPACITY_QUALIFICATION_EXIT.md) is limited to its exact
source/hardware/socket/runtime profile. Final packaged-candidate trust,
endurance, cluster and operator gates remain open. No old result was regraded.
