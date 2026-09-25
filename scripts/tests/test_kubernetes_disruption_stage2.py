#!/usr/bin/env python3
"""Offline contract tests for Kubernetes disruption qualification stage 2."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
PDB_RENDERER = SCRIPTS / "render-kubernetes-pdb.py"
WORKFLOW = ROOT / ".github" / "workflows" / "phase56-kubernetes-sctp.yml"
VALIDATOR = SCRIPTS / "validate-kubernetes-sctp-evidence.py"


class PdbRendererTests(unittest.TestCase):
    def render(
        self,
        *,
        namespace: str = "sigtran-phase56",
        name: str = "sigtran-pdb-123-1",
    ):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "pdb.json"
            run = subprocess.run(
                [
                    sys.executable,
                    str(PDB_RENDERER),
                    "--namespace",
                    namespace,
                    "--name",
                    name,
                    "--output",
                    str(output),
                ],
                capture_output=True,
                text=True,
                timeout=5,
            )
            value = json.loads(output.read_text()) if output.exists() else None
            return run, value

    def test_renderer_emits_run_scoped_policy_v1_budget(self):
        run, value = self.render()
        self.assertEqual(run.returncode, 0, run.stderr)
        self.assertEqual(value["apiVersion"], "policy/v1")
        self.assertEqual(value["kind"], "PodDisruptionBudget")
        self.assertEqual(value["metadata"]["name"], "sigtran-pdb-123-1")
        self.assertEqual(value["metadata"]["namespace"], "sigtran-phase56")
        self.assertEqual(value["spec"]["maxUnavailable"], 1)
        self.assertEqual(value["spec"]["unhealthyPodEvictionPolicy"], "AlwaysAllow")
        self.assertEqual(
            value["spec"]["selector"]["matchLabels"],
            {"app.kubernetes.io/name": "sigtran-node"},
        )

    def test_renderer_rejects_unsafe_names(self):
        for kwargs in (
            {"namespace": "bad;namespace"},
            {"name": "bad;pdb"},
        ):
            with self.subTest(kwargs=kwargs):
                run, value = self.render(**kwargs)
                self.assertNotEqual(run.returncode, 0)
                self.assertIsNone(value)


class DisruptionStage2WorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text()
        cls.validator = VALIDATOR.read_text()

    def test_stage2_is_explicit_opt_in(self):
        self.assertIn("- disruption", self.workflow)
        self.assertIn(
            'test "$MATRIX_CONFIRMATION" = "run kubernetes disruption $K8S_NAMESPACE"',
            self.workflow,
        )
        self.assertIn(
            "K8S_PDB_NAME: sigtran-pdb-${{ github.run_id }}-${{ github.run_attempt }}",
            self.workflow,
        )

    def test_graceful_termination_uses_structured_shutdown_event(self):
        start = self.workflow.index("- name: Qualify graceful pod termination")
        end = self.workflow.index("- name: Verify run-scoped PodDisruptionBudget", start)
        block = self.workflow[start:end]
        self.assertIn('"EventName":"m3ua.shutdown.completed"', block)
        self.assertIn("--grace-period=30", block)
        self.assertIn("terminationGracePeriodSeconds", block)
        self.assertIn("replacementReady", block)
        self.assertIn('kill -0 "$log_pid"', block)

    def test_pdb_is_run_scoped_observed_and_cleanup_verified(self):
        start = self.workflow.index("- name: Verify run-scoped PodDisruptionBudget")
        node_drain = self.workflow.index("- name: Qualify node drain and rescheduling", start)
        block = self.workflow[start:node_drain]
        self.assertIn("render-kubernetes-pdb.py", block)
        self.assertIn('"observedGenerationMatches": observed_generation == generation', block)
        self.assertIn('"disruptionsAllowed": disruptions_allowed', block)
        self.assertIn("unhealthyPodEvictionPolicy", block)

        cleanup = self.workflow.index("- name: Remove qualification PodDisruptionBudget", node_drain)
        cleanup_end = self.workflow.index("- name: Capture final readiness and SCTP state", cleanup)
        cleanup_block = self.workflow[cleanup:cleanup_end]
        self.assertIn("if: always()", cleanup_block)
        self.assertIn('delete poddisruptionbudget "$K8S_PDB_NAME"', cleanup_block)
        self.assertIn('test -z "$remaining"', cleanup_block)

    def test_node_drain_uses_eviction_and_has_owned_rollback(self):
        start = self.workflow.index("- name: Qualify node drain and rescheduling")
        end = self.workflow.index("- name: Restore drained node", start)
        block = self.workflow[start:end]
        self.assertIn('kubectl drain "$node"', block)
        self.assertNotIn("--disable-eviction", block)
        self.assertNotIn("--force", block)
        self.assertIn('kubectl annotate node "$node"', block)
        self.assertIn('phase56.sigtran.net/drain-owner="$RUN_ID"', block)
        self.assertIn("node-role.kubernetes.io/control-plane", block)
        self.assertIn("node-role.kubernetes.io/master", block)
        self.assertIn("another schedulable Ready Linux node", block)
        self.assertIn("sctp-assocs-after-node-drain.txt", block)
        self.assertIn('kill -0 "$log_pid"', block)

        restore_end = self.workflow.index("- name: Capture final readiness and SCTP state", end)
        restore_block = self.workflow[end:restore_end]
        self.assertIn("if: always()", restore_block)
        self.assertIn("drain-owner", restore_block)
        self.assertIn('kubectl uncordon "$owned_node"', restore_block)
        self.assertIn('test "$unschedulable" = "false"', restore_block)

    def test_stage2_outcomes_are_fail_closed_in_finalizer(self):
        finalize = self.workflow.index("- name: Finalize and persist evidence")
        block = self.workflow[finalize:]
        for outcome in (
            "GRACEFUL_OUTCOME",
            "PDB_OUTCOME",
            "PDB_CLEANUP_OUTCOME",
            "NODE_DRAIN_OUTCOME",
            "NODE_RESTORE_OUTCOME",
        ):
            self.assertIn(outcome, block)
        self.assertIn(
            'if [[ "$K8S_MATRIX_PROFILE" == "disruption" ]]; then',
            block,
        )
        self.assertIn("matrix_steps_ok=false", block)

    def test_validator_requires_pdb_in_full_matrix(self):
        self.assertIn(
            '"podDisruptionBudget": "pdb.json"',
            self.validator,
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)
