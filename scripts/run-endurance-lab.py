#!/usr/bin/env python3
"""Run a frozen host-local endurance job; retain failed attempts and restore scoped host changes.

The root-owned controller does not compile, install dependencies, restart other
services or accept arbitrary shell commands during measured traffic. Only the
SDK and capture children are demoted to the unprivileged lab account.
"""
import argparse
import base64
import hashlib
import importlib.util
import json
import os
import pathlib
import secrets
import signal
import shutil
import subprocess
import time

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('endurance_host', HERE / 'endurance-host.py')
host = importlib.util.module_from_spec(spec); spec.loader.exec_module(host)


def rpc(value, root, request):
    arguments = ['ssh', '-i', str(root / 'private/identity'), '-o', 'IdentitiesOnly=yes', '-o', 'BatchMode=yes',
                 '-o', 'StrictHostKeyChecking=yes', '-o', 'UserKnownHostsFile=' + str(root / 'private/known_hosts'),
                 '-o', 'ConnectTimeout=10', '-o', 'ServerAliveInterval=10', '-o', 'ServerAliveCountMax=3',
                 'ammar@' + value['peerIp'], json.dumps(request, separators=(',', ':'))]
    result = host.execute(arguments, timeout=240)
    if len(result.stdout) > 131072: raise ValueError('Oversized peer reply')
    response = json.loads(result.stdout)
    if response.get('ok') is not True: raise RuntimeError('Peer action failed: ' + str(response.get('error')))
    return response['value']


def network(root, action, incident=None):
    args = ['/usr/bin/python3', '-I', str(HERE / 'endurance-network.py'), action, str(root / 'network.json')]
    if incident is not None: args.append(str(incident))
    return host.execute(args, timeout=120).stdout


def executable(name):
    """Resolve the installed tool; a distribution-specific symlink is not a transport contract."""
    found = shutil.which(name)
    if found is None or not pathlib.Path(found).resolve().is_file(): raise ValueError('Required tool unavailable: ' + name)
    return str(pathlib.Path(found).resolve())


def cleanup(value, root):
    """Independent ExecStopPost is idempotent; a peer heartbeat lease also restores if SSH is lost."""
    errors = []
    try: network(root, 'rollback-all')
    except Exception as error: errors.append('network:' + type(error).__name__)
    if (root / 'host-started').exists() and not (root / 'peer-restored').exists():
        try: rpc(value, root, dict(action='finish'))
        except Exception as error: errors.append('peer:' + type(error).__name__)
    if errors: raise RuntimeError('Cleanup incomplete: ' + ','.join(errors))


class Driver:
    def __init__(self, value, root):
        self.value, self.root, self.raw = value, root, root / 'work'
        self.sdk = None; self.sdk_log = None; self.captures = []; self.active_captures = []
        self.last_heartbeat = 0; self.sequence = 0; self.error = None; self.before = None; self.after = None
        self.sdk_identity = None

    def heartbeat(self):
        if time.monotonic() - self.last_heartbeat >= 10:
            state = rpc(self.value, self.root, dict(action='heartbeat'))
            if state['finished']: raise RuntimeError('Peer maintenance ended early')
            self.last_heartbeat = time.monotonic()

    def wait(self, condition, seconds, allow_exit=False):
        deadline = time.monotonic() + seconds
        while True:
            result = condition()
            if result: return result
            if self.sdk is not None and self.sdk.poll() is not None and not allow_exit: raise RuntimeError('SDK ended before control completed')
            if time.monotonic() >= deadline: raise TimeoutError('Endurance controller deadline')
            self.heartbeat(); time.sleep(.1)

    def capture(self, identity):
        name = 'baseline' if identity is None else 'incident-' + str(identity)
        directory = self.raw / 'captures' / name
        directory.mkdir(parents=True); os.chown(directory, host.demotion()['user'], host.demotion()['group'])
        log = (directory / 'capture.log').open('wb')
        route = json.loads(host.execute(['ip', '-j', 'route', 'get', self.value['peerIp']]).stdout)[0]['dev']
        args = ['dumpcap', '-p', '-P', '-q', '-i', route, '-f',
                'sctp and host ' + self.value['peerIp'] + ' and port ' + str(self.value['port']), '-w', str(directory / 'sample.pcap')]
        args += ['-c', '8192'] if identity is None else ['-b', 'filesize:1024', '-b', 'files:8']
        child = subprocess.Popen(args, stdout=log, stderr=subprocess.STDOUT, start_new_session=True, **host.demotion())
        item = dict(incidentId=identity, directory=directory, logHandle=log, process=child)
        self.active_captures.append(item)
        time.sleep(.5)
        if child.poll() is not None: raise RuntimeError('Capture did not start')
        return item

    def stop_capture(self, item):
        child = item['process']
        if child.poll() is None: child.send_signal(signal.SIGINT)
        code = child.wait(timeout=10); item['logHandle'].close()
        files = sorted(item['directory'].glob('*.pcap'))
        if not files or len(files) > 8 or sum(p.stat().st_size for p in files) > 9 * 1048576:
            raise ValueError('Capture budget/availability failed')
        result = dict(incidentId=item['incidentId'], files=[p.relative_to(self.raw).as_posix() for p in files],
                      log=(item['directory'] / 'capture.log').relative_to(self.raw).as_posix(), exitCode=code)
        self.captures.append(result); self.active_captures.remove(item)
        host.atomic(self.raw / 'captures.json', self.captures)
        if code != 0: raise RuntimeError('Capture exit was not successful')

    def finish_probe_capture(self, item):
        """Keep ordinary admission paused until the final capture backend buffer has retired."""
        settled = time.monotonic()
        self.wait(lambda: time.monotonic() - settled >= 3, 5)
        self.stop_capture(item)

    def header(self):
        path = self.raw / 'sdk/checkpoints.jsonl'
        if not path.exists(): return None
        with path.open('rb') as stream: data = stream.readline(32769)
        if not data.endswith(b'\n'): return None
        return json.loads(base64.b64decode(json.loads(data)['PayloadBase64'], validate=True))

    def command(self, identity, action, nonce):
        self.sequence += 1
        host.atomic(self.raw / 'sdk/control-command.json', dict(Sequence=self.sequence, Id=identity, Action=action, Nonce=nonce))
        def acknowledgement():
            path = self.raw / 'sdk/control-ack.json'
            if not path.exists(): return None
            value = json.loads(path.read_text())
            if value.get('Sequence') == self.sequence:
                if value.get('Id') != identity or value.get('Action') != action or value.get('Nonce') != nonce:
                    raise ValueError('Control acknowledgement identity differs')
                return value
            return None
        return self.wait(acknowledgement, 90)

    def event(self, incident, nonce, phase):
        host.append(self.raw / 'controller-events.jsonl', dict(id=incident['Id'], kind=incident['Kind'],
                    nonce=nonce, phase=phase, observedAtUtc=host.now()))

    def export(self, name):
        with (self.raw / name).open('xb') as stream:
            offset = 0
            while True:
                result = rpc(self.value, self.root, dict(action='read', name=name, offset=offset))
                if type(result['size']) is not int or not 0 <= result['size'] <= 64 * 1048576: raise ValueError('Peer export exceeds budget')
                data = base64.b64decode(result['data'], validate=True)
                if len(data) > 65536 or offset + len(data) > result['size']: raise ValueError('Peer export truncated/oversized')
                stream.write(data); offset += len(data)
                if offset == result['size']: break
                if not data: raise ValueError('Peer export made no progress')
            stream.flush(); os.fsync(stream.fileno())

    def run(self):
        if (self.raw / 'inventory.json').exists() or (self.root / 'host-started').exists(): raise ValueError('A run cannot restart')
        tools = {name: executable(name) for name in ('dotnet', 'dumpcap', 'mergecap', 'tshark', 'ip', 'iptables', 'iptables-save', 'ssh')}
        runtime = subprocess.run([tools['dotnet'], '--list-runtimes'], check=True, capture_output=True, text=True,
                                 timeout=30, **host.demotion())
        if 'Microsoft.NETCore.App 10.0.12 ' not in runtime.stdout: raise ValueError('Qualified .NET runtime is unavailable')
        topology = dict(sdk=host.inventory(), peer=rpc(self.value, self.root, dict(action='inventory')))
        if topology['sdk']['machineHash'] == topology['peer']['machineHash'] or topology['sdk']['kafkaProcesses']:
            raise ValueError('Two distinct hosts and an unshared SDK host are required')
        for role in ('sdk', 'peer'):
            addresses = {entry['local'] for link in topology[role]['addresses'] for entry in link.get('addr_info', [])}
            if self.value[role + 'Ip'] not in addresses: raise ValueError('Data address not owned by authenticated host')
        # This durable marker tells ExecStopPost to request restoration even if prepare loses its SSH reply.
        (self.root / 'host-started').touch(exist_ok=False)
        try:
            rpc(self.value, self.root, dict(action='prepare'))
            self.before = dict(sdk=host.inventory(), peer=rpc(self.value, self.root, dict(action='inventory')))
            if any(state['kafkaProcesses'] for state in self.before.values()): raise RuntimeError('Workload hosts are not quiesced')
            baseline = self.capture(None)
            self.sdk_log = (self.raw / 'sdk.log').open('wb')
            environment = dict(os.environ, DOTNET_ThreadPool_ForceMaxWorkerThreads='4')
            args = [tools['dotnet'], str(self.root / 'sdk-build/Sigtran.NET.PerformanceLab.dll'), 'endurance',
                    '--remote-ip', self.value['peerIp'], '--remote-port', str(self.value['port']),
                    '--source-commit', self.value['sourceCommit'], '--output', str(self.raw / 'sdk'),
                    '--duration-seconds', str(self.value['durationSeconds']), '--diagnostic', str(self.value['diagnostic']).lower(),
                    '--socket-profile', 'lan-bounded-v1', '--concurrency', '256', '--queue-capacity', '8192']
            self.sdk = subprocess.Popen(args, stdout=self.sdk_log, stderr=subprocess.STDOUT, env=environment,
                                        start_new_session=True, **host.demotion())
            ticks = pathlib.Path('/proc/' + str(self.sdk.pid) + '/stat').read_text().rsplit(')', 1)[1].split()[19]
            self.sdk_identity = dict(pid=self.sdk.pid, startTicks=int(ticks))
            header = self.wait(self.header, 45)
            self.wait(lambda: baseline['process'].poll() is not None, 30)
            self.stop_capture(baseline)
            start_wall = datetime_from(header['StartedAtUtc'])
            origin = time.monotonic() - (datetime_from(host.now()) - start_wall).total_seconds()
            for incident in header['FaultPlan']:
                # Capture begins just before the scheduled SDK window; the SDK independently enforces the offset.
                self.wait(lambda: time.monotonic() - origin >= incident['OffsetSeconds'] - .5, self.value['durationSeconds'])
                capture = self.capture(incident['Id']); nonce = secrets.token_hex(16)
                self.command(incident['Id'], 'begin', nonce)
                if incident['Kind'] == 'sctp-partition': network(self.root, 'partition', incident['Id'])
                else: rpc(self.value, self.root, dict(action='stop', id=incident['Id'], nonce=nonce))
                self.event(incident, nonce, 'injected')
                hold = time.monotonic()
                self.wait(lambda: time.monotonic() - hold >= incident['HoldSeconds'], incident['HoldSeconds'] + 5)
                if incident['Kind'] == 'sctp-partition': network(self.root, 'rollback', incident['Id'])
                else: rpc(self.value, self.root, dict(action='start', id=incident['Id'], nonce=nonce))
                self.event(incident, nonce, 'released')
                self.command(incident['Id'], 'release', nonce)
                # Admission stays paused while the capture backend retires its final kernel buffer.
                # Immediate SIGINT can omit an entire short probe window despite zero reported drops.
                self.finish_probe_capture(capture)
                self.command(incident['Id'], 'capture-stopped', nonce)
            remaining = max(1, self.value['durationSeconds'] + 30 - (time.monotonic() - origin))
            self.wait(lambda: self.sdk.poll() is not None, remaining, allow_exit=True)
            code = self.sdk.wait(); (self.raw / 'sdk.exit').write_text(str(code) + '\n')
            self.after = dict(sdk=host.inventory(), peer=rpc(self.value, self.root, dict(action='inventory')))
            if code: raise RuntimeError('SDK execution failed')
        except BaseException as error:
            self.error = type(error).__name__
            raise
        finally:
            errors = []
            if self.sdk is not None and self.sdk.poll() is None:
                self.sdk.send_signal(signal.SIGINT)
                try: self.sdk.wait(timeout=20)
                except subprocess.TimeoutExpired: self.sdk.kill(); self.sdk.wait(timeout=10)
            if self.sdk is not None and not (self.raw / 'sdk.exit').exists():
                (self.raw / 'sdk.exit').write_text(str(self.sdk.returncode) + '\n')
            if self.sdk_log: self.sdk_log.close()
            for capture in self.active_captures[:]:
                try: self.stop_capture(capture)
                except Exception as error: errors.append('capture:' + type(error).__name__)
            try:
                network(self.root, 'rollback-all')
                ids = [3] if self.value['diagnostic'] else [3, 6, 9]
                states = [json.loads(network(self.root, 'status', incident)) for incident in ids]
                host.atomic(self.raw / 'network-cleanup.json', dict(rulesAbsent=all(s['rulesAbsent'] for s in states), checkedIncidents=ids))
                (self.raw / 'network-events.jsonl').write_bytes((self.root / 'network-events.jsonl').read_bytes())
            except Exception as error: errors.append('network:' + type(error).__name__)
            try:
                # A service can be transitioning during restart. Retry only idempotent restoration, never MAP work.
                deadline = time.monotonic() + 120
                while True:
                    try: rpc(self.value, self.root, dict(action='finish')); break
                    except RuntimeError:
                        if time.monotonic() >= deadline: raise
                        time.sleep(2)
                for name in ('peer-result.json', 'peer-counters.bin', 'peer.log', 'host-maintenance.json'): self.export(name)
                audit = json.loads((self.raw / 'host-maintenance.json').read_text())
                if not audit['restorationVerified'] or not audit['fallbackTimerCancelled']: raise RuntimeError('Peer restoration unverified')
                (self.root / 'peer-restored').touch(exist_ok=False)
                rpc(self.value, self.root, dict(action='shutdown'))
            except Exception as error: errors.append('peer:' + type(error).__name__)
            inventory = dict(schemaVersion=1, sourceCommit=self.value['sourceCommit'], before=self.before, after=self.after,
                sdkProcess=self.sdk_identity, port=self.value['port'], sdkExitCode=self.sdk.returncode if self.sdk else None,
                completed=self.error is None, cleanupVerified=not errors, collectionErrors=errors,
                sdkBinarySha256=sha(self.raw / 'sdk-executable.dll'), sdkLibrarySha256=sha(self.raw / 'sdk-library.dll'))
            host.atomic(self.raw / 'inventory.json', inventory)
            if errors and self.error is None: raise RuntimeError('Collection/restoration incomplete: ' + ','.join(errors))


def datetime_from(value):
    import datetime
    import re
    return datetime.datetime.fromisoformat(re.sub(r'(\.\d{6})\d(?=Z|[+-])', r'\1', value).replace('Z', '+00:00'))


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        while data := stream.read(1048576): digest.update(data)
    return digest.hexdigest()


def collect(value, root):
    """Only the supervisor records the controller's real exit, after it returns."""
    raw = root / 'work'
    inventory = json.loads((raw / 'inventory.json').read_text())
    inventory['controllerExitCode'] = int((raw / 'controller.exit').read_text())
    inventory['supervisorCleanupVerified'] = json.loads((raw / 'supervisor-cleanup.json').read_text())['verified']
    host.atomic(raw / 'inventory.json', inventory)
    files = {}
    for path in sorted(raw.rglob('*')):
        if path.is_symlink(): raise ValueError('Linked artifact rejected')
        if path.is_file() and path.name != 'sha256.json': files[path.relative_to(raw).as_posix()] = sha(path)
    host.atomic(raw / 'sha256.json', files)
    verifier = host.load_module('independent_endurance_verifier', 'verify-endurance-evidence.py')
    report = verifier.verify(raw, value['sourceCommit'])
    # Publication is separate from originals and exclusive; no report is fabricated on verifier failure.
    with (root / 'verified-summary.json').open('x') as stream: json.dump(report, stream, indent=2); stream.write('\n')
    if not value['diagnostic'] and not report['qualificationPassed']:
        raise RuntimeError('Completed execution did not qualify the declared endurance target')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('run', 'supervise', 'cleanup', 'collect'))
    parser.add_argument('plan')
    args = parser.parse_args()
    if os.geteuid() != 0: raise ValueError('Frozen host-local controller requires root')
    value, root = host.plan(args.plan)
    if args.action == 'cleanup': cleanup(value, root)
    elif args.action == 'collect': collect(value, root)
    elif args.action == 'run':
        def interrupt(*_): raise InterruptedError('Host controller interrupted')
        signal.signal(signal.SIGTERM, interrupt); signal.signal(signal.SIGINT, interrupt)
        Driver(value, root).run()
    else:
        import fcntl
        lock = (root.parent.parent / 'sdk-job.lock').open('a')
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        # A separate process prevents writing a success receipt before the actual driver has exited.
        child = subprocess.Popen(['/usr/bin/python3', '-I', str(HERE / 'run-endurance-lab.py'), 'run', str(root / 'host.json')])
        def forward(sig, _): child.send_signal(sig)
        signal.signal(signal.SIGTERM, forward); signal.signal(signal.SIGINT, forward)
        code = child.wait()
        (root / 'work/controller.exit').write_text(str(code) + '\n')
        cleanup_verified = False
        try:
            cleanup(value, root)
            cleanup_verified = True
        finally:
            host.atomic(root / 'work/supervisor-cleanup.json', dict(verified=cleanup_verified, observedAtUtc=host.now()))
            collect(value, root)
        if code: raise SystemExit(1)


if __name__ == '__main__': main()
