#!/usr/bin/env python3
"""Read-only host identity and resources; stdout belongs in protected evidence."""
import hashlib
import json
import os
import pathlib
import platform
import socket
import subprocess


def collect():
    """Retain identities, not process command lines that may contain unrelated secrets."""
    processes = []
    for process in pathlib.Path('/proc').iterdir():
        if not process.name.isdigit():
            continue
        try:
            command = (process / 'cmdline').read_bytes()
            if command.startswith(b'java\0') or b'/java\0' in command:
                if b'kafka' in command.lower():
                    stat = (process / 'stat').read_text().rsplit(')', 1)[1].split()
                    processes.append({'pid': int(process.name), 'startTicks': int(stat[19])})
        except (OSError, ValueError, IndexError):
            continue
    memory = {}
    for line in pathlib.Path('/proc/meminfo').read_text().splitlines():
        key, value = line.split(':', 1)
        if key in ('MemTotal', 'MemAvailable'):
            memory[key] = int(value.split()[0])
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 132):
        pass
    addresses = subprocess.run(['ip', '-j', '-4', 'addr'], check=True, capture_output=True, text=True)
    return {'schemaVersion': 1, 'hostname': socket.gethostname(),
            'machineHash': hashlib.sha256(pathlib.Path('/etc/machine-id').read_bytes().strip()).hexdigest(),
            'kernel': platform.release(), 'processors': len(os.sched_getaffinity(0)),
            'memoryKiB': memory, 'kafkaProcesses': processes, 'nativeSctp': True,
            'addresses': json.loads(addresses.stdout)}


if __name__ == '__main__':
    print(json.dumps(collect(), indent=2))
