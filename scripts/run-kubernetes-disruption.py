#!/usr/bin/env python3
"""Exercise a single run-owned pod eviction and restore the approved worker."""

import argparse
from contextlib import contextmanager
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time


spec = importlib.util.spec_from_file_location("lab_inventory", Path(__file__).with_name("check-kubernetes-lab-inventory.py"))
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)

RUN_LABEL = "sigtran.net/qualification-run"
APP_LABEL = "app.kubernetes.io/name"
DRAIN_LABEL = "sigtran.net/qualification-drain"
OWNER = "sigtran.net/drain-owner"
NAME = re.compile(r"[a-z0-9](?:[-a-z0-9.]{0,251}[a-z0-9])?")
RUN_ID = re.compile(r"phase56-k8s-[0-9]+-[0-9]+")


def uid(value):
    """Require a Kubernetes object identity before building a precondition."""
    result = value["metadata"]["uid"]
    if not inventory.UID.fullmatch(result):
        raise ValueError("Object UID is invalid")
    return result


def wait_for(check, seconds, description):
    """Poll boundedly without resubmitting an eviction or widening it."""
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if result := check():
            return result
        time.sleep(1)
    raise ValueError(description)


class Kubectl:
    """Bound command time and keep cluster identifiers and errors out of public logs."""

    def run(self, *args, body=None):
        result = subprocess.run(["kubectl", "--request-timeout=15s", *args],
                                input=None if body is None else json.dumps(body), capture_output=True,
                                encoding="utf-8", timeout=20, check=False)
        if result.returncode:
            raise ValueError(f"kubectl {args[0]} failed; verify permissions, budget and lab state")
        return result.stdout

    def get(self, *args, absent=False):
        result = self.run("get", *args, *(('--ignore-not-found=true',) if absent else ()), "-o", "json")
        return json.loads(result) if result else None

    @contextmanager
    def follow_shutdown(self, namespace, pod, path):
        with path.open("xb") as stream:
            process = subprocess.Popen(["kubectl", "--request-timeout=240s", "-n", namespace, "logs",
                                        "--follow", "--tail=0", pod], stdout=stream, stderr=subprocess.DEVNULL)
            try:
                time.sleep(1)
                if process.poll() is not None:
                    raise ValueError("Cannot start the shutdown event stream")
                yield
            finally:
                if process.poll() is None:
                    process.terminate()
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=5)


def node_patch(node, run_id, *, restore):
    """Compare identity and version before an atomic owner/scheduling transition."""
    metadata = node["metadata"]
    annotations = dict(metadata.get("annotations") or {})
    unschedulable = node["spec"].get("unschedulable", False)
    if restore:
        if annotations.get(OWNER) != run_id or unschedulable is not True:
            raise ValueError("Cannot restore a node without exact active ownership")
        del annotations[OWNER]
    else:
        if OWNER in annotations or unschedulable is not False:
            raise ValueError("Cannot claim an owned or already cordoned node")
        annotations[OWNER] = run_id
    return [
        {"op": "test", "path": "/metadata/uid", "value": uid(node)},
        {"op": "test", "path": "/metadata/resourceVersion", "value": metadata["resourceVersion"]},
        {"op": "add", "path": "/metadata/annotations", "value": annotations},
        {"op": "add", "path": "/spec/unschedulable", "value": not restore},
    ]


class Disruption:
    def __init__(self, client, root, namespace, cluster_uid, namespace_uid, run_id, source):
        if not NAME.fullmatch(namespace) or not RUN_ID.fullmatch(run_id) or len(run_id) > 59:
            raise ValueError("Invalid qualification namespace or run identifier")
        if not re.fullmatch(r"[0-9a-f]{40}", source):
            raise ValueError("A complete source commit is required")
        self.client, self.root, self.namespace, self.run_id = client, root, namespace, run_id
        self.binding = {"runId": run_id, "sourceSha": source, "namespace": namespace,
                        "clusterUid": cluster_uid, "namespaceUid": namespace_uid}
        self.pdb = f"{run_id}-pdb"
        self.selector = {APP_LABEL: "sigtran-node", RUN_LABEL: run_id}

    def save(self, file, data):
        with (self.root / file).open("x", encoding="utf-8") as stream:
            json.dump(data, stream, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())

    def intent(self, file, **data):
        self.save(file, {"binding": self.binding, **data})

    def load_intent(self, file):
        with (self.root / file).open(encoding="utf-8") as stream:
            value = json.load(stream)
        if value["binding"] != self.binding:
            raise ValueError("Cleanup journal is bound to a different target or run")
        return value

    def check_target(self):
        for name, key, dedicated in (("kube-system", "clusterUid", False), (self.namespace, "namespaceUid", True)):
            inventory.validate_namespace(self.client.get("namespace", name), name, self.binding[key], dedicated=dedicated)

    def pods(self):
        selector = ",".join(f"{key}={value}" for key, value in self.selector.items())
        return self.client.get("pods", "-n", self.namespace, "-l", selector)["items"]

    def ready_pod(self, pod):
        metadata = pod["metadata"]
        return (metadata["namespace"] == self.namespace and metadata.get("deletionTimestamp") is None
                and NAME.fullmatch(metadata["name"]) and all(metadata.get("labels", {}).get(k) == v for k, v in self.selector.items())
                and pod["spec"].get("terminationGracePeriodSeconds") == 30
                and [c["status"] for c in pod["status"].get("conditions", []) if c["type"] == "Ready"] == ["True"])

    def check_worker(self, pod):
        node_name = pod["spec"]["nodeName"]
        if not NAME.fullmatch(node_name):
            raise ValueError("Invalid worker name")
        node = self.client.get("node", node_name)
        workers = inventory.ready_linux_workers(self.client.get("nodes"))
        if (node["metadata"]["name"] != node_name or node_name not in workers or len(workers) < 2
                or not inventory.ready_linux_workers({"kind": "List", "apiVersion": "v1", "items": [node]})
                or node["metadata"].get("labels", {}).get(DRAIN_LABEL) != "true"):
            raise ValueError("Drain requires a labelled Ready Linux worker and another eligible worker")
        colocated = self.client.get("pods", "--all-namespaces", "--field-selector", f"spec.nodeName={node_name}")["items"]
        for other in colocated:
            if uid(other) == uid(pod) or other["status"].get("phase") in ("Failed", "Succeeded"):
                continue
            if not any(ref.get("controller") is True and ref.get("kind") == "DaemonSet"
                       for ref in other["metadata"].get("ownerReferences", [])):
                raise ValueError("Refusing to cordon a worker with unrelated active workloads")
        return node

    def build_pdb(self):
        """Use the PR #33 single-replica budget, scoped to this run's pod label."""
        return {"apiVersion": "policy/v1", "kind": "PodDisruptionBudget",
                "metadata": {"name": self.pdb, "namespace": self.namespace, "labels": {RUN_LABEL: self.run_id}},
                "spec": {"maxUnavailable": 1, "selector": {"matchLabels": self.selector}}}

    def observed_pdb(self):
        value = self.client.get("pdb", self.pdb, "-n", self.namespace)
        status, metadata, budget = value.get("status", {}), value["metadata"], value["spec"]
        expected = self.build_pdb()
        if (type(budget.get("maxUnavailable")) is not int or budget["maxUnavailable"] != 1
                or budget.get("minAvailable") is not None or budget.get("selector") != expected["spec"]["selector"]
                or budget.get("unhealthyPodEvictionPolicy") not in (None, "IfHealthyBudget")
                or metadata["labels"].get(RUN_LABEL) != self.run_id
                or metadata["name"] != self.pdb or metadata["namespace"] != self.namespace):
            raise ValueError("Observed PDB is not bound to this qualification run")
        generation = metadata["generation"]
        if (type(generation) is int and generation > 0 and status.get("observedGeneration") == generation
                and all(type(status.get(key)) is int and status[key] == 1
                        for key in ("expectedPods", "currentHealthy", "disruptionsAllowed"))):
            return value
        return None

    def evict(self, pod):
        """Ask the API to honor the PDB and never fall back to pod deletion."""
        name = pod["metadata"]["name"]
        self.client.run("create", "--raw", f"/api/v1/namespaces/{self.namespace}/pods/{name}/eviction", "-f", "-",
                        body={"apiVersion": "policy/v1", "kind": "Eviction",
                              "metadata": {"name": name, "namespace": self.namespace},
                              "deleteOptions": {"preconditions": {"uid": uid(pod)}}})

    def replacement(self, original, node_name):
        pods = self.pods()
        if (len(pods) == 1 and self.ready_pod(pods[0]) and uid(pods[0]) != uid(original)
                and replica_set_uid(pods[0]) == replica_set_uid(original)):
            replacement_node = pods[0]["spec"]["nodeName"]
            if replacement_node != node_name and replacement_node in inventory.ready_linux_workers(self.client.get("nodes")):
                return pods[0]
        return None

    def exercise(self):
        self.check_target()
        pods = self.pods()
        if len(pods) != 1 or not self.ready_pod(pods[0]):
            raise ValueError("Exactly one Ready, run-owned pod is required")
        original = pods[0]
        replica_set_uid(original)
        node = self.check_worker(original)
        node_patch(node, self.run_id, restore=False)
        self.intent("pdb-intent.json", name=self.pdb)
        created = json.loads(self.client.run("create", "-f", "-", "-o", "json", body=self.build_pdb()))
        self.save("pdb-created.json", {"uid": uid(created)})
        budget = wait_for(self.observed_pdb, 90, "The PDB did not allow one voluntary disruption")
        if uid(budget) != uid(created):
            raise ValueError("Observed PDB is not the created object")
        self.save("pdb-observed.json", budget)
        self.receipt("pdb.json", maxUnavailable=1, disruptionsAllowed=1, singleReplicaAvailabilityClaim=False)

        node = self.check_worker(original)
        node_name = node["metadata"]["name"]
        patch = node_patch(node, self.run_id, restore=False)
        log = self.root / "node-drain-shutdown.log"
        with self.client.follow_shutdown(self.namespace, original["metadata"]["name"], log):
            self.intent("drain-intent.json", name=node_name, uid=uid(node))
            self.client.run("patch", "node", node_name, "--type=json", "-p", json.dumps(patch))
            cordoned = self.client.get("node", node_name)
            if (uid(cordoned) != uid(node) or cordoned["spec"].get("unschedulable") is not True
                    or (cordoned["metadata"].get("annotations") or {}).get(OWNER) != self.run_id):
                raise ValueError("Exact cordon ownership was not observed before eviction")
            self.evict(original)
            replacement = wait_for(lambda: self.replacement(original, node_name), 180,
                                   "No Ready run-owned replacement on a different worker")
        if not any(shutdown_event(line) for line in log.read_text(encoding="utf-8").splitlines()):
            raise ValueError("Owned pod did not report its structured shutdown completion")
        associations = self.client.run("exec", "-n", self.namespace, replacement["metadata"]["name"],
                                       "--", "cat", "/proc/net/sctp/assocs")
        (self.root / "sctp-assocs-after-node-drain.txt").write_text(associations, encoding="utf-8")
        if sum(bool(line.strip()) for line in associations.splitlines()) < 2:
            raise ValueError("No SCTP association row after owned pod eviction")
        self.save("drain-replacement-pod.json", replacement)
        self.receipt("node-drain.json", evictionPath="policy-v1-single-pod", rescheduledToDifferentNode=True,
                     sctpAssociationObserved=True, shutdownEventObserved=True)

    def receipt(self, file, **fields):
        self.save(file, {"schemaVersion": 1, "sourceSha": self.binding["sourceSha"], "runId": self.run_id,
                         "passed": True, **fields})

    def restore_node(self):
        record = self.load_intent("drain-intent.json")
        name = record["name"]
        if not NAME.fullmatch(name):
            raise ValueError("Invalid recorded node name")
        node = self.client.get("node", name)
        if uid(node) != record["uid"] or node["metadata"]["name"] != name:
            raise ValueError("Cannot restore a replacement worker")
        owner = (node["metadata"].get("annotations") or {}).get(OWNER)
        if owner == self.run_id:
            patch = node_patch(node, self.run_id, restore=True)
            self.client.run("patch", "node", name, "--type=json", "-p", json.dumps(patch))
            node = self.client.get("node", name)
        if (uid(node) != record["uid"] or node["spec"].get("unschedulable", False) is not False
                or OWNER in (node["metadata"].get("annotations") or {})):
            raise ValueError("Worker ownership or scheduling was not safely restored")
        self.save("drain-node-restored.json", node)

    def remove_pdb(self):
        if self.load_intent("pdb-intent.json")["name"] != self.pdb:
            raise ValueError("PDB journal identity mismatch")
        value = self.client.get("pdb", self.pdb, "-n", self.namespace, absent=True)
        if value is not None:
            metadata = value["metadata"]
            if (metadata["labels"].get(RUN_LABEL) != self.run_id or metadata["name"] != self.pdb
                    or metadata["namespace"] != self.namespace):
                raise ValueError("Refusing to remove an unowned PDB")
            created = self.root / "pdb-created.json"
            if created.is_file() and json.loads(created.read_text(encoding="utf-8"))["uid"] != uid(value):
                raise ValueError("Refusing to remove a replacement PDB")
            self.client.run("delete", "--raw", f"/apis/policy/v1/namespaces/{self.namespace}/poddisruptionbudgets/{self.pdb}",
                            "-f", "-", body={"apiVersion": "v1", "kind": "DeleteOptions",
                                               "preconditions": {"uid": uid(value), "resourceVersion": metadata["resourceVersion"]}})
            wait_for(lambda: self.client.get("pdb", self.pdb, "-n", self.namespace, absent=True) is None,
                     30, "Run-owned PDB absence was not confirmed")

    def cleanup(self):
        node = (self.root / "drain-intent.json").is_file()
        pdb = (self.root / "pdb-intent.json").is_file()
        if not (node or pdb):
            return
        self.check_target()
        try:
            if node:
                self.restore_node()
        finally:
            if pdb:
                self.remove_pdb()
        if node and pdb:
            self.receipt("disruption-restoration.json", nodeRestored=True, pdbAbsent=True)


def replica_set_uid(pod):
    """Require a single controller so rescheduling cannot adopt an unrelated pod."""
    refs = [ref for ref in pod["metadata"].get("ownerReferences", []) if ref.get("controller") is True]
    if len(refs) != 1 or refs[0].get("kind") != "ReplicaSet" or not inventory.UID.fullmatch(refs[0]["uid"]):
        raise ValueError("Expected a pod owned by one ReplicaSet")
    return refs[0]["uid"]


def shutdown_event(line):
    """Only count a structured completion event from the followed pod log."""
    try:
        value = json.loads(line)
        return isinstance(value, dict) and value.get("EventName") == "m3ua.shutdown.completed"
    except json.JSONDecodeError:
        return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("exercise", "cleanup"))
    parser.add_argument("raw_root", type=Path)
    for option in ("namespace", "cluster-uid", "namespace-uid", "run-id", "source"):
        parser.add_argument("--" + option, required=True)
    args = parser.parse_args()
    try:
        drill = Disruption(Kubectl(), args.raw_root, args.namespace, args.cluster_uid,
                           args.namespace_uid, args.run_id, args.source)
        (drill.exercise if args.action == "exercise" else drill.cleanup)()
    except (ValueError, OSError, subprocess.SubprocessError, TypeError, KeyError, AttributeError):
        print(f"Disruption {args.action} failed; inspect the approved lab and private receipts", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
