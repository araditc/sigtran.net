"""Offline workload drain, restoration and workflow boundaries; no cluster calls."""

from contextlib import contextmanager
from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"


def load(script):
    spec = importlib.util.spec_from_file_location(script, SCRIPTS / (script + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


disruption = load("run-kubernetes-disruption")
renderer = load("render-kubernetes-deployment")
NS = "sigtran-lab"
RUN = "phase56-k8s-123-1"


def uid(number):
    return f"10000000-0000-4000-a000-{number:012d}"


def node(name, number):
    return {"metadata": {"name": name, "uid": uid(number), "resourceVersion": "1",
                         "labels": {disruption.DRAIN_LABEL: "true"}, "annotations": {"retained": "value"}},
            "spec": {}, "status": {"nodeInfo": {"operatingSystem": "linux"},
                                   "conditions": [{"type": "Ready", "status": "True"}]}}


def pod(name, node_name, number):
    return {"metadata": {"name": name, "namespace": NS, "uid": uid(number),
                         "labels": {disruption.APP_LABEL: "sigtran-node", disruption.RUN_LABEL: RUN},
                         "ownerReferences": [{"kind": "ReplicaSet", "controller": True, "uid": uid(20)}]},
            "spec": {"nodeName": node_name, "terminationGracePeriodSeconds": 30},
            "status": {"phase": "Running", "conditions": [{"type": "Ready", "status": "True"}]}}


class FakeCluster:
    def __init__(self):
        self.cluster_uid, self.namespace_uid = uid(1), uid(2)
        self.nodes = {"worker-a": node("worker-a", 3), "worker-b": node("worker-b", 4)}
        self.original = pod("sigtran-before", "worker-a", 5)
        self.after = pod("sigtran-after", "worker-b", 6)
        self.other = []
        self.budget = None
        self.evicted = False
        self.block_eviction = False
        self.mutations = []

    def get(self, *args, absent=False):
        return deepcopy(self.read(*args))

    def read(self, kind, *args):
        if kind == "namespace":
            name = args[0]
            return {"apiVersion": "v1", "kind": "Namespace", "metadata": {"name": name,
                    "uid": self.cluster_uid if name == "kube-system" else self.namespace_uid,
                    "labels": {"sigtran.net/qualification": "enabled"}}, "status": {"phase": "Active"}}
        if kind == "pods":
            if "--all-namespaces" in args:
                return {"items": [self.original, *self.other]}
            assert args == ("-n", NS, "-l", f"{disruption.APP_LABEL}=sigtran-node,{disruption.RUN_LABEL}={RUN}")
            return {"items": [self.after if self.evicted else self.original]}
        if kind == "node":
            return self.nodes[args[0]]
        if kind == "nodes":
            return {"kind": "List", "apiVersion": "v1", "items": list(self.nodes.values())}
        if kind == "pdb":
            assert args[1:] == ("-n", NS)
            return self.budget
        raise AssertionError((kind, args))

    def run(self, *args, body=None):
        if args[0] == "exec":
            assert args[1:4] == ("-n", NS, self.after["metadata"]["name"])
            return "ASSOC STI\nrow 1\n"
        self.mutations.append((args, body))
        if args[:2] == ("create", "-f"):
            self.budget = deepcopy(body)
            self.budget["metadata"].update(uid=uid(7), generation=2, resourceVersion="3")
            self.budget["status"] = dict(observedGeneration=2, expectedPods=1, currentHealthy=1, disruptionsAllowed=1)
            return json.dumps(self.budget)
        if args[:2] == ("patch", "node"):
            target = self.nodes[args[2]]
            patch = json.loads(args[-1])
            assert [item["op"] for item in patch] == ["test", "test", "add", "add"]
            assert patch[0]["value"] == target["metadata"]["uid"]
            assert patch[1]["value"] == target["metadata"]["resourceVersion"]
            target["metadata"]["annotations"] = patch[2]["value"]
            target["spec"]["unschedulable"] = patch[3]["value"]
        elif args[:2] == ("create", "--raw"):
            if self.block_eviction:
                raise ValueError("PDB forbids eviction")
            self.evicted = True
        elif args[:2] == ("delete", "--raw"):
            self.budget = None
        else:
            raise AssertionError(args)
        return ""

    @contextmanager
    def follow_shutdown(self, namespace, pod_name, path):
        assert namespace == NS and pod_name == self.original["metadata"]["name"]
        yield
        path.write_text('{"EventName":"m3ua.shutdown.completed"}\n', encoding="utf-8")


class DisruptionTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.cluster = FakeCluster()
        self.drill = disruption.Disruption(self.cluster, self.root, NS, uid(1), uid(2), RUN, "a" * 40)

    def test_exact_eviction_and_restoration_and_preconditioned_budget_delete(self):
        self.drill.exercise()
        pdb, claim, eviction = self.cluster.mutations
        self.assertEqual(pdb[1]["spec"], {"maxUnavailable": 1, "selector": {"matchLabels": self.drill.selector}})
        self.assertEqual(claim[0][:3], ("patch", "node", "worker-a"))
        self.assertEqual(eviction[0], ("create", "--raw", "/api/v1/namespaces/sigtran-lab/pods/sigtran-before/eviction", "-f", "-"))
        self.assertEqual(eviction[1]["deleteOptions"], {"preconditions": {"uid": uid(5)}})
        self.drill.cleanup()
        removal = self.cluster.mutations[-1]
        self.assertIn("/apis/policy/v1/namespaces/sigtran-lab/poddisruptionbudgets/", removal[0][2])
        self.assertEqual(removal[1]["preconditions"], {"uid": uid(7), "resourceVersion": "3"})
        self.assertFalse(self.cluster.nodes["worker-a"]["spec"]["unschedulable"])
        self.assertEqual(self.cluster.nodes["worker-a"]["metadata"]["annotations"], {"retained": "value"})
        receipt = json.loads((self.root / "disruption-restoration.json").read_text())
        self.assertTrue(receipt["nodeRestored"] and receipt["pdbAbsent"])

    def test_wrong_cluster_or_namespace_prevents_mutation(self):
        for field in ("cluster_uid", "namespace_uid"):
            with self.subTest(field=field):
                expected = getattr(self.cluster, field)
                setattr(self.cluster, field, uid(99))
                with self.assertRaisesRegex(ValueError, "identity does not match"):
                    self.drill.exercise()
                setattr(self.cluster, field, expected)
        self.assertEqual(self.cluster.mutations, [])

    def test_unrelated_active_pod_prevents_even_pdb_creation(self):
        self.cluster.other.append(pod("foreign", "worker-a", 50))
        with self.assertRaisesRegex(ValueError, "unrelated active workloads"):
            self.drill.exercise()
        self.assertEqual(self.cluster.mutations, [])

    def test_pdb_rejection_never_falls_back_to_direct_delete(self):
        self.cluster.block_eviction = True
        with self.assertRaisesRegex(ValueError, "PDB forbids eviction"):
            self.drill.exercise()
        self.drill.cleanup()
        deletes = [args for args, body in self.cluster.mutations if args[0] == "delete"]
        self.assertEqual(len(deletes), 1)
        self.assertIn("/poddisruptionbudgets/", deletes[0][2])
        self.assertFalse((self.root / "node-drain.json").exists())

    def test_replaced_node_or_other_owner_blocks_uncordon_but_not_pdb_cleanup(self):
        self.drill.exercise()
        worker = self.cluster.nodes["worker-a"]
        worker["metadata"]["annotations"][disruption.OWNER] = "foreign-run"
        with self.assertRaisesRegex(ValueError, "not safely restored"):
            self.drill.cleanup()
        self.assertTrue(worker["spec"]["unschedulable"])
        self.assertIsNone(self.cluster.budget)
        self.assertFalse((self.root / "disruption-restoration.json").exists())
        worker["metadata"]["uid"] = uid(99)
        with self.assertRaisesRegex(ValueError, "replacement worker"):
            self.drill.restore_node()

    def test_patch_conflict_leaves_owner_and_cordon_intact(self):
        self.drill.exercise()
        original = self.cluster.run

        def conflict(*args, **kwargs):
            if args[:2] == ("patch", "node"):
                raise ValueError("patch conflict")
            return original(*args, **kwargs)

        self.cluster.run = conflict
        with self.assertRaisesRegex(ValueError, "patch conflict"):
            self.drill.cleanup()
        worker = self.cluster.nodes["worker-a"]
        self.assertTrue(worker["spec"]["unschedulable"])
        self.assertEqual(worker["metadata"]["annotations"][disruption.OWNER], RUN)
        self.assertIsNone(self.cluster.budget)

    def test_cleanup_refuses_recreated_pdb(self):
        self.drill.exercise()
        self.cluster.budget["metadata"]["uid"] = uid(99)
        with self.assertRaisesRegex(ValueError, "replacement PDB"):
            self.drill.cleanup()
        self.assertIsNotNone(self.cluster.budget)

    def test_cleanup_without_intent_does_not_even_read_cluster(self):
        self.cluster.get = lambda *args: self.fail("Unexpected cluster read")
        self.drill.cleanup()
        self.assertEqual(self.cluster.mutations, [])

    def test_renderer_labels_only_the_pod_template_and_rejects_injection(self):
        template = ROOT / "deploy/kubernetes/deployment.yaml"
        output = self.root / "rendered.yaml"
        image = "registry.example/sigtran/node@sha256:" + "a" * 64
        renderer.render(template, output, image, RUN)
        value = output.read_text()
        self.assertEqual(value.count(disruption.RUN_LABEL), 1)
        self.assertIn(f'        {disruption.RUN_LABEL}: "{RUN}"', value)
        with self.assertRaises(ValueError):
            renderer.render(template, output, image, 'unsafe\nlabels: injected')

    def test_workflow_requires_confirmation_two_workers_and_cleanup_success(self):
        workflow = (ROOT / ".github/workflows/phase56-kubernetes-sctp.yml").read_text()
        self.assertIn('test "$MATRIX_CONFIRMATION" = "run kubernetes disruption $K8S_NAMESPACE"', workflow)
        self.assertIn('minimum_workers=2', workflow)
        self.assertIn('if: always() && inputs.matrix_profile == \'disruption\' && steps.cluster.outcome == \'success\'', workflow)
        self.assertIn('"$DISRUPTION_CLEANUP_OUTCOME" != "success"', workflow)


if __name__ == "__main__":
    unittest.main(verbosity=2)
