#!/usr/bin/env python3
"""Control only one approved SCTP lab flow using a root-owned immutable run plan and independent rollback."""
import argparse
import datetime
import ipaddress
import json
import os
import pathlib
import re
import shlex
import stat
import subprocess
import time

BASE = pathlib.Path('/opt/sigtran-endurance/runs')


def validate_plan(plan):
    """There is no generic protocol, address prefix, chain, command or port-range input."""
    if set(plan) != {'runId', 'sdkIp', 'peerIp', 'port', 'holdSeconds', 'networkFaultIds', 'expiresAtUtc'}:
        raise ValueError('Unexpected network plan fields')
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', plan['runId']):
        raise ValueError('Invalid run identity')
    sdk, peer = (ipaddress.IPv4Address(plan[key]) for key in ('sdkIp', 'peerIp'))
    if sdk == peer or any(not ip.is_private or ip.is_loopback or ip.is_unspecified or ip.is_multicast or ip.is_link_local for ip in (sdk, peer)):
        raise ValueError('Two distinct private unicast lab addresses are required')
    if type(plan['port']) is not int or not 1024 <= plan['port'] <= 65535:
        raise ValueError('One unprivileged port is required')
    if type(plan['holdSeconds']) is not int or not 1 <= plan['holdSeconds'] <= 60:
        raise ValueError('Fault hold is not bounded')
    ids = plan['networkFaultIds']
    if not isinstance(ids, list) or any(type(value) is not int for value in ids) or ids not in ([3], [3, 6, 9]):
        raise ValueError('Only declared network incident identities are permitted')
    expires = datetime.datetime.fromisoformat(plan['expiresAtUtc'])
    if expires.utcoffset() != datetime.timedelta(0):
        raise ValueError('Lease expiry must be UTC')
    return plan


def rules(plan, incident):
    if incident not in plan['networkFaultIds']:
        raise ValueError('Incident is not a declared network fault')
    tail = ['-m', 'comment', '--comment', 'endurance-' + plan['runId'] + '-' + str(incident), '-j', 'DROP']
    return [('OUTPUT', ['-p', 'sctp', '-d', plan['peerIp'], '--dport', str(plan['port']), *tail]),
            ('INPUT', ['-p', 'sctp', '-s', plan['peerIp'], '--sport', str(plan['port']), *tail])]


def execute(arguments):
    return subprocess.run(arguments, capture_output=True, text=True, timeout=20)


def absent(plan, incident, run=execute):
    missing = []
    for chain, rule in rules(plan, incident):
        status = run(['iptables', '-w', '5', '-C', chain, *rule]).returncode
        if status not in (0, 1):
            raise RuntimeError('Rule presence could not be verified')
        missing.append(status == 1)
    return all(missing)


def guard_name(plan, incident):
    return 'sigtran-net-restore-' + plan['runId'] + '-' + str(incident)


def dropped_packets(plan, incident, run=execute):
    """Retain counters only for our exact tagged flow; do not export unrelated firewall policy."""
    result = run(['iptables-save', '-c'])
    if result.returncode: raise RuntimeError('Owned packet counters unavailable')
    tag = 'endurance-' + plan['runId'] + '-' + str(incident)
    packets = 0
    for line in result.stdout.splitlines():
        match = re.fullmatch(r'\[([0-9]+):[0-9]+\] (.*)', line)
        if not match: continue
        tokens = shlex.split(match[2])
        if '--comment' in tokens and tokens[tokens.index('--comment') + 1] == tag:
            if tokens[:2] not in (['-A', 'INPUT'], ['-A', 'OUTPUT']) or '-j' not in tokens or tokens[tokens.index('-j') + 1] != 'DROP':
                raise RuntimeError('Owned rule identity differs')
            packets += int(match[1])
    return packets


def record(path, incident, event, **values):
    with (path.parent / 'network-events.jsonl').open('a') as stream:
        if stream.tell() > 1040000: raise ValueError('Network event budget exceeded')
        stream.write(json.dumps(dict(incident=incident, event=event,
            observedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), **values)) + '\n')
        stream.flush(); os.fsync(stream.fileno())


def rollback(plan, incident, run=execute):
    """Delete exact tagged rules only; uncertainty never disarms independent rollback."""
    for chain, rule in rules(plan, incident):
        for _ in range(8):
            checked = run(['iptables', '-w', '5', '-C', chain, *rule]).returncode
            if checked == 1:
                break
            if checked != 0 or run(['iptables', '-w', '5', '-D', chain, *rule]).returncode:
                raise RuntimeError('Owned rule removal failed')
    if not absent(plan, incident, run):
        raise RuntimeError('Owned rules remain; rollback stays armed')
    timer = guard_name(plan, incident) + '.timer'
    if run(['systemctl', 'stop', timer]).returncode:
        observed = run(['systemctl', 'show', '--property=LoadState,ActiveState', timer])
        values = dict(line.split('=', 1) for line in observed.stdout.splitlines() if '=' in line)
        if observed.returncode or values != {'LoadState': 'not-found', 'ActiveState': 'inactive'}:
            raise RuntimeError('Rollback timer cancellation could not be verified')


def partition(plan, incident, config_path, run=execute):
    """Verify the independent timer before inserting either rule, then roll back any partial insertion."""
    expiry = datetime.datetime.fromisoformat(plan['expiresAtUtc'])
    if datetime.datetime.now(datetime.timezone.utc) >= expiry or not absent(plan, incident, run):
        raise ValueError('Expired lease or pre-existing owned rule')
    guard = guard_name(plan, incident)
    command = ['systemd-run', '--quiet', '--collect', '--unit=' + guard,
               '--on-active=' + str(plan['holdSeconds'] + 60) + 's', '--property=Type=oneshot',
               '--property=TimeoutStartSec=infinity', '/usr/bin/python3', '-I', str(pathlib.Path(__file__).resolve()),
               'guard', str(config_path), str(incident)]
    if run(command).returncode or run(['systemctl', 'is-active', '--quiet', guard + '.timer']).returncode:
        raise RuntimeError('Independent rollback was not armed')
    try:
        for chain, rule in rules(plan, incident):
            if run(['iptables', '-w', '5', '-I', chain, '1', *rule]).returncode:
                raise RuntimeError('Scoped rule insertion failed')
    except Exception:
        rollback(plan, incident, run)
        raise


def load_root_plan(path):
    """The unprivileged runner cannot supply or replace a privileged network plan."""
    path = pathlib.Path(path)
    resolved = path.resolve(strict=True)
    if path.is_symlink() or resolved.name != 'network.json' or resolved.parent.parent != BASE:
        raise ValueError('A protected per-run network plan is required')
    for item in (resolved, resolved.parent, BASE, BASE.parent, pathlib.Path(__file__).resolve()):
        info = item.stat()
        if info.st_uid != 0 or info.st_mode & 0o022:
            raise ValueError('Root-owned non-writable helper/plan required')
    if resolved.stat().st_size > 4096 or not stat.S_ISREG(resolved.stat().st_mode):
        raise ValueError('Network plan is not a bounded regular file')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result: raise ValueError('Duplicate network plan field')
            result[key] = value
        return result
    plan = validate_plan(json.loads(resolved.read_text(), object_pairs_hook=unique))
    if plan['runId'] != resolved.parent.name:
        raise ValueError('Network plan identity differs from protected directory')
    return plan, resolved


def main():
    import fcntl
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('partition', 'rollback', 'rollback-all', 'status', 'guard'))
    parser.add_argument('plan', type=pathlib.Path)
    parser.add_argument('incident', nargs='?', type=int)
    args = parser.parse_args()
    if os.geteuid() != 0: raise ValueError('Scoped network helper requires root')
    plan, path = load_root_plan(args.plan)
    if args.action != 'rollback-all' and args.incident not in plan['networkFaultIds']:
        raise ValueError('Invalid network incident')
    while True:
        try:
            with (path.parent / 'network.lock').open('a') as lock:
                fcntl.flock(lock, fcntl.LOCK_EX)
                if args.action == 'partition':
                    partition(plan, args.incident, path)
                    if any(execute(['iptables', '-w', '5', '-C', chain, *rule]).returncode != 0 for chain, rule in rules(plan, args.incident)):
                        rollback(plan, args.incident); raise RuntimeError('Both partition rules were not verified')
                    record(path, args.incident, 'injected', guardArmed=True)
                elif args.action == 'status':
                    print(json.dumps(dict(incident=args.incident, rulesAbsent=absent(plan, args.incident))))
                else:
                    for incident in (plan['networkFaultIds'] if args.action == 'rollback-all' else [args.incident]):
                        packets = dropped_packets(plan, incident)
                        rollback(plan, incident)
                        record(path, incident, 'rollback', droppedPackets=packets, rulesAbsent=True)
            return 0
        except (RuntimeError, subprocess.TimeoutExpired):
            if args.action != 'guard': raise
            time.sleep(1)  # Independent root rollback retries until the exact owned rules are verified absent.


if __name__ == '__main__':
    raise SystemExit(main())
