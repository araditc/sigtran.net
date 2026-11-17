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
| 4 | Separate fail-closed administrative assessment | Complete; build/test/API/pack and commit/push passed; actual status pending |
| 5 | Tamper rejection and CI boundaries | Complete; focused tests, build/test/API/pack and commit/push passed |
| 6 | Final report, docs and handoff | Complete implementation; independent human approval remains OPEN |

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
empty. [Review PR #38](https://github.com/araditc/sigtran.net/pull/38) is now
bound on the review branch for the handoff. Preparing this contract does not perform
the human review; the owner must designate that independent reviewer later.

## Administrative Assessment

```text
python scripts/assess-evidence-deviation.py
python scripts/assess-evidence-deviation.py --verify-review
```

Exit 3 means review pending, 1 means ineligible, and 0 means scoped
administrative acceptance only. Both commands currently return pending without
network access because no reviewer is designated. Live verification uses
read-only GitHub PR/review/file APIs, a roster already on the base branch,
exact reviewed commit/files/bundle digest, human identity and a stated rationale.
Expired, dismissed, changed or owner/self approvals fail closed. No SDK API,
strict original verifier or stable release evaluator is modified.

Live assessment must run on the exact reviewed PR head with committed decision
files; it rechecks PR state after reading reviews. The risk-event register is
append-only across Git history: recording a recurrence or another reopening
condition blocks acceptance, and erasing it cannot revive an old approval.
It is a manual incident register, not an automatic production anomaly detector.
CI publishes a fresh **pending** assessment and exercises synthetic review
transcripts; those fixtures are never treated as actual human approval.
The case scope and owner decision must also already be present on the PR base;
reviewer approval alone cannot install a broader owner risk decision.

## Delivery And Open Approval

All six implementation units are delivered. Their private verification roots
are `artifacts/qualification/phase-62/evidence-acceptance/units/01..06`, with
the final reruns for units 5 and 6 under `evidence-acceptance-final/units/05..06`. Failed/earlier
receipts remain retained. Release build, SDK/qualification/trace tests, API
comparison and pack are required for each commit; no SDK runtime/API changes
or release-policy edits are part of this package.

The [current review handoff](evidence/ENDURANCE_REVIEW_PREPARATION_20261117.json)
matches a fresh assessment and is checked by CI. Its state is
`PENDING_INDEPENDENT_REVIEW`, owner acceptance true, final exception/closure
false. A separate read-only replay rechecked all 94 original hashes and all
259,201 legacy observations; strict replay still rejected the same first
negative value. The retained one-hour stream replayed all 3,601 rows. These
checks do not manufacture independent human approval or a new passing run.

```text
python scripts/assess-evidence-deviation.py --check-report docs/evidence/ENDURANCE_REVIEW_PREPARATION_20261117.json
python scripts/assess-evidence-deviation.py --output artifacts/qualification/evidence-deviations/NEW_RECEIPT.json
```

Only reviewer designation and a real authenticated independent review remain
open for this acceptance package. Use a full-history checkout. PR #38 was
created at the owner's request, but no reviewer was assigned and no approval
has been fabricated or submitted. The previous
[delivery receipt](evidence/ENDURANCE_ACCEPTANCE_ASSESSMENT_20261117.json) is
retained unchanged as the pre-PR snapshot. If review is
later accepted, the only allowed closure is `CLOSED_WITH_ACCEPTED_DEVIATION`;
the original run still fails strict qualification. Phase 62 is **not yet closed**.
Phases 63-65, final package-bound evidence and all stable gates remain separate.

The final local verification includes 555 SDK checks, 40 qualification checks,
both trace-analysis self-tests, the unchanged 2,308-member public API and pack.
There are 21 focused acceptance tests; the 43-test endurance suite has two
Linux-only skips on Windows. Separate original/diagnostic replays preserve
all raw bytes. The actual GitHub workflow, not synthetic review fixtures,
determines CI success; it does not supply the missing human review.
