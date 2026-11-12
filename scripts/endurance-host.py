#!/usr/bin/env python3
"""Protected per-run host primitives and an owned independent peer with expiring maintenance.

Install a frozen copy and plan as root; never grant a generic sudo/SSH shell to
the load process. The root daemon operates only declared units and its children.
"""
import argparse
import base64
import datetime
import hashlib
import importlib.util
import json
import mmap
import os
import pathlib
import re
import signal
import socket
import stat
import subprocess
import sys
import time
import traceback

BASE = pathlib.Path('/opt/sigtran-endurance/runs')
UNITS = ['kafka-anw-health.timer', 'kafka-anw-health.service',
         'kafka-anw@broker.service', 'kafka-anw@controller.service']
HERE = pathlib.Path(__file__).resolve().parent


def now(): return datetime.datetime.now(datetime.timezone.utc).isoformat()


def load_module(name, filename):
    spec = importlib.util.spec_from_file_location(name, HERE / filename)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def execute(args, timeout=180, check=True):
    return subprocess.run(args, check=check, capture_output=True, text=True, timeout=timeout)


def atomic(path, value):
    """Persist state before acknowledging an irreversible operation."""
    path = pathlib.Path(path)
    pending = path.with_name(path.name + '.pending')
    with pending.open('w') as stream:
        json.dump(value, stream); stream.write('\n'); stream.flush(); os.fsync(stream.fileno())
    os.replace(pending, path)
    descriptor = os.open(path.parent, os.O_DIRECTORY)
    try: os.fsync(descriptor)
    finally: os.close(descriptor)


def append(path, value):
    path = pathlib.Path(path)
    if path.exists() and path.stat().st_size > 1040000: raise ValueError('Event budget exceeded')
    with path.open('a') as stream:
        stream.write(json.dumps(value) + '\n'); stream.flush(); os.fsync(stream.fileno())


def plan(path):
    """Root plans may not be supplied through a writable parent or symbolic link."""
    path = pathlib.Path(path)
    resolved = path.resolve(strict=True)
    if path.is_symlink() or resolved.name != 'host.json' or resolved.parent.parent != BASE:
        raise ValueError('Protected host plan required')
    for item in (resolved, resolved.parent, BASE, BASE.parent, pathlib.Path(__file__).resolve()):
        info = item.stat()
        if info.st_uid != 0 or info.st_mode & 0o022: raise ValueError('Mutable root host plan/helper')
    if resolved.stat().st_size > 8192: raise ValueError('Oversized plan')
    value = json.loads(resolved.read_text())
    validate(value)
    if value['runId'] != resolved.parent.name: raise ValueError('Run identity differs')
    return value, resolved.parent


def validate(value):
    import ipaddress
    if set(value) != {'runId', 'sourceCommit', 'sdkIp', 'peerIp', 'port', 'durationSeconds', 'diagnostic',
                      'expiresAtUtc', 'explicitMaintenanceApproval', 'peerSha256', 'librarySha256', 'peerCpu'}:
        raise ValueError('Unexpected host plan fields')
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', value['runId']): raise ValueError('Invalid run identity')
    if not re.fullmatch('[0-9a-f]{40}', value['sourceCommit']): raise ValueError('Full source identity required')
    for key in ('peerSha256', 'librarySha256'):
        if not re.fullmatch('[0-9a-f]{64}', value[key]): raise ValueError('Executable identity required')
    addresses = [ipaddress.IPv4Address(value[key]) for key in ('sdkIp', 'peerIp')]
    if addresses[0] == addresses[1] or any(not a.is_private or a.is_loopback or a.is_unspecified or a.is_multicast or a.is_link_local for a in addresses):
        raise ValueError('Two private unicast hosts required')
    if type(value['port']) is not int or not 1024 <= value['port'] <= 65535: raise ValueError('Invalid port')
    if type(value['diagnostic']) is not bool or type(value['durationSeconds']) is not int: raise ValueError('Invalid duration')
    if (value['diagnostic'] and not 300 <= value['durationSeconds'] <= 3600) or (not value['diagnostic'] and value['durationSeconds'] != 259200):
        raise ValueError('Qualification cannot be shortened')
    if value['explicitMaintenanceApproval'] is not True: raise ValueError('Explicit maintenance approval required')
    expiry = datetime.datetime.fromisoformat(value['expiresAtUtc'])
    if expiry.utcoffset() != datetime.timedelta(0): raise ValueError('UTC expiry required')
    if type(value['peerCpu']) is not int or not 0 <= value['peerCpu'] <= 1023: raise ValueError('Invalid peer CPU')


def inventory():
    value = load_module('host_inventory', 'capacity-host-inventory.py').collect()
    value['bootId'] = pathlib.Path('/proc/sys/kernel/random/boot_id').read_text().strip()
    return value


def snapshot(strict=True):
    result = {}
    for unit in UNITS:
        output = execute(['systemctl', 'show', unit, '--no-pager',
                          '--property=LoadState,ActiveState,SubState,UnitFileState,MainPID,FragmentPath']).stdout
        state = dict(line.split('=', 1) for line in output.splitlines() if '=' in line)
        if state.get('LoadState') != 'loaded' or (strict and state['ActiveState'] not in ('active', 'inactive')):
            raise ValueError('Maintenance unit unavailable/transitioning')
        state['fragmentSha256'] = hashlib.sha256(pathlib.Path(state['FragmentPath']).read_bytes()).hexdigest()
        result[unit] = state
    return result


def restored(before, after):
    return all(before[unit][key] == after[unit][key] for unit in UNITS
               for key in ('ActiveState', 'SubState', 'UnitFileState', 'fragmentSha256'))


def guard_name(value): return 'sigtran-host-restore-' + value['runId']


def restore(value, root):
    """Retry independently on errors; cancel the hard deadline only after verified restoration."""
    import fcntl
    audit_path = root / 'host-maintenance.json'
    if not audit_path.exists(): return None
    with (root / 'restore.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        audit = json.loads(audit_path.read_text())
        if audit.get('restorationVerified'): return audit
        errors = []
        for unit in audit['restoreOrder']:
            try: execute(['systemctl', 'start', unit], timeout=120)
            except Exception as error: errors.append(unit + ':' + type(error).__name__)
        deadline = time.monotonic() + 90
        while True:
            audit['after'] = snapshot(strict=False)
            if restored(audit['before'], audit['after']) or time.monotonic() >= deadline: break
            time.sleep(2)
        audit.update(restorationVerified=restored(audit['before'], audit['after']), restorationErrors=errors,
                     unitFilesUnchanged=all(audit['before'][u]['fragmentSha256'] == audit['after'][u]['fragmentSha256'] for u in UNITS))
        if audit['restorationVerified']:
            audit['restoredAtUtc'] = now()
            execute(['systemctl', 'stop', guard_name(value) + '.timer'])
            audit['fallbackTimerCancelled'] = True
        atomic(audit_path, audit)
        if not audit['restorationVerified'] or errors: raise RuntimeError('Host restoration requires retry')
        return audit


def prepare(value, root):
    """Snapshot once, arm a root fallback, then stop only the four approved units."""
    audit_path = root / 'host-maintenance.json'
    if audit_path.exists(): raise ValueError('Maintenance cannot be restarted or resnapshotted')
    remaining = (datetime.datetime.fromisoformat(value['expiresAtUtc']) - datetime.datetime.now(datetime.timezone.utc)).total_seconds()
    if not value['durationSeconds'] + 300 <= remaining <= value['durationSeconds'] + 3600:
        raise ValueError('Lease must cover execution and bounded cleanup')
    before = snapshot()
    audit = dict(schemaVersion=1, explicitlyAuthorized=True, startedAtUtc=now(), before=before,
                 stoppedUnits=UNITS, restoreOrder=[u for u in reversed(UNITS) if before[u]['ActiveState'] == 'active'],
                 restorationVerified=False, fallbackTimerArmed=False, fallbackTimerCancelled=False, restorationErrors=[])
    atomic(audit_path, audit)
    execute(['systemd-run', '--quiet', '--collect', '--unit=' + guard_name(value), '--on-active=' + str(int(remaining)) + 's',
             '--timer-property=AccuracySec=1s', '--property=Type=oneshot', '--property=TimeoutStartSec=infinity',
             '/usr/bin/python3', '-I', str(HERE / 'endurance-host.py'), 'guard', str(root / 'host.json')])
    execute(['systemctl', 'is-active', '--quiet', guard_name(value) + '.timer'])
    audit['fallbackTimerArmed'] = True; atomic(audit_path, audit)
    try:
        for unit in UNITS: execute(['systemctl', 'stop', unit], timeout=120)
        audit['quiesced'] = snapshot()
        if any(s['ActiveState'] != 'inactive' or (u.endswith('.service') and s.get('MainPID') != '0') for u, s in audit['quiesced'].items()):
            raise RuntimeError('Quiescence not verified')
        if inventory()['kafkaProcesses']: raise RuntimeError('Kafka processes remain')
        audit['quiescedAtUtc'] = now(); atomic(audit_path, audit)
        return audit
    except BaseException:
        restore(value, root); raise


def demotion():
    import pwd
    user = pwd.getpwnam('ammar')
    return dict(user=user.pw_uid, group=user.pw_gid, extra_groups=os.getgrouplist('ammar', user.pw_gid))


def flush_counters(path):
    with path.open('r+b') as stream:
        with mmap.mmap(stream.fileno(), 64) as memory: memory.flush()
        os.fsync(stream.fileno())


class Peer:
    """Only this object owns peer PIDs; remote clients cannot supply commands, PIDs, units or paths."""
    def __init__(self, value, root):
        self.value, self.root, self.data = value, root, root / 'data'
        self.process = None; self.log = None; self.prepared = False; self.finished = False; self.held = False
        self.last_heartbeat = time.monotonic(); self.actions = []; self.generations = []; self.forced = 0; self.cache = {}
        self.incident = 0; self.nonce = None
        for name, key in (('peer', 'peerSha256'), ('libsctp.so.1', 'librarySha256')):
            if hashlib.sha256((root / name).read_bytes()).hexdigest() != value[key]: raise ValueError('Peer binary/library mismatch')

    def start(self):
        if self.process is not None: raise ValueError('Peer already running')
        number = len(self.generations) + 1
        if number > 64: raise RuntimeError('Peer generation budget exceeded')
        self.log = (self.data / 'peer.log').open('ab')
        environment = dict(os.environ, LD_LIBRARY_PATH=str(self.root), SIGTRAN_CAPACITY_SOCKET_PROFILE='lan-bounded-v1')
        # taskset affects only this owned child; Kafka and machine-wide CPU policy are unchanged.
        self.process = subprocess.Popen(['/usr/bin/taskset', '-c', str(self.value['peerCpu']), str(self.root / 'peer'),
            self.value['peerIp'], str(self.value['port']), '0', 'endurance', str(self.data / ('summary-' + str(number) + '.json')),
            str(self.data / 'peer-counters.bin')], stdout=self.log, stderr=subprocess.STDOUT, env=environment,
            start_new_session=True, **demotion())
        self.generations.append(dict(pid=self.process.pid, startedAtUtc=now(), number=number))

    def reap(self, terminate=None):
        if self.process is None: return None
        if terminate is not None and self.process.poll() is None:
            self.process.send_signal(terminate)
            if terminate == signal.SIGKILL: self.forced += 1
        try: code = self.process.wait(timeout=2 if terminate is not None else .01)
        except subprocess.TimeoutExpired:
            if terminate is None: return None
            self.process.kill(); self.forced += 1; code = self.process.wait(timeout=5)
        self.generations[-1].update(exitCode=code, exitedAtUtc=now())
        self.log.close(); self.log = None; self.process = None
        flush_counters(self.data / 'peer-counters.bin')
        return code

    def finish(self):
        self.held = True
        try:
            if self.process is not None: self.reap(signal.SIGTERM)
        finally:
            audit = restore(self.value, self.root)
        counters = load_module('endurance_peer_counter', 'endurance-peer-counters.py').read(self.data / 'peer-counters.bin', self.forced)
        result = dict(sourceCommit=self.value['sourceCommit'], binarySha256=self.value['peerSha256'],
                      librarySha256=self.value['librarySha256'], forcedKills=self.forced, counters=counters,
                      actions=self.actions, generations=self.generations)
        atomic(self.root / 'peer-result.json', result)
        self.finished = True
        return dict(result=result, maintenance=audit)

    def tick(self):
        if self.finished: return
        if not self.prepared:
            if time.monotonic() - self.last_heartbeat > 180: raise TimeoutError('Unused peer control lease expired')
            return
        if time.monotonic() - self.last_heartbeat > 180 or datetime.datetime.now(datetime.timezone.utc) >= datetime.datetime.fromisoformat(self.value['expiresAtUtc']):
            self.finish(); raise TimeoutError('Peer lease expired; maintenance restored')
        if self.process is not None and self.process.poll() is not None: self.reap()
        if self.process is None and not self.held: self.start()
        if (self.data / 'peer.log').stat().st_size > 64 * 1048576: raise RuntimeError('Peer log exceeded bound')

    def request(self, request):
        if not isinstance(request, dict) or not isinstance(request.get('action'), str): raise ValueError('Invalid request')
        action = request['action']
        if action == 'heartbeat':
            self.last_heartbeat = time.monotonic()
            return dict(prepared=self.prepared, finished=self.finished, held=self.held, pid=self.process.pid if self.process else None)
        if action == 'inventory': return inventory()
        if action == 'prepare':
            if not self.prepared:
                prepare(self.value, self.root)
                counters = self.data / 'peer-counters.bin'
                with counters.open('xb') as stream: stream.write(bytes(64)); stream.flush(); os.fsync(stream.fileno())
                os.chown(counters, demotion()['user'], demotion()['group']); os.chmod(counters, 0o600)
                self.prepared = True; self.start()
            return dict(prepared=True)
        if action in ('stop', 'start'):
            identity, nonce = request.get('id'), request.get('nonce')
            maximum = 3 if self.value['diagnostic'] else 9
            if type(identity) is not int or not 1 <= identity <= maximum or identity % 3 == 0 or not re.fullmatch('[0-9a-f]{32}', nonce or ''):
                raise ValueError('Not a declared peer incident')
            key = (action, identity, nonce)
            if key in self.cache: return self.cache[key]
            if not self.prepared or self.finished: raise ValueError('Peer not available')
            if action == 'stop':
                expected = next((i for i in range(self.incident + 1, maximum + 1) if i % 3), None)
                if identity != expected or self.held or self.process is None: raise ValueError('Peer incident order differs')
                self.held = True; self.incident = identity; self.nonce = nonce
                code = self.reap(signal.SIGKILL if identity % 3 == 1 else signal.SIGTERM)
                result = dict(id=identity, kind='peer-restart' if identity % 3 == 1 else 'peer-outage', nonce=nonce,
                              processExited=True, exitCode=code, observedAtUtc=now())
                self.actions.append(result); atomic(self.root / 'peer-actions.json', self.actions)
            else:
                if not self.held or self.incident != identity or self.nonce != nonce: raise ValueError('Restart identity differs')
                self.start(); self.held = False; result = dict(started=True, observedAtUtc=now(), pid=self.process.pid)
            self.cache[key] = result
            return result
        if action == 'finish':
            if self.finished:
                return dict(result=json.loads((self.root / 'peer-result.json').read_text()),
                            maintenance=json.loads((self.root / 'host-maintenance.json').read_text()))
            return self.finish()
        if action == 'read':
            names = {'peer-counters.bin': self.data / 'peer-counters.bin', 'peer.log': self.data / 'peer.log',
                     'peer-result.json': self.root / 'peer-result.json', 'host-maintenance.json': self.root / 'host-maintenance.json'}
            name, offset = request.get('name'), request.get('offset')
            if not self.finished or name not in names or type(offset) is not int or offset < 0: raise ValueError('Invalid export')
            path = names[name]
            if path.is_symlink() or path.stat().st_size > 64 * 1048576: raise ValueError('Invalid peer artifact')
            with path.open('rb') as stream: stream.seek(offset); data = stream.read(65536)
            return dict(data=base64.b64encode(data).decode(), size=path.stat().st_size)
        if action == 'shutdown' and self.finished: return dict(shutdown=True)
        raise ValueError('Unsupported peer request')


def serve(value, root):
    peer = Peer(value, root)
    endpoint = '/run/sigtran-endurance-' + value['runId'] + '.sock'
    if pathlib.Path(endpoint).exists(): raise ValueError('Socket already exists')
    running = True
    def stop(*_):
        nonlocal running
        running = False
    signal.signal(signal.SIGTERM, stop); signal.signal(signal.SIGINT, stop)
    try:
        with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as listener:
            listener.bind(endpoint); os.chmod(endpoint, 0o600); os.chown(endpoint, demotion()['user'], demotion()['group'])
            listener.listen(8); listener.settimeout(1)
            while running:
                peer.tick()
                try: client, _ = listener.accept()
                except socket.timeout: continue
                with client:
                    client.settimeout(15); data = b''
                    while not data.endswith(b'\n'):
                        part = client.recv(4096)
                        if not part or len(data) + len(part) > 8192: raise ValueError('Malformed peer control')
                        data += part
                    try: response = dict(ok=True, value=peer.request(json.loads(data)))
                    except Exception as error:
                        traceback.print_exc(file=sys.stderr)
                        response = dict(ok=False, error=type(error).__name__)
                    client.sendall(json.dumps(response).encode() + b'\n')
                    if response.get('value', {}).get('shutdown'): running = False
    finally:
        try:
            if peer.prepared and not peer.finished: peer.finish()
            else: restore(value, root)
        finally: pathlib.Path(endpoint).unlink(missing_ok=True)


def bridge(endpoint):
    """OpenSSH forced command passes JSON as data, never through a command interpreter."""
    request = os.environ.get('SSH_ORIGINAL_COMMAND', '')
    if len(request) > 8192 or '\n' in request: raise ValueError('Invalid bridge command')
    json.loads(request)
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as client:
        client.settimeout(180); client.connect(endpoint); client.sendall(request.encode() + b'\n')
        data = b''
        while not data.endswith(b'\n'):
            part = client.recv(65536)
            if not part or len(data) + len(part) > 131072: raise ValueError('Invalid bridge response')
            data += part
    sys.stdout.buffer.write(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('serve', 'restore', 'guard', 'bridge'))
    parser.add_argument('path')
    args = parser.parse_args()
    if args.action == 'bridge': bridge(args.path); return
    if os.geteuid() != 0: raise ValueError('Protected host operation requires root')
    value, root = plan(args.path)
    if args.action == 'serve': serve(value, root)
    elif args.action == 'restore': restore(value, root)
    else:
        while True:
            try: restore(value, root); return
            except Exception: time.sleep(5)


if __name__ == '__main__': main()
