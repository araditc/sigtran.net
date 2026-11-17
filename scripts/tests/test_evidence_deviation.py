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
    def test_prepared_pr_does_not_designate_an_independent_reviewer(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit)
        value = deviation.review_material(audit, owner)
        self.assertFalse(value['reviewerDesignated'])
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
    def test_actual_case_is_pending_and_never_contacts_github(self):
        api = unittest.mock.Mock(side_effect=AssertionError('Pending case must not use network'))
        value = assessment.assess(verify_review=True, api=api)
        self.assertEqual(value['status'], 'PENDING_INDEPENDENT_REVIEW')
        self.assertTrue(value['ownerRiskAccepted'])
        self.assertEqual(value['blockers'], ['independent-reviewer-not-designated'])
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


class ReportOwnershipTests(unittest.TestCase):
    def test_public_handoff_matches_fresh_pending_assessment(self):
        path = ROOT / 'docs/evidence/ENDURANCE_REVIEW_PREPARATION_20261117.json'
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
