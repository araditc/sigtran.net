#!/usr/bin/env python3
"""Temporarily exclude one owned lab service from automatic library-update restarts.

Security updates and every unrelated restart selection remain unchanged. The
additive needrestart snippet expires by its own Perl time condition even when
the owner cannot perform cleanup; normal cleanup removes the exact owned file.
"""
import datetime
import hashlib
import json
import os
import pathlib
import re
import subprocess

DIRECTORY = pathlib.Path('/etc/needrestart/conf.d')
CONFIG = pathlib.Path('/etc/needrestart/needrestart.conf')
STATE = 'restart-policy.json'


def utc(): return datetime.datetime.now(datetime.timezone.utc).isoformat()


def service(run_id, role):
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', run_id) or role not in ('sdk', 'peer'):
        raise ValueError('Only an exact owned service may be protected')
    return 'sigtran-endurance-' + role + '-' + run_id + '.service'


def content(run_id, role, expiry):
    unit = service(run_id, role)
    deadline = datetime.datetime.fromisoformat(expiry)
    if deadline.utcoffset() != datetime.timedelta(0): raise ValueError('UTC policy expiry required')
    return ('# Temporary owned lab selection; does not replace global restart policy.\n'
            'if (time() < ' + str(int(deadline.timestamp())) + ') {\n'
            '    $nrconf{override_rc}{qr(^' + re.escape(unit) + '$)} = 0;\n}\n').encode()


def save(root, state):
    target = root / STATE
    pending = target.with_name(target.name + '.pending')
    with pending.open('w') as stream:
        json.dump(state, stream); stream.write('\n'); stream.flush(); os.fsync(stream.fileno())
    os.replace(pending, target)


def automatic_reboot():
    result = subprocess.run(['apt-config', 'shell', 'STATE', 'Unattended-Upgrade::Automatic-Reboot'],
                            check=True, capture_output=True, text=True, timeout=20)
    value = result.stdout.strip()
    if value not in ('', "STATE='false'", "STATE='0'"):
        raise ValueError('Automatic reboot would invalidate continuous execution')
    return False


def verify_effective(unit):
    """Read the installed selector exactly as needrestart does, without scanning/restarting services."""
    code = ('our %nrconf; do $ARGV[0]; die $@ if $@; my $unit=$ARGV[1]; '
            'for my $pattern (sort keys %{$nrconf{override_rc}}) { '
            'if ($unit =~ $pattern) { exit($nrconf{override_rc}{$pattern} == 0 ? 0 : 1); }} exit 2;')
    subprocess.run(['perl', '-e', code, str(CONFIG), unit], check=True, capture_output=True, timeout=20)


def install(value, root, role):
    """Create only a new, additive, exact-service override before any maintenance/load starts."""
    unit = service(value['runId'], role)
    expected = content(value['runId'], role, value['expiresAtUtc'])
    if root.name != value['runId']: raise ValueError('Protected directory identity differs')
    if (root / STATE).exists(): raise ValueError('A protected service cannot restart or reuse a run')
    state = dict(schemaVersion=1, runId=value['runId'], role=role, unit=unit, expiresAtUtc=value['expiresAtUtc'],
                 installedAtUtc=utc(), automaticRebootEnabled=automatic_reboot(), available=CONFIG.is_file(),
                 protected=False, removed=False, policyAbsent=False, unrelatedRestartPolicyChanged=False,
                 snippetSha256=hashlib.sha256(expected).hexdigest())
    if not state['available']:
        state.update(removed=True, policyAbsent=True, reason='needrestart-not-installed', removedAtUtc=utc())
        save(root, state); return state
    if datetime.datetime.now(datetime.timezone.utc) >= datetime.datetime.fromisoformat(value['expiresAtUtc']):
        raise ValueError('Cannot protect an expired run')
    subprocess.run(['perl', '-c', '-'], input=b'our %nrconf;\n' + expected, check=True, capture_output=True, timeout=20)
    if not DIRECTORY.is_dir() or DIRECTORY.is_symlink() or DIRECTORY.stat().st_uid != 0 or DIRECTORY.stat().st_mode & 0o022:
        raise ValueError('Protected host policy directory required')
    path = DIRECTORY / ('sigtran-endurance-' + value['runId'] + '.conf')
    descriptor = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY | os.O_NOFOLLOW, 0o644)
    try:
        with os.fdopen(descriptor, 'wb') as stream: stream.write(expected); stream.flush(); os.fsync(stream.fileno())
        verify_effective(unit)
        state.update(protected=True, effectiveSelectionVerified=True)
        save(root, state)
        return state
    except BaseException:
        if not path.is_symlink() and path.read_bytes() == expected: path.unlink()
        raise


def release(root, directory=None):
    """Removal is idempotent and cannot delete a substituted or unrelated configuration file."""
    state_path = root / STATE
    if not state_path.exists(): return None
    state = json.loads(state_path.read_text())
    if state['runId'] != root.name or service(root.name, state['role']) != state['unit']:
        raise ValueError('Policy ownership identity differs')
    path = (DIRECTORY if directory is None else directory) / ('sigtran-endurance-' + root.name + '.conf')
    if path.is_symlink(): raise ValueError('Linked policy file rejected')
    if path.exists():
        if hashlib.sha256(path.read_bytes()).hexdigest() != state['snippetSha256']:
            raise ValueError('Changed policy file is not owned by this cleanup')
        path.unlink()
    state.update(removed=True, policyAbsent=True, removedAtUtc=state.get('removedAtUtc', utc()), lastCheckedAtUtc=utc())
    save(root, state)
    return state
