#!/usr/bin/env python3
"""Read/discover server-local endurance state; never start, stop, resume or qualify a workload."""
import base64
import datetime
import hashlib
import json
import pathlib
import re
import struct
import subprocess
import sys

BASE = pathlib.Path('/opt/sigtran-endurance/runs')
ID = r'[a-z0-9][a-z0-9-]{0,47}'


def metadata(root):
    """A retained, hash-checked start identifies the source/process after desktop state is lost."""
    path = root / 'work/sdk/checkpoints.jsonl'
    if not path.is_file() or path.is_symlink(): return None
    with path.open('rb') as stream: line = stream.readline(32769)
    if not line.endswith(b'\n') or len(line) > 32768: return None
    frame = json.loads(line)
    body = base64.b64decode(frame['PayloadBase64'], validate=True)
    if len(body) > 16384 or frame['Sequence'] != 1 or frame['PreviousSha256'] != '0' * 64:
        raise ValueError('Invalid retained start frame')
    if hashlib.sha256(bytes(32) + body).hexdigest() != frame['Sha256']: raise ValueError('Start frame digest differs')
    header = json.loads(body)
    if header['Kind'] != 'start' or not re.fullmatch('[0-9a-f]{40}', header['SourceCommit']):
        raise ValueError('Invalid retained source identity')
    # Python 3.10 discards only the .NET timestamp's unrepresentable 100 ns digit.
    started = datetime.datetime.fromisoformat(re.sub(r'(\.\d{6})\d(?=Z|[+-])', r'\1', header['StartedAtUtc']).replace('Z', '+00:00'))
    return dict(sourceCommit=header['SourceCommit'], startedAtUtc=header['StartedAtUtc'],
                expectedMeasuredFinishUtc=(started + datetime.timedelta(seconds=header['DurationSeconds'])).isoformat(),
                durationSeconds=header['DurationSeconds'], diagnostic=header['Diagnostic'],
                sdkProcessId=header['ProcessId'], runtime=header['Runtime'])


def unit_state(identity):
    result = subprocess.run(['systemctl', 'show', 'sigtran-endurance-sdk-' + identity + '.service',
        '--property=LoadState,ActiveState,SubState,Result,ExecMainStatus,MainPID,ExecMainStartTimestamp,NRestarts'],
        check=True, capture_output=True, text=True, timeout=10)
    return dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)


def choose(candidates):
    """Prefer the one live full run; diagnostics and prior failed attempts never displace it."""
    full = [value for value in candidates if value['measurement']['diagnostic'] is False
            and value['measurement']['durationSeconds'] == 259200]
    active = [value for value in full if value['unit'].get('ActiveState') in ('active', 'activating', 'deactivating', 'reloading')]
    if len(active) > 1: raise ValueError('Multiple full runs are active; explicit run identity required')
    if active: return active[0]
    if not full: raise ValueError('No retained full endurance run exists')
    return max(full, key=lambda value: value['measurement']['startedAtUtc'])


def bounded_json(path):
    if path.is_symlink() or path.stat().st_size > 2097152: raise ValueError('Invalid retained status document')
    return json.loads(path.read_text())


def read(identity=None):
    if identity is not None and not re.fullmatch(ID, identity): raise ValueError('Invalid run identity')
    candidates = []
    roots = [BASE / identity] if identity is not None else sorted(BASE.iterdir())
    for root in roots:
        if root.is_symlink() or not root.is_dir() or not re.fullmatch(ID, root.name): continue
        start = metadata(root)
        if start is None: continue
        if identity is None and (start['diagnostic'] is not False or start['durationSeconds'] != 259200): continue
        candidates.append(dict(runId=root.name, measurement=start, unit=unit_state(root.name)))
    if identity is not None:
        if len(candidates) != 1: raise ValueError('Retained start is not available yet')
        result = candidates[0]
    else: result = choose(candidates)
    root = BASE / result['runId']
    result.update(management='reachable', qualificationPassed=False, readOnly=True,
                  checkedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
    path = root / 'work/sdk/resource-observations.bin'
    if path.exists():
        size = path.stat().st_size
        if size >= 216:
            with path.open('rb') as stream:
                if stream.read(8) != b'SGENOBS1': raise ValueError('Invalid resource observation header')
                stream.seek(8 + ((size - 8) // 208 - 1) * 208)
                row = struct.unpack('<d6qd18q', stream.read(208))
            result['observation'] = dict(elapsedSeconds=row[0], started=row[14], successful=row[15], pending=row[16],
                peakRssMiB=row[2] / 1048576, managedMiB=row[3] / 1048576, heapMiB=row[4] / 1048576)
    for name, key in [('work/inventory.json', 'collection'), ('verified-summary.json', 'verifiedSummary')]:
        path = root / name
        if path.exists():
            value = bounded_json(path)
            fields = ('completed', 'cleanupVerified', 'collectionErrors', 'sdkExitCode', 'controllerExitCode', 'supervisorCleanupVerified') if key == 'collection' else (
                'testedSourceCommit', 'diagnostic', 'executionValid', 'qualificationPassed', 'stablePromotion',
                'durationSeconds', 'completedTps', 'recoveredIncidents', 'unexpectedFailures', 'unresolvedOperations', 'reasons')
            result[key] = {field: value[field] for field in fields if field in value}
    path = root / 'work/sdk/control-ack.json'
    if path.exists():
        value = bounded_json(path)
        result['lastControlAcknowledgement'] = {key: value[key] for key in ('Sequence', 'Id', 'Action', 'ElapsedSeconds')}
    state = result['unit'].get('ActiveState')
    if state in ('active', 'activating', 'reloading', 'deactivating'):
        result['executionState'] = 'running' if state != 'deactivating' else 'stopping'
    elif state == 'failed': result['executionState'] = 'failed'
    elif 'verifiedSummary' in result:
        result['executionState'] = 'completed' if result['unit'].get('Result') == 'success' else 'completed-supervision-unconfirmed'
    elif 'collection' in result: result['executionState'] = 'ended-without-verified-summary'
    else: result['executionState'] = 'unknown'
    return result


if __name__ == '__main__':
    if len(sys.argv) != 2: raise ValueError('One run identity or @latest required')
    print(json.dumps(read(None if sys.argv[1] == '@latest' else sys.argv[1]), indent=2))
