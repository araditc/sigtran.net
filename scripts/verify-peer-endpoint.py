#!/usr/bin/env python3
"""Fail closed unless an authenticated peer owns the configured IPv4 endpoint."""
import ipaddress
import re
import sys
from pathlib import Path

if len(sys.argv) != 3:
    raise SystemExit("usage: verify-peer-endpoint.py <remote-ip> <peer-address-inventory>")

target = ipaddress.ip_address(sys.argv[1])
if target.version != 4:
    raise SystemExit("peer endpoint verification currently requires IPv4")

inventory_path = Path(sys.argv[2])
if not inventory_path.is_file():
    raise SystemExit("peer address inventory is missing")

addresses = set()
for line in inventory_path.read_text(encoding="utf-8").splitlines():
    match = re.search(r"\binet\s+([0-9.]+)/([0-9]+)\b", line)
    if match is None:
        continue
    try:
        interface = ipaddress.ip_interface(f"{match.group(1)}/{match.group(2)}")
    except ValueError as exc:
        raise SystemExit(f"invalid peer address inventory: {exc}") from exc
    addresses.add(interface.ip)

if target not in addresses:
    raise SystemExit("authenticated peer does not own REMOTE_IP")
