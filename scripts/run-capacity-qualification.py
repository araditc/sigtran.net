#!/usr/bin/env python3
"""Run a rootless two-VM capacity measurement. Never install or restart services."""
import argparse
import hashlib
import ipaddress
import json
import os
import pathlib
import re
import secrets
import shlex
import subprocess
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]


def command(client, arguments, timeout=120):
    """Execute a bounded, shell-quoted command; stdout must stay small."""
    _, output, error = client.exec_command(shlex.join(arguments), timeout=timeout)
    result = output.read().decode()
    detail = error.read().decode()
    status = output.channel.recv_exit_status()
    if status:
        raise RuntimeError(f'Remote command failed ({status}): {detail[:1000]}')
    return result.strip()


def validate_hosts(sdk, peer, peer_ip, shared):
    """Reject aliased machines and data endpoints not owned by the authenticated peer."""
    if not re.fullmatch(r'[0-9a-f]{64}', sdk['machineHash']) or not re.fullmatch(r'[0-9a-f]{64}', peer['machineHash']):
        raise ValueError('Missing machine identity')
    if sdk['machineHash'] == peer['machineHash'] or sdk['hostname'] == peer['hostname']:
        raise ValueError('Two distinct hosts are required')
    address = ipaddress.IPv4Address(peer_ip)
    if address.is_loopback or address.is_unspecified or address.is_multicast:
        raise ValueError('A remote unicast data address is required')
    owners = [entry['local'] for link in peer['addresses'] for entry in link.get('addr_info', [])]
    local = [entry['local'] for link in sdk['addresses'] for entry in link.get('addr_info', [])]
    if peer_ip not in owners or peer_ip in local:
        raise ValueError('Data endpoint must belong only to the authenticated peer')
    if sdk['kafkaProcesses']:
        raise ValueError('SDK host must not run Kafka')
    if peer['kafkaProcesses'] and not shared:
        raise ValueError('Shared peer requires explicit --shared-peer; it is not qualification eligible')
    return not shared and not peer['kafkaProcesses']


def remote_inventory(client):
    """Use the identical tracked collector on both machines without installing it."""
    source = (ROOT / 'scripts/capacity-host-inventory.py').read_text(encoding='utf-8')
    input_stream, output, error = client.exec_command('python3 -', timeout=20)
    input_stream.write(source)
    input_stream.channel.shutdown_write()
    data = output.read()
    if output.channel.recv_exit_status():
        raise RuntimeError('Host inventory failed: ' + error.read().decode()[:500])
    return json.loads(data)


def worker_environment(maximum, processors):
    """Bound only the owned .NET process; never change services or machine-wide runtime policy."""
    if maximum is None:
        return []
    if type(maximum) is not int or not processors <= maximum <= 1024:
        raise ValueError('Maximum workers must cover the declared logical processors and be <= 1024')
    # .NET 10 documents this environment value as hexadecimal, unlike runtimeconfig.json.
    return ['env', 'DOTNET_ThreadPool_ForceMaxWorkerThreads=' + format(maximum, 'x')]


def private_remote_root(client):
    with client.open_sftp() as sftp:
        root = sftp.normalize('.') + '/sigtran-capacity-' + secrets.token_hex(8)
        sftp.mkdir(root, mode=0o700)
    return root


def start_owned(client, root, name, shell):
    """Keep the SSH channel and timeout alive; cancellation kills only this owned timeout process."""
    pid_path = shlex.quote(root + '/' + name + '.pid')
    # The shell becomes timeout, so its saved PID cannot identify a different job.
    text = f'umask 077; echo $$ > {pid_path}; exec ' + shell
    _, output, _ = client.exec_command('bash -c ' + shlex.quote(text), timeout=2000)
    return output.channel


def stop_owned(client, root, name):
    """Require both the run directory and command ownership before signaling a saved PID."""
    shell = ('p=$(cat ' + shlex.quote(root + '/' + name + '.pid') + '); '
             '[[ $p =~ ^[0-9]+$ ]] || exit 1; '
             '[[ -r /proc/$p/cmdline ]] || exit 0; '
             'tr "\\0" " " < /proc/$p/cmdline | grep -F -- ' + shlex.quote(root) + ' >/dev/null || exit 1; '
             'kill -TERM "$p"')
    command(client, ['bash', '-c', shell], 10)


def run(args):
    """Build a clean source archive once and retain incomplete evidence on any failure."""
    import paramiko  # Controller-only dependency; neither VM needs Paramiko or a Python package install.
    if args.duration not in range(1, 1801) or (args.duration != 1800 and not args.diagnostic):
        raise ValueError('Short runs require --diagnostic')
    for host in (args.sdk_host, args.peer_host):
        ipaddress.IPv4Address(host)
    if args.peer_cpu < 0:
        raise ValueError('Invalid peer CPU')
    if not 1 <= args.concurrency <= 4096 or not 1024 <= args.port <= 65535:
        raise ValueError('Invalid concurrency or unprivileged SCTP port')
    if subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=no'], cwd=ROOT).strip():
        raise ValueError('Commit tracked changes before capturing candidate source')
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    if not re.fullmatch('[0-9a-f]{40}', commit):
        raise ValueError('Invalid source commit')
    output = pathlib.Path(args.output).resolve()
    output.mkdir(parents=True, mode=0o700, exist_ok=False)
    password = os.environ.pop('SIGTRAN_CAPACITY_PEER_PASSWORD', None)
    sdk = paramiko.SSHClient()
    peer = paramiko.SSHClient()
    for client in (sdk, peer):
        client.load_host_keys(str(pathlib.Path(args.known_hosts).expanduser()))
        client.set_missing_host_key_policy(paramiko.RejectPolicy())
    roots = {}
    channels = []
    try:
        sdk.connect(args.sdk_host, username=args.user, key_filename=args.sdk_key, timeout=10)
        peer.connect(args.peer_host, username=args.user, password=password,
                     key_filename=args.peer_key, look_for_keys=False, allow_agent=False, timeout=10)
        before = {'sdk': remote_inventory(sdk), 'peer': remote_inventory(peer)}
        runtime_environment = worker_environment(args.maximum_workers, before['sdk']['processors'])
        eligible = validate_hosts(before['sdk'], before['peer'], args.peer_host, args.shared_peer) and args.dedicated_hosts
        if args.peer_cpu >= before['peer']['processors']:
            raise ValueError('Peer CPU is outside available affinity')
        route = json.loads(command(sdk, ['ip', '-j', '-4', 'route', 'get', args.peer_host]))
        if not route or route[0].get('type') == 'local' or route[0].get('dev') == 'lo':
            raise ValueError('Local or missing data route')
        for name, client in (('sdk', sdk), ('peer', peer)):
            roots[name] = private_remote_root(client)
        manifest = {'schemaVersion': 1, 'sourceCommit': commit, 'before': before, 'route': route,
                    'roots': roots, 'dedicatedDeclaration': eligible, 'sharedPeer': args.shared_peer,
                    'peerCpu': args.peer_cpu, 'durationSeconds': args.duration,
                    'diagnostic': args.diagnostic or not eligible, 'concurrency': args.concurrency, 'port': args.port}
        manifest['maximumWorkerThreads'] = args.maximum_workers
        (output / 'inventory.json').write_text(json.dumps(manifest, indent=2))
        archive = output / 'source.tar'
        subprocess.run(['git', 'archive', '--format=tar', '-o', str(archive), commit, 'src',
                        'tools/interop-peer', 'README.md', 'LICENSE'], cwd=ROOT, check=True)
        with sdk.open_sftp() as sftp:
            sftp.put(str(archive), roots['sdk'] + '/source.tar')
        root = roots['sdk']
        build = (f'cd {shlex.quote(root)}; tar -xf source.tar; '
                 'dotnet build src/Sigtran.NET.PerformanceLab -c Release -v:q -m:1 >build.log 2>&1; '
                 'gcc -std=c11 -O2 -Wall -Wextra -Werror tools/interop-peer/sigtran_reference_peer.c '
                 '-lsctp -o peer >peer-build.log 2>&1; ./peer --self-test >>peer-build.log')
        command(sdk, ['bash', '-euc', build], 300)
        with sdk.open_sftp() as source, peer.open_sftp() as destination:
            for remote, name in ((root + '/peer', 'peer'), ('/lib/x86_64-linux-gnu/libsctp.so.1', 'libsctp.so.1')):
                source.get(remote, str(output / name))
                destination.put(str(output / name), roots['peer'] + '/' + name)
            destination.chmod(roots['peer'] + '/peer', 0o700)
        budget = args.duration + 80
        peer_root = roots['peer']
        peer_line = ('timeout --signal=TERM --kill-after=5s ' + str(budget) + 's '
                     + shlex.join(['env', 'LD_LIBRARY_PATH=' + peer_root, 'nice', '-n', '10',
                                   'taskset', '-c', str(args.peer_cpu), peer_root + '/peer', args.peer_host,
                                   str(args.port), '0', 'capacity', peer_root + '/peer.json'])
                     + ' >' + shlex.quote(peer_root + '/peer.log') + ' 2>&1')
        peer_channel = start_owned(peer, peer_root, 'peer', peer_line)
        channels.append((peer, peer_root, 'peer', peer_channel))
        for _ in range(100):
            with peer.open_sftp() as sftp:
                try:
                    with sftp.open(peer_root + '/peer.log') as stream:
                        ready = b'event=listening' in stream.read(4096)
                except OSError:
                    ready = False
            if ready:
                break
            if peer_channel.exit_status_ready():
                raise RuntimeError('Peer exited before listening')
            time.sleep(.1)
        else:
            raise TimeoutError('Peer listen deadline')
        interface = route[0]['dev']
        capture_line = shlex.join(['timeout', '--kill-after=5s', '25s', 'dumpcap', '-p', '-P', '-i', interface,
                                  '-a', 'packets:8192', '-a', 'duration:20', '-a', 'filesize:16384',
                                  '-f', f'sctp and host {args.peer_host} and port {args.port}',
                                  '-w', root + '/sample.pcap']) + ' >' + shlex.quote(root + '/capture.log') + ' 2>&1'
        capture = start_owned(sdk, root, 'capture', capture_line)
        channels.append((sdk, root, 'capture', capture))
        time.sleep(1)
        with sdk.open_sftp() as sftp:
            if capture.exit_status_ready() or sftp.stat(root + '/sample.pcap').st_size < 24:
                raise RuntimeError('Capture failed to open before workload admission')
        diagnostic = args.diagnostic or not eligible
        executable = root + '/src/Sigtran.NET.PerformanceLab/bin/Release/net10.0/Sigtran.NET.PerformanceLab.dll'
        workload = shlex.join(['timeout', '--signal=INT', '--kill-after=10s', str(budget) + 's',
                              *runtime_environment, 'dotnet', executable, 'capacity', '--remote-ip', args.peer_host,
                              '--remote-port', str(args.port), '--source-commit', commit, '--output', root + '/result',
                              '--duration-seconds', str(args.duration), '--warmup-seconds', '10',
                              '--concurrency', str(args.concurrency), '--diagnostic', str(diagnostic).lower()])
        workload += ' >' + shlex.quote(root + '/sdk.log') + ' 2>&1'
        sdk_channel = start_owned(sdk, root, 'sdk', workload)
        channels.append((sdk, root, 'sdk', sdk_channel))
        deadline = time.monotonic() + budget + 15
        while not sdk_channel.exit_status_ready():
            if time.monotonic() > deadline:
                raise TimeoutError('Workload controller deadline')
            time.sleep(1)
        manifest['sdkExitCode'] = sdk_channel.recv_exit_status()
        for channel in (peer_channel, capture):
            end = time.monotonic() + 30
            while not channel.exit_status_ready() and time.monotonic() < end:
                time.sleep(.2)
            if not channel.exit_status_ready():
                raise TimeoutError('Owned child did not stop')
        manifest['peerExitCode'] = peer_channel.recv_exit_status()
        manifest['captureExitCode'] = capture.recv_exit_status()
        manifest['after'] = {'sdk': remote_inventory(sdk), 'peer': remote_inventory(peer)}
        manifest['completed'] = True
        print(f"Capacity execution finished; SDK={manifest['sdkExitCode']} peer={manifest['peerExitCode']}", flush=True)
    finally:
        for client, root, name, channel in reversed(channels):
            if not channel.exit_status_ready():
                try:
                    stop_owned(client, root, name)
                except Exception:
                    pass  # The independent timeout remains armed; no unrelated PID is signaled.
        cleanup_deadline = time.monotonic() + 10
        while any(not channel.exit_status_ready() for _, _, _, channel in channels) and time.monotonic() < cleanup_deadline:
            time.sleep(.1)
        if 'manifest' in locals():
            manifest['cleanupVerified'] = all(channel.exit_status_ready() for _, _, _, channel in channels)
        for name, client in (('sdk', sdk), ('peer', peer)):
            if name not in roots:
                continue
            files = ['build.log', 'peer-build.log', 'sdk.log', 'capture.log', 'sample.pcap', 'result/sdk.json'] if name == 'sdk' else ['peer.log', 'peer.json']
            with client.open_sftp() as sftp:
                for file in files:
                    try:
                        sftp.get(roots[name] + '/' + file, str(output / file.replace('/', '-')))
                    except OSError:
                        pass
        if 'manifest' in locals():
            (output / 'inventory.json').write_text(json.dumps(manifest, indent=2))
        sdk.close()
        peer.close()
        hashes = {file.name: hashlib.sha256(file.read_bytes()).hexdigest()
                  for file in output.iterdir() if file.is_file() and file.name != 'sha256.json'}
        (output / 'sha256.json').write_text(json.dumps(hashes, indent=2))
    return manifest['sdkExitCode'] or manifest['peerExitCode'] or manifest['captureExitCode']


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sdk-host', required=True)
    parser.add_argument('--peer-host', required=True)
    parser.add_argument('--sdk-key', required=True)
    parser.add_argument('--peer-key')
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    parser.add_argument('--user', default='ammar')
    parser.add_argument('--port', type=int, default=33061)
    parser.add_argument('--duration', type=int, default=1800)
    parser.add_argument('--concurrency', type=int, default=256)
    parser.add_argument('--peer-cpu', type=int, default=3)
    parser.add_argument('--maximum-workers', type=int)
    parser.add_argument('--diagnostic', action='store_true')
    parser.add_argument('--shared-peer', action='store_true')
    parser.add_argument('--dedicated-hosts', action='store_true')
    parser.add_argument('--output', required=True)
    raise SystemExit(run(parser.parse_args()))
