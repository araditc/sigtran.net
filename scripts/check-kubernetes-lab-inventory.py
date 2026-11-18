#!/usr/bin/env python3
"""Check read-only Kubernetes inventory before touching a qualification namespace."""

import argparse
import json
from pathlib import Path
import re
import sys


UID = re.compile(r"[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}")
RESERVED_NAMESPACES = {"default", "kube-system", "kube-public", "kube-node-lease"}
CONTROL_PLANE = {"node-role.kubernetes.io/control-plane", "node-role.kubernetes.io/master"}


def validate_namespace(value, name, uid, *, dedicated=False):
    """Bind an active namespace to its operator-pinned UID and requested role."""
    if not UID.fullmatch(uid):
        raise ValueError("A valid pinned namespace UID is required")
    metadata = value["metadata"]
    if (value["kind"] != "Namespace" or value["apiVersion"] != "v1"
            or metadata["name"] != name or metadata["uid"] != uid):
        raise ValueError("Qualification cluster or namespace identity does not match its pin")
    if metadata.get("deletionTimestamp") is not None or value["status"]["phase"] != "Active":
        raise ValueError("Pinned namespace must be active and not terminating")
    if dedicated and (name in RESERVED_NAMESPACES or
                      metadata.get("labels", {}).get("sigtran.net/qualification") != "enabled"):
        raise ValueError("An explicitly labelled, dedicated qualification namespace is required")


def count_ready_linux_workers(inventory):
    """Count distinct Ready Linux workers without blocking scheduling taints."""
    if inventory["kind"] not in ("List", "NodeList") or inventory["apiVersion"] != "v1":
        raise ValueError("Expected a Kubernetes node list")
    seen = set()
    count = 0
    for node in inventory["items"]:
        metadata, spec, status = node["metadata"], node["spec"], node["status"]
        name = metadata["name"]
        if not isinstance(name, str) or not name or name in seen:
            raise ValueError("Node inventory identities are missing or duplicated")
        seen.add(name)
        ready = [item["status"] for item in status.get("conditions", []) if item["type"] == "Ready"]
        if (status.get("nodeInfo", {}).get("operatingSystem") == "linux"
                and ready == ["True"] and spec.get("unschedulable", False) is False
                and metadata.get("deletionTimestamp") is None
                and not CONTROL_PLANE.intersection(metadata.get("labels", {}))
                and not any(taint["effect"] in ("NoSchedule", "NoExecute")
                            for taint in spec.get("taints", []))):
            count += 1
    return count


def validate_cni(value, name, namespace):
    """Check the named, observed DaemonSet rollout without inferring SCTP support."""
    metadata, status = value["metadata"], value["status"]
    if (value["kind"] != "DaemonSet" or value["apiVersion"] != "apps/v1"
            or metadata["name"] != name or metadata["namespace"] != namespace
            or metadata.get("deletionTimestamp") is not None):
        raise ValueError("Expected the configured non-terminating CNI DaemonSet")
    desired, generation = status["desiredNumberScheduled"], metadata["generation"]
    counts = ("currentNumberScheduled", "updatedNumberScheduled", "numberReady", "numberAvailable")
    if (type(desired) is not int or desired < 1 or type(generation) is not int or generation < 1
            or type(status.get("observedGeneration")) is not int or status["observedGeneration"] != generation
            or type(status.get("numberMisscheduled")) is not int or status["numberMisscheduled"] != 0
            or any(type(status.get(key)) is not int or status[key] != desired for key in counts)):
        raise ValueError("CNI DaemonSet has not converged on its current generation")


def check_inventory(root, *, cluster_uid, namespace, namespace_uid, cni_namespace, cni_name):
    """Return a worker count only after all prerequisites have passed."""
    def read(name):
        with (root / name).open(encoding="utf-8") as stream:
            return json.load(stream)

    validate_namespace(read("cluster-anchor.json"), "kube-system", cluster_uid)
    validate_namespace(read("qualification-namespace.json"), namespace, namespace_uid, dedicated=True)
    workers = count_ready_linux_workers(read("nodes.json"))
    if workers < 1:
        raise ValueError("No Ready, schedulable Linux worker found")
    validate_cni(read("cni-daemonset.json"), cni_name, cni_namespace)
    return workers


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("raw_root", type=Path)
    for option in ("cluster-uid", "namespace", "namespace-uid", "cni-namespace", "cni-name"):
        parser.add_argument("--" + option, required=True)
    options = vars(parser.parse_args())
    root = options.pop("raw_root")
    try:
        workers = check_inventory(root, **options)
    except (KeyError, TypeError, AttributeError, OSError, UnicodeError, json.JSONDecodeError):
        print("Kubernetes lab preflight: missing or malformed inventory", file=sys.stderr)
        return 1
    except ValueError as exc:
        print(f"Kubernetes lab preflight: {exc}", file=sys.stderr)
        return 1
    print(f"Kubernetes lab preflight: {workers} Ready Linux worker(s); SCTP not tested")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
