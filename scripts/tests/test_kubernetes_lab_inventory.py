"""Exercise the read-only target preflight with synthetic Kubernetes API objects."""

import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github/workflows/phase56-kubernetes-sctp.yml"
CLUSTER_UID = "a1111111-a111-4111-a111-a11111111111"
NAMESPACE_UID = "b2222222-b222-4222-b222-b22222222222"


def namespace(name, uid, labels):
    return {"apiVersion": "v1", "kind": "Namespace", "metadata": {
        "name": name, "uid": uid, "labels": labels}, "status": {"phase": "Active"}}


class LabInventoryTests(unittest.TestCase):
    def setUp(self):
        self.node = {"metadata": {"name": "private-worker-a", "labels": {}}, "spec": {}, "status": {
            "nodeInfo": {"operatingSystem": "linux"}, "conditions": [{"type": "Ready", "status": "True"}]}}
        self.snapshots = {
            "cluster-anchor.json": namespace("kube-system", CLUSTER_UID, {}),
            "qualification-namespace.json": namespace("sigtran-lab", NAMESPACE_UID, {"sigtran.net/qualification": "enabled"}),
            "nodes.json": {"apiVersion": "v1", "kind": "List", "items": [self.node]},
            "cni-daemonset.json": {
                "apiVersion": "apps/v1", "kind": "DaemonSet",
                "metadata": {"name": "lab-cni", "namespace": "kube-system", "generation": 4},
                "status": {"observedGeneration": 4, "desiredNumberScheduled": 2, "numberMisscheduled": 0,
                           "currentNumberScheduled": 2, "updatedNumberScheduled": 2,
                           "numberReady": 2, "numberAvailable": 2}},
        }

    def check(self, *, cluster_uid=CLUSTER_UID, namespace_name="sigtran-lab", namespace_uid=NAMESPACE_UID, minimum_workers=1):
        with tempfile.TemporaryDirectory() as directory:
            for path, contents in self.snapshots.items():
                (Path(directory) / path).write_text(json.dumps(contents), encoding="utf-8")
            return subprocess.run([sys.executable, str(ROOT / "scripts/check-kubernetes-lab-inventory.py"), directory,
                                   "--cluster-uid", cluster_uid, "--namespace", namespace_name,
                                   "--namespace-uid", namespace_uid, "--cni-namespace", "kube-system",
                                   "--cni-name", "lab-cni", "--minimum-workers", str(minimum_workers)],
                                  capture_output=True, text=True, timeout=10)

    def assert_rejected(self, run, reason):
        self.assertEqual(run.returncode, 1, run.stderr)
        self.assertIn(reason, run.stderr)
        self.assertFalse(run.stdout)
        self.assertNotIn("private-worker-a", run.stderr)

    def test_valid_target_reports_only_worker_count_and_explicit_non_claim(self):
        run = self.check()
        self.assertEqual(run.returncode, 0, run.stderr)
        self.assertIn("1 Ready Linux worker(s); SCTP not tested", run.stdout)
        for private in (CLUSTER_UID, NAMESPACE_UID, "lab-cni", "private-worker-a"):
            self.assertNotIn(private, run.stdout)

    def test_wrong_cluster_and_recreated_namespace_are_rejected(self):
        self.assert_rejected(self.check(cluster_uid=NAMESPACE_UID), "identity does not match")
        self.assert_rejected(self.check(namespace_uid=CLUSTER_UID), "identity does not match")
        self.assert_rejected(self.check(cluster_uid=""), "UID is required")

    def test_system_or_unlabelled_namespace_is_rejected(self):
        value = self.snapshots["qualification-namespace.json"]
        value["metadata"]["name"] = "default"
        self.assert_rejected(self.check(namespace_name="default"), "dedicated qualification namespace")
        value["metadata"].update(name="sigtran-lab", labels={})
        self.assert_rejected(self.check(), "dedicated qualification namespace")

    def test_terminating_namespace_is_rejected(self):
        self.snapshots["qualification-namespace.json"]["metadata"]["deletionTimestamp"] = "2026-11-18T00:00:00Z"
        self.assert_rejected(self.check(), "not terminating")

    def test_unavailable_nodes_do_not_count_as_ready_workers(self):
        for section, key, value in (
            ("metadata", "labels", {"node-role.kubernetes.io/control-plane": ""}),
            ("status", "nodeInfo", {"operatingSystem": "windows"}),
            ("status", "conditions", [{"type": "Ready", "status": "False"}]),
            ("spec", "unschedulable", True),
            ("spec", "taints", [{"key": "dedicated", "effect": "NoSchedule"}]),
        ):
            with self.subTest(section=section, key=key, value=value):
                node = copy.deepcopy(self.node)
                node[section][key] = value
                self.snapshots["nodes.json"]["items"] = [node]
                self.assert_rejected(self.check(), "No Ready, schedulable Linux worker")

    def test_cni_rollout_must_be_current_and_converged(self):
        status = self.snapshots["cni-daemonset.json"]["status"]
        for key, value in (("observedGeneration", 3), ("numberReady", 1), ("numberAvailable", 1),
                           ("numberMisscheduled", 1), ("updatedNumberScheduled", 1), ("numberReady", "2")):
            with self.subTest(key=key, value=value):
                before = status[key]
                status[key] = value
                self.assert_rejected(self.check(), "has not converged")
                status[key] = before

    def test_missing_inventory_fails_closed(self):
        self.snapshots.pop("cni-daemonset.json")
        self.assert_rejected(self.check(), "missing or malformed inventory")

    def test_disruption_needs_two_distinct_eligible_workers(self):
        self.assert_rejected(self.check(minimum_workers=2), "requires two Ready")
        alternate = copy.deepcopy(self.node)
        alternate["metadata"]["name"] = "private-worker-b"
        self.snapshots["nodes.json"]["items"].append(alternate)
        self.assertEqual(self.check(minimum_workers=2).returncode, 0)

    def test_workflow_checks_pinned_target_before_any_namespace_mutation(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")
        cluster = workflow.index("- name: Capture cluster and CNI identity")
        deploy = workflow.index("- name: Deploy operations host")
        self.assertIn("if: github.ref == 'refs/heads/main'", workflow)
        self.assertIn("scripts/check-kubernetes-lab-inventory.py", workflow[cluster:deploy])
        self.assertNotIn("create namespace", workflow)
        cleanup = workflow[workflow.index("- name: Remove qualification CNI SCTP NetworkPolicy"):]
        delete = cleanup.index("delete networkpolicy")
        self.assertIn("steps.cluster.outcome == 'success'", cleanup[:delete])
        self.assertIn('[[ -f "$EVIDENCE_SCRATCH/network-policy-attempted" ]] || exit 0', cleanup[:delete])


if __name__ == "__main__":
    unittest.main(verbosity=2)
