"""Dossier and acceptance fixtures do not grant independent human approval."""
import base64
import copy
import datetime
import importlib.util
import json
import pathlib
import tempfile
import subprocess
import unittest
from types import SimpleNamespace
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('deviation_test_contract', ROOT / 'scripts/evidence-deviation.py')
deviation = importlib.util.module_from_spec(spec); spec.loader.exec_module(deviation)
spec = importlib.util.spec_from_file_location('deviation_assessment_test', ROOT / 'scripts/assess-evidence-deviation.py')
assessment = importlib.util.module_from_spec(spec); spec.loader.exec_module(assessment)


class DossierTests(unittest.TestCase):
    def test_real_public_dossier_preserves_rejection_and_unknown_cause(self):
        value = deviation.dossier()
        self.assertTrue(value['publicRecordHashesVerified'] and value['sdkSourceTreeUnchanged'])
        self.assertFalse(value['qualificationPassed'] or value['stablePromotion'] or value['privateArtifactsReplayed'])
        self.assertEqual(len(value['case']['observations']), 7)

    def test_changed_sdk_tree_cannot_inherit_the_deviation(self):
        with patch.object(deviation, 'implementation_tree', return_value='0' * 40):
            with self.assertRaisesRegex(ValueError, 'Changed SDK'): deviation.dossier()

    def test_invalid_run_scope_missing_readings_and_changed_digest_are_rejected(self):
        original = deviation.evidence.read_json
        for key, value in [('scope', 'all-resource-errors'), ('observations', []), ('qualificationPassed', True)]:
            case = copy.deepcopy(original(ROOT / deviation.CASE)); case[key] = value
            def read(path): return case if str(path).endswith('endurance-managed-memory.json') else original(path)
            with patch.object(deviation.evidence, 'read_json', side_effect=read):
                with self.assertRaises(ValueError): deviation.dossier()
        case = original(ROOT / deviation.CASE); case['evidence'][0]['sha256'] = '0' * 64
        with patch.object(deviation.evidence, 'read_json', side_effect=lambda p: case if str(p).endswith('endurance-managed-memory.json') else original(p)):
            with self.assertRaisesRegex(ValueError, 'hash differs'): deviation.dossier()


class OwnerDecisionTests(unittest.TestCase):
    def test_owner_instruction_is_exact_scope_not_independent_approval(self):
        value = deviation.owner_acceptance(deviation.dossier())
        self.assertTrue(value['independentReviewRequired'])
        self.assertFalse(value['releasePromotionAuthorized'] or value['originalVerdictMayChange'])
        self.assertEqual(value['actor'], 'ammarheidari')

    def test_wrong_dossier_authority_and_release_bypass_are_rejected(self):
        audit = deviation.dossier(); original = deviation.evidence.read_json
        for key, value in [('dossierSha256', '0' * 64), ('actor', 'unapproved-owner'), ('decision', 'accept-all-failures'),
                           ('independentReviewRequired', False), ('releasePromotionAuthorized', True), ('revocationTriggers', [])]:
            bad = copy.deepcopy(original(ROOT / deviation.OWNER)); bad[key] = value
            with patch.object(deviation.evidence, 'read_json', return_value=bad):
                with self.assertRaises(ValueError): deviation.owner_acceptance(audit)


class IndependentReviewTests(unittest.TestCase):
    def test_admitted_reviewer_and_bound_case_still_require_authenticated_approval(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit)
        value = deviation.review_material(audit, owner)
        self.assertEqual(value['policy']['allowedReviewers'], ['akhiabanchian'])
        self.assertTrue(value['reviewerDesignated'])
        self.assertTrue(value['requestAssigned'])
        self.assertEqual(value['request']['pullRequest'], 38)

    def test_self_review_duplicate_identity_and_declared_approval_are_rejected(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit); original = deviation.evidence.read_json
        for names in (['ammarheidari'], ['AMMARHEIDARI'], ['reviewer', 'Reviewer'], ['robot[bot]']):
            bad = original(ROOT / deviation.REVIEW_POLICY); bad['allowedReviewers'] = names
            with patch.object(deviation.evidence, 'read_json', side_effect=lambda p: bad if str(p).endswith('.review-policy.json') else original(p)):
                with self.assertRaises(ValueError): deviation.review_material(audit, owner)
        bad = original(ROOT / deviation.REVIEW_REQUEST); bad['approved'] = True
        with patch.object(deviation.evidence, 'read_json', side_effect=lambda p: bad if str(p).endswith('.review-request.json') else original(p)):
            with self.assertRaises(ValueError): deviation.review_material(audit, owner)


class AssessmentTests(unittest.TestCase):
    def test_offline_case_stays_pending_without_contacting_github(self):
        api = unittest.mock.Mock(side_effect=AssertionError('Pending case must not use network'))
        value = assessment.assess(verify_review=False, api=api)
        self.assertEqual(value['status'], 'PENDING_INDEPENDENT_REVIEW')
        self.assertTrue(value['ownerRiskAccepted'])
        material = deviation.review_material(deviation.dossier(), deviation.owner_acceptance(deviation.dossier()))
        expected = ['live-independent-review-verification-required'] if material['requestAssigned'] else ['independent-review-pr-not-assigned']
        self.assertEqual(value['blockers'], expected)
        for key in ('independentReviewApproved', 'administrativeClosureApproved', 'acceptanceExceptionGranted',
                    'qualificationPassed', 'stablePromotion', 'rootCauseProven'):
            self.assertFalse(value[key])
        api.assert_not_called()

    def test_case_failure_cannot_be_suppressed_by_owner_acceptance(self):
        with patch.object(assessment.contract, 'dossier', side_effect=ValueError('Changed source')):
            value = assessment.assess()
        self.assertEqual(value['status'], 'INELIGIBLE')
        self.assertFalse(value['administrativeClosureApproved'])

    def test_bundle_digest_covers_exact_file_names_and_bytes(self):
        files = assessment.snapshot(ROOT); digest = assessment.bundle_digest(files)
        self.assertEqual(digest, assessment.bundle_digest(dict(reversed(list(files.items())))))
        files[deviation.OWNER] = '0' * 64
        self.assertNotEqual(digest, assessment.bundle_digest(files))

    def test_pending_steps_follow_designation_and_request_lifecycle(self):
        base = dict(status='PENDING_INDEPENDENT_REVIEW', independentReviewApproved=False, administrativeClosureApproved=False)
        value = assessment.remaining_review_steps(dict(base, blockers=['independent-review-pr-not-assigned']))
        self.assertEqual(value, ['independent-review-pr-assignment', 'authenticated-independent-review'])
        value = assessment.remaining_review_steps(dict(base, blockers=['live-independent-review-verification-required']))
        self.assertEqual(value, ['authenticated-independent-review'])
        with self.assertRaises(ValueError):
            assessment.remaining_review_steps(dict(base, status='CLOSED_WITH_ACCEPTED_DEVIATION', blockers=[]))
        with self.assertRaises(ValueError): assessment.remaining_review_steps(dict(base, blockers=['unrecognized-bypass']))

    def test_designated_reviewer_with_assigned_pr_still_needs_live_verification(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit)
        material = deviation.review_material(audit, owner)
        material['request']['pullRequest'] = 38; material['requestAssigned'] = True
        api = unittest.mock.Mock(side_effect=AssertionError('Offline assessment must not use network'))
        with patch.object(assessment.contract, 'review_material', return_value=material):
            value = assessment.assess(verify_review=False, api=api)
        self.assertEqual(value['blockers'], ['live-independent-review-verification-required'])
        self.assertFalse(value['independentReviewApproved'] or value['administrativeClosureApproved'])
        api.assert_not_called()


class AuthenticatedReviewTests(unittest.TestCase):
    """Synthetic API transcripts exercise authentication logic; they are not real approval receipts."""
    def setUp(self):
        self.now = datetime.datetime(2026, 11, 17, 13, tzinfo=datetime.timezone.utc)
        self.head = 'a' * 40; self.base = 'b' * 40
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit)
        self.material = deviation.review_material(audit, owner)
        self.material['policy']['allowedReviewers'] = ['independent-reviewer']
        self.material['request']['pullRequest'] = 7
        self.material.update(reviewerDesignated=True, requestAssigned=True)
        self.raw = {p: (ROOT / p).read_bytes() for p in assessment.BUNDLE_FILES}
        self.raw[deviation.REVIEW_POLICY] = json.dumps(self.material['policy']).encode()
        self.raw[deviation.REVIEW_REQUEST] = json.dumps(self.material['request']).encode()
        self.files = {p: assessment.hashlib.sha256(b).hexdigest() for p, b in self.raw.items()}
        self.digest = assessment.bundle_digest(self.files)
        self.pr = dict(number=7, base=dict(sha=self.base, ref='main', repo=dict(full_name='araditc/sigtran.net')),
                       head=dict(sha=self.head, repo=dict(full_name='araditc/sigtran.net')),
                       user=dict(login='case-author'), draft=False, state='open', merged=False)
        self.review = dict(id=10, user=dict(login='independent-reviewer', type='User'), state='APPROVED',
            commit_id=self.head, submitted_at='2026-11-17T12:00:00Z',
            body='Evidence-Deviation-Case: endurance-managed-memory-observations\nEvidence-Deviation-SHA256: '
                 + self.digest + '\nDisposition: ACCEPT_SCOPED_DEVIATION\n'
                 + 'Synthetic rationale accepts only missing readings, not proof of causation or stable qualification.')
        self.reviews = [self.review]

    def api(self, path):
        if '/contents/' in path:
            name = path.split('/contents/', 1)[1].split('?ref=')[0]; data = self.raw[name]
            return dict(type='file', path=name, size=len(data), encoding='base64', content=base64.b64encode(data).decode())
        if '/reviews?' in path: return self.reviews
        return self.pr

    def verify(self, api=None):
        return assessment.live_review(self.material, self.files, self.digest, 'endurance-managed-memory-observations',
                                      self.now, self.head, api or self.api)

    def test_authentication_requires_exact_head_bundle_and_human_rationale(self):
        self.assertTrue(self.verify()['authenticated'])
        self.assertEqual(self.verify()['approvals'][0]['reviewId'], 10)
        for key, value in [('commit_id', 'c' * 40), ('body', 'approved'), ('state', 'DISMISSED'),
                           ('submitted_at', '2026-09-01T00:00:00Z'), ('submitted_at', '2026-11-18T00:00:00Z')]:
            old = self.review[key]; self.review[key] = value
            with self.assertRaises(ValueError): self.verify()
            self.review[key] = old

    def test_case_and_disposition_without_digest_cannot_reuse_github_approve(self):
        self.review['body'] = ('Evidence-Deviation-Case: endurance-managed-memory-observations\n'
            'Disposition: ACCEPT_SCOPED_DEVIATION\n'
            'Synthetic acceptance of limited observation risk does not establish a runtime cause or stable qualification.')
        with self.assertRaisesRegex(ValueError, 'scope/digest/rationale'): self.verify()

    def test_synthetic_administrative_acceptance_never_qualifies_release(self):
        actual = assessment.subprocess.check_output
        def committed(args, **kwargs):
            if args[:2] == ['git', 'show'] and args[2].startswith('HEAD:'):
                return self.raw[args[2][5:]]
            if args == ['git', 'rev-parse', 'HEAD']: return self.head
            return actual(args, **kwargs)
        with patch.object(assessment.contract, 'review_material', return_value=self.material), \
                patch.object(assessment, 'snapshot', return_value=self.files), \
                patch.object(assessment, 'risk_events', return_value=[]), \
                patch.object(assessment.subprocess, 'check_output', side_effect=committed):
            result = assessment.assess(verify_review=True, api=self.api, now=self.now)
        self.assertEqual(result['status'], 'CLOSED_WITH_ACCEPTED_DEVIATION')
        self.assertTrue(result['independentReviewApproved'] and result['administrativeClosureApproved'])
        self.assertFalse(result['qualificationPassed'] or result['stablePromotion'] or result['rootCauseProven'])
        self.assertEqual(result['originalVerdict'], 'rejected')

    def test_owner_author_bot_and_unlisted_identity_cannot_approve(self):
        for login, user_type in [('ammarheidari', 'User'), ('case-author', 'User'),
                                 ('independent-reviewer', 'Bot'), ('someone-else', 'User')]:
            self.review['user'] = dict(login=login, type=user_type)
            with self.assertRaises(ValueError): self.verify()

    def test_later_change_request_cannot_replay_an_older_approval(self):
        self.reviews.append(dict(self.review, id=11, state='CHANGES_REQUESTED'))
        with self.assertRaises(ValueError): self.verify()

    def test_later_limited_approval_cannot_replay_an_older_acceptance(self):
        self.reviews.append(dict(self.review, id=11, body='Synthetic limited review does not authorize administrative closure.'))
        with self.assertRaisesRegex(ValueError, 'scope/digest/rationale'): self.verify()

    def test_changed_case_and_newly_installed_trust_are_rejected(self):
        for path in (deviation.OWNER, deviation.REVIEW_POLICY):
            before = self.raw[path]; self.raw[path] = before + b'\n'
            with self.assertRaises(ValueError): self.verify()
            self.raw[path] = before

    def test_changed_pr_head_and_pagination_overflow_are_rejected(self):
        calls = 0
        def changing(path):
            nonlocal calls
            if '/contents/' not in path and '/reviews?' not in path:
                calls += 1
                if calls > 1: return dict(self.pr, head=dict(self.pr['head'], sha='c' * 40))
            return self.api(path)
        with self.assertRaises(ValueError): self.verify(changing)
        self.reviews = [self.review] * 100
        with self.assertRaises(ValueError): self.verify()

    def test_cli_reads_only_and_pending_exit_is_not_success(self):
        command = subprocess.run(['python' if assessment.os.name == 'nt' else 'python3', str(ROOT / 'scripts/assess-evidence-deviation.py')],
                                 capture_output=True, text=True, check=False)
        self.assertEqual(command.returncode, 3)
        with patch.object(assessment.shutil, 'which', return_value='gh'), patch.object(assessment.subprocess, 'run',
                return_value=SimpleNamespace(stdout=b'{}')) as run:
            assessment.github_read('repos/araditc/sigtran.net/pulls/7')
        args = run.call_args.args[0]
        self.assertEqual(args[args.index('--method') + 1], 'GET')
        self.assertEqual(args[args.index('--hostname') + 1], 'github.com')

    def test_recorded_recurrence_blocks_closure_and_cannot_be_erased(self):
        owner = deviation.owner_acceptance(deviation.dossier())
        event = dict(id=1, kind='negative-managed-reading-recurrence', observedAtUtc='2026-11-17T13:00:00Z', reference='synthetic-issue')
        with patch.object(assessment, 'risk_events', return_value=[event]):
            result = assessment.assess()
        self.assertEqual(result['status'], 'RISK_REOPENED')
        self.assertFalse(result['acceptanceExceptionGranted'])
        with patch.object(assessment.subprocess, 'check_output', side_effect=['false\n', 'a' * 40 + '\n',
                json.dumps(dict(events=[event])).encode()]):
            with self.assertRaisesRegex(ValueError, 'erased'):
                assessment.risk_events(ROOT, owner, 'endurance-managed-memory-observations')

    def test_shallow_history_cannot_hide_a_prior_reopening_event(self):
        owner = deviation.owner_acceptance(deviation.dossier())
        with patch.object(assessment.subprocess, 'check_output', return_value='true\n'):
            with self.assertRaisesRegex(ValueError, 'Complete history'):
                assessment.risk_events(ROOT, owner, 'endurance-managed-memory-observations')


class HistoricalClosureTests(unittest.TestCase):
    """Audit a retained historical decision; cached JSON never authenticates a new candidate."""
    def record(self):
        return json.loads((ROOT / 'docs/evidence/ENDURANCE_ACCEPTANCE_CLOSURE_20261117.json').read_text(encoding='utf-8'))

    def test_closure_binds_exact_reviewed_decision_bytes_and_authenticated_identity(self):
        value = self.record(); approved = value['assessment']; head = value['reviewedHeadCommit']
        case = deviation.dossier()['case']
        self.assertEqual(value['status'], 'CLOSED_WITH_ACCEPTED_DEVIATION')
        self.assertEqual(value['scope'], 'seven-missing-managed-memory-readings-only')
        self.assertEqual(value['runId'], case['runId'])
        self.assertEqual(value['testedSourceCommit'], case['sourceCommit'])
        self.assertEqual(value['originalManifestSha256'], case['originalManifestSha256'])
        self.assertEqual(value['reviewRecord']['commit'], head)
        self.assertEqual(value['reviewRecord']['login'], 'akhiabanchian')
        self.assertEqual(value['reviewRecord']['reviewId'], 5942538225)
        self.assertTrue(approved['review']['authenticated'])
        approval = approved['review']['approvals'][0]
        self.assertEqual((approval['commit'], approval['reviewId'], approval['login']),
                         (head, value['reviewRecord']['reviewId'], value['reviewRecord']['login']))
        files = {path: assessment.hashlib.sha256(subprocess.check_output(['git', 'show', head + ':' + path],
                 cwd=ROOT, timeout=15)).hexdigest() for path in assessment.BUNDLE_FILES}
        self.assertEqual(approved['decisionFileSha256'], files)
        self.assertEqual(approved['bundleSha256'], assessment.bundle_digest(files))
        body = value['reviewRecord']['body'].splitlines()
        self.assertIn('Evidence-Deviation-SHA256: ' + approved['bundleSha256'], body)
        self.assertIn('Disposition: ACCEPT_SCOPED_DEVIATION', body)

    def test_owner_authorization_preserves_failed_evidence_and_records_review_limitations(self):
        value = self.record(); approved = value['assessment']; owner = value['ownerAuthorization']
        self.assertEqual(approved['originalVerdict'], 'rejected')
        self.assertTrue(approved['administrativeClosureApproved'] and approved['acceptanceExceptionGranted'])
        self.assertFalse(any(approved[key] for key in ('qualificationPassed', 'stablePromotion',
            'rootCauseProven', 'freshEnduranceRunStarted', 'privateArtifactsReplayed')))
        self.assertEqual(owner['actor'], 'ammarheidari')
        self.assertEqual(owner['basis'], 'explicit-owner-instruction-in-Codex')
        self.assertTrue(owner['detailedReplayExecutionEvidenceDeferred'])
        self.assertFalse(owner['originalOwnerDecisionModified'] or owner['originalFailedEvidenceMayChange']
                         or owner['releasePromotionAuthorized'])
        self.assertTrue(value['reviewRecord']['rawReplayIsReviewerAttestation'])
        self.assertFalse(value['reviewRecord']['replayCommandsVersionsAndOutputsInReviewBody']
                         or value['reviewRecord']['rawReplayIndependentlyRepeatedByThisClosure'])
        self.assertFalse(any(value['boundaries'][key] for key in ('qualificationPassed', 'stablePromotion',
            'rootCauseProven', 'finalPackageQualified', 'publicationAuthorized', 'originalArtifactsModified',
            'currentHeadInheritsAcceptance', 'freshEnduranceRunStarted', 'kafkaChangedByThisClosure', 'consumedByReleaseGates')))

    def test_historical_closure_is_not_consumed_by_current_offline_assessment(self):
        original = assessment.contract.evidence.read_json
        def guarded(path):
            if pathlib.Path(path).name == 'ENDURANCE_ACCEPTANCE_CLOSURE_20261117.json':
                raise AssertionError('A historical receipt cannot authorize a current candidate')
            return original(path)
        with patch.object(assessment.contract.evidence, 'read_json', side_effect=guarded):
            value = assessment.assess(verify_review=False)
        self.assertEqual(value['status'], 'PENDING_INDEPENDENT_REVIEW')
        self.assertFalse(value['administrativeClosureApproved'] or value['qualificationPassed'] or value['stablePromotion'])


class ReportOwnershipTests(unittest.TestCase):
    def test_public_handoff_matches_fresh_pending_assessment(self):
        path = ROOT / assessment.CURRENT_REPORT
        value = assessment.check_report(path, assessment.assess())
        self.assertTrue(value['assessment']['ownerRiskAccepted'])
        self.assertFalse(value['assessment']['administrativeClosureApproved'])
        altered = copy.deepcopy(value); altered['assessment']['status'] = 'CLOSED_WITH_ACCEPTED_DEVIATION'
        with patch.object(assessment.contract.evidence, 'read_json', return_value=altered):
            with self.assertRaises(ValueError): assessment.check_report(path, value['assessment'])

    def test_reports_cannot_overwrite_originals_or_existing_receipts(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory).resolve()
            path = root / 'artifacts/qualification/evidence-deviations/report.json'
            assessment.write_assessment(path, {'synthetic': True}, root)
            with self.assertRaises(FileExistsError): assessment.write_assessment(path, {}, root)
            self.assertTrue(json.loads(path.read_text())['synthetic'])
            for bad in (root / 'docs/report.json', root / 'artifacts/qualification/old/original/report.json'):
                with self.assertRaises(ValueError): assessment.write_assessment(bad, {}, root)


if __name__ == '__main__': unittest.main()
