#!/usr/bin/env python3
"""Own one bounded loopback-only SDK/peer diagnostic; no SSH, Kafka, firewall or system-wide tuning."""
import argparse
import datetime
import hashlib
import json
import os
import pathlib
import re
import shutil
import signal
import socket
import subprocess
import time


def validate(value):
    if set(value) != {'sourceCommit', 'durationSeconds', 'port', 'inputs'}:
        raise ValueError('Unexpected diagnostic configuration')
    if not re.fullmatch('[0-9a-f]{40}', value['sourceCommit']): raise ValueError('A frozen source is required')
    if type(value['durationSeconds']) is not int or not 10 <= value['durationSeconds'] <= 3600:
        raise ValueError('Only 10..3600 diagnostic seconds are permitted')
    if type(value['port']) is not int or not 1024 <= value['port'] <= 65535: raise ValueError('Invalid local port')
    if not isinstance(value['inputs'], dict) or not 1 <= len(value['inputs']) <= 64: raise ValueError('Invalid input inventory')
    for name, digest in value['inputs'].items():
        if not re.fullmatch(r'[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*', name) or '..' in pathlib.PurePosixPath(name).parts:
            raise ValueError('Unsafe input name')
        if not re.fullmatch('[0-9a-f]{64}', digest): raise ValueError('Invalid input digest')
    return value


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        while data := stream.read(1048576): value.update(data)
    return value.hexdigest()


def save(path, value):
    with path.open('x') as stream: json.dump(value, stream, indent=2); stream.write('\n')


def stop(child, grace=10):
    """Only a child created by this invocation may be signalled; never discover/kill another workload."""
    if child.poll() is None:
        child.send_signal(signal.SIGINT)
        try: return child.wait(timeout=grace)
        except subprocess.TimeoutExpired:
            child.kill()
    return child.wait(timeout=5)


def run(path):
    import fcntl
    root = path.resolve().parent
    if os.geteuid() == 0 or root.is_symlink() or root.stat().st_uid != os.geteuid():
        raise ValueError('A private unprivileged diagnostic root is required')
    os.umask(0o077)
    value = validate(json.loads(path.read_text()))
    for name, expected in value['inputs'].items():
        target = root / name
        if target.is_symlink() or root not in target.resolve().parents or digest(target) != expected:
            raise ValueError('Substituted diagnostic input: ' + name)
    if shutil.disk_usage(root).free < 2 * 1073741824: raise ValueError('At least 2 GiB free is required')
    if 3 not in os.sched_getaffinity(0): raise ValueError('The declared peer CPU is unavailable')
    with (root.parent / 'resource-session.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        work = root / 'work'; work.mkdir(exist_ok=False)
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 132) as probe: probe.bind(('127.0.0.1', value['port']))
        with (work / 'peer-build.log').open('wb') as log:
            subprocess.run(['gcc', '-std=c11', '-O2', '-Wall', '-Wextra', '-Werror', str(root / 'peer.c'),
                            '-lsctp', '-o', str(root / 'peer')], check=True, stdout=log, stderr=subprocess.STDOUT, timeout=30)
        with (work / 'peer-counters.bin').open('xb') as stream: stream.write(bytes(64))
        children, logs, errors = {}, [], []
        started = time.monotonic()
        def launch(name, command, environment=None):
            log = (work / (name + '.log')).open('wb'); logs.append(log)
            child = subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT, env=environment)
            children[name] = child
            return child
        def interrupted(sig, frame): raise InterruptedError('Diagnostic supervisor interrupted')
        signal.signal(signal.SIGTERM, interrupted); signal.signal(signal.SIGINT, interrupted)
        try:
            capture = launch('capture', ['dumpcap', '-p', '-P', '-q', '-i', 'lo', '-f',
                'sctp and port ' + str(value['port']), '-c', '8192', '-w', str(work / 'sample.pcap')])
            time.sleep(.5)
            if capture.poll() is not None: raise RuntimeError('Owned non-root capture did not start')
            peer_env = dict(os.environ, SIGTRAN_CAPACITY_SOCKET_PROFILE='lan-bounded-v1', LD_LIBRARY_PATH=str(root / 'sdk-build'))
            peer = launch('peer', ['taskset', '-c', '3', str(root / 'peer'), '127.0.0.1', str(value['port']), '0',
                'endurance', str(work / 'peer-summary.json'), str(work / 'peer-counters.bin')], peer_env)
            deadline = time.monotonic() + 10
            while b'"listening"' not in (work / 'peer.log').read_bytes():
                if peer.poll() is not None or time.monotonic() > deadline: raise RuntimeError('Owned local peer did not listen')
                time.sleep(.05)
            sdk_env = dict(os.environ, DOTNET_ThreadPool_ForceMaxWorkerThreads='4',
                DOTNET_EnableEventPipe='1', DOTNET_EventPipeConfig='Microsoft-Windows-DotNETRuntime:0x1:4',
                DOTNET_EventPipeOutputPath=str(work / 'gc.nettrace'), DOTNET_EventPipeCircularMB='20',
                DOTNET_EventPipeOutputStreaming='1', LD_LIBRARY_PATH=str(root / 'sdk-build'))
            sdk = launch('sdk', ['/usr/share/dotnet/dotnet', str(root / 'sdk-build/Sigtran.NET.PerformanceLab.dll'),
                'resource-session', '--remote-ip', '127.0.0.1', '--remote-port', str(value['port']),
                '--source-commit', value['sourceCommit'], '--output', str(work / 'sdk'),
                '--duration-seconds', str(value['durationSeconds']), '--concurrency', '256',
                '--queue-capacity', '8192', '--socket-profile', 'lan-bounded-v1'], sdk_env)
            ticks = pathlib.Path('/proc/' + str(sdk.pid) + '/stat').read_text().rsplit(')', 1)[1].split()[19]
            save(work / 'processes.json', dict(sdkPid=sdk.pid, sdkStartTicks=int(ticks), peerPid=peer.pid,
                sourceCommit=value['sourceCommit'], durationSeconds=value['durationSeconds'], peerBinarySha256=digest(root / 'peer')))
            while sdk.poll() is None:
                if time.monotonic() - started > value['durationSeconds'] + 75: raise TimeoutError('Diagnostic hard deadline')
                peer_exit = peer.poll()
                if peer_exit is not None and (peer_exit != 0 or not (work / 'sdk/sdk.json').is_file()):
                    raise RuntimeError('Owned peer exited before SDK drain')
                if capture.poll() not in (None, 0): raise RuntimeError('Owned capture failed')
                if any(p.stat().st_size > 256 * 1048576 for p in work.rglob('*') if p.is_file()):
                    raise RuntimeError('Diagnostic artifact exceeds byte budget')
                if sum(p.stat().st_size for p in work.rglob('*') if p.is_file()) > 512 * 1048576:
                    raise RuntimeError('Diagnostic total byte budget exceeded')
                if shutil.disk_usage(root).free < 1073741824: raise RuntimeError('Diagnostic free-space reserve breached')
                time.sleep(1)
            if sdk.returncode != 0: raise RuntimeError('SDK diagnostic failed')
            if peer.wait(timeout=15) != 0: raise RuntimeError('Peer did not finish with orderly EOF')
            if capture.wait(timeout=10) != 0: raise RuntimeError('Capture did not finish successfully')
        except BaseException as error:
            errors.append(type(error).__name__ + ': ' + str(error))
        finally:
            exits = {name: stop(child) for name, child in children.items()}
            for log in logs: log.close()
            save(work / 'controller-result.json', dict(schemaVersion=1, evidenceKind='local-map-resource-diagnostic',
                sourceCommit=value['sourceCommit'], diagnostic=True, qualificationPassed=False, stablePromotion=False,
                kafkaControl=False, externalPeerTraffic=False, childExits=exits, errors=errors,
                ownedChildrenStopped=all(child.poll() is not None for child in children.values()),
                finishedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat()))
            save(work / 'sha256.json', {p.relative_to(work).as_posix(): digest(p) for p in sorted(work.rglob('*')) if p.is_file()})
        return 0 if not errors and len(children) == 3 and all(code == 0 for code in exits.values()) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('config', type=pathlib.Path)
    raise SystemExit(run(parser.parse_args().config))
