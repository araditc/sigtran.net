#!/usr/bin/env python3
"""Offline contract tests for the representative Kubernetes qualification matrix."""
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
DEPLOYMENT = ROOT / "deploy" / "kubernetes" / "deployment.yaml"
PDB = ROOT / "deploy" / "kubernetes" / "pod-disruption-budget.yaml"


class NetworkPolicyRendererTests(unittest.TestCase):
    def render(self, mode: str, *, namespace="sigtran-phase56",
               remote_ip="192.0.2.44", remote_port="2905"):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "policy.json"
            run = subprocess.run(
                [
                    sys.executable, str(RENDERER),
                    "--namespace", namespace,
                    "--remote-ip", remote_ip,
                    "--remote-port", remote_port,
                    "--mode", mode,
                    "--output", str(output),
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
        egress = value["spec"]["egress"]
        self.assertEqual(len(egress), 1)
        self.assertEqual(egress[0]["to"], [{"ipBlock": {"cidr": "192.0.2.44/32"}}])
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
            {"remote_ip": "127.0.0.1"},
            {"remote_ip": "::1"},
            {"remote_port": "0"},
        ]
        for kwargs in cases:
            with self.subTest(kwargs=kwargs):
                run, value = self.render("allow", **kwargs)
                self.assertNotEqual(run.returncode, 0)
                self.assertIsNone(value)


class RepresentativeWorkflowContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text()
        cls.deployment = DEPLOYMENT.read_text()
        cls.pdb = PDB.read_text()

    def test_representative_matrix_is_explicit_opt_in(self):
        self.assertIn("matrix_profile:", self.workflow)
        self.assertIn("default: core", self.workflow)
        self.assertIn("- representative", self.workflow)
        self.assertIn(
            'test "$DISRUPTION_CONFIRMATION" = "run kubernetes disruptive $K8S_NAMESPACE"',
            self.workflow,
        )
        self.assertIn(
            'test "$K8S_NETWORK_PROFILE" = "cni"',
            self.workflow,
        )

    def test_disruptive_steps_are_representative_only(self):
        for step_id in (
            "service_exposure",
            "network_policy",
            "graceful",
            "pdb",
            "node_drain",
        ):
            marker = f"id: {step_id}"
            start = self.workflow.index(marker)
            block = self.workflow[start:start + 220]
            self.assertIn("if: inputs.matrix_profile == 'representative'", block)

    def test_network_policy_requires_observed_denial_and_recovery(self):
        self.assertIn("network-policy-ready-denied.json", self.workflow)
        self.assertIn('if [[ "$code" == "503" ]]', self.workflow)
        self.assertIn("sctpAssociationRecovered", self.workflow)

    def test_graceful_shutdown_requires_m3ua_shutdown_event(self):
        self.assertIn('"EventName":"m3ua.shutdown.completed"', self.workflow)
        self.assertIn('"terminationGracePeriodSeconds":30', self.workflow)
        self.assertIn('"replacementReady":replacement=="true"', self.workflow)

    def test_node_drain_is_labeled_guarded_and_restored(self):
        self.assertIn('kubectl get node "$node" -l "$K8S_DRAIN_NODE_LABEL"', self.workflow)
        self.assertIn("node-role.kubernetes.io/control-plane", self.workflow)
        self.assertIn("kubectl uncordon", self.workflow)
        self.assertIn("rescheduledToDifferentNode", self.workflow)

    def test_rolling_update_and_pdb_are_bounded_for_single_association_pod(self):
        self.assertIn("type: RollingUpdate", self.deployment)
        self.assertIn("maxSurge: 0", self.deployment)
        self.assertIn("maxUnavailable: 1", self.deployment)
        self.assertIn("kind: PodDisruptionBudget", self.pdb)
        self.assertIn("maxUnavailable: 1", self.pdb)

    def test_finalizer_counts_representative_step_outcomes(self):
        finalize = self.workflow.index("- name: Finalize and persist evidence")
        block = self.workflow[finalize:]
        for outcome in (
            "SERVICE_EXPOSURE_OUTCOME",
            "NETWORK_POLICY_OUTCOME",
            "GRACEFUL_OUTCOME",
            "PDB_OUTCOME",
            "NODE_DRAIN_OUTCOME",
        ):
            self.assertIn(outcome, block)
        self.assertIn(
            'if [[ "$K8S_MATRIX_PROFILE" == "representative" ]]',
            block,
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)
