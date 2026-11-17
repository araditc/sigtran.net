"""Dossier and acceptance fixtures do not grant independent human approval."""
import copy
import importlib.util
import pathlib
import tempfile
import unittest
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('deviation_test_contract', ROOT / 'scripts/evidence-deviation.py')
deviation = importlib.util.module_from_spec(spec); spec.loader.exec_module(deviation)


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
    def test_actual_reviewer_remains_unassigned(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit)
        value = deviation.review_material(audit, owner)
        self.assertFalse(value['reviewerDesignated'] or value['requestAssigned'])
        self.assertIsNone(value['request']['pullRequest'])

    def test_self_review_duplicate_identity_and_declared_approval_are_rejected(self):
        audit = deviation.dossier(); owner = deviation.owner_acceptance(audit); original = deviation.evidence.read_json
        for names in (['ammarheidari'], ['AMMARHEIDARI'], ['reviewer', 'Reviewer'], ['robot[bot]']):
            bad = original(ROOT / deviation.REVIEW_POLICY); bad['allowedReviewers'] = names
            with patch.object(deviation.evidence, 'read_json', side_effect=lambda p: bad if str(p).endswith('.review-policy.json') else original(p)):
                with self.assertRaises(ValueError): deviation.review_material(audit, owner)
        bad = original(ROOT / deviation.REVIEW_REQUEST); bad['approved'] = True
        with patch.object(deviation.evidence, 'read_json', side_effect=lambda p: bad if str(p).endswith('.review-request.json') else original(p)):
            with self.assertRaises(ValueError): deviation.review_material(audit, owner)


if __name__ == '__main__': unittest.main()
