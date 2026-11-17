# Independent Reviewer Designation

The owner nominated GitHub account `akhiabanchian` (Atefeh Khiabanchian).
The separate main-based [PR #39](https://github.com/araditc/sigtran.net/pull/39)
registered that identity. Its genuine policy-only approval was submitted at
`2026-11-17T15:49:45Z`; it was merged at `2026-11-17T15:51:08Z`, as commit
`305d6953f21fb30fb9210d7f77db76928f1cf3a5`. That review expressly excludes
endurance-case acceptance and private raw verification. It does not accept
the case, change the owner decision or waive a release gate.

PR #38 now incorporates that admitted policy. Its
[refreshed handoff](evidence/ENDURANCE_REVIEW_REFRESH_20261117.json) supersedes,
without rewriting, the earlier preparation/designation receipts. The exact
new head/digest are published in the PR description after validation.

The subsequent explicit case review `5942538225` was authenticated on head
`73d6855`, then PR #38 was normally merged. The owner's later authorization
and the [administrative closure](ENDURANCE_CLOSURE_20261117.md) record the
missing detailed replay receipts as follow-up; old snapshots remain unchanged.

## Observed Review And Limits

GitHub recorded an [Approve review](https://github.com/araditc/sigtran.net/pull/38#pullrequestreview-5940071799)
by that account at `2026-11-17T14:16:21Z` on head
`779f9b6552dd810a5d73bafcf77764156a10f0e4`. At policy preparation time its body
lacked the required `Evidence-Deviation-SHA256` line; the reviewer later added
the old digest to that same review. This does not change its reviewed commit
or authorize the refreshed case. Findings and actual raw-evidence access are
not documented in that text. This does not imply
the reviewer did not inspect anything; it means this receipt does not attest
that scope. The earlier empty review and the new real review remain untouched.

No review is authored, edited, dismissed or approved on the reviewer's behalf.
The original rejected measurement, unknown cause and all raw artifacts remain
unchanged. A real GitHub Approve alone is not scoped evidence acceptance.

## Safe Admission Sequence

1. Review and merge this policy registration into `main` through normal repository controls.
2. Update PR #38 from the admitted base policy. Resolve its request binding to #38 and create a fresh pending assessment; retain old receipts.
3. Re-run build/tests/API/pack and CI, publish the new exact head and bundle digest, then freeze that head.
4. Arrange private read-only original evidence access. The reviewer must state actual access, findings, rationale and any limitations.
5. The reviewer submits their own final review on that head with the exact digest. Then run authenticated assessment without checking an archived pending receipt.

The admitted registration snapshot deliberately kept `main`'s null case-request
reference. The refreshed case branch instead retains its binding to PR #38;
neither counts a stale approval. The roster remains limited to one nominated
human, excludes the owner, and retains exact-commit/digest binding and the
30-day review age. No administrative bypass or human impersonation is used.

## Lifecycle-Safe Reporting

Tests/CI previously described the empty-roster snapshot. They now verify
the appropriate pending prerequisites after designation as well. A single
source-controlled current-report path avoids stale hard-coded report paths.
Archived receipts remain immutable; no live GitHub approval is synthesized.
Focused coverage explicitly rejects case/disposition-only approval text without
the digest, and confirms that a designated reviewer plus PR assignment still
cannot grant acceptance in offline CI.

```text
python scripts/tests/test_evidence_deviation.py
python scripts/assess-evidence-deviation.py --check-current-report
```

The retained registration snapshot expected exit 3 with case assignment and
authenticated review outstanding. The pre-approval refreshed snapshot still
expects exit 3 in offline CI. The later authenticated historical acceptance
does not turn that offline check into proof for a new head or candidate.
Technical qualification, stable publication and operator acceptance remain false/open.

## Registration Validation

Local Release build passed without warnings/errors. All 555 SDK tests,
40 qualification tests and two trace-analysis self-tests passed. The focused
deviation suite passed 24 tests; the endurance Python suite checked 43 tests
(41 passed, two Linux-only skips on Windows). The 2,308-member public API is unchanged,
and development package/symbol-package creation passed. The current-report
check returns the expected pending exit 3, not administrative acceptance.
No lab load, Kafka change, new human review or publication was performed.
