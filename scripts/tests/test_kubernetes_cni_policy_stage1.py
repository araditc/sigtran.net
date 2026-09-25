#!/usr/bin/env python3
"""Offline contract tests for Kubernetes CNI policy qualification stage 1."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
RENDERER = SCRIPTS / "render-kubernetes-network-policy.py"
WORKFLOW = ROOT / ".github" / "workflows" / "phase56-kubernetes-sctp.yml"
VALIDATOR = SCRIPTS / "validate-kubernetes-sctp-evidence.py"


class NetworkPolicyRendererTests(unittest.TestCase):
    def render(
        self,
        mode: str,
        *,
        namespace: str = "sigtran-phase56",
        policy_name: str = "sigtran-sctp-test-1",
        remote_ip: str = "192.0.2.44",
        remote_port: str = "2905",
    ):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "policy.json"
            run = subprocess.run(
                [
                    sys.executable,
                    str(RENDERER),
                    "--namespace",
                    namespace,
                    "--name",
                    policy_name,
                    "--remote-ip",
                    remote_ip,
                    "--remote-port",
                    remote_port,
                    "--mode",
                    mode,
                    "--output",
                    str(output),
                ],
                capture_output=True,
                text=True,
                timeout=5,
            )
            value = json.loads(output.read_text()) if output.exists() else None
            return run, value

    def test_allow_policy_is_exact_sctp_egress_to_peer(self):
        run, value = self.render("allow")
        self.assertEqual(run.returncode, 0, run.stderr)
        self.assertEqual(value["kind"], "NetworkPolicy")
        self.assertEqual(value["metadata"]["namespace"], "sigtran-phase56")
        self.assertEqual(value["metadata"]["name"], "sigtran-sctp-test-1")
        egress = value["spec"]["egress"]
        self.assertEqual(len(egress), 1)
        self.assertEqual(
            egress[0]["to"],
            [{"ipBlock": {"cidr": "192.0.2.44/32"}}],
        )
        self.assertEqual(
            egress[0]["ports"],
            [{"port": 2905, "protocol": "SCTP"}],
        )

    def test_deny_policy_has_no_egress_exceptions(self):
        run, value = self.render("deny")
        self.assertEqual(run.returncode, 0, run.stderr)
        self.assertEqual(value["spec"]["policyTypes"], ["Egress"])
        self.assertEqual(value["spec"]["egress"], [])

    def test_unsafe_or_nonrepresentative_values_fail_closed(self):
        cases = [
            {"namespace": "bad;namespace"},
            {"policy_name": "bad;policy"},
            {"remote_ip": "127.0.0.1"},
            {"remote_ip": "::1"},
            {"remote_port": "0"},
        ]
        for kwargs in cases:
            with self.subTest(kwargs=kwargs):
                run, value = self.render("allow", **kwargs)
                self.assertNotEqual(run.returncode, 0)
                self.assertIsNone(value)


class CniPolicyStage1WorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text()
        cls.validator = VALIDATOR.read_text()

    def test_stage1_is_explicit_opt_in_and_cni_only(self):
        self.assertIn("matrix_profile:", self.workflow)
        self.assertIn("default: core", self.workflow)
        self.assertIn("- cni-policy", self.workflow)
        self.assertIn(
            'test "$MATRIX_CONFIRMATION" = "run kubernetes cni-policy $K8S_NAMESPACE"',
            self.workflow,
        )
        self.assertIn(
            'test "$K8S_NETWORK_PROFILE" = "cni"',
            self.workflow,
        )
        self.assertIn(
            "K8S_POLICY_NAME: sigtran-sctp-${{ github.run_id }}-${{ github.run_attempt }}",
            self.workflow,
        )

    def test_stage1_steps_run_only_for_cni_policy_profile(self):
        for step_id in ("service_exposure", "network_policy"):
            marker = f"id: {step_id}"
            start = self.workflow.index(marker)
            block = self.workflow[start:start + 220]
            self.assertIn("if: inputs.matrix_profile == 'cni-policy'", block)

    def test_network_policy_forces_fresh_connection_under_deny(self):
        deny = self.workflow.index('kubectl apply -f "$deny"')
        delete = self.workflow.index('delete pod "$original_pod"', deny)
        blocked = self.workflow.index("sctp-assocs-policy-denied.txt", delete)
        allow = self.workflow.index('kubectl apply -f "$allow"', blocked)
        recovered = self.workflow.index("sctp-assocs-after-network-policy.txt", allow)
        self.assertLess(deny, delete)
        self.assertLess(delete, blocked)
        self.assertLess(blocked, allow)
        self.assertLess(allow, recovered)
        self.assertIn('"associationBlocked": association_blocked == "true"', self.workflow)
        self.assertIn('"sctpAssociationRecovered": association_recovered == "true"', self.workflow)

    def test_network_policy_cleanup_removes_run_unique_policy(self):
        self.assertIn("cleanup_policy()", self.workflow)
        self.assertIn(
            'delete networkpolicy "$K8S_POLICY_NAME"',
            self.workflow,
        )
        self.assertIn("trap cleanup_policy EXIT", self.workflow)

        cleanup = self.workflow.index("- name: Remove qualification CNI SCTP NetworkPolicy")
        rollout = self.workflow.index("- name: Verify pod restart and rollout rollback")
        block = self.workflow[cleanup:rollout]
        self.assertIn(
            "if: always() && inputs.matrix_profile == 'cni-policy'",
            block,
        )
        self.assertIn('--ignore-not-found=true', block)
        self.assertIn('get networkpolicy "$K8S_POLICY_NAME"', block)
        self.assertIn('test "$raise_cleanup_error" = "false"', block)

    def test_network_policy_trap_stays_armed_through_step_exit(self):
        start = self.workflow.index(
            "- name: Qualify CNI SCTP NetworkPolicy enforcement and recovery"
        )
        cleanup = self.workflow.index(
            "- name: Remove qualification CNI SCTP NetworkPolicy",
            start,
        )
        block = self.workflow[start:cleanup]
        self.assertIn("trap cleanup_policy EXIT", block)
        self.assertNotIn("trap - EXIT", block)

    def test_stage1_outcomes_are_fail_closed_in_finalizer(self):
        finalize = self.workflow.index("- name: Finalize and persist evidence")
        block = self.workflow[finalize:]
        self.assertIn("SERVICE_EXPOSURE_OUTCOME", block)
        self.assertIn("NETWORK_POLICY_OUTCOME", block)
        self.assertIn("POLICY_CLEANUP_OUTCOME", block)
        self.assertIn(
            'if [[ "$K8S_MATRIX_PROFILE" == "cni-policy" ]]; then',
            block,
        )
        self.assertIn("matrix_steps_ok=false", block)

    def test_validator_requires_service_exposure_in_full_matrix(self):
        self.assertIn(
            '"serviceExposure": "service-exposure.json"',
            self.validator,
        )
        self.assertIn(
            '"networkPolicy": "network-policy.json"',
            self.validator,
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)
