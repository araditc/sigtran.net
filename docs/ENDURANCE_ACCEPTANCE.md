# Scoped Endurance Evidence Acceptance

The product owner explicitly accepted the risk of the seven missing managed
readings on 2026-11-17 and authorized six implementation units without a new
72-hour run. **Independent review remains outstanding.** Owner risk acceptance
is not a final acceptance exception, an original verifier pass or stable GO.

## Bound Dossier

`eng/qualification/deviations/endurance-managed-memory.json` pins the rejected
run, measured source, unchanged SDK implementation tree, original raw inventory,
seven exact signed values/timestamps and three supporting report hashes.
`scripts/evidence-deviation.py` audits those public records. It explicitly does
not claim to have replayed private artifacts merely by reading public metadata.
The original signed observations, strict rejection and failed exit remain intact.
An unchanged source tree is not an identical final package attestation.

## Delivery Units

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Immutable evidence dossier and source-tree comparison | Complete; build/test/API/pack and commit/push passed |
| 2 | Exact-scope owner risk acceptance | Complete; build/test/API/pack and commit/push passed |
| 3 | Independent review contract and request | Complete; build/test/API/pack and commit/push passed; human approval OPEN |
| 4 | Separate fail-closed administrative assessment | Implemented; actual result PENDING_INDEPENDENT_REVIEW |
| 5 | Tamper rejection and CI boundaries | Pending |
| 6 | Final report, docs and handoff | Pending |

The strict endurance/release policies are not changed by this work. No VM load,
Kafka control, package publication or stable promotion is authorized here.

## Owner Decision

The `.owner.json` record transcribes the explicit owner instruction from this
conversation and binds the exact dossier SHA-256. The authenticated local
GitHub account was observed as `ammarheidari`; no token is stored. The record
does not pretend to be a signed GitHub review or an independent approval.
It accepts only the seven unknown readings, not an instrumentation-only cause.
Changing scope, evidence or SDK implementation, finding an additional failure,
recurring negative readings or reviewer rejection reopens the risk decision.
Repository attributes preserve these decision bytes across OS checkouts.

The [independent review request](ENDURANCE_REVIEW_REQUEST.md) names the review
questions and authenticated approval path. Its trust roster is deliberately
empty and its PR reference is null. Preparing this contract does not perform
the human review; the owner must designate that independent reviewer later.

## Administrative Assessment

```text
python scripts/assess-evidence-deviation.py
python scripts/assess-evidence-deviation.py --verify-review
```

Exit 3 means review pending, 1 means ineligible, and 0 means scoped
administrative acceptance only. Both commands currently return pending without
network access because the reviewer/PR are unassigned. Live verification uses
read-only GitHub PR/review/file APIs, a roster already on the base branch,
exact reviewed commit/files/bundle digest, human identity and a stated rationale.
Expired, dismissed, changed or owner/self approvals fail closed. No SDK API,
strict original verifier or stable release evaluator is modified.
