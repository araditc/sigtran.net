#!/usr/bin/env python3
"""Audit a narrowly bound evidence deviation; never repair observations or qualify a release."""
import importlib.util
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]
CASE = 'eng/qualification/deviations/endurance-managed-memory.json'
OWNER = 'eng/qualification/deviations/endurance-managed-memory.owner.json'
spec = importlib.util.spec_from_file_location('deviation_endurance_contract', ROOT / 'scripts/verify-endurance-evidence.py')
evidence = importlib.util.module_from_spec(spec); spec.loader.exec_module(evidence)
require = evidence.require


def implementation_tree(root, commit):
    """Compare committed SDK source trees, not package identity or runtime-equivalence claims."""
    require(commit == 'HEAD' or re.fullmatch('[0-9a-f]{40}', commit), 'A complete source commit is required')
    return subprocess.check_output(['git', 'rev-parse', commit + ':src/Sigtran.NET'], cwd=root,
                                   text=True, timeout=15).strip()


def dossier(root=ROOT):
    """Validate every pinned public record while keeping the original rejection and unknown cause visible."""
    root = pathlib.Path(root).resolve()
    case = evidence.read_json(evidence.relative_file(root, CASE))
    require(set(case) == {'schemaVersion', 'id', 'runId', 'sourceCommit', 'sdkImplementationTree', 'policy',
        'originalManifestSha256', 'scope', 'rootCauseProven', 'qualificationPassed', 'stablePromotion',
        'observations', 'evidence'}, 'Unexpected dossier schema')
    require(case['schemaVersion'] == 1 and case['id'] == 'endurance-managed-memory-observations'
            and case['scope'] == 'seven-missing-managed-memory-readings-only'
            and case['policy'] == evidence.POLICY, 'Unknown deviation scope/policy')
    require(all(case[key] is False for key in ('rootCauseProven', 'qualificationPassed', 'stablePromotion')),
            'A dossier cannot qualify execution or prove a cause')
    require(len(case['evidence']) == 3, 'Exactly three pinned supporting reports are required')
    records = {}
    for item in case['evidence']:
        require(set(item) == {'kind', 'path', 'sha256'} and item['kind'] not in records, 'Repeated or unknown evidence entry')
        path = evidence.relative_file(root, item['path'])
        require(re.fullmatch('[0-9a-f]{64}', item['sha256']) and evidence.capacity.digest(path) == item['sha256'],
                'Supporting evidence hash differs')
        records[item['kind']] = evidence.read_json(path)
    require(set(records) == {'rejected-run', 'resource-analysis', 'integration-diagnostic'}, 'Evidence roles differ')
    rejected, analysis, diagnostic = (records[k] for k in ('rejected-run', 'resource-analysis', 'integration-diagnostic'))
    require(case['runId'] == rejected['runId'] and case['sourceCommit'] == rejected['testedSourceCommit'] == analysis['testedSourceCommit']
            and case['originalManifestSha256'] == rejected['originalManifestSha256'] == analysis['originalManifestSha256'],
            'Run/source/original inventory binding differs')
    require(rejected['qualificationPassed'] is rejected['executionValid'] is False
            and rejected['supervision']['ExecMainStatus'] == '1'
            and analysis['strictResourceReplayPassed'] is False, 'Original failed verdict must remain failed')
    bad = analysis['diagnosticFindings']
    actual = [dict(elapsedSeconds=x['elapsedSeconds'], managedBytes=x['managedBytes']) for x in bad['invalidManagedDetails']]
    require(len(case['observations']) == bad['negativeManagedObservations'] == 7
            and case['observations'] == rejected['resources']['invalidManagedObservations'] == actual
            and all(type(x['managedBytes']) is int and x['managedBytes'] < 0 for x in actual), 'Exact missing observations differ')
    require(all(bad[key] == 0 for key in ('otherNegativeFields', 'cumulativeOrTimeRollbackRows',
            'unexpectedProtocolValues', 'observedBoundViolationRows'))
            and bad['historicalManagedReadingsComplete'] is False and analysis['missingValuesSubstituted'] is False,
            'An additional defect or substituted value is outside this deviation')
    require(rejected['durationSeconds'] == 259200 and rejected['unexpectedFailures'] == rejected['unresolvedOperations'] == 0
            and rejected['recoveredIncidents'] == 9 and rejected['journalAccountingPassed'] is True
            and rejected['peerCountersReconciled'] is True and rejected['maintenance']['restorationVerified'] is True,
            'Traffic, recovery and restoration cannot be waived')
    require(diagnostic['diagnostic'] is diagnostic['executionValid'] is True
            and diagnostic['rootCauseProven'] is diagnostic['qualificationPassed'] is diagnostic['stablePromotion'] is False,
            'Short diagnostic cannot replace the rejected verdict')
    require(implementation_tree(root, case['sourceCommit']) == implementation_tree(root, 'HEAD') == case['sdkImplementationTree'],
            'Changed SDK implementation requires a separate assessment')
    return dict(case=case, records=records, dossierSha256=evidence.capacity.digest(root / CASE),
                publicRecordHashesVerified=True, sdkSourceTreeUnchanged=True, privateArtifactsReplayed=False,
                qualificationPassed=False, stablePromotion=False, rootCauseProven=False)


def owner_acceptance(audit, root=ROOT):
    """Record the actual owner instruction, not an independent review or a cryptographic signature."""
    value = evidence.read_json(evidence.relative_file(pathlib.Path(root).resolve(), OWNER))
    require(set(value) == {'schemaVersion', 'caseId', 'dossierSha256', 'actor', 'role', 'recordedOn',
        'decision', 'basis', 'statement', 'independentReviewRequired', 'rootCauseProven',
        'originalVerdictMayChange', 'releasePromotionAuthorized', 'revocationTriggers'}, 'Unexpected owner decision schema')
    require(value['schemaVersion'] == 1 and value['caseId'] == audit['case']['id']
            and value['dossierSha256'] == audit['dossierSha256'], 'Owner acceptance does not bind this dossier')
    require(value['actor'] == 'ammarheidari' and value['role'] == 'product-owner'
            and value['decision'] == 'accept-seven-reading-risk' and value['basis'] == 'explicit-owner-instruction-in-Codex'
            and isinstance(value['statement'], str) and 20 <= len(value['statement']) <= 1024,
            'Owner authority or scope differs')
    require(re.fullmatch(r'\d{4}-\d{2}-\d{2}', value['recordedOn']), 'Owner decision date is required')
    evidence.utc_time(value['recordedOn'] + 'T00:00:00+00:00')
    require(value['independentReviewRequired'] is True and all(value[key] is False for key in
            ('rootCauseProven', 'originalVerdictMayChange', 'releasePromotionAuthorized')), 'Owner acceptance cannot bypass review or release gates')
    require(value['revocationTriggers'] == ['scope-or-evidence-change', 'sdk-implementation-change',
        'additional-unexpected-failure', 'negative-managed-reading-recurrence', 'independent-review-rejection'],
        'Risk reopening conditions differ')
    return value


if __name__ == '__main__':
    print(json.dumps(dossier(), indent=2))
