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
