#!/usr/bin/env python3
"""Observe only the run-owned single-threaded C peer and aggregate host counters."""
import argparse
import datetime
import json
import os
import pathlib
import time


def parse_stat(text):
    """Fields after the final comm parenthesis retain Linux procfs numbering."""
    fields = text.rsplit(')', 1)[1].split()
    return dict(state=fields[0], userTicks=int(fields[11]), kernelTicks=int(fields[12]),
                nice=int(fields[16]), startTicks=int(fields[19]), majorFaults=int(fields[9]))


def host_counters(cpu):
    lines = pathlib.Path('/proc/stat').read_text().splitlines()
    ticks = {line.split()[0]: [int(n) for n in line.split()[1:9]]
             for line in lines if line.startswith('cpu ') or line.startswith('cpu' + str(cpu) + ' ')}
    names = {'SctpT3Retransmits', 'SctpFastRetransmits', 'SctpInPktDiscards', 'SctpInDataChunkDiscards'}
    sctp = {fields[0]: int(fields[1]) for line in pathlib.Path('/proc/net/sctp/snmp').read_text().splitlines()
            if len(fields := line.split()) == 2 and fields[0] in names}
    return ticks, sctp


def sample(root, cpu, budget):
    """Never inspect Kafka tasks; a command/root/start-time match prevents PID reuse confusion."""
    root = pathlib.Path(root).resolve()
    parent = int((root / 'peer.pid').read_text())
    children = (pathlib.Path('/proc') / str(parent) / 'task' / str(parent) / 'children').read_text().split()
    candidates = [int(pid) for pid in children
                  if (pathlib.Path('/proc') / pid / 'cmdline').read_bytes().split(b'\0')[0] == os.fsencode(root / 'peer')]
    if len(candidates) != 1:
        raise ValueError('Exactly one owned C peer is required')
    process = pathlib.Path('/proc') / str(candidates[0])
    first = parse_stat((process / 'stat').read_text())
    records = []
    origin = time.monotonic()
    exited = False
    while time.monotonic() - origin < budget:
        try:
            current = parse_stat((process / 'stat').read_text())
            if current['startTicks'] != first['startTicks']:
                exited = True
                break
            current['capturedAtUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
            current['elapsedSeconds'] = time.monotonic() - origin
            current['hostCpuTicks'], current['namespaceSctp'] = host_counters(cpu)
            records.append(current)
        except FileNotFoundError:
            exited = True
            break
        time.sleep(1)
    if not exited:
        raise TimeoutError('Owned peer observer exceeded its bounded lifetime')
    return dict(schemaVersion=1, processId=candidates[0], startTicks=first['startTicks'],
                ticksPerSecond=os.sysconf('SC_CLK_TCK'), observedProcessExit=True, records=records,
                caveat='Host counters are aggregate and cross-host clocks require independent validation; no Kafka process was sampled.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--cpu', type=int, required=True)
    parser.add_argument('--budget', type=int, required=True)
    args = parser.parse_args()
    if args.cpu < 0 or not 1 <= args.budget <= 1880:
        raise ValueError('Invalid bounded observer options')
    result = sample(args.root, args.cpu, args.budget)
    with (args.root / 'peer-runtime.json').open('x') as stream:
        stream.write(json.dumps(result, separators=(',', ':')) + '\n')
