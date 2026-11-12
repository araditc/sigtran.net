#!/usr/bin/env python3
"""Run explicitly approved temporary host quiescence with restoration and a remote fallback timer."""
import datetime
import importlib.util
import json
import os
import pathlib
import re
import secrets
import shlex
import time

SPEC = importlib.util.spec_from_file_location('capacity_runner', pathlib.Path(__file__).with_name('run-capacity-qualification.py'))
runner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(runner)


def validate_units(units):
    """Only explicitly listed, bounded service/timer names can be operated on; never essential host services."""
    if not 1 <= len(units) <= 8 or len(set(units)) != len(units):
        raise ValueError('One to eight distinct maintenance units are required')
    for unit in units:
        if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_.@:-]*\.(service|timer)', unit):
            raise ValueError('Invalid maintenance unit name')
        if unit.lower().startswith(('ssh.', 'sshd.', 'systemd-', 'network', 'dbus.', 'docker.', 'containerd.')):
            raise ValueError('Essential host services are outside this maintenance scope')
    return units


def restoration_script(units):
    """Try every restoration, even when one unit fails; retain a nonzero final status."""
    return ('status=0; for unit in ' + ' '.join(shlex.quote(unit) for unit in units)
            + '; do systemctl start -- "$unit" || status=1; done; exit "$status"')


def restored(before, after):
    """PIDs may change; service state, enablement and unit definitions must not."""
    return all(after[name]['ActiveState'] == state['ActiveState']
               and after[name]['SubState'] == state['SubState']
               and after[name]['UnitFileState'] == state['UnitFileState']
               and after[name]['fragmentSha256'] == state['fragmentSha256']
               for name, state in before.items())


def restoration_order(units, before):
    """Reverse the approved stop order, excluding units that were initially inactive."""
    if any(before[unit]['ActiveState'] not in ('active', 'inactive') for unit in units):
        raise ValueError('Maintenance cannot start while a selected unit is transitioning or failed')
    return [unit for unit in reversed(units) if before[unit]['ActiveState'] == 'active']


def run(args):
    import paramiko
    if not args.allow_temporary_unit_stop or args.shared_peer or args.diagnostic or not args.dedicated_hosts or args.duration != 1800:
        raise ValueError('Explicit approval and a full dedicated-host, non-diagnostic run are required')
    units = validate_units(args.maintenance_unit)
    if not args.duration + 600 <= args.restoration_guard_seconds <= 7200:
        raise ValueError('Restoration guard must cover the run and be <= two hours')
    sudo_password = os.environ.pop('SIGTRAN_MAINTENANCE_SUDO_PASSWORD')
    peer_password = os.environ.get('SIGTRAN_CAPACITY_PEER_PASSWORD')
    output = pathlib.Path(args.output).resolve()
    if output.exists():
        raise ValueError('A new evidence directory is required')
    audit_root = output.with_name(output.name + '-maintenance')
    audit_root.mkdir(parents=True, exist_ok=False)
    audit = dict(schemaVersion=1, explicitlyAuthorized=True, peerHost=args.peer_host,
                 startedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                 stoppedUnits=units, restorationVerified=False, unitFilesUnchanged=False)
    client = paramiko.SSHClient()
    client.load_host_keys(str(pathlib.Path(args.known_hosts).expanduser()))
    client.set_missing_host_key_policy(paramiko.RejectPolicy())

    def connect():
        client.close()
        client.connect(args.peer_host, username=args.user, password=peer_password, key_filename=args.peer_key,
                       look_for_keys=False, allow_agent=False, timeout=10, auth_timeout=10,
                       banner_timeout=10, channel_timeout=10)
        client.get_transport().set_keepalive(15)

    def command(arguments, timeout=180):
        if client.get_transport() is None or not client.get_transport().is_active():
            connect()
        input_stream, stdout, stderr = client.exec_command(shlex.join(['sudo', '-S', '-p', '', *arguments]), timeout=timeout)
        input_stream.write(sudo_password + '\n')
        input_stream.flush()
        input_stream.channel.shutdown_write()
        text, error = stdout.read().decode(), stderr.read().decode()
        status = stdout.channel.recv_exit_status()
        if status:
            raise RuntimeError('Maintenance command failed: ' + str(status) + ' ' + error[:300])
        return text

    def snapshot():
        code = '''import subprocess,pathlib,hashlib,json
units=%s
result={}
for unit in units:
 r=subprocess.run(['systemctl','show',unit,'--no-pager','--property=LoadState,ActiveState,SubState,UnitFileState,MainPID,FragmentPath'],capture_output=True,text=True,check=True)
 values=dict(line.split('=',1) for line in r.stdout.splitlines() if '=' in line)
 if values.get('LoadState')!='loaded':raise ValueError('Maintenance unit is not loaded')
 values['fragmentSha256']=hashlib.sha256(pathlib.Path(values['FragmentPath']).read_bytes()).hexdigest()
 result[unit]=values
print(json.dumps(result))
''' % repr(units)
        return json.loads(command(['python3', '-c', code], 30))

    def save():
        (audit_root / 'host-maintenance.json').write_text(json.dumps(audit, indent=2) + '\n')

    guard = 'sigtran-capacity-restore-' + secrets.token_hex(8)
    restore_order = []
    may_be_stopped = False

    def restore(evidence_root=None):
        if not may_be_stopped:
            return
        if not audit['restorationVerified']:
            failures = []
            for unit in restore_order:
                try:
                    command(['systemctl', 'start', '--', unit])
                except Exception as error:
                    failures.append(unit + ':' + type(error).__name__)
            deadline = time.monotonic() + 90
            while True:
                audit['after'] = snapshot()
                audit['restorationVerified'] = restored(audit['before'], audit['after'])
                if audit['restorationVerified'] or time.monotonic() >= deadline:
                    break
                time.sleep(2)
            audit['unitFilesUnchanged'] = all(audit['before'][unit]['fragmentSha256'] == audit['after'][unit]['fragmentSha256'] for unit in units)
            audit['restorationErrors'] = failures
            if audit['restorationVerified']:
                audit['restoredAtUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
            save()
        # Cancellation is independently retryable after a successful restoration.
        if audit['restorationVerified'] and not audit.get('fallbackTimerCancelled'):
            try:
                command(['systemctl', 'stop', '--', guard + '.timer'], 30)
                audit['fallbackTimerCancelled'] = True
            finally:
                save()
        if evidence_root is not None:
            (evidence_root / 'host-maintenance.json').write_text(json.dumps(audit, indent=2) + '\n')
        if not audit['restorationVerified']:
            raise RuntimeError('Host restoration is not verified; the fallback timer remains armed')

    try:
        connect()
        audit['before'] = snapshot()
        restore_order = restoration_order(units, audit['before'])
        if not restore_order:
            raise ValueError('No active units need the approved temporary stop')
        audit['restoreOrder'] = restore_order
        audit['fallbackTimer'] = guard
        command(['systemd-run', '--quiet', '--collect', '--unit=' + guard,
                 '--on-active=' + str(args.restoration_guard_seconds) + 's', '--timer-property=AccuracySec=1s',
                 '--property=Type=oneshot', '/bin/sh', '-c', restoration_script(restore_order)], 30)
        command(['systemctl', 'is-active', '--quiet', guard + '.timer'], 15)
        audit['fallbackTimerArmed'] = True
        save()
        may_be_stopped = True
        for unit in units:
            command(['systemctl', 'stop', '--', unit])
        audit['quiesced'] = snapshot()
        if any(state['ActiveState'] != 'inactive' for state in audit['quiesced'].values()):
            raise RuntimeError('Maintenance units did not become inactive')
        audit['quiescedAtUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        save()
        print('Approved units temporarily stopped; fallback restoration armed.', flush=True)
        result = runner.run(args, restore_host=restore)
        return result
    finally:
        try:
            restore()
        finally:
            save()
            client.close()


if __name__ == '__main__':
    parser = runner.create_parser()
    parser.description = __doc__
    parser.add_argument('--allow-temporary-unit-stop', action='store_true')
    parser.add_argument('--maintenance-unit', action='append', default=[])
    parser.add_argument('--restoration-guard-seconds', type=int, default=3300)
    raise SystemExit(run(parser.parse_args()))
