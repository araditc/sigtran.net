#!/usr/bin/env python3
"""Retain an interrupted SDK run plus separate peer recovery evidence without rewriting its failed receipt."""
import argparse
import importlib.util
import json
import os
import pathlib
import re

HERE = pathlib.Path(__file__).resolve().parent


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, HERE / file)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def download(sftp, remote, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with sftp.open(remote, 'rb') as source, path.open('xb') as target:
        while data := source.read(65536): target.write(data)


def collect(args):
    import paramiko
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', args.run_id): raise ValueError('Invalid run identity')
    root = '/opt/sigtran-endurance/runs/' + args.run_id
    args.output.mkdir(parents=True, exist_ok=False)
    bootstrap = load('endurance_archive_bootstrap', 'start-endurance-lab.py')
    verifier = load('endurance_archive_verifier', 'verify-endurance-evidence.py')
    clients = []
    try:
        for address, key, password in ((args.sdk_host, args.key, None), (args.peer_host, None, os.environ.pop('SIGTRAN_LAB_PEER_PASSWORD'))):
            client = paramiko.SSHClient(); client.load_host_keys(args.known_hosts)
            client.set_missing_host_key_policy(paramiko.RejectPolicy())
            client.connect(address, username='ammar', key_filename=key, password=password,
                           look_for_keys=False, allow_agent=False, timeout=15)
            clients.append(client)
        sdk, peer = clients
        for role, client in (('sdk', sdk), ('peer', peer)):
            state = bootstrap.command(client, ['systemctl', 'show', 'sigtran-endurance-' + role + '-' + args.run_id,
                '--property=ActiveState'])
            if state not in ('ActiveState=failed', 'ActiveState=inactive'): raise ValueError('Stop the owned run before archival')
        with sdk.open_sftp() as sftp:
            download(sftp, root + '/work/sha256.json', args.output / 'original/sha256.json')
            manifest = verifier.read_json(args.output / 'original/sha256.json')
            for name, expected in manifest.items():
                if not re.fullmatch(r'[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*', name) or '..' in pathlib.PurePosixPath(name).parts:
                    raise ValueError('Unsafe original artifact path')
                path = args.output / 'original' / name
                download(sftp, root + '/work/' + name, path)
                if verifier.capacity.digest(path) != expected: raise ValueError('Original interrupted bytes changed')
        recovery = args.output / 'peer-recovery'
        with peer.open_sftp() as sftp:
            for name, source in [('peer-result.json', '/peer-result.json'), ('host-maintenance.json', '/host-maintenance.json'),
                                 ('peer-counters.bin', '/data/peer-counters.bin'), ('peer.log', '/data/peer.log')]:
                download(sftp, root + source, recovery / name)
        code = ('import pathlib,re; p=pathlib.Path("/var/log/unattended-upgrades/unattended-upgrades-dpkg.log"); '
                'print("\\n".join(line for line in p.read_text(errors="replace").splitlines() '
                'if "systemctl restart" in line and ' + repr('sigtran-endurance-peer-' + args.run_id + '.service') + ' in line))')
        proof = bootstrap.command(peer, ['python3', '-c', code])
        (recovery / 'package-restart.log').write_text(proof + '\n')
        original = args.output / 'original'
        sdk_result = verifier.read_json(original / 'sdk/sdk.json'); peer_result = verifier.read_json(recovery / 'peer-result.json')
        source = sdk_result['SourceCommit']
        if peer_result['sourceCommit'] != source or sdk_result['Run']['Aborted'] is not True:
            raise ValueError('This archive is not one interrupted source-bound attempt')
        verifier.chain(original / 'sdk/checkpoints.jsonl')
        terminal = verifier.counts(sdk_result['Run']['Counts'])
        maintenance = verifier.maintenance(verifier.read_json(recovery / 'host-maintenance.json'), sdk_result['Run'])
        if (original / 'sdk.exit').read_text() == '0\n' or (original / 'controller.exit').read_text() == '0\n':
            raise ValueError('An interrupted archive must retain actual failed exits')
        report = dict(schemaVersion=1, evidenceKind='interrupted-map-endurance', runId=args.run_id,
            testedSourceCommit=source, executionValid=False, qualificationPassed=False, stablePromotion=False,
            startedAtUtc=sdk_result['Run']['StartedAtUtc'], finishedAtUtc=sdk_result['Run']['FinishedAtUtc'],
            measuredSeconds=sdk_result['Run']['MeasuredSeconds'], requiredSeconds=259200,
            completedTransactions=sum(sdk_result['Run']['MeasuredByOperation']), terminalAdmissions=terminal['started'],
            declaredFaultTerminals=terminal['faultTerminals'], unexpectedTerminalOutcomes=terminal['unexpected'],
            unresolvedOperations=terminal['pending'], recoveredIncidents=sdk_result['Run']['Counts']['RecoveredFaults'],
            resourceBoundsPassed=sdk_result['ResourceSummary']['WithinBounds'], peakWorkingSetBytes=sdk_result['ResourceSummary']['PeakWorkingSetBytes'],
            sdkExitCode=int((original / 'sdk.exit').read_text()), controllerExitCode=int((original / 'controller.exit').read_text()),
            originalCollection=verifier.read_json(original / 'inventory.json')['collectionErrors'],
            originalReceiptsUnmodified=True, maintenanceSupplement=maintenance,
            packageRestartOfOwnedPeerRecorded=bool(proof), reasons=['interrupted-before-72-hours', 'unplanned-peer-service-restart', 'original-peer-export-failed'])
        report['privateArchiveSha256'] = {p.relative_to(args.output).as_posix(): verifier.capacity.digest(p)
            for p in sorted(args.output.rglob('*')) if p.is_file()}
        with args.summary.open('x') as stream: json.dump(report, stream, indent=2); stream.write('\n')
        print(json.dumps({key: value for key, value in report.items() if key != 'privateArchiveSha256'}, indent=2))
    finally:
        for client in clients: client.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--key', required=True)
    parser.add_argument('--sdk-host', default='192.168.100.28')
    parser.add_argument('--peer-host', default='192.168.100.43')
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    parser.add_argument('--output', required=True, type=pathlib.Path)
    parser.add_argument('--summary', required=True, type=pathlib.Path)
    collect(parser.parse_args())
