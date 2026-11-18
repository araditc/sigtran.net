"""Exercise metadata checks and manifest generation without contacting a lab."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]


def load(script):
    spec = importlib.util.spec_from_file_location(script, ROOT / "scripts" / (script + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


controls = load("check-kubernetes-lab-controls")
rbac = load("render-kubernetes-lab-rbac")
REPO = "araditc/sigtran.net"
ENV = f"repos/{REPO}/environments/kubernetes-sctp"
GROUP = "orgs/araditc/actions/runner-groups"
PAGE = "?per_page=100"


def page(key, values):
    return {"total_count": len(values), key: values}


class LabControlTests(unittest.TestCase):
    def setUp(self):
        self.metadata = {
            f"repos/{REPO}/branches/main": {"protected": True},
            ENV: {"name": controls.ENVIRONMENT,
                  "deployment_branch_policy": {"custom_branch_policies": True, "protected_branches": False},
                  "protection_rules": [{"type": "required_reviewers", "prevent_self_review": True,
                                        "reviewers": [{"type": "User", "reviewer": {"id": 1}}]}]},
            f"{ENV}/deployment-branch-policies{PAGE}": page("branch_policies", [{"type": "branch", "name": "main"}]),
            f"{ENV}/secrets{PAGE}": page("secrets", [{"name": key} for key in controls.SECRETS]),
            f"{ENV}/variables{PAGE}": page("variables", [{"name": key, "value": "private-placeholder"}
                                                         for key in controls.VARIABLES]),
            f"{GROUP}{PAGE}": page("runner_groups", [{"name": controls.GROUP, "id": 7, "visibility": "selected",
                                                     "default": False, "allows_public_repositories": True,
                                                     "restricted_to_workflows": True,
                                                     "selected_workflows": [f"{REPO}/{controls.WORKFLOW}"]}]),
            f"{GROUP}/7/repositories{PAGE}": page("repositories", [{"full_name": REPO}]),
            f"{GROUP}/7/runners{PAGE}": page("runners", [{"status": "online", "name": "private-runner",
                                                         "labels": [{"name": name} for name in controls.LABELS]}]),
        }

    def test_exact_metadata_does_not_activate_or_qualify_a_lab(self):
        result = controls.assess(REPO, self.metadata.__getitem__)
        self.assertTrue(result["githubMetadataConfigured"])
        for key in ("activationEnabled", "adminBypassVerified", "runnerIsolationVerifiedOnHost", "clusterVerified",
                    "peerVerified", "qualificationPassed"):
            self.assertIs(result[key], False)
        self.assertNotIn("private-", json.dumps(result))

    def test_broad_tags_group_workflows_and_other_repos_block_readiness(self):
        originals = deepcopy(self.metadata)
        for endpoint, name, extra, check in (
            (f"{ENV}/deployment-branch-policies{PAGE}", "branch_policies", {"type": "tag", "name": "main"}, "environmentMainOnly"),
            (f"{GROUP}/7/repositories{PAGE}", "repositories", {"full_name": "araditc/other"}, "runnerGroupRepositoryScope"),
        ):
            with self.subTest(check=check):
                self.metadata = deepcopy(originals)
                self.metadata[endpoint][name].append(extra)
                self.metadata[endpoint]["total_count"] += 1
                result = controls.assess(REPO, self.metadata.__getitem__)
                self.assertFalse(result["githubMetadataConfigured"])
                self.assertEqual(result["checks"][check], "missing-or-mismatched")
        self.metadata = originals
        self.metadata[f"{GROUP}{PAGE}"]["runner_groups"][0]["selected_workflows"].append("another-workflow")
        self.assertEqual(controls.assess(REPO, self.metadata.__getitem__)["checks"]["restrictedRunnerGroup"],
                         "missing-or-mismatched")

    def test_permission_denied_and_incomplete_pages_never_mean_ready(self):
        self.metadata[f"{ENV}/secrets{PAGE}"]["total_count"] += 1

        def denied(endpoint):
            if endpoint.startswith(GROUP):
                raise subprocess.CalledProcessError(1, ["gh"], stderr=b"403 private details")
            return self.metadata[endpoint]

        result = controls.assess(REPO, denied)
        self.assertFalse(result["githubMetadataConfigured"])
        self.assertEqual(result["checks"]["restrictedRunnerGroup"], "unavailable-or-invalid")
        self.assertEqual(result["checks"]["onlineRunnerInGroup"], "not-checked")
        self.assertEqual(result["checks"]["environmentSecretNames"], "unavailable-or-invalid")
        self.assertEqual(result["checks"]["environmentApproval"], "pass")

    def test_online_runner_requires_all_labels_and_environment_prevents_self_review(self):
        self.metadata[f"{GROUP}/7/runners{PAGE}"]["runners"][0]["labels"] = [{"name": controls.GROUP}]
        self.metadata[ENV]["protection_rules"][0]["prevent_self_review"] = False
        result = controls.assess(REPO, self.metadata.__getitem__)
        self.assertFalse(result["githubMetadataConfigured"])
        self.assertEqual(result["checks"]["environmentApproval"], "missing-or-mismatched")
        self.assertEqual(result["checks"]["onlineRunnerInGroup"], "missing-or-mismatched")

    def test_gh_transport_is_explicit_read_only(self):
        with patch.object(controls.shutil, "which", return_value="gh"), \
                patch.object(controls.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, b"{}")) as run:
            controls.gh_read(ENV)
        self.assertEqual(run.call_args.args[0], ["gh", "api", "--hostname", "github.com", "--method", "GET", ENV])


class LabManifestTests(unittest.TestCase):
    def test_core_role_has_no_secrets_admin_or_cluster_mutations(self):
        objects = rbac.build("sigtran-lab", "kube-system", "lab-cni")["items"]
        cluster = next(item for item in objects if item["kind"] == "ClusterRole")
        self.assertEqual(cluster["rules"], [rbac.rule("", "namespaces", "get", ["sigtran-lab", "kube-system"]),
                                           rbac.rule("", "nodes", "get list")])
        self.assertIs(objects[0]["automountServiceAccountToken"], False)
        for item in objects:
            for rule in item.get("rules", []):
                self.assertFalse({"*", "secrets", "serviceaccounts/token", "roles", "rolebindings"}.intersection(rule["resources"]))
                self.assertFalse({"*", "escalate", "bind", "impersonate"}.intersection(rule["verbs"]))
            for subject in item.get("subjects", []):
                self.assertEqual(subject, {"kind": "ServiceAccount", "name": rbac.ACCOUNT, "namespace": "sigtran-lab"})

    def test_disruption_patches_only_one_named_worker_and_cni_is_read_only(self):
        objects = rbac.build("sigtran-lab", "cni-system", "test-cni", "worker-a.lab")["items"]
        cluster = next(item for item in objects if item["kind"] == "ClusterRole")
        mutations = [rule for rule in cluster["rules"] if "patch" in rule["verbs"]]
        self.assertEqual(mutations, [rbac.rule("", "nodes", "patch", ["worker-a.lab"])])
        cni = next(item for item in objects if item["kind"] == "Role" and item["metadata"]["namespace"] == "cni-system")
        self.assertEqual(cni["rules"], [rbac.rule("apps", "daemonsets", "get", ["test-cni"])])
        lab = next(item for item in objects if item["kind"] == "Role" and item["metadata"]["namespace"] == "sigtran-lab")
        self.assertIn(rbac.rule("", "pods/eviction", "create"), lab["rules"])

    def test_reserved_names_injection_and_reused_output_are_rejected(self):
        for namespace, cni, worker in (("default", "kube-system", None), ("cni-system", "cni-system", None),
                                       ("sigtran-lab", "kube-system", "worker\nother")):
            with self.subTest(namespace=namespace, worker=worker), self.assertRaises(ValueError):
                rbac.build(namespace, cni, "lab-cni", worker)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "roles.json"
            path.write_bytes(b"retained")
            result = subprocess.run([sys.executable, str(ROOT / "scripts/render-kubernetes-lab-rbac.py"),
                                     "--namespace", "sigtran-lab", "--cni-daemonset", "test-cni", "--output", str(path)],
                                    capture_output=True, timeout=10)
            self.assertEqual(result.returncode, 1)
            self.assertEqual(path.read_bytes(), b"retained")

    def test_workflow_requires_group_and_activation_before_credentials(self):
        text = (ROOT / ".github/workflows/phase56-kubernetes-sctp.yml").read_text()
        self.assertEqual(text.count("group: sigtran-kubernetes-lab"), 2)
        self.assertNotIn("runs-on: [self-hosted", text)
        self.assertLess(text.index('[[ "$K8S_LAB_ENABLED" != "true" ]]'), text.index("Configure private kubectl credentials"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
