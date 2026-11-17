# Independent Evidence Review Request

Status: **OPEN / PR preparation in progress / reviewer not designated / no approval received**.
The owner accepted the seven-reading risk; this document is a handoff, not a
review receipt. Codex must not approve its own implementation or fill in a
reviewer identity. The owner-designated reviewer should have relevant .NET
runtime and signalling reliability experience and access to private originals.
The [review packet](reviews/ENDURANCE_EVIDENCE_REVIEW.md) includes the exact
implementation range, private-evidence requirements and reviewer checklist.

## Materials And Questions

Review the [acceptance dossier](ENDURANCE_ACCEPTANCE.md), its three pinned
reports, original failed supervision/resource stream and retained hash inventory.
Confirm the 72-hour traffic/accounting/recovery/restoration facts separately
from the seven missing managed values. Check the one-hour diagnostic's limits,
defensive retry semantics and unchanged SDK implementation tree. Neither a
passing short run nor positive RSS/last-GC heap reconstructs missing readings.

Decide whether this residual risk is acceptable for this exact run/source/scope.
Record reasons, concerns and follow-up conditions. Reject or request changes
if another failure, changed evidence or an unsupported causation claim appears.
The decision must not assert stable/operator readiness or qualify a new package.

## Authenticated Approval Path

The owner must first designate an independent human in the case review policy
through repository review. Then assign a repository PR in `.review-request.json`
containing the exact case files to be reviewed. A PR author or the product owner
cannot be the independent approver; an empty allowlist never authorizes anyone.
The assessment tool will obtain review state from GitHub, not a local `approved`
flag. A later change request/dismissal or changed case bytes invalidates approval.

The formal approval must bind the exact PR head commit and the assessment
tool's bundle digest using these separate lines in the review body:

```text
Evidence-Deviation-Case: endurance-managed-memory-observations
Evidence-Deviation-SHA256: ACTUAL_BUNDLE_SHA256
Disposition: ACCEPT_SCOPED_DEVIATION
```

The reviewer must provide an actual rationale (at least 40 characters beyond
those binding lines) in the same review. The allowlist must already exist on
the PR base branch; a case PR cannot install its own reviewer trust. Approval
also requires the owner decision and dossier already admitted on that base;
the review PR cannot broaden the owner's accepted scope. Approval
expires for assessment after 30 days; changed scope or recurring anomalies
require re-review. No PR is created, no reviewer is impersonated and no new
load is launched by preparing this request. See the
[GitHub review API contract](https://docs.github.com/en/rest/pulls/reviews).

Run live assessment from the exact reviewed PR head. Register newly discovered
defects/recurrences in `.risk-events.json`; that history cannot be cleared to
reuse approval. The human reviewer must inspect the private raw evidence; an
offline CI report validates public bindings only, not private lab artifacts.
