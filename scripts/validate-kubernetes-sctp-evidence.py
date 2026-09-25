#!/usr/bin/env python3
"""Validate sanitized Kubernetes SCTP qualification evidence.

The validator is intentionally package-neutral and offline: it consumes raw files
captured by the protected cluster workflow and emits a sanitized summary/report.
It never contacts a cluster and it fails closed when required observations are
missing or contradictory.
"""
from __future__ import annotations

import argparse
import ipaddress
import json
from pathlib import Path
import re
import sys
from typing import Any

_SHA_RE = re.compile(r"^[0-9a-f]{40}$")
_DIGEST_IMAGE_RE = re.compile(r"@sha256:([0-9a-f]{64})$")

CORE_FILES = {
    "kube": "kube-version.json",
    "nodes": "nodes.json",
    "cni": "cni-daemonset.json",
    "deployment": "deployment.json",
    "pod_initial": "pod-initial.json",
    "pod_final": "pod-final.json",
    "live_initial": "live.json",
    "ready_initial": "ready.json",
    "live_final": "live-final.json",
    "ready_final": "ready-final.json",
    "sctp_initial": "sctp-assocs.txt",
    "sctp_final": "sctp-assocs-final.txt",
    "image_revision": "image-source-revision.txt",
}

MATRIX_FILES = {
    "networkPolicy": "network-policy.json",
    "gracefulTermination": "graceful-termination.json",
    "rolloutRollback": "rollout-rollback.json",
    "nodeDrainReschedule": "node-drain.json",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--raw-root", required=True)
    parser.add_argument("--safe-root", required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--network-profile", choices=("cni", "hostNetwork"), required=True)
    parser.add_argument("--image", required=True)
    return parser.parse_args()


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"invalid JSON evidence {path.name}: {exc}") from exc
    if not isinstance(value, dict):
        raise ValueError(f"evidence {path.name} must contain a JSON object")
    return value


def nested(value: dict[str, Any], *keys: str, default: Any = None) -> Any:
    current: Any = value
    for key in keys:
        if not isinstance(current, dict) or key not in current:
            return default
        current = current[key]
    return current


def health_is_healthy(value: dict[str, Any]) -> bool:
    return str(value.get("status", "")).lower() == "healthy"


def sctp_association_count(path: Path) -> int:
    try:
        lines = path.read_text(encoding="utf-8", errors="strict").splitlines()
    except OSError as exc:
        raise ValueError(f"cannot read {path.name}: {exc}") from exc
    rows = []
    for line in lines:
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.upper().startswith("ASSOC"):
            continue
        rows.append(stripped)
    return len(rows)


def cni_identity(value: dict[str, Any]) -> tuple[str, list[str]]:
    name = str(nested(value, "metadata", "name", default="")).strip()
    containers = nested(value, "spec", "template", "spec", "containers", default=[])
    images = sorted({
        str(container.get("image", "")).strip()
        for container in containers
        if isinstance(container, dict) and str(container.get("image", "")).strip()
    })
    return name, images


def sanitize_image_location(image: str) -> str:
    """Remove registry/repository path while retaining the public artifact identity."""
    return image.rsplit("/", 1)[-1].strip()


def pseudonymize_nodes(*nodes: str) -> tuple[str, ...]:
    aliases: dict[str, str] = {}
    result: list[str] = []
    for node in nodes:
        if not node:
            result.append("")
            continue
        if node not in aliases:
            aliases[node] = f"node-{len(aliases) + 1}"
        result.append(aliases[node])
    return tuple(result)


def linux_node_facts(value: dict[str, Any]) -> tuple[list[str], list[str], int]:
    items = value.get("items", [])
    if not isinstance(items, list):
        return [], [], 0
    kernels: set[str] = set()
    os_images: set[str] = set()
    linux_nodes = 0
    for item in items:
        if not isinstance(item, dict):
            continue
        node_info = nested(item, "status", "nodeInfo", default={})
        if not isinstance(node_info, dict):
            continue
        operating_system = str(node_info.get("operatingSystem", "")).lower()
        if operating_system != "linux":
            continue
        linux_nodes += 1
        kernel = str(node_info.get("kernelVersion", "")).strip()
        os_image = str(node_info.get("osImage", "")).strip()
        if kernel:
            kernels.add(kernel)
        if os_image:
            os_images.add(os_image)
    return sorted(kernels), sorted(os_images), linux_nodes


def pod_identity(value: dict[str, Any]) -> tuple[str, str, str, str]:
    uid = str(nested(value, "metadata", "uid", default="")).strip()
    node = str(nested(value, "spec", "nodeName", default="")).strip()
    containers = nested(value, "spec", "containers", default=[])
    image = ""
    if isinstance(containers, list) and containers and isinstance(containers[0], dict):
        image = str(containers[0].get("image", "")).strip()
    statuses = nested(value, "status", "containerStatuses", default=[])
    image_id = ""
    if isinstance(statuses, list):
        for status in statuses:
            if isinstance(status, dict) and status.get("name") == "sigtran-node":
                image_id = str(status.get("imageID", "")).strip()
                break
        if not image_id and statuses and isinstance(statuses[0], dict):
            image_id = str(statuses[0].get("imageID", "")).strip()
    return uid, node, image, image_id


def matrix_result(path: Path, source_sha: str) -> tuple[bool, str]:
    if not path.exists():
        return False, "not executed"
    value = load_json(path)
    if value.get("schemaVersion") != 1:
        return False, "invalid schemaVersion"
    if value.get("sourceSha") != source_sha:
        return False, "source SHA mismatch"
    if value.get("passed") is not True:
        return False, "reported failure"
    return True, "passed"


def main() -> int:
    args = parse_args()
    source_sha = args.source_sha.lower()
    if not _SHA_RE.fullmatch(source_sha):
        raise SystemExit("--source-sha must be a 40-character lowercase commit SHA")
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,120}", args.run_id):
        raise SystemExit("--run-id is not a bounded safe identifier")

    digest_match = _DIGEST_IMAGE_RE.search(args.image)
    if digest_match is None:
        raise SystemExit("--image must be pinned by sha256 digest, not a mutable tag")
    expected_image_digest = digest_match.group(1)

    raw = Path(args.raw_root)
    safe = Path(args.safe_root)
    safe.mkdir(parents=True, exist_ok=True)

    paths = {name: raw / filename for name, filename in CORE_FILES.items()}
    missing = [path.name for path in paths.values() if not path.is_file()]
    if missing:
        raise SystemExit("missing required raw evidence: " + ", ".join(sorted(missing)))

    kube = load_json(paths["kube"])
    nodes = load_json(paths["nodes"])
    cni = load_json(paths["cni"])
    deployment = load_json(paths["deployment"])
    pod_initial = load_json(paths["pod_initial"])
    pod_final = load_json(paths["pod_final"])
    live_initial = load_json(paths["live_initial"])
    ready_initial = load_json(paths["ready_initial"])
    live_final = load_json(paths["live_final"])
    ready_final = load_json(paths["ready_final"])

    server_version = str(nested(kube, "serverVersion", "gitVersion", default="")).strip()
    kernels, os_images, linux_nodes = linux_node_facts(nodes)
    cni_name, cni_images = cni_identity(cni)

    host_network = bool(nested(
        deployment, "spec", "template", "spec", "hostNetwork", default=False
    ))
    expected_host_network = args.network_profile == "hostNetwork"

    initial_uid, initial_node, initial_image, initial_image_id = pod_identity(pod_initial)
    final_uid, final_node, final_image, final_image_id = pod_identity(pod_final)
    public_initial_node, public_final_node = pseudonymize_nodes(initial_node, final_node)
    public_cni_images = sorted({
        sanitized
        for image in cni_images
        if (sanitized := sanitize_image_location(image))
    })

    image_digest_visible_initial = f"sha256:{expected_image_digest}" in initial_image_id
    image_digest_visible_final = f"sha256:{expected_image_digest}" in final_image_id

    initial_associations = sctp_association_count(paths["sctp_initial"])
    final_associations = sctp_association_count(paths["sctp_final"])
    image_revision = paths["image_revision"].read_text(
        encoding="utf-8", errors="strict"
    ).strip().lower()

    core_checks = {
        "kubernetesServerVersionObserved": bool(server_version),
        "linuxWorkerObserved": linux_nodes > 0 and bool(kernels),
        "cniDaemonSetIdentityObserved": bool(cni_name) and bool(cni_images),
        "deploymentProfileMatches": host_network == expected_host_network,
        "initialLivenessHealthy": health_is_healthy(live_initial),
        "initialReadinessHealthy": health_is_healthy(ready_initial),
        "finalLivenessHealthy": health_is_healthy(live_final),
        "finalReadinessHealthy": health_is_healthy(ready_final),
        "initialSctpAssociationObserved": initial_associations > 0,
        "finalSctpAssociationObserved": final_associations > 0,
        "podRestartObserved": bool(initial_uid) and bool(final_uid) and initial_uid != final_uid,
        "podNodeIdentityObserved": bool(initial_node) and bool(final_node),
        "digestPinnedImageConfigured": initial_image == args.image and final_image == args.image,
        "runtimeImageDigestObserved": image_digest_visible_initial and image_digest_visible_final,
        "imageRevisionMatchesSource": image_revision == source_sha,
    }
    execution_passed = all(core_checks.values())

    matrix: dict[str, dict[str, Any]] = {}
    for name, filename in MATRIX_FILES.items():
        passed, detail = matrix_result(raw / filename, source_sha)
        matrix[name] = {"passed": passed, "detail": detail}

    gate_eligible = execution_passed and all(
        value["passed"] is True for value in matrix.values()
    )

    result = {
        "schemaVersion": 2,
        "runId": args.run_id,
        "sourceSha": source_sha,
        "networkProfile": args.network_profile,
        "image": args.image,
        "serverVersion": server_version,
        "linuxNodeCount": linux_nodes,
        "kernelVersions": kernels,
        "osImages": os_images,
        "cniDaemonSet": cni_name,
        "cniImages": public_cni_images,
        "initialPodNode": public_initial_node,
        "finalPodNode": public_final_node,
        "initialSctpAssociationCount": initial_associations,
        "finalSctpAssociationCount": final_associations,
        "imageRevision": image_revision,
        "checks": core_checks,
        "matrix": matrix,
        "executionPassed": execution_passed,
        "gateEligible": gate_eligible,
        "passed": gate_eligible,
    }

    summary = safe / "summary.json"
    summary.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    report_lines = [
        "# Kubernetes SCTP Qualification Evidence",
        "",
        f"- Run id: `{args.run_id}`",
        f"- Source SHA: `{source_sha}`",
        f"- Network profile: `{args.network_profile}`",
        f"- Image: `{args.image}`",
        f"- Kubernetes server: `{server_version or 'missing'}`",
        f"- CNI DaemonSet: `{cni_name or 'missing'}`",
        f"- CNI images: `{', '.join(public_cni_images) or 'missing'}`",
        f"- Linux worker nodes observed: `{linux_nodes}`",
        f"- Initial/final SCTP associations: `{initial_associations}/{final_associations}`",
        f"- Image source revision: `{image_revision or 'missing'}`",
        "",
        "## Core observations",
        "",
    ]
    report_lines.extend(
        f"- {name}: {'PASS' if passed else 'FAIL'}"
        for name, passed in core_checks.items()
    )
    report_lines.extend(["", "## Required representative matrix", ""])
    report_lines.extend(
        f"- {name}: {'PASS' if value['passed'] else 'OPEN'} — {value['detail']}"
        for name, value in matrix.items()
    )
    report_lines.extend([
        "",
        f"- Core execution result: `{'PASS' if execution_passed else 'FAIL'}`",
        f"- Gate-eligible result: `{'PASS' if gate_eligible else 'NO'}`",
        "",
        "A core PASS is not stable-gate evidence by itself. Gate eligibility also",
        "requires retained source-bound evidence for NetworkPolicy behavior, graceful",
        "termination, rollout/rollback, and node drain/rescheduling.",
        "",
    ])
    (safe / "report.md").write_text("\n".join(report_lines), encoding="utf-8")

    print(json.dumps({
        "executionPassed": execution_passed,
        "gateEligible": gate_eligible,
        "summary": str(summary),
    }, separators=(",", ":")))
    return 0 if execution_passed else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        raise SystemExit(2)
