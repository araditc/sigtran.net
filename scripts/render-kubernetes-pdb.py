#!/usr/bin/env python3
"""Render a run-scoped PodDisruptionBudget for Kubernetes qualification."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import sys

_DNS_LABEL = re.compile(r"^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$")


def validate_name(value: str, field: str) -> str:
    if _DNS_LABEL.fullmatch(value) is None:
        raise ValueError(f"{field} must be a Kubernetes DNS label")
    return value


def build(namespace: str, name: str) -> dict:
    validate_name(namespace, "namespace")
    validate_name(name, "PDB name")
    return {
        "apiVersion": "policy/v1",
        "kind": "PodDisruptionBudget",
        "metadata": {
            "name": name,
            "namespace": namespace,
        },
        "spec": {
            "maxUnavailable": 1,
            "unhealthyPodEvictionPolicy": "AlwaysAllow",
            "selector": {
                "matchLabels": {
                    "app.kubernetes.io/name": "sigtran-node",
                }
            },
        },
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    value = build(args.namespace, args.name)
    output = Path(args.output)
    output.write_text(
        json.dumps(value, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        raise SystemExit(2)
