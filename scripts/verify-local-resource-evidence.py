#!/usr/bin/env python3
"""Replay a completed local resource diagnostic without granting endurance or capacity qualification."""
import argparse
import importlib.util
import json
import pathlib
import re

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('local_resource_endurance_contract', HERE / 'verify-endurance-evidence.py')
evidence = importlib.util.module_from_spec(spec); spec.loader.exec_module(evidence)
require = evidence.require
POLICY = 'local-map-resource-diagnostic-v1'


def contexts(path, binary, header):
    """Compare every JSON sample with the original binary row; separate GC reads are not atomic snapshots."""
    require(path.stat().st_size <= 16 * 1048576, 'GC observation JSON exceeds budget')
    changed_index = changed_gen0 = number = 0
    with binary.open('rb') as raw, path.open('rb') as stream:
        record = evidence.observation_schema(raw.read(8), header)
        require(record is evidence.OBSERVATION_WITH_READS, 'Local diagnostic requires audited observations')
        while line := stream.readline(8193):
            require(len(line) <= 8192 and line.endswith(b'\n'), 'Truncated or unbounded GC observation')
            value = evidence.json_bytes(line); sample, stack = value['Sample'], value['Stack']
            resources = sample['Resources']; ledger = evidence.counts(sample['Counts'])
            for key in evidence.RESOURCE_KEYS:
                if key == 'ProcessCpuSeconds': evidence.finite(resources[key])
                else: evidence.bounded_int(resources[key])
            require(sample['StartedAtUtc'] == header['StartedAtUtc'], 'GC observation origin differs')
            data = raw.read(record.size)
            require(len(data) == record.size, 'JSON observation has no complete binary row')
            row = record.unpack(data)
            expected = (sample['ElapsedSeconds'], *(resources[key] for key in evidence.RESOURCE_KEYS),
                ledger['started'], ledger['successes'], ledger['pending'], stack['Active'], stack['Pending'],
                *(stack[key] for key in ('OutboundQueue', 'InboundQueue', 'Malformed', 'DroppedEvents',
                                        'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns')))
            reading = evidence.managed_reading(resources['ManagedBytes'], row[26:])
            require(row[:26] == expected and reading == resources['ManagedMemory']
                    and all(type(resources['ManagedMemory'][key]) is int for key in reading), 'JSON/binary observation differs')
            context = resources['GcContext']
            require(set(context) == {'BeforeIndex', 'AfterIndex', 'Gen0Before', 'Gen0After'}, 'Unexpected GC context schema')
            for number_value in context.values(): evidence.bounded_int(number_value)
            require(context['Gen0After'] >= context['Gen0Before'], 'GC bracket count decreased')
            changed_index += context['BeforeIndex'] != context['AfterIndex']
            changed_gen0 += context['Gen0Before'] != context['Gen0After']
            number += 1
        require(not raw.read(1), 'JSON observations omit binary rows')
    require(number > 0, 'GC context observations are absent')
    return dict(observations=number, changedIndexBrackets=changed_index, changedGen0Brackets=changed_gen0,
                atomicSnapshot=False, rootCauseProven=False)


def trace_inventory(root, value, seconds, pid, exits, hashes):
    """Check retained window identity and actual exits, not a claim of whole-hour event coverage."""
    windows = value['windows']
    scheduled = [item for item in windows if item['reason'] == 'scheduled']
    expected = [2] if seconds < 60 else sorted({2, seconds // 2, max(2, seconds - 60)})
    require(len(scheduled) == len(expected) and len(windows) <= len(expected) + 2, 'Trace window inventory differs')
    names = set()
    for item in windows:
        name = item['name']
        require(re.fullmatch(r'trace-[0-9]{3}', name) and name not in names, 'Duplicate or unsafe trace window')
        names.add(name)
        require(item['file'] == name + '.nettrace' and {item['file'], name + '.log'} <= hashes.keys(), 'Trace window files missing')
        require(item['sdkPid'] == pid and item['exitCode'] == exits.get(name) == 0
                and item['requestedSeconds'] == min(20, seconds - 5)
                and item['reason'] in ('scheduled', 'post-negative-observation'), 'Trace identity/duration/exit differs')
        require(0 < (root / item['file']).stat().st_size <= 32 * 1048576, 'Trace window byte budget differs')
        require(evidence.utc_time(item['finishedAtUtc']) > evidence.utc_time(item['startedAtUtc']), 'Trace window timestamps differ')
    for item, offset in zip(scheduled, expected):
        require(offset <= item['offsetFromSdkSpawnSeconds'] <= offset + 30, 'Trace scheduling delay exceeds bound')
    require(set(exits) == {'sdk', 'peer', 'capture'} | names, 'Unexpected or missing owned child exit')
    return dict(windows=len(windows), scheduledWindows=len(scheduled), fullIntervalCoverage=False,
                independentlyDecoded=False, negativeObservationAlerts=len(value['negativeObservationAlerts']))


def verify(root, source, seconds):
    root = pathlib.Path(root).resolve()
    require(re.fullmatch('[0-9a-f]{40}', source) and type(seconds) is int and 10 <= seconds <= 3600,
            'Expected source and diagnostic duration must be explicit')
    hashes = evidence.read_json(root / 'sha256.json')
    required = {'controller-result.json', 'processes.json', 'peer-counters.bin', 'peer-summary.json',
                'sdk/sdk.json', 'sdk/checkpoints.jsonl', 'sdk/resource-observations.bin',
                'sdk/gc-observations.jsonl', 'sample.pcap', 'capture.log', 'trace-windows.json', 'trace-tool.json'}
    require(required <= hashes.keys() and len(hashes) <= 64, 'Diagnostic evidence is incomplete')
    for name, digest in hashes.items():
        require(re.fullmatch('[0-9a-f]{64}', digest)
                and evidence.capacity.digest(evidence.relative_file(root, name)) == digest, 'Artifact digest differs: ' + name)
    sdk, controller, process, peer = [evidence.read_json(root / name) for name in
        ('sdk/sdk.json', 'controller-result.json', 'processes.json', 'peer-summary.json')]
    frames = evidence.chain(root / 'sdk/checkpoints.jsonl'); header = frames[0]; run = sdk['Run']
    require(header['Policy'] == sdk['Policy'] == POLICY and frames[-1]['Report'] == sdk
            and header['SourceCommit'] == sdk['SourceCommit'] == controller['sourceCommit'] == process['sourceCommit'] == source,
            'Source, journal or policy differs')
    require(header['Diagnostic'] is sdk['Diagnostic'] is controller['diagnostic'] is True
            and sdk['LocalOnly'] is True and sdk['QualificationPassed'] is controller['qualificationPassed'] is False
            and sdk['StablePromotion'] is controller['stablePromotion'] is sdk['RootCauseProven'] is False,
            'Diagnostic fence differs')
    require(controller['errors'] == [] and controller['ownedChildrenStopped'] is True
            and controller['kafkaControl'] is controller['externalPeerTraffic'] is False
            and all(type(code) is int and code == 0 for code in controller['childExits'].values()), 'Owned child execution failed')
    require(header['ProcessId'] == process['sdkPid'] and process['sdkStartTicks'] > 0
            and process['peerPid'] == peer['processId'] != process['sdkPid'], 'Process identities differ')
    require(header['Workload'] == peer['workload'] == evidence.capacity.WORKLOAD
            and header['ServerGc'] is True and header['MaximumWorkers'] == 4
            and header['Concurrency'] == 256 and header['QueueCapacity'] == 8192, 'Executed workload profile differs')
    require(header['DurationSeconds'] == process['durationSeconds'] == run['MeasuredSeconds'] == seconds
            and run['StartedAtUtc'] == header['StartedAtUtc'] == header['Sample']['StartedAtUtc']
            and seconds <= run['ElapsedSeconds'] <= seconds + 30
            and seconds <= (evidence.utc_time(run['FinishedAtUtc']) - evidence.utc_time(run['StartedAtUtc'])).total_seconds() <= seconds + 30,
            'Actual diagnostic duration/origin differs')
    require(all(frame['Kind'] == 'checkpoint' for frame in frames[1:-1]), 'Diagnostic includes unexpected control events')
    samples = [header['Sample']] + [frame['Sample'] for frame in frames[1:-1]]
    require(all(sample['StartedAtUtc'] == run['StartedAtUtc'] for sample in samples), 'Checkpoint origins differ')
    resources = evidence.replay_resources(root / 'sdk/resource-observations.bin', header, samples, sdk)
    context = contexts(root / 'sdk/gc-observations.jsonl', root / 'sdk/resource-observations.bin', header)
    require(context['observations'] == resources['observations'], 'Resource/context row counts differ')
    terminal = evidence.counts(run['Counts']); measured = sum(evidence.vector(run['MeasuredByOperation'], 5))
    require(sdk['ExecutionPassed'] is True and run['Aborted'] is False and run['Errors'] == []
            and terminal['pending'] == terminal['unexpected'] == terminal['faultTerminals'] == run['Counts']['AccountingViolations'] == 0
            and run['Counts']['Fatal'] is False and run['Counts']['ActiveFaultId'] is None and run['Counts']['RecoveredFaults'] == 0
            and measured > 0 and measured + evidence.bounded_int(run['LateSuccesses']) == terminal['successes']
            and run['Latency']['Count'] == measured, 'Final diagnostic ledger differs')
    quantiles = [evidence.finite(run['Latency'][key]) for key in
                 ('P50Milliseconds', 'P95Milliseconds', 'P99Milliseconds', 'MaximumMilliseconds')]
    require(quantiles == sorted(quantiles) and run['Latency']['ResolutionMilliseconds'] == .1, 'Latency quantiles/resolution differ')
    stack = sdk['Stack']
    require(all(stack[key] == terminal['successes'] == terminal['started'] for key in ('Opened', 'Closed', 'Sent', 'Received'))
            and all(stack[key] == 0 for key in ('Pending', 'Active', 'Faults', 'Reconnects', 'Malformed', 'DroppedEvents',
                                             'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns', 'OutboundQueue', 'InboundQueue'))
            and stack['ReadersHealthy'] is True, 'Final stack accounting/cleanup differs')
    counter = evidence.peer_counters.read(root / 'peer-counters.bin', 0)
    require(counter['received'] == counter['acceptedResponses'] == peer['received'] == peer['responses'] == terminal['successes']
            and counter['byOperation'] == peer['byOperation'] == run['Counts']['StartedByOperation']
            and counter['validationFailures'] == 0 and peer['passed'] is True, 'Independent peer accounting differs')
    evidence.validate_socket_profile(header['SocketProfile']); evidence.validate_socket_profile(peer['sctpSocketProfile'])
    require(header['SocketProfile'] == sdk['SocketProfile'], 'SDK profile changed')
    log = (root / 'capture.log').read_text()
    require(re.search(r"Packets received/dropped on interface '[^']+': [0-9]+/0 \(pcap:0/dumpcap:0/flushed:0/ps_ifdrop:0\)", log),
            'Capture drops or missing capture receipt')
    traces = trace_inventory(root, evidence.read_json(root / 'trace-windows.json'), seconds, process['sdkPid'], controller['childExits'], hashes)
    require(traces['negativeObservationAlerts'] <= resources['negativeManagedReadings'], 'Trace alerts exceed raw negative readings')
    return dict(schemaVersion=1, evidenceKind='local-map-resource-diagnostic', testedSourceCommit=source,
                executionValid=True, diagnostic=True, qualificationPassed=False, stablePromotion=False, rootCauseProven=False,
                durationSeconds=seconds, completedTransactions=measured, lateSuccesses=run['LateSuccesses'],
                terminalAdmissions=terminal['started'], unexpectedFailures=0, unresolvedOperations=0,
                resources=resources, gcContext=context, traces=traces, rawArtifactSha256=hashes,
                caveat='Wire payload decoding, GC trace decoding, input provenance and host cleanup need separate verification; no endurance or capacity qualification.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--seconds', type=int, required=True)
    args = parser.parse_args()
    print(json.dumps(verify(args.root, args.source_commit, args.seconds), indent=2))
