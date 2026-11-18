#!/usr/bin/env python3
"""Render a Kubernetes lab service account and bounded roles; never contact the API."""

import argparse
import json
from pathlib import Path
import re
import sys


ACCOUNT = "sigtran-kubernetes-lab"
DNS_LABEL = re.compile(r"[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?")
RESERVED_NAMESPACES = {"default", "kube-system", "kube-public", "kube-node-lease"}


def rule(group, resources, verbs, names=None):
    value = {"apiGroups": [group], "resources": resources.split(), "verbs": verbs.split()}
    if names is not None:
        value["resourceNames"] = names
    return value


def build(namespace, cni_namespace, cni_name, drain_node=None):
    """Keep workload mutation namespaced and make named node patch opt-in."""
    if (not DNS_LABEL.fullmatch(namespace) or namespace in RESERVED_NAMESPACES or namespace == cni_namespace
            or not DNS_LABEL.fullmatch(cni_namespace)):
        raise ValueError("Separate valid lab and CNI namespaces are required")
    for name in (cni_name, drain_node):
        if name is not None and (len(name) > 253 or not all(DNS_LABEL.fullmatch(part) for part in name.split("."))):
            raise ValueError("CNI DaemonSet and worker names must be Kubernetes DNS subdomains")

    account = {"apiVersion": "v1", "kind": "ServiceAccount", "metadata": {"name": ACCOUNT, "namespace": namespace},
               "automountServiceAccountToken": False}
    subject = {"kind": "ServiceAccount", "name": ACCOUNT, "namespace": namespace}
    cluster_name = f"sigtran-lab-{namespace}"

    def pair(name, rules, scoped_namespace=None):
        """Bind exactly the lab service account, including for the CNI read role."""
        role_kind = "Role" if scoped_namespace is not None else "ClusterRole"
        metadata = {"name": name}
        if scoped_namespace is not None:
            metadata["namespace"] = scoped_namespace
        common = {"apiVersion": "rbac.authorization.k8s.io/v1", "metadata": metadata}
        return [{**common, "kind": role_kind, "rules": rules},
                {**common, "kind": role_kind + "Binding", "subjects": [subject],
                 "roleRef": {"apiGroup": "rbac.authorization.k8s.io", "kind": role_kind, "name": name}}]

    namespaced = [
        rule("", "configmaps services", "create"),
        rule("", "configmaps", "get patch", ["sigtran-node"]),
        rule("", "services", "get patch", ["sigtran-node-management"]),
        rule("", "pods", "get list watch delete"),
        rule("", "pods/log", "get"),
        rule("", "pods/exec pods/portforward", "create"),
        rule("apps", "deployments", "create list watch"),
        rule("apps", "deployments", "get patch", ["sigtran-node"]),
        rule("apps", "replicasets", "get list"),
        rule("discovery.k8s.io", "endpointslices", "get list"),
        rule("networking.k8s.io", "networkpolicies", "create get list patch delete"),
    ]
    cluster = [rule("", "namespaces", "get", [namespace, "kube-system"]), rule("", "nodes", "get list")]
    if drain_node is not None:
        namespaced.extend([rule("policy", "poddisruptionbudgets", "create get delete"), rule("", "pods/eviction", "create")])
        cluster.extend([rule("", "pods", "list"), rule("", "nodes", "patch", [drain_node])])

    return {"apiVersion": "v1", "kind": "List", "items": [account,
            *pair(cluster_name, cluster), *pair(ACCOUNT, namespaced, namespace),
            *pair(cluster_name + "-cni-read", [rule("apps", "daemonsets", "get", [cni_name])], cni_namespace)]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--cni-namespace", default="kube-system")
    parser.add_argument("--cni-daemonset", required=True)
    parser.add_argument("--drain-node", help="Optional exact approved worker; omit for core and CNI-policy")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        value = build(args.namespace, args.cni_namespace, args.cni_daemonset, args.drain_node)
        with args.output.open("x", encoding="utf-8") as stream:
            json.dump(value, stream, indent=2)
            stream.write("\n")
    except (ValueError, OSError) as exc:
        print(f"Lab RBAC was not rendered: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
