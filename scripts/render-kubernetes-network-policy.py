#!/usr/bin/env python3
"""Render a namespace-scoped SCTP egress NetworkPolicy as JSON.

Workflow-supplied values are validated and serialized strictly as data. The
renderer never constructs shell, kubectl, or template expressions from input.
"""
from __future__ import annotations

import argparse
import ipaddress
import json
from pathlib import Path
import re
import sys

_DNS_LABEL = re.compile(r"^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$")


def validate_name(value: str, field: str) -> str:
    if _DNS_LABEL.fullmatch(value) is None:
        raise ValueError(f"{field} must be a Kubernetes DNS label")
    return value


def validate(namespace: str, policy_name: str, remote_ip: str, remote_port: int) -> str:
    validate_name(namespace, "namespace")
    validate_name(policy_name, "policy name")

    address = ipaddress.ip_address(remote_ip)
    if address.version != 4:
        raise ValueError("representative SCTP policy currently requires IPv4")
    if (
        address.is_loopback
        or address.is_unspecified
        or address.is_multicast
        or address.is_link_local
    ):
        raise ValueError("remote IP must be a representative non-local IPv4 address")

    if remote_port < 1 or remote_port > 65535:
        raise ValueError("remote port must be between 1 and 65535")

    return str(address)


def build(
    namespace: str,
    policy_name: str,
    remote_ip: str,
    remote_port: int,
    mode: str,
) -> dict:
    remote_ip = validate(namespace, policy_name, remote_ip, remote_port)
    egress: list[dict] = []

    if mode == "allow":
        egress = [
            {
                "to": [{"ipBlock": {"cidr": f"{remote_ip}/32"}}],
                "ports": [{"protocol": "SCTP", "port": remote_port}],
            }
        ]
    elif mode != "deny":
        raise ValueError("mode must be allow or deny")

    return {
        "apiVersion": "networking.k8s.io/v1",
        "kind": "NetworkPolicy",
        "metadata": {
            "name": policy_name,
            "namespace": namespace,
        },
        "spec": {
            "podSelector": {
                "matchLabels": {"app.kubernetes.io/name": "sigtran-node"}
            },
            "policyTypes": ["Egress"],
            "egress": egress,
        },
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--remote-ip", required=True)
    parser.add_argument("--remote-port", required=True, type=int)
    parser.add_argument("--mode", choices=("allow", "deny"), required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    value = build(
        args.namespace,
        args.name,
        args.remote_ip,
        args.remote_port,
        args.mode,
    )
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
