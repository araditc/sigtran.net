#!/usr/bin/env python3
"""Stage one clean committed build on authorized VMs and launch durable endurance supervision.

Controller-only dependency: Paramiko. Passwords are consumed from the environment
and sudo stdin only. An expiring restricted key replaces password authentication
for the host-local run; its private half never leaves the SDK host.
"""
import argparse
import datetime
import hashlib
import io
import json
import os
import pathlib
import re
import shlex
import subprocess
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
BASE = '/opt/sigtran-endurance/runs/'


def command(client, args, password=None, timeout=180):
    if password is not None: args = ['sudo', '-S', '-p', '', *args]
    stream, output, error = client.exec_command(shlex.join(args), timeout=timeout)
    if password is not None: stream.write(password + '\n'); stream.flush()
    stream.channel.shutdown_write()
    data = output.read().decode(); detail = error.read().decode()
    status = output.channel.recv_exit_status()
    if status: raise RuntimeError('Remote step failed (' + str(status) + '): ' + detail[-2000:] + data[-1000:])
    return data.strip()


INSTALL = '''import json,os,pathlib,shutil,subprocess,sys
root=pathlib.Path(sys.argv[1]);stage=pathlib.Path(sys.argv[2]);plan=json.loads(sys.argv[3]);role=sys.argv[4]
if root.parent != pathlib.Path('/opt/sigtran-endurance/runs') or root.name != plan['runId']:raise ValueError('Invalid root')
if root.exists():raise ValueError('Run already exists')
root.mkdir(parents=True,mode=0o755)
os.chmod(root.parent,0o755);os.chmod(root.parent.parent,0o755)
tools=root/'tools';tools.mkdir()
for path in (stage/'source/scripts').glob('*.py'):shutil.copyfile(path,tools/path.name);os.chmod(tools/path.name,0o555)
(root/'host.json').write_text(json.dumps(plan));os.chmod(root/'host.json',0o600)
for name in ('peer','libsctp.so.1'):shutil.copyfile(stage/name,root/name);os.chmod(root/name,0o555 if name=='peer' else 0o444)
if role=='sdk':
 private=root/'private';private.mkdir(mode=0o700)
 subprocess.run(['ssh-keygen','-q','-t','ed25519','-N','','-C','sigtran-endurance-'+plan['runId'],'-f',str(private/'identity')],check=True)
 shutil.copytree(stage/'source/src/Sigtran.NET.PerformanceLab/bin/Release/net10.0',root/'sdk-build')
 for path in (root/'sdk-build').rglob('*'):
  if path.is_file():os.chmod(path,0o444)
 work=root/'work';work.mkdir(mode=0o700)
 shutil.chown(work,user='ammar',group='ammar')
 (work/'sdk').mkdir(mode=0o700);shutil.chown(work/'sdk',user='ammar',group='ammar')
 shutil.copyfile(stage/'source.tar',work/'source.tar')
 shutil.copyfile(stage/'source/eng/qualification/endurance-policy.json',work/'policy.json')
 for name in ('peer','libsctp.so.1'):shutil.copyfile(root/name,work/name)
 for original,name in [('Sigtran.NET.PerformanceLab.dll','sdk-executable.dll'),('Sigtran.NET.dll','sdk-library.dll')]:shutil.copyfile(root/'sdk-build'/original,work/name)
 net=dict(runId=plan['runId'],sdkIp=plan['sdkIp'],peerIp=plan['peerIp'],port=plan['port'],holdSeconds=10,networkFaultIds=[3] if plan['diagnostic'] else [3,6,9],expiresAtUtc=plan['expiresAtUtc'])
 (root/'network.json').write_text(json.dumps(net));os.chmod(root/'network.json',0o600)
else:
 (root/'data').mkdir(mode=0o700);shutil.chown(root/'data',user='ammar',group='ammar')
print(str(root))
'''

BUILD = '''import pathlib,subprocess,sys,tarfile
stage=pathlib.Path(sys.argv[1]);source=stage/'source';source.mkdir()
with tarfile.open(stage/'source.tar') as archive:
 for member in archive.getmembers():
  if member.issym() or member.islnk() or pathlib.PurePosixPath(member.name).is_absolute() or '..' in pathlib.PurePosixPath(member.name).parts:raise ValueError('Unsafe source archive')
 archive.extractall(source)
with (stage/'build.log').open('wb') as log:
 subprocess.run(['dotnet','build','src/Sigtran.NET.PerformanceLab','-c','Release','-v:q','-m:1','/nodeReuse:false','/p:UseSharedCompilation=false'],cwd=source,stdout=log,stderr=subprocess.STDOUT,check=True)
 subprocess.run(['gcc','-std=c11','-O2','-Wall','-Wextra','-Werror','tools/interop-peer/sigtran_reference_peer.c','-lsctp','-o',str(stage/'peer')],cwd=source,stdout=log,stderr=subprocess.STDOUT,check=True)
print('Build complete')
'''


def start(args):
    import paramiko
    if not args.allow_kafka_maintenance: raise ValueError('Explicit approved maintenance flag required')
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', args.run_id): raise ValueError('Invalid run identity')
    if (args.diagnostic and not 300 <= args.duration <= 3600) or (not args.diagnostic and args.duration != 259200): raise ValueError('Invalid duration fence')
    if subprocess.check_output(['git', 'status', '--porcelain'], cwd=ROOT).strip(): raise ValueError('Commit all source before staging')
    source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT).decode().strip()
    archive = subprocess.check_output(['git', 'archive', '--format=tar', source], cwd=ROOT)
    password = os.environ.pop('SIGTRAN_LAB_PEER_PASSWORD')
    sdk_password = os.environ.pop('SIGTRAN_LAB_SDK_SUDO_PASSWORD', password)
    clients = []
    try:
        for address, key, secret in ((args.sdk_host, args.key, None), (args.peer_host, None, password)):
            client = paramiko.SSHClient(); client.load_host_keys(str(pathlib.Path(args.known_hosts).expanduser()))
            client.set_missing_host_key_policy(paramiko.RejectPolicy())
            client.connect(address, username='ammar', key_filename=key, password=secret, look_for_keys=False,
                           allow_agent=False, timeout=15, auth_timeout=15, banner_timeout=15, channel_timeout=180)
            client.get_transport().set_keepalive(15); clients.append(client)
        sdk, peer = clients
        stage = command(sdk, ['python3', '-c', "import pathlib,sys; p=pathlib.Path.home()/('sigtran-endurance-stage-'+sys.argv[1]);p.mkdir(mode=0o700);print(p)", args.run_id])
        with sdk.open_sftp() as sftp:
            sftp.putfo(io.BytesIO(archive), stage + '/source.tar')
        print(command(sdk, ['python3', '-c', BUILD, stage], timeout=600), flush=True)
        command(sdk, ['cp', '/lib/x86_64-linux-gnu/libsctp.so.1', stage + '/libsctp.so.1'])
        with sdk.open_sftp() as sftp:
            binaries = {name: sftp.open(stage + '/' + name, 'rb').read() for name in ('peer', 'libsctp.so.1')}
        expires = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(seconds=args.duration + 1800)
        value = dict(runId=args.run_id, sourceCommit=source, sdkIp=args.sdk_host, peerIp=args.peer_host, port=33072,
                     durationSeconds=args.duration, diagnostic=args.diagnostic, expiresAtUtc=expires.isoformat(),
                     explicitMaintenanceApproval=True, peerSha256=hashlib.sha256(binaries['peer']).hexdigest(),
                     librarySha256=hashlib.sha256(binaries['libsctp.so.1']).hexdigest(), peerCpu=3)
        root = BASE + args.run_id
        command(sdk, ['python3', '-c', INSTALL, root, stage, json.dumps(value), 'sdk'], sdk_password)
        public = command(sdk, ['cat', root + '/private/identity.pub'], sdk_password)
        peer_stage = command(peer, ['python3', '-c', "import pathlib,sys; p=pathlib.Path.home()/('sigtran-endurance-stage-'+sys.argv[1]);p.mkdir(mode=0o700);print(p)", args.run_id])
        with peer.open_sftp() as sftp:
            sftp.putfo(io.BytesIO(archive), peer_stage + '/source.tar')
            for name, data in binaries.items(): sftp.putfo(io.BytesIO(data), peer_stage + '/' + name)
        command(peer, ['python3', '-c', "import pathlib,tarfile,sys; s=pathlib.Path(sys.argv[1]);(s/'source').mkdir();tarfile.open(s/'source.tar').extractall(s/'source')", peer_stage])
        command(peer, ['python3', '-c', INSTALL, root, peer_stage, json.dumps(value), 'peer'], password)
        remote_key = peer.get_transport().get_remote_server_key()
        known = args.peer_host + ' ' + remote_key.get_name() + ' ' + remote_key.get_base64() + '\n'
        command(sdk, ['python3', '-c', "import pathlib,sys; p=pathlib.Path(sys.argv[1]);p.write_text(sys.argv[2]);p.chmod(0o600)", root + '/private/known_hosts', known], sdk_password)
        forced = '/usr/bin/python3 -I ' + root + '/tools/endurance-host.py bridge /run/sigtran-endurance-' + args.run_id + '.sock'
        line = 'restrict,from="' + args.sdk_host + '",expiry-time="' + expires.strftime('%Y%m%d%H%M%SZ') + '",command="' + forced + '" ' + public + '\n'
        command(peer, ['python3', '-c', "import pathlib,sys; d=pathlib.Path.home()/'.ssh';d.mkdir(exist_ok=True,mode=0o700);p=d/'authorized_keys';p.touch(mode=0o600);p.chmod(0o600);f=p.open('a');f.write(sys.argv[1]);f.close()", line])
        tools = root + '/tools/'
        command(peer, ['systemd-run', '--quiet', '--unit=sigtran-endurance-peer-' + args.run_id,
                      '--property=Type=simple', '--property=Restart=no', '--property=KillMode=control-group',
                      '--property=TimeoutStopSec=300', '--property=RuntimeMaxSec=' + str(args.duration + 2100),
                      '--property=ExecStopPost=/usr/bin/python3 -I ' + tools + 'endurance-host.py restore ' + root + '/host.json',
                      '/usr/bin/python3', '-I', tools + 'endurance-host.py', 'serve', root + '/host.json'], password)
        time.sleep(2)
        command(peer, ['systemctl', 'is-active', '--quiet', 'sigtran-endurance-peer-' + args.run_id], password)
        # Forced-command authentication is tested before the job can quiesce Kafka.
        command(sdk, ['ssh', '-i', root + '/private/identity', '-o', 'IdentitiesOnly=yes', '-o', 'BatchMode=yes',
                     '-o', 'StrictHostKeyChecking=yes', '-o', 'UserKnownHostsFile=' + root + '/private/known_hosts',
                     'ammar@' + args.peer_host, '{"action":"heartbeat"}'], sdk_password)
        command(sdk, ['systemd-run', '--quiet', '--unit=sigtran-endurance-sdk-' + args.run_id,
                     '--property=Type=simple', '--property=Restart=no', '--property=KillMode=control-group',
                     '--property=TimeoutStopSec=300', '--property=RuntimeMaxSec=' + str(args.duration + 1200),
                     '--property=ExecStopPost=/usr/bin/python3 -I ' + tools + 'run-endurance-lab.py cleanup ' + root + '/host.json',
                     '/usr/bin/python3', '-I', tools + 'run-endurance-lab.py', 'supervise', root + '/host.json'], sdk_password)
        print(json.dumps(dict(runId=args.run_id, sourceCommit=source, diagnostic=args.diagnostic,
                             durationSeconds=args.duration, privateRemoteRoot=root, expiresAtUtc=value['expiresAtUtc']), indent=2))
    finally:
        for client in clients: client.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--sdk-host', default='192.168.100.28')
    parser.add_argument('--peer-host', default='192.168.100.43')
    parser.add_argument('--key', required=True)
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    parser.add_argument('--duration', type=int, default=259200)
    parser.add_argument('--diagnostic', action='store_true')
    parser.add_argument('--allow-kafka-maintenance', action='store_true')
    start(parser.parse_args())
