#!/usr/bin/env python3
"""Collect read-only Linux prerequisites. Store stdout only in protected lab storage."""
import ctypes.util
import datetime
import hashlib
import json
import pathlib
import platform
import shutil
import socket
import subprocess
import sys


def collect(node_id, role):
    """Probe capabilities without installing packages or changing kernel configuration."""
    def command(*args):
        try:
            result = subprocess.run(args, text=True, capture_output=True, timeout=10, check=False)
            return result.returncode == 0, result.stdout.strip()
        except (OSError, subprocess.TimeoutExpired):
            return False, ""

    machine = pathlib.Path("/etc/machine-id")
    machine_hash = hashlib.sha256(machine.read_bytes().strip()).hexdigest() if machine.exists() else ""
    sctp = False
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 132):
            sctp = True
    except OSError:
        pass
    _, dotnet_version = command("dotnet", "--version")
    sudo_available, _ = command("sudo", "-n", "true")
    # Listing link-layer types opens the device but captures/stores no packets.
    capture_available, _ = command("dumpcap", "-i", "lo", "-p", "-L")
    names = ("gcc", "tcpdump", "tshark", "ip", "ssh", "timeout", "python3")
    return {
        "schemaVersion": 1,
        "nodeId": node_id,
        "role": role,
        "machineHash": machine_hash,
        "operatingSystem": platform.system().lower(),
        "kernel": platform.release(),
        "dotnetVersion": dotnet_version,
        "sctpAvailable": sctp,
        "lksctpAvailable": ctypes.util.find_library("sctp") is not None,
        "sudoAvailable": sudo_available,
        "captureAvailable": capture_available,
        "tools": {name: shutil.which(name) is not None for name in names},
        "capturedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    }


if __name__ == "__main__":
    if len(sys.argv) != 3 or sys.argv[2] not in ("sdk", "peer"):
        raise SystemExit("Usage: collect-host-inventory.py <node-id> <sdk|peer>")
    print(json.dumps(collect(sys.argv[1], sys.argv[2]), indent=2))
