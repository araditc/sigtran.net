#!/usr/bin/env python3
"""Recompute MAP endurance eligibility from protected original artifacts; never promote stable."""
import argparse
import base64
import csv
import hashlib
import importlib.util
import io
import json
import math
import pathlib
import re
import struct
import subprocess
import tarfile
import tempfile

HERE = pathlib.Path(__file__).resolve().parent


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, HERE / filename)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


capacity = module('endurance_capacity_contract', 'verify-capacity-evidence.py')
peer_counters = module('endurance_peer_counters', 'endurance-peer-counters.py')
require, read_json, utc_time = capacity.require, capacity.read_json, capacity.utc_time
POLICY = 'completed-map-endurance-v1'
SECONDS = 259200
OBSERVATION = struct.Struct('<d6qd18q')
KINDS = ('peer-restart', 'peer-outage', 'sctp-partition')
OFFSETS = (900, 1800, 2700, 87300, 88200, 89100, 173700, 174600, 175500)
RESOURCE_KEYS = ('WorkingSetBytes', 'PeakWorkingSetBytes', 'ManagedBytes', 'HeapSizeBytes',
                 'FragmentedBytes', 'AllocatedBytes', 'ProcessCpuSeconds', 'Gen0', 'Gen1', 'Gen2',
                 'ThreadPoolThreads', 'ThreadPoolPending', 'ThreadPoolCompleted')


def bounded_int(value, maximum=(1 << 63) - 1):
    require(type(value) is int and 0 <= value <= maximum, 'Invalid bounded nonnegative integer')
    return value


def vector(value, length):
    require(isinstance(value, list) and len(value) == length, 'Invalid counter vector width')
    return [bounded_int(number) for number in value]


def finite(value):
    require(type(value) in (int, float) and math.isfinite(value) and value >= 0, 'Invalid finite observation')
    return value


def unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON property')
        result[key] = value
    return result


def json_bytes(data):
    def invalid(value): raise ValueError('Non-finite JSON constant')
    return json.loads(data, object_pairs_hook=unique, parse_constant=invalid)


def counts(value):
    started = vector(value['StartedByOperation'], 5)
    successes = vector(value['SuccessfulByOperation'], 5)
    outcomes = vector(value['Outcomes'], 8)
    fault = vector(value['FaultWindowOutcomes'], 8)
    pending = bounded_int(value['Pending'], 1024)
    require(sum(started) - sum(outcomes) == pending and sum(successes) == outcomes[0]
            and fault[0] == 0 and all(a <= b for a, b in zip(fault, outcomes))
            and all(a <= b for a, b in zip(successes, started)), 'Unreconciled admission/terminal counters')
    require(max(started) - min(started) <= 1, 'MAP admission mix is not equal round robin')
    return dict(started=sum(started), successes=sum(successes), terminal=sum(outcomes), pending=pending,
                unexpected=sum(outcomes[1:]) - sum(fault), faultTerminals=sum(fault), operations=started)


def chain(path):
    """Validate exact payload bytes before interpreting any saved success flag."""
    require(path.stat().st_size <= 64 * 1024 * 1024, 'Checkpoint journal exceeds byte budget')
    previous, sequence, result = '0' * 64, 0, []
    with path.open('rb') as stream:
        while line := stream.readline(32769):
            require(len(line) <= 32768 and line.endswith(b'\n') and sequence < 5000, 'Truncated or unbounded journal frame')
            frame = json_bytes(line)
            require(set(frame) == {'Sequence', 'PreviousSha256', 'PayloadBase64', 'Sha256'}, 'Unexpected journal frame schema')
            sequence += 1
            require(type(frame['Sequence']) is int and frame['Sequence'] == sequence and frame['PreviousSha256'] == previous,
                    'Journal sequence/predecessor differs')
            body = base64.b64decode(frame['PayloadBase64'], validate=True)
            require(len(body) <= 16384, 'Checkpoint payload exceeds bound')
            digest = hashlib.sha256(bytes.fromhex(previous) + body).hexdigest()
            require(frame['Sha256'] == digest, 'Checkpoint payload digest differs')
            result.append(json_bytes(body))
            previous = digest
    require(len(result) >= 2 and result[0]['Kind'] == 'start' and result[-1]['Kind'] == 'finish'
            and all(item['Kind'] not in ('start', 'finish', 'failed') for item in result[1:-1]), 'Run is incomplete, repeated or stitched')
    return result


def fault_plan(header):
    duration = bounded_int(header['DurationSeconds'], SECONDS)
    diagnostic = header['Diagnostic']
    require(type(diagnostic) is bool, 'Diagnostic marker must be boolean')
    require((not diagnostic and duration == SECONDS) or (diagnostic and 300 <= duration <= 3600), 'Duration/diagnostic fence differs')
    offsets = (duration // 12, duration // 3, duration * 7 // 12) if diagnostic else OFFSETS
    expected = [dict(Id=i + 1, Kind=KINDS[i % 3], OffsetSeconds=offset, HoldSeconds=10)
                for i, offset in enumerate(offsets)]
    require(header['FaultPlan'] == expected, 'Declared fault schedule differs from the fixed policy')
    return expected


def replay_resources(path, header, checkpoint_samples, final):
    """Stream every fixed-width observation, independently recalculating the reported resource bounds."""
    size = path.stat().st_size
    require(8 < size <= 64 * 1024 * 1024 and (size - 8) % OBSERVATION.size == 0,
            'Observation stream is truncated or over budget')
    require((size - 8) // OBSERVATION.size <= SECONDS + 60, 'Observation count exceeds bound')
    wanted = {sample['ElapsedSeconds']: sample for sample in checkpoint_samples}
    require(len(wanted) == len(checkpoint_samples), 'Repeated checkpoint timestamp')
    previous = first = None
    number, maximum_gap, maximum_idle, last_progress, last_success = 0, 0.0, 0.0, 0.0, 0
    rss = managed = heap = growth = max_pending = max_active = max_out = max_in = 0
    baseline = None
    after = header['DurationSeconds'] / 6.0 if header['Diagnostic'] else 600
    with path.open('rb') as stream:
        require(stream.read(8) == b'SGENOBS1', 'Observation format differs')
        while data := stream.read(OBSERVATION.size):
            row = OBSERVATION.unpack(data)
            if first is None: first = row
            elapsed, *rest = row
            finite(elapsed); finite(row[7])
            require(all(type(value) is int and value >= 0 for i, value in enumerate(row) if i not in (0, 7)), 'Negative resource counter')
            require(elapsed <= header['DurationSeconds'] + 30 and row[2] >= row[1], 'Observation duration or RSS bounds differ')
            if previous is not None:
                require(elapsed > previous[0], 'Observation times are not strictly monotonic')
                require(all(row[i] >= previous[i] for i in (2, 6, 7, 8, 9, 10, 13, 14, 15)), 'Cumulative resource/traffic counter decreased')
                maximum_gap = max(maximum_gap, elapsed - previous[0])
            require(row[15] <= row[14] and row[16] <= header['Concurrency'] and max(row[17:21]) <= header['QueueCapacity'],
                    'Observed request/context/queue bound differs')
            require(all(value == 0 for value in row[21:]), 'Unexpected protocol work was observed')
            number += 1
            rss, managed, heap = max(rss, row[2]), max(managed, row[3]), max(heap, row[4])
            max_pending, max_active = max(max_pending, row[16]), max(max_active, row[17])
            max_out, max_in = max(max_out, row[19]), max(max_in, row[20])
            if baseline is None and elapsed >= after: baseline = row[1]
            if baseline is not None: growth = max(growth, row[1] - baseline)
            if row[15] > last_success: last_success, last_progress = row[15], elapsed
            maximum_idle = max(maximum_idle, elapsed - last_progress)
            if elapsed in wanted:
                sample = wanted.pop(elapsed)
                require(tuple(sample['Resources'][key] for key in RESOURCE_KEYS) == row[1:14], 'Checkpoint resource values differ from original stream')
                ledger = counts(sample['Counts'])
                require((ledger['started'], ledger['successes'], ledger['pending']) == row[14:17], 'Checkpoint ledger differs from observation stream')
            previous = row
    summary = final['ResourceSummary']
    require(not wanted and baseline is not None, 'Missing resource checkpoint/baseline observation')
    require(summary['Observations'] == number and summary['LastElapsedSeconds'] == previous[0]
            and summary['PeakWorkingSetBytes'] == rss and summary['MaximumObservedManagedBytes'] == managed
            and summary['MaximumObservedHeapBytes'] == heap and summary['BaselineWorkingSetBytes'] == baseline
            and summary['BaselineAfterSeconds'] == after and summary['MaximumResidentGrowthBytes'] == growth,
            'Saved resource aggregate differs from replayed original observations')
    require(abs(summary['MaximumObservationGapSeconds'] - maximum_gap) < 1e-8
            and abs(summary['MaximumNoProgressSeconds'] - maximum_idle) < 1e-8, 'Saved gap/progress aggregate differs')
    require(summary['PeakPending'] <= header['Concurrency'] and summary['PeakPending'] >= max_pending
            and summary['PeakActive'] == max_active and summary['PeakOutboundQueue'] >= max_out
            and summary['PeakInboundQueue'] >= max_in and max(summary['PeakActive'], summary['PeakOutboundQueue'], summary['PeakInboundQueue']) <= header['QueueCapacity'],
            'Reported queue/context bounds differ')
    require(rss <= 512 * 1048576 and max(managed, heap) <= 256 * 1048576 and growth <= 128 * 1048576
            and max(maximum_gap, maximum_idle) <= 120 and summary['WithinBounds'] is True and summary['Violations'] == [], 'Resource/progress qualification failed')
    terminal = counts(final['Run']['Counts'])
    require((terminal['started'], terminal['successes'], terminal['pending']) == previous[14:17], 'Final ledger differs from last original observation')
    return dict(observations=number, maximumObservationGapSeconds=maximum_gap, peakWorkingSetBytes=rss,
                maximumObservedManagedBytes=managed, maximumObservedHeapBytes=heap, maximumResidentGrowthBytes=growth,
                processCpuSeconds=previous[7] - first[7], allocatedBytes=previous[6] - first[6],
                gen0Collections=previous[8] - first[8], gen1Collections=previous[9] - first[9], gen2Collections=previous[10] - first[10])


def journal_contract(frames, sdk, expected_source):
    header = frames[0]
    require(header['SchemaVersion'] == sdk['SchemaVersion'] == 1 and header['Policy'] == sdk['Policy'] == POLICY
            and header['SourceCommit'] == sdk['SourceCommit'] == expected_source, 'SDK policy/source/schema differs')
    require(header['Workload'] == capacity.WORKLOAD and header['Diagnostic'] == sdk['Diagnostic'], 'Workload/diagnostic declaration differs')
    require(header['ServerGc'] is True and header['ThreadPoolLimits']['MaximumWorkers'] == 4
            and header['ObservationSeconds'] == 1 and header['CheckpointSeconds'] == 60
            and header['HeartbeatIntervalSeconds'] == header['HeartbeatTimeoutSeconds'] == 2,
            'Executed supervision/worker profile differs')
    bounded_int(header['Concurrency'], 1024); bounded_int(header['QueueCapacity'], 65536)
    require(header['Concurrency'] > 0 and header['QueueCapacity'] >= header['Concurrency'], 'Invalid concurrency/queue declaration')
    validate_socket_profile(header['SocketProfile'])
    require(sdk['SocketProfile'] == header['SocketProfile'], 'Native profile changed between initial and final association')
    require(frames[-1]['Report'] == sdk, 'Final JSON differs from final journal frame')
    plan = fault_plan(header)
    run = sdk['Run']
    require(run['StartedAtUtc'] == header['StartedAtUtc'] == header['Sample']['StartedAtUtc'], 'Measurement origins differ')
    require(run['MeasuredSeconds'] == header['DurationSeconds'] and run['ElapsedSeconds'] >= run['MeasuredSeconds']
            and header['DurationSeconds'] <= (utc_time(run['FinishedAtUtc']) - utc_time(run['StartedAtUtc'])).total_seconds() <= header['DurationSeconds'] + 30,
            'Run did not complete its contiguous monotonic/wall-clock duration')
    samples = [header['Sample']] + [frame['Sample'] for frame in frames if frame['Kind'] == 'checkpoint']
    previous = -1
    for sample in samples:
        elapsed = finite(sample['ElapsedSeconds'])
        require(sample['StartedAtUtc'] == run['StartedAtUtc'] and elapsed > previous
                and (previous < 0 or elapsed - previous <= 120), 'Checkpoint origin/time/gap differs')
        counts(sample['Counts'])
        previous = elapsed
    require(run['ElapsedSeconds'] - previous <= 120, 'Final checkpoint gap exceeds bound')
    current, faults, observed_faults = None, {}, 0
    for frame in frames[1:-1]:
        kind = frame['Kind']
        if kind == 'checkpoint': continue
        if kind == 'transport-fault-observed':
            require(current is not None and frame['FaultId'] == current and frame['Previous'] == observed_faults
                    and frame['Current'] > observed_faults, 'Unclassified or non-monotonic transport fault')
            observed_faults = frame['Current']; continue
        require(kind in ('fault-begin', 'recovery-probes-start', 'fault-recovered', 'fault-resumed'), 'Unknown journal event')
        incident = frame['Incident']; identity = incident['Id']
        require(1 <= identity <= len(plan) and incident == plan[identity - 1]
                and re.fullmatch('[0-9a-f]{32}', frame['Nonce']), 'Fault declaration identity differs')
        event = faults.setdefault(identity, {})
        require(kind not in event, 'Duplicate fault stage')
        event[kind] = frame
        if kind == 'fault-begin':
            require(current is None and identity == len(faults) and frame['ElapsedSeconds'] >= incident['OffsetSeconds']
                    and counts(frame['Counts'])['pending'] > 0, 'Fault was not armed once under scheduled live traffic')
            current = identity
        else: require(current == identity and frame['Nonce'] == event['fault-begin']['Nonce'], 'Fault identity changed')
        if kind == 'fault-resumed': current = None
    require(current is None and len(faults) == len(plan) and observed_faults == sdk['Stack']['Faults'], 'Fault inventory/counter incomplete')
    nonces = set()
    for identity, event in faults.items():
        require(set(event) == {'fault-begin', 'recovery-probes-start', 'fault-recovered', 'fault-resumed'}, 'Fault recovery stages incomplete')
        ordered = [event[key] for key in ('fault-begin', 'recovery-probes-start', 'fault-recovered', 'fault-resumed')]
        times = [finite(item['ElapsedSeconds']) for item in ordered]
        require(times == sorted(times) and times[-1] - times[0] <= 90, 'Fault recovery exceeded budget')
        nonce = ordered[0]['Nonce']; require(nonce not in nonces, 'Fault nonce was reused'); nonces.add(nonce)
        probe, recovered = counts(ordered[1]['Counts']), counts(ordered[2]['Counts'])
        require(probe['pending'] == recovered['pending'] == 0 and recovered['started'] - probe['started'] == 5
                and recovered['successes'] - probe['successes'] == 5 and ordered[2]['ProbeSuccesses'] == 5,
                'Recovery lacks five fresh terminal successes after drain')
    terminal = counts(run['Counts'])
    measured = sum(vector(run['MeasuredByOperation'], 5)); late = bounded_int(run['LateSuccesses'])
    require(measured + late == terminal['successes'] and run['Latency']['Count'] == measured, 'Measured/drain/latency populations differ')
    quantiles = [finite(run['Latency'][key]) for key in ('P50Milliseconds', 'P95Milliseconds', 'P99Milliseconds', 'MaximumMilliseconds')]
    require(quantiles == sorted(quantiles) and run['Latency']['ResolutionMilliseconds'] == .1, 'Invalid latency quantiles/resolution')
    require(sdk['ExecutionPassed'] is True and run['Aborted'] is False and run['Errors'] == []
            and terminal['pending'] == terminal['unexpected'] == run['Counts']['AccountingViolations'] == 0
            and run['Counts']['Fatal'] is False and run['Counts']['ActiveFaultId'] is None
            and run['Counts']['RecoveredFaults'] == len(plan) and terminal['faultTerminals'] <= header['Concurrency'] * len(plan),
            'SDK execution/terminal accounting did not pass')
    stack = sdk['Stack']
    require(stack['Opened'] == stack['Closed'] and stack['ReadersHealthy'] is True
            and all(stack[key] == 0 for key in ('Pending', 'Active', 'Malformed', 'DroppedEvents', 'ForeignTransfers',
                                               'UnsolicitedInvokes', 'SccpReturns', 'OutboundQueue', 'InboundQueue')),
            'SDK final layer cleanup/protocol counters differ')
    return header, samples, faults, terminal, measured


def validate_socket_profile(profile):
    require(profile['Name'] == 'lan-bounded-v1' and profile['SendBufferBytes'] > 0 and profile['ReceiveBufferBytes'] > 0
            and [profile[key] for key in ('RtoInitialMilliseconds', 'RtoMinimumMilliseconds', 'RtoMaximumMilliseconds')] == [300, 200, 1000],
            'Observed bounded-LAN socket profile differs')


def relative_file(root, name):
    require(isinstance(name, str) and re.fullmatch(r'[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*', name)
            and all(part not in ('.', '..') for part in pathlib.PurePosixPath(name).parts), 'Unsafe artifact name')
    path = root.joinpath(*pathlib.PurePosixPath(name).parts)
    require(path.is_file() and not any(item.is_symlink() for item in (path, *path.parents) if item == root or root in item.parents)
            and root in path.resolve().parents, 'Artifact is missing, substituted or outside root')
    return path


def event_log(path):
    require(path.stat().st_size <= 1048576, 'Event log exceeds byte budget')
    rows = path.read_bytes().splitlines()
    require(len(rows) <= 256 and all(len(row) <= 4096 for row in rows), 'Event log exceeds record bounds')
    return [json_bytes(row) for row in rows]


def verify_wire(root, captures, hashes, header, faults, port):
    """Validate the baseline and each fresh recovery-probe window, not orphan prefixes of mid-traffic captures."""
    require(len(captures) == len(faults) + 1 and sum(relative_file(root, name).stat().st_size
            for item in captures for name in item['files']) <= 128 * 1024 * 1024, 'Capture inventory/byte budget differs')
    paired = 0
    require(captures[0]['incidentId'] is None and sum(item['incidentId'] is None for item in captures) == 1,
            'Exactly one baseline capture is required')
    for item in captures:
        identity = item['incidentId']
        require(item['exitCode'] == 0 and item['log'] in hashes and all(name in hashes for name in item['files']), 'Capture receipt/files missing')
        log = relative_file(root, item['log']).read_text()
        require(re.search(r"Packets received/dropped on interface '[^']+': [0-9]+/0 \(pcap:0/dumpcap:0/flushed:0/ps_ifdrop:0\)", log), 'Capture drop counters absent or nonzero')
        with tempfile.TemporaryDirectory(prefix='sigtran-endurance-wire-') as temporary:
            merged = pathlib.Path(temporary) / 'sample.pcapng'
            subprocess.run(['mergecap', '-w', str(merged), *[str(relative_file(root, name)) for name in item['files']]],
                           check=True, capture_output=True, timeout=60)
            args = ['tshark', '-n', '-r', str(merged), '--disable-protocol', 'm3ua', '-o', 'sctp.tsn_analysis:FALSE',
                    '-Y', 'sctp.data_payload_proto_id == 3', '-T', 'fields', '-E', 'header=y', '-e', 'frame.time_epoch']
            for field in capacity.wire.FIELDS: args += ['-e', field]
            rows = list(csv.DictReader(io.StringIO(subprocess.run(args, check=True, capture_output=True, text=True, timeout=120).stdout), delimiter='\t'))
        if identity is None:
            require(item == captures[0], 'Baseline capture order differs')
            local = header['LocalPort']; selected = rows
        else:
            require(identity in faults and sum(other['incidentId'] == identity for other in captures) == 1, 'Fault capture identity differs')
            event = faults[identity]; begin = event['recovery-probes-start']; end = event['fault-recovered']
            local = begin['LocalPort']
            low, high = utc_time(begin['ObservedAtUtc']).timestamp(), utc_time(end['ObservedAtUtc']).timestamp()
            subset = [row for row in rows if local in (int(row['sctp.srcport']), int(row['sctp.dstport']))]
            # Keep actual captured control messages for this association, then only fresh probe-window DATA.
            selected = [row for row in subset if int(row['sctp.data_sid'].split(',')[0], 0) == 0
                        or low <= float(row['frame.time_epoch']) <= high]
        selected = [row for row in selected if local in (int(row['sctp.srcport']), int(row['sctp.dstport']))]
        table = io.StringIO(); writer = csv.DictWriter(table, fieldnames=capacity.wire.FIELDS, delimiter='\t')
        writer.writeheader(); writer.writerows({key: row[key] for key in capacity.wire.FIELDS} for row in selected)
        result = capacity.verify_sample(table.getvalue(), port)
        if identity is not None:
            require(result['matchedPairs'] == 5 and result['pairedByOperation'] == [1] * 5 and result['trailingInvokes'] == 0,
                    'Fault capture does not independently match exactly five fresh probes')
        paired += result['matchedPairs']
    return dict(passed=True, captures=len(captures), independentlyMatchedPairs=paired,
                recoveryProbePairs=len(faults) * 5, boundedSamplesOnly=True)


def maintenance(audit, run):
    require(audit['explicitlyAuthorized'] is True and audit['restorationVerified'] is True
            and audit['unitFilesUnchanged'] is True and audit['fallbackTimerArmed'] is True
            and audit['fallbackTimerCancelled'] is True and audit['restorationErrors'] == [], 'Host restoration is unverified')
    units = audit['stoppedUnits']
    require(1 <= len(units) <= 8 and len(set(units)) == len(units), 'Invalid maintenance unit list')
    for unit in units:
        before, quiet, after = (audit[key][unit] for key in ('before', 'quiesced', 'after'))
        require(quiet['ActiveState'] == 'inactive' and (unit.endswith('.timer') or quiet.get('MainPID') == '0'), 'Host was not quiesced')
        for key in ('ActiveState', 'SubState', 'UnitFileState', 'fragmentSha256'):
            require(before[key] == after[key], 'Original unit state/definition not restored')
        require(quiet['fragmentSha256'] == before['fragmentSha256'] and quiet['UnitFileState'] == before['UnitFileState'], 'Unit definition/enablement changed')
    require(audit['restoreOrder'] == [unit for unit in reversed(units) if audit['before'][unit]['ActiveState'] == 'active'], 'Restore order starts an originally inactive unit')
    begin, quiet, restored = (utc_time(audit[key]) for key in ('startedAtUtc', 'quiescedAtUtc', 'restoredAtUtc'))
    require(begin <= quiet <= utc_time(run['StartedAtUtc']) < utc_time(run['FinishedAtUtc']) <= restored
            and (restored - begin).total_seconds() <= SECONDS + 3600, 'Maintenance window does not bracket the workload')
    return dict(explicitlyAuthorized=True, quiescenceVerified=True, restorationVerified=True,
                unitFilesUnchanged=True, fallbackTimerCancelled=True)


def supervisor_cleanup(receipt, inventory):
    """A successful child does not imply its privileged supervisor verified restoration."""
    require(receipt.get('verified') is True and inventory.get('supervisorCleanupVerified') is True,
            'Independent supervisor cleanup receipt missing or unsuccessful')
    utc_time(receipt['observedAtUtc'])


def verify(root, expected_source):
    root = pathlib.Path(root).resolve()
    require(re.fullmatch('[0-9a-f]{40}', expected_source), 'Expected source must be a full commit')
    hashes = read_json(root / 'sha256.json')
    required = {'source.tar', 'policy.json', 'inventory.json', 'sdk/sdk.json', 'sdk/checkpoints.jsonl',
                'sdk/resource-observations.bin', 'sdk.exit', 'controller.exit', 'controller-events.jsonl',
                'peer-result.json', 'peer-counters.bin', 'host-maintenance.json', 'network-cleanup.json',
                'network-events.jsonl', 'captures.json', 'peer', 'peer.log', 'libsctp.so.1', 'sdk-executable.dll', 'sdk-library.dll',
                'supervisor-cleanup.json'}
    require(required <= hashes.keys() and len(hashes) <= 512, 'Required raw evidence is incomplete')
    for name, expected in hashes.items():
        require(re.fullmatch('[0-9a-f]{64}', expected) and capacity.digest(relative_file(root, name)) == expected, 'Raw artifact hash mismatch: ' + name)
    policy = read_json(root / 'policy.json')
    require(policy['id'] == POLICY and policy['minimumDurationSeconds'] == SECONDS and policy['checkpointSeconds'] == 60
            and policy['minimumStableCompletedTps'] == 10000 and policy['maximumWorkingSetMiB'] == 512
            and policy['maximumManagedHeapMiB'] == 256 and policy['maximumResidentGrowthMiB'] == 128
            and policy['faultOffsetsSeconds'] == list(OFFSETS) and policy['automaticStablePromotion'] is False, 'Fixed policy was modified')
    with tarfile.open(root / 'source.tar') as archive:
        require(archive.pax_headers.get('comment') == expected_source, 'Archive source commit differs')
        require(archive.extractfile('eng/qualification/endurance-policy.json').read() == (root / 'policy.json').read_bytes(), 'Executed policy differs from source archive')
    inventory, sdk, peer = (read_json(root / name) for name in ('inventory.json', 'sdk/sdk.json', 'peer-result.json'))
    require(inventory['sourceCommit'] == peer['sourceCommit'] == expected_source and inventory['completed'] is True
            and inventory['cleanupVerified'] is True and inventory['collectionErrors'] == [], 'Controller/peer collection is incomplete')
    supervisor_cleanup(read_json(root / 'supervisor-cleanup.json'), inventory)
    for role in ('sdk', 'controller'):
        require((root / (role + '.exit')).read_bytes() == b'0\n' and inventory[role + 'ExitCode'] == 0, 'Actual child exit is not zero')
    require(peer['binarySha256'] == hashes['peer'] and peer['librarySha256'] == hashes['libsctp.so.1']
            and inventory['sdkBinarySha256'] == hashes['sdk-executable.dll'] and inventory['sdkLibrarySha256'] == hashes['sdk-library.dll'], 'Executed binary identity differs')
    before, after = inventory['before'], inventory['after']
    require(before['sdk']['machineHash'] != before['peer']['machineHash'], 'Host identities alias')
    for role in ('sdk', 'peer'):
        require(before[role]['machineHash'] == after[role]['machineHash'] and before[role]['bootId'] == after[role]['bootId']
                and before[role]['kafkaProcesses'] == after[role]['kafkaProcesses'] == [], 'Host identity/boot/maintenance changed')
    frames = chain(root / 'sdk/checkpoints.jsonl')
    header, samples, faults, terminal, measured = journal_contract(frames, sdk, expected_source)
    require((root / 'peer.log').stat().st_size <= 64 * 1048576, 'Peer log exceeds budget')
    profiles = []
    with (root / 'peer.log').open() as stream:
        for line in stream:
            if '"event":"socket-profile"' in line:
                entry = json_bytes(line); validate_socket_profile(entry['profile']); profiles.append(entry)
    require(len(profiles) >= 3 and len(profiles) <= 64 and len({p['processId'] for p in profiles}) == len(profiles),
            'Independent peer association profiles missing/repeated')
    require(header['ProcessId'] == inventory['sdkProcess']['pid'] and inventory['sdkProcess']['startTicks'] > 0,
            'Observed SDK process identity differs')
    resources = replay_resources(root / 'sdk/resource-observations.bin', header, samples, sdk)
    counter = peer_counters.read(root / 'peer-counters.bin', bounded_int(peer['forcedKills'], 32))
    require(counter == peer['counters'] and counter['validationFailures'] == 0
            and terminal['successes'] <= counter['received'] <= terminal['started']
            and terminal['successes'] <= counter['acceptedResponses'] + peer['forcedKills']
            and counter['acceptedResponses'] <= terminal['successes'] + terminal['faultTerminals'], 'Peer/SDK terminal bounds differ')
    restored = maintenance(read_json(root / 'host-maintenance.json'), sdk['Run'])
    cleanup = read_json(root / 'network-cleanup.json')
    network_ids = [identity for identity in faults if identity % 3 == 0]
    require(cleanup['rulesAbsent'] is True and cleanup['checkedIncidents'] == network_ids, 'Scoped network cleanup is unverified')
    events = event_log(root / 'controller-events.jsonl')
    network = event_log(root / 'network-events.jsonl')
    for identity, fault in faults.items():
        expected = fault['fault-begin']
        actual = [event for event in events if event.get('id') == identity and event.get('phase') == 'injected']
        released = [event for event in events if event.get('id') == identity and event.get('phase') == 'released']
        require(len(actual) == len(released) == 1 and actual[0]['kind'] == expected['Incident']['Kind']
                and actual[0]['nonce'] == released[0]['nonce'] == expected['Nonce'], 'Actual fault receipt differs from SDK intent')
        hold = (utc_time(released[0]['observedAtUtc']) - utc_time(actual[0]['observedAtUtc'])).total_seconds()
        require(expected['Incident']['HoldSeconds'] - .1 <= hold <= expected['Incident']['HoldSeconds'] + 15, 'Observed fault hold differs')
        if identity % 3:
            actions = [action for action in peer['actions'] if action['id'] == identity]
            require(len(actions) == 1 and actions[0]['kind'] == actual[0]['kind'] and actions[0]['nonce'] == expected['Nonce']
                    and actions[0]['processExited'] is True and (identity % 3 != 1 or actions[0]['exitCode'] == -9), 'Peer process fault is unproved')
        else:
            removed = [event for event in network if event.get('incident') == identity and event.get('event') == 'rollback']
            require(removed and any(event['droppedPackets'] > 0 and event['rulesAbsent'] is True for event in removed), 'Network partition did not prove actual packet drops and rollback')
    wire = verify_wire(root, read_json(root / 'captures.json'), hashes, header, faults, inventory['port'])
    seconds = sdk['Run']['MeasuredSeconds']; tps = measured / seconds
    qualified = seconds == SECONDS and not header['Diagnostic'] and tps >= 10000
    reasons = ([] if seconds == SECONDS else ['duration-below-72-hours'])
    if header['Diagnostic']: reasons.append('diagnostic-execution')
    if tps < 10000: reasons.append('mean-completed-tps-below-target')
    return dict(schemaVersion=1, evidenceKind='completed-map-endurance', testedSourceCommit=expected_source,
                policy=POLICY, executionValid=True, qualificationPassed=qualified, stablePromotion=False,
                diagnostic=header['Diagnostic'], reasons=reasons, durationSeconds=seconds,
                completedTransactions=measured, completedTps=tps, faultWindowTerminalOutcomes=terminal['faultTerminals'],
                lateSuccesses=sdk['Run']['LateSuccesses'], terminalAdmissions=terminal['started'], latency=sdk['Run']['Latency'],
                unexpectedFailures=terminal['unexpected'], unresolvedOperations=terminal['pending'],
                recoveredIncidents=len(faults), faultScenarios=len(KINDS), resources=resources,
                wire=wire, maintenance=restored, socketProfile=header['SocketProfile'],
                peerAssociationProfiles=[entry['profile'] for entry in profiles], rawArtifactSha256=hashes)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--source-commit', required=True)
    args = parser.parse_args()
    print(json.dumps(verify(args.root, args.source_commit), indent=2))
