# Independent Endurance Evidence Review

This is a review packet, not an approval or a passing endurance receipt.
The product owner accepted the risk of seven missing managed-memory readings;
independent human approval remains outstanding. Review the exact PR head and
current bundle digest from the PR description before giving any disposition.

## Review Scope

The six acceptance implementation commits already exist on `main`. The new PR
prepares their review handoff; its own diff does not contain all of that code.
Review this explicitly pinned implementation range as well as the new PR diff:

[Implementation comparison](https://github.com/araditc/sigtran.net/compare/6772bfd...069cba18b2de423037618ac7b07d6d7a209825fa)

| Implementation Unit | Commit |
| --- | --- |
| Dossier and unchanged SDK source tree | `78e731481e3bb8d4114ee09f3f5ee403d197135f` |
| Exact-scope owner decision | `815a6388842882fa429cb6a025a0893e4b95549c` |
| Independent review policy | `f1e574ebedaf69afc22f73f05153e3ece8c6a0b2` |
| Separate administrative assessment | `ed915462ee8e174eb753a1d060c4ba5ef633b18c` |
| Tamper rejection and CI | `b9f0110b3bccf13647b20eb8fc2e1e3217bd5def` |
| Six-unit delivery and open review | `069cba18b2de423037618ac7b07d6d7a209825fa` |

The [dossier](../ENDURANCE_ACCEPTANCE.md) binds run
`map-endurance-full-20261113-a`, source
`1579bad8b2c731819bfeb063260f23d10a0e427f`, original manifest
`8998131ed9ce028f2e52e840ec21232c9bdc392529ed6739c8e1bef42dc50b88`,
and exactly seven signed values/timestamps. The SDK implementation tree is
`28ca2436d16c069bf5f1c153967d3b0182b818de`. This is not final package equivalence.

## Required Evidence

| Record | Purpose |
| --- | --- |
| [Original rejected attempt](../evidence/ENDURANCE_COMPLETED_REJECTED_20261117.json) | 259,200 seconds, 3,545,239,973 measured completions, nine recovered incidents, failed resource gate |
| [Resource analysis](../evidence/ENDURANCE_RESOURCE_ANALYSIS_20261117.json) | All 259,201 observations; seven invalid managed values; no imputation |
| [Local diagnostic](../evidence/LOCAL_RESOURCE_DIAGNOSTIC_20261117.json) | 3,600 seconds, 37,021,965 measured completions, no reproduced negative reading; not endurance qualification |
| [Owner acceptance](../../eng/qualification/deviations/endurance-managed-memory.owner.json) | Explicit risk decision bound to the dossier; not independent approval |
| [Review contract](../ENDURANCE_REVIEW_REQUEST.md) | Human identity, scope, exact commit/digest, rationale and expiry |

Public reports and passing CI are not substitutes for access to the private
originals. The owner must arrange a read-only, access-controlled handoff of the
original failed run, diagnostic outputs and hash inventories. Retained local
copies are under the repository's private `artifacts/qualification/phase-62/`
directories `completed-rejected-20261117/original` and
`local-resource-hour-20261117-a/original`. These are not GitHub attachments.
Do not upload raw PCAP, host configuration/logs, passwords or private keys to
the public PR. Credentials are not evidence and must not enter the handoff.
If raw access is unavailable, record that limitation and do not attest that
private evidence was independently verified.

## Reviewer Checklist

- Confirm all original hashes and the actual 72-hour duration, source/PID and exit receipts.
- Reconcile admissions, measured/drain successes, declared fault terminals and independent peer counters.
- Inspect all nine recovery windows, bounded PCAP comparisons, network rollback and original Kafka-state restoration receipts.
- Confirm exactly seven unknown managed readings, with no dropped/clamped/reconstructed values or extra defects excused.
- Review bounded retry semantics and the limits of the one-hour GC/loopback diagnostic; the historical cause remains unproven.
- Inspect evaluator/CI rejection tests and confirm that neither owner acceptance nor administrative closure grants strict qualification or stable GO.
- State actual raw access, tools/versions, findings, residual risk, rationale and any follow-up conditions in the review.

Use a fresh full-history review checkout and private copies, not an active lab
workload. No new traffic, Kafka maintenance or 72-hour run is required here.

```text
python scripts/tests/test_evidence_deviation.py
python scripts/assess-evidence-deviation.py
python scripts/analyze-endurance-resources.py PRIVATE_ORIGINAL_ROOT --source-commit 1579bad8b2c731819bfeb063260f23d10a0e427f --output NEW_PRIVATE_ANALYSIS.json
```

The assessment currently exits 3 (pending). The strict original verifier must
still reject the negative managed reading; do not patch it to skip that gate.
The analysis command rechecks hashes/journal/resources, not every wire and
restoration subcheck. Review those original artifacts separately; a short
capture is not proof of full-run throughput or absence of loss.

## Before Formal Approval

The owner must designate an independent human in the reviewer allowlist and
admit that policy on `main` separately. Do not add a reviewer to the trust roster
inside this case PR. Then update this PR from the admitted base policy, refresh
its assessment/digest, pass CI and freeze the final head. Any earlier review of
another head/digest does not authorize the final case. No reviewer is currently
designated, and Codex will not approve or merge this PR.

The reviewer must use their own GitHub account, not the owner/PR-author account.
Only after completing the review, submit an actual GitHub **Approve** review
with an independent rationale and the exact bindings from the final PR body:

```text
Evidence-Deviation-Case: endurance-managed-memory-observations
Evidence-Deviation-SHA256: ACTUAL_FINAL_BUNDLE_SHA256
Disposition: ACCEPT_SCOPED_DEVIATION
```

Use **Request changes** when the evidence is insufficient. Do not copy the
acceptance disposition if you reject the risk. A later authenticated assessment
can at most report `CLOSED_WITH_ACCEPTED_DEVIATION`; strict technical
qualification remains false and stable/operator/package gates remain separate.
