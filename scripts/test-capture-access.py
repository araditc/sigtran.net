#!/usr/bin/env python3
"""Verify unprivileged dumpcap using only generated loopback SCTP traffic."""
import hashlib
import json
import os
from pathlib import Path
import socket
import stat
import subprocess
import sys
import time


def verify(output_root):
    """Retain private PCAP and sanitized observations; never request sudo."""
    if os.geteuid() == 0:
        raise ValueError("Run this proof as the lab user, not root")
    output_root.mkdir(mode=0o700, parents=True, exist_ok=False)
    os.chmod(output_root, 0o700)
    pcap = output_root / "capture-access.pcap"
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 132) as listener:
        listener.bind(("127.0.0.1", 0))
        listener.listen(1)
        listener.settimeout(5)
        port = listener.getsockname()[1]
        process = subprocess.Popen(
            ["dumpcap", "-q", "-p", "-P", "-i", "lo", "-a", "duration:5",
             "-w", str(pcap), "-f", f"sctp and port {port}"],
            stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True, umask=0o077)
        try:
            deadline = time.monotonic() + 4
            while not pcap.exists() or pcap.stat().st_size < 24:
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError("Unprivileged capture did not start")
                time.sleep(0.05)
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 132) as client:
                client.settimeout(5)
                client.connect(("127.0.0.1", port))
                connection, _ = listener.accept()
                with connection:
                    connection.settimeout(5)
                    client.sendall(b"sigtran-capture-proof")
                    if connection.recv(128) != b"sigtran-capture-proof":
                        raise RuntimeError("SCTP proof payload mismatch")
                    connection.sendall(b"ack")
                    if client.recv(128) != b"ack":
                        raise RuntimeError("SCTP proof response mismatch")
            _, error = process.communicate(timeout=8)
            if process.returncode != 0:
                raise RuntimeError("Unprivileged dumpcap failed: " + error)
        finally:
            if process.poll() is None:
                process.terminate()
                process.communicate(timeout=5)
    decoded = subprocess.run(["tshark", "-r", str(pcap), "-Y", "sctp", "-T", "fields", "-e", "frame.number"],
                             capture_output=True, text=True, timeout=10, check=True)
    frames = len(decoded.stdout.splitlines())
    info = pcap.stat()
    if frames == 0 or info.st_uid != os.getuid() or stat.S_IMODE(info.st_mode) != 0o600:
        raise RuntimeError("Capture content/ownership/permissions validation failed")
    report = {"schemaVersion": 1, "captureBackend": "dumpcap", "captureSucceeded": True,
              "effectiveUid": os.geteuid(), "capturedSctpFrames": frames, "pcapBytes": info.st_size,
              "pcapSha256": hashlib.sha256(pcap.read_bytes()).hexdigest(),
              "fileOwnedByCaller": True, "pcapMode": "0600", "outputDirectoryMode": "0700"}
    (output_root / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    return report


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: test-capture-access.py <new-private-output-directory>")
    print(json.dumps(verify(Path(sys.argv[1]).resolve()), indent=2))
