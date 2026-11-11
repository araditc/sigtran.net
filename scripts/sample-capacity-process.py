#!/usr/bin/env python3
"""Read bounded Linux CPU counters for the owned capacity workload without attaching a profiler."""
import argparse
import datetime
import json
import os
import pathlib
import time


def summarize(before, after, elapsed, ticks_per_second):
    """Process CPU seconds can exceed wall seconds on multiple cores; reported steal is host-wide."""
    if elapsed <= 0 or ticks_per_second <= 0 or before['startTicks'] != after['startTicks']:
        raise ValueError('Invalid sample interval or reused process identity')
    deltas = [b - a for a, b in zip(before['hostCpuTicks'], after['hostCpuTicks'])]
    user = after['userTicks'] - before['userTicks']
    kernel = after['kernelTicks'] - before['kernelTicks']
    if len(deltas) < 8 or min(deltas) < 0 or user < 0 or kernel < 0:
        raise ValueError('CPU counters regressed or are incomplete')
    return {'sampleSeconds': elapsed, 'sdkUserCpuSeconds': user / ticks_per_second,
            'sdkKernelCpuSeconds': kernel / ticks_per_second,
            'hostReportedStealPercent': 100 * deltas[7] / max(1, sum(deltas[:8])), 'sampleOnly': True}


def read_snapshot(pid):
    """Parse procfs fields after the final comm parenthesis, preserving correct field positions."""
    process = pathlib.Path('/proc') / str(pid)
    fields = (process / 'stat').read_text().rsplit(')', 1)[1].split()
    cpu = pathlib.Path('/proc/stat').read_text().splitlines()[0].split()[1:]
    return {'userTicks': int(fields[11]), 'kernelTicks': int(fields[12]), 'startTicks': int(fields[19]),
            'hostCpuTicks': [int(value) for value in cpu]}


def sample(root, seconds):
    """Require the run-owned timeout's direct capacity child, not a process selected by name alone."""
    if not .1 <= seconds <= 5:
        raise ValueError('Sample duration must be between 0.1 and 5 seconds')
    root = pathlib.Path(root).resolve()
    parent = int((root / 'sdk.pid').read_text())
    children = (pathlib.Path('/proc') / str(parent) / 'task' / str(parent) / 'children').read_text().split()
    candidates = []
    for child in children:
        command = (pathlib.Path('/proc') / child / 'cmdline').read_bytes()
        if os.fsencode(root) in command and b'Sigtran.NET.PerformanceLab.dll\0capacity\0' in command:
            candidates.append(int(child))
    if len(candidates) != 1:
        raise ValueError('Exactly one owned capacity process is required')
    before = read_snapshot(candidates[0])
    started = time.monotonic()
    time.sleep(seconds)
    after = read_snapshot(candidates[0])
    result = summarize(before, after, time.monotonic() - started, os.sysconf('SC_CLK_TCK'))
    result['capturedAtUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    result['cpuModel'] = next((line.split(':', 1)[1].strip()
                               for line in pathlib.Path('/proc/cpuinfo').read_text().splitlines()
                               if line.startswith('model name')), 'unreported')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--seconds', type=float, default=2)
    args = parser.parse_args()
    print(json.dumps(sample(args.root, args.seconds), indent=2))
