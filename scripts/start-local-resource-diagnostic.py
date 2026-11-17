#!/usr/bin/env python3
"""Freeze/launch or read one local SDK diagnostic; never operate the Kafka host or restart a retained run."""
import argparse
import datetime
import errno
import hashlib
import importlib.util
import io
import json
import os
import pathlib
import re
import shlex
import subprocess
import tarfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
BASE = '/home/ammar/sigtran-resource-sessions/'
STATUS = '''import pathlib,json,subprocess,sys,datetime
r=pathlib.Path(sys.argv[1]);unit='sigtran-endurance-sdk-'+r.name+'.service'
q=subprocess.run(['systemctl','show',unit,'--property=LoadState,ActiveState,SubState,MainPID,ExecMainStatus,NRestarts,Result'],capture_output=True,text=True,check=True)
result=dict(runId=r.name,checkedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),unit=dict(x.split('=',1) for x in q.stdout.splitlines() if '=' in x),diagnostic=True,qualificationPassed=False,stablePromotion=False,readOnly=True)
for name,key in [('work/sdk/live-status.json','live'),('work/controller-result.json','controller'),('work/sdk/sdk.json','sdk')]:
 p=r/name
 if p.is_file() and p.stat().st_size<=2097152:result[key]=json.loads(p.read_text())
result['traceFiles']=[dict(name=p.name,bytes=p.stat().st_size) for p in sorted((r/'work').glob('*.nettrace'))]
result['traceBytes']=sum(p['bytes'] for p in result['traceFiles'])
p=r/'restart-policy.json'
try:
 if p.is_file():result['restartPolicy']=json.loads(p.read_text())
except PermissionError:result['restartPolicyReceiptReadable']=False
try:result['restartGuardFileAbsent']=not pathlib.Path('/etc/needrestart/conf.d/sigtran-endurance-'+r.name+'.conf').exists()
except PermissionError:result['restartGuardFileAbsent']=None
print(json.dumps(result,indent=2))
'''
CLEANUP = 'import sys,pathlib,importlib.util;sys.dont_write_bytecode=True;s=importlib.util.spec_from_file_location("policy",sys.argv[2]);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);m.release(pathlib.Path(sys.argv[1]))'
INSTALL = 'import sys,pathlib,importlib.util,json;sys.dont_write_bytecode=True;s=importlib.util.spec_from_file_location("policy",sys.argv[2]);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);print(json.dumps(m.install(json.loads(sys.argv[3]),pathlib.Path(sys.argv[1]),"sdk")))'


def command(client, args, password=None, timeout=120):
    argv = args if password is None else ['sudo', '-S', '-p', '', *args]
    stdin, stdout, stderr = client.exec_command(shlex.join(argv), timeout=timeout)
    if password is not None: stdin.write(password + '\n'); stdin.flush()
    output, error = stdout.read().decode(), stderr.read().decode()
    if stdout.channel.recv_exit_status() != 0: raise RuntimeError(error or output)
    return output


def upload_new(sftp, path, data):
    """Paramiko requires write plus exclusive creation; bare x does not grant write access."""
    with sftp.open(path, 'wx') as stream: stream.write(data)


def main():
    import paramiko
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--status', action='store_true')
    parser.add_argument('--seconds', type=int, default=3600)
    parser.add_argument('--port', type=int, default=33462)
    parser.add_argument('--host', default=os.environ.get('SIGTRAN_LAB_SDK_HOST', '192.168.100.28'))
    parser.add_argument('--key', default=os.environ.get('SIGTRAN_LAB_SDK_KEY', str(pathlib.Path.home() / '.ssh/sigtran_vm_release_ed25519')))
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    args = parser.parse_args()
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', args.run_id): raise ValueError('Invalid diagnostic identity')
    if not 10 <= args.seconds <= 3600 or not 1024 <= args.port <= 65535: raise ValueError('Invalid duration/port')
    client = paramiko.SSHClient(); client.load_host_keys(args.known_hosts); client.set_missing_host_key_policy(paramiko.RejectPolicy())
    remote = BASE + args.run_id
    installed = False; password = None; helper = None
    try:
        client.connect(args.host, username='ammar', key_filename=args.key, look_for_keys=False, allow_agent=False, timeout=15)
        if args.status:
            print(command(client, ['python3', '-c', STATUS, remote], timeout=30)); return
        password = os.environ.pop('SIGTRAN_LAB_SUDO_PASSWORD')
        if subprocess.check_output(['git', 'status', '--porcelain'], cwd=ROOT).strip(): raise ValueError('Freeze a clean committed source first')
        source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
        reader = (ROOT / 'scripts/read-endurance-status.py').read_text()
        prior = json.loads(command(client, ['python3', '-c', reader, '@latest'], timeout=30))
        if prior['unit'].get('ActiveState') not in ('failed', 'inactive'): raise ValueError('The measured full run is still active')
        helper = '/opt/sigtran-endurance/runs/' + prior['runId'] + '/tools/endurance-service-policy.py'
        local = ROOT / 'artifacts/qualification/phase-62' / args.run_id
        local.mkdir(parents=True, exist_ok=False)
        subprocess.run(['dotnet', 'publish', 'src/Sigtran.NET.PerformanceLab', '-c', 'Release', '--no-restore',
                        '--nologo', '-o', str(local / 'sdk-build')], cwd=ROOT, check=True)
        archive = subprocess.check_output(['git', 'archive', '--format=tar', source], cwd=ROOT)
        with tarfile.open(fileobj=io.BytesIO(archive)) as tar:
            expected_helper = tar.extractfile('scripts/endurance-service-policy.py').read()
        command(client, ['python3', '-c', 'import pathlib,sys;pathlib.Path(sys.argv[1]).mkdir(mode=0o700,parents=True,exist_ok=False)', remote])
        files = {name: (local / 'sdk-build' / name).read_bytes() for name in
                 ('Sigtran.NET.PerformanceLab.dll', 'Sigtran.NET.PerformanceLab.deps.json',
                  'Sigtran.NET.PerformanceLab.runtimeconfig.json', 'Sigtran.NET.dll')}
        inputs = {'sdk-build/' + name: data for name, data in files.items()}
        inputs.update({'source.tar': archive, 'peer.c': (ROOT / 'tools/interop-peer/sigtran_reference_peer.c').read_bytes(),
                       'run-local-resource-diagnostic.py': (ROOT / 'scripts/run-local-resource-diagnostic.py').read_bytes()})
        with client.open_sftp() as sftp:
            with sftp.open(helper, 'rb') as stream:
                if stream.read() != expected_helper: raise ValueError('Installed policy helper differs from frozen source')
            sftp.mkdir(remote + '/sdk-build', mode=0o700)
            with sftp.open('/opt/sigtran-endurance/runs/' + prior['runId'] + '/libsctp.so.1', 'rb') as stream:
                inputs['sdk-build/libsctp.so.1'] = stream.read()
            for name, data in inputs.items():
                upload_new(sftp, remote + '/' + name, data)
            config = dict(sourceCommit=source, durationSeconds=args.seconds, port=args.port,
                          inputs={name: hashlib.sha256(data).hexdigest() for name, data in inputs.items()})
            upload_new(sftp, remote + '/config.json', json.dumps(config))
        expiry = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(seconds=args.seconds + 600)
        command(client, ['/usr/bin/python3', '-c', INSTALL, remote, helper,
                         json.dumps(dict(runId=args.run_id, expiresAtUtc=expiry.isoformat()))], password)
        installed = True
        cleanup = 'ExecStopPost=+' + shlex.join(['/usr/bin/python3', '-c', CLEANUP, remote, helper])
        unit = 'sigtran-endurance-sdk-' + args.run_id
        command(client, ['systemd-run', '--unit=' + unit, '--uid=ammar', '--gid=ammar',
            '--property=Restart=no', '--property=KillMode=control-group', '--property=UMask=0077',
            '--property=RuntimeMaxSec=' + str(args.seconds + 180), '--property=TimeoutStopSec=30',
            '--property=TasksMax=128', '--property=' + cleanup,
            '/usr/bin/python3', '-I', remote + '/run-local-resource-diagnostic.py', remote + '/config.json'], password)
        with (local / 'launch.json').open('x', newline='\n') as stream:
            json.dump(dict(runId=args.run_id, sourceCommit=source, unit=unit, remoteRoot=remote,
                           diagnostic=True, qualificationPassed=False, config=config), stream, indent=2); stream.write('\n')
        print(command(client, ['python3', '-c', STATUS, remote], timeout=30))
    except (paramiko.SSHException, EOFError, TimeoutError, ConnectionError) as error:
        print(json.dumps(dict(management='unreachable', executionState='unknown', readOnly=args.status,
                              qualificationPassed=False, workloadRestarted=False, error=type(error).__name__)))
        raise SystemExit(2)
    except OSError as error:
        if error.errno in (errno.ECONNREFUSED, errno.ECONNRESET, errno.ENETUNREACH, errno.EHOSTUNREACH, errno.ETIMEDOUT):
            print(json.dumps(dict(management='unreachable', executionState='unknown', readOnly=args.status,
                                  qualificationPassed=False, workloadRestarted=False, error=type(error).__name__)))
            raise SystemExit(2)
        if installed: print('Inspect the same run identity; local file I/O did not establish remote execution failure.')
        raise
    except BaseException:
        # Do not release protection or restart work after a possibly successful launch/connection loss.
        if installed: print('Launch may have reached the server; inspect the same run identity before any further action.')
        raise
    finally:
        password = None; client.close()


if __name__ == '__main__': main()
