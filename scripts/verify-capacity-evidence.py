#!/usr/bin/env python3
"""Re-decode a bounded wire sample and reconcile complete MAP throughput evidence."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import math
import pathlib
import re
import subprocess
import tarfile

SPEC = importlib.util.spec_from_file_location('wire', pathlib.Path(__file__).with_name('verify-reference-service-evidence.py'))
wire = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(wire)
WORKLOAD = 'itu-map-sms-five-operation-equal-mix-v1'
OPCODES = (45, 46, 44, 47, 64)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def integers(values, length):
    require(isinstance(values, list) and len(values) == length
            and all(type(n) is int and n >= 0 for n in values), 'Invalid bounded counter vector')
    return values


def read_json(path):
    """Reject ambiguous JSON and bound metadata before decoding untrusted evidence."""
    require(path.stat().st_size <= 2 * 1024 * 1024, 'Evidence metadata exceeds the bounded size')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, 'Duplicate JSON property')
            result[key] = value
        return result
    def invalid_constant(value):
        raise ValueError('Non-finite JSON number: ' + value)
    return json.loads(path.read_text(), object_pairs_hook=unique, parse_constant=invalid_constant)


def utc_time(value):
    """Read .NET's seven fractional digits on Python 3.10 without changing original evidence bytes."""
    match = re.fullmatch(r'(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d{1,7}))?(Z|[+-]\d{2}:\d{2})', value)
    require(match is not None, 'Invalid RFC3339 timestamp')
    # datetime has microsecond resolution; discard only the unrepresentable 100 ns digit.
    fraction = ((match[2] or '') + '000000')[:6]
    zone = '+00:00' if match[3] == 'Z' else match[3]
    result = datetime.datetime.fromisoformat(match[1] + '.' + fraction + zone)
    require(result.utcoffset() == datetime.timedelta(0), 'Evidence timestamp must be UTC')
    return result


def stage(value, seconds):
    """A completion outside measurement remains terminal, but never raises measured TPS."""
    require(type(seconds) is int and 1 <= seconds <= 1800, 'Invalid measured stage duration')
    counts = value['Counts']
    started = integers(counts['StartedByOperation'], 5)
    measured = integers(counts['MeasuredByOperation'], 5)
    late = integers(counts['LateByOperation'], 5)
    outcomes = integers(counts['Outcomes'], 8)
    windows = integers(counts['CompletedPerSecond'], seconds)
    require(sum(started) == sum(outcomes) and outcomes[0] == sum(measured) + sum(late)
            and sum(windows) == sum(measured), 'Unreconciled terminal accounting')
    require(all(a + b <= c for a, b, c in zip(measured, late, started)), 'Operation totals exceed admissions')
    require(max(started) - min(started) <= 1, 'Workload is not the equal five-operation admission mix')
    require(value['Aborted'] is False and value['MeasuredSeconds'] == seconds
            and value['ElapsedSeconds'] >= seconds, 'Incomplete monotonic measurement interval')
    latency = value['Latency']
    require(latency['Count'] == sum(measured) and latency['ResolutionMilliseconds'] == .1,
            'Latency population or resolution differs')
    quantiles = [latency[key] for key in ('P50Milliseconds', 'P95Milliseconds', 'P99Milliseconds', 'MaximumMilliseconds')]
    require(all(type(n) in (int, float) and math.isfinite(n) and n >= 0 for n in quantiles)
            and quantiles == sorted(quantiles), 'Invalid latency quantiles')
    resources = value['Resources']
    require(all(type(resources[key]) in (int, float) and math.isfinite(resources[key]) and resources[key] >= 0
                for key in ('AllocatedBytes', 'Gen0Collections', 'Gen1Collections', 'Gen2Collections',
                            'AverageCpuPercent', 'PeakCpuPercent', 'PeakWorkingSetMegabytes')),
            'Invalid process resource observations')
    require(type(resources['ServerGc']) is bool and type(resources['LogicalProcessors']) is int
            and resources['LogicalProcessors'] > 0, 'Missing observed runtime mode/resources')
    return {'started': started, 'measured': sum(measured), 'late': sum(late),
            'failed': sum(outcomes[1:]), 'windows': windows}


def verify_sample(table, port):
    """Correlate all complete pairs in a prefix capture; truncated trailing invokes are not called losses."""
    messages = wire.messages(table, port)
    activated = False
    pending = {}
    paired = [0] * 5
    for frame, request, stream, kind, subtype, parameters in messages:
        if (kind, subtype) != (1, 1):
            require(stream == 0, 'Control traffic is not on stream zero')
            if (kind, subtype) == (4, 3) and not request:
                require(parameters.get(6) == (100).to_bytes(4, 'big'), 'Wrong activation context')
                activated = True
            continue
        require(activated and stream == 1 and parameters.get(6) == (100).to_bytes(4, 'big'),
                'DATA preceded activation or used the wrong stream/context')
        protocol = parameters.get(0x210, b'')
        require(len(protocol) >= 12 and protocol[8:10] == b'\x03\x02', 'Wrong service/network indicator')
        pcs = int.from_bytes(protocol[:4], 'big'), int.from_bytes(protocol[4:8], 'big')
        require(pcs == ((1, 2) if request else (2, 1)), 'Wrong point-code pair')
        called, calling, transaction = wire.sccp(protocol[12:])
        package = wire.tlvs(transaction)
        require(len(package) == 1 and package[0][0] == (0x62 if request else 0x64), 'Wrong TCAP package')
        content = package[0][1]
        components = wire.tlvs(wire.one(content, 0x6c))
        require(len(components) == 1 and components[0][0] == (0xa1 if request else 0xa2), 'Wrong terminal component')
        fields = wire.tlvs(components[0][1])
        require(fields and fields[0][0] == 2 and len(fields[0][1]) == 1, 'Wrong invoke identity')
        invoke = fields[0][1]
        if request:
            require(len(fields) == 3 and fields[1][0] == 2 and len(fields[1][1]) == 1, 'Invalid Invoke fields')
            code = fields[1][1][0]
            require(code in OPCODES, 'Undeclared MAP operation')
            tid = wire.one(content, 0x48)
            require(tid and tid not in pending, 'Duplicate originating transaction')
            wire.dialogue(wire.one(content, 0x6b), 0x60, wire.CONTEXTS[code])
            pending[tid] = code, invoke, called, calling
        else:
            tid = wire.one(content, 0x49)
            require(tid in pending, 'Response without a captured Invoke')
            code, expected, remote, local = pending.pop(tid)
            require(invoke == expected and called == local and calling == remote, 'Invoke/address correlation differs')
            wire.dialogue(wire.one(content, 0x6b), 0x61, wire.CONTEXTS[code])
            if code == 45:
                result = wire.one(components[0][1], 0x30)
                require(wire.one(result, 2) == b'\x2d', 'SRI result opcode differs')
                info = wire.one(result, 0x30)
                require(wire.one(info, 4) == bytes.fromhex('34129078563412f0')
                        and wire.one(wire.one(info, 0xa0), 0x81) == bytes.fromhex('91442143'), 'Invalid synthetic SRI result')
            else:
                require(fields == [(2, invoke)], 'Unexpected non-SRI result payload')
            paired[OPCODES.index(code)] += 1
    require(activated and all(n > 0 for n in paired), 'Wire sample lacks complete pairs for all five operations')
    return {'pairedByOperation': paired, 'matchedPairs': sum(paired), 'trailingInvokes': len(pending),
            'sampleOnly': True, 'redecoded': True}


def evaluate(sdk, peer, inventory, sample, expected_source):
    """Keep protocol execution, numeric throughput and dedicated-host qualification distinct."""
    require(re.fullmatch('[0-9a-f]{40}', expected_source)
            and sdk['SourceCommit'] == inventory['sourceCommit'] == expected_source, 'Candidate source mismatch')
    require(sdk['SchemaVersion'] == peer['schemaVersion'] == inventory['schemaVersion'] == 1,
            'Unsupported evidence schema')
    require(sdk['Workload'] == peer['workload'] == WORKLOAD and sdk['TargetTps'] == 20000, 'Workload/target mismatch')
    seconds = inventory['durationSeconds']
    require(type(seconds) is int and 1 <= seconds <= 1800 and sdk['DurationSeconds'] == seconds, 'Duration mismatch')
    warm = stage(sdk['Warmup'], sdk['WarmupSeconds'])
    measured = stage(sdk['Measured'], seconds)
    totals = [a + b for a, b in zip(warm['started'], measured['started'])]
    require(peer['passed'] is True and peer['received'] == peer['responses'] == sum(totals)
            and integers(peer['byOperation'], 5) == totals, 'Peer/SDK operation totals differ')
    stack = sdk['Stack']
    require(all(stack[key] == sum(totals) for key in ('Sent', 'Received', 'Opened', 'Closed')), 'Stack totals differ')
    zero = ('Reconnects', 'Faults', 'Pending', 'Active', 'Malformed', 'DroppedEvents',
            'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns', 'OutboundQueue', 'InboundQueue')
    execution = warm['failed'] == measured['failed'] == 0 and all(stack[key] == 0 for key in zero) and stack['ReadersHealthy'] is True
    require(sdk['ExecutionPassed'] is execution, 'Execution flag disagrees with raw counters')
    require(sample['redecoded'] is True and len(sample['pairedByOperation']) == 5
            and min(sample['pairedByOperation']) > 0, 'No independently re-decoded wire sample')
    before, after = inventory['before'], inventory['after']
    require(before['sdk']['machineHash'] != before['peer']['machineHash'], 'Aliased machines')
    for name in ('sdk', 'peer'):
        require(before[name]['machineHash'] == after[name]['machineHash'], 'Host identity changed')
        require(before[name]['kafkaProcesses'] == after[name]['kafkaProcesses'], 'Kafka process identity changed during test')
    require(inventory['sdkExitCode'] == inventory['peerExitCode'] == inventory['captureExitCode'] == 0
            and inventory['completed'] is True and inventory.get('cleanupVerified') is True,
            'Child execution or owned-process cleanup did not complete cleanly')
    start = utc_time(sdk['StartedAtUtc'])
    finish = utc_time(sdk['FinishedAtUtc'])
    require(start.utcoffset() == finish.utcoffset() == datetime.timedelta(0)
            and seconds <= (finish - start).total_seconds() <= seconds + 60,
            'Wall-clock interval disagrees with monotonic duration')
    numeric = execution and seconds == 1800 and min(measured['windows']) >= 20000
    require(sdk['NumericTargetMet'] is numeric, 'Numeric target flag disagrees with per-second observations')
    dedicated = inventory['dedicatedDeclaration'] is True and not inventory['sharedPeer']
    dedicated = dedicated and all(not before[name]['kafkaProcesses'] for name in ('sdk', 'peer'))
    diagnostic = sdk['Diagnostic'] or inventory['diagnostic']
    qualified = numeric and dedicated and not diagnostic
    reasons = []
    if not execution: reasons.append('protocol-or-accounting-failure')
    if seconds != 1800: reasons.append('duration-below-1800-seconds')
    if min(measured['windows']) < 20000: reasons.append('completed-tps-below-target')
    if not dedicated: reasons.append('hosts-not-both-dedicated')
    if diagnostic: reasons.append('diagnostic-execution')
    return {'schemaVersion': 1, 'evidenceKind': 'completed-map-capacity', 'testedSourceCommit': expected_source,
            'executionValid': execution, 'numericTargetMet': numeric, 'qualificationPassed': qualified,
            'stablePromotion': False, 'reasons': reasons, 'durationSeconds': seconds,
            'completedTransactions': measured['measured'], 'lateSuccesses': measured['late'],
            'failedTransactions': measured['failed'], 'unresolvedTransactions': 0,
            'completedTps': measured['measured'] / seconds, 'minimumOneSecondTps': min(measured['windows']),
            'maximumOneSecondTps': max(measured['windows']), 'latency': sdk['Measured']['Latency'],
            'sdkResources': sdk['Measured']['Resources'], 'peerCpuSeconds': peer['cpuSeconds'],
            'peerPeakRssKiB': peer['peakRssKiB'], 'wireSample': sample,
            'topology': 'two-host-shared-peer' if inventory['sharedPeer'] else 'two-host-declared-dedicated'}


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def verify(root, expected_source):
    """Re-read original bytes and run TShark, rather than trusting a saved pass flag or TSV."""
    root = pathlib.Path(root).resolve()
    hashes = read_json(root / 'sha256.json')
    required = {'source.tar', 'inventory.json', 'result-sdk.json', 'peer.json', 'sample.pcap', 'capture.log', 'peer', 'libsctp.so.1'}
    require(required <= hashes.keys(), 'Missing required raw artifacts')
    for name, expected in hashes.items():
        path = root / name
        require(path.parent == root and path.is_file() and not path.is_symlink(), 'Invalid artifact path')
        require(digest(path) == expected, 'Artifact hash mismatch: ' + name)
    with tarfile.open(root / 'source.tar') as archive:
        require(archive.pax_headers.get('comment') == expected_source, 'Source archive commit differs')
    inventory = read_json(root / 'inventory.json')
    sdk = read_json(root / 'result-sdk.json')
    peer = read_json(root / 'peer.json')
    log = (root / 'capture.log').read_text()
    require(re.search(r"Packets received/dropped on interface '[^']+': [0-9]+/0 \(pcap:0/dumpcap:0/flushed:0/ps_ifdrop:0\)", log),
            'Missing capture counters or packet drops')
    port = inventory.get('port', 33061)
    args = ['tshark', '-n', '-r', str(root / 'sample.pcap'), '--disable-protocol', 'm3ua',
            '-Y', 'sctp.data_payload_proto_id == 3', '-T', 'fields', '-E', 'header=y']
    for field in wire.FIELDS:
        args += ['-e', field]
    table = subprocess.run(args, capture_output=True, text=True, check=True, timeout=60).stdout
    result = evaluate(sdk, peer, inventory, verify_sample(table, port), expected_source)
    result['hardware'] = {name: {'logicalProcessors': inventory['before'][name]['processors'],
                                'memoryMiB': inventory['before'][name]['memoryKiB']['MemTotal'] // 1024,
                                'kernel': inventory['before'][name]['kernel']}
                          for name in ('sdk', 'peer')}
    result['runtime'] = sdk['Runtime']
    if 'cpu-sample.json' in hashes:
        sample = read_json(root / 'cpu-sample.json')
        require(sample.get('sampleOnly') is True and .1 <= sample['sampleSeconds'] <= 6
                and all(type(sample[key]) in (int, float) and math.isfinite(sample[key]) and sample[key] >= 0
                        for key in ('sdkUserCpuSeconds', 'sdkKernelCpuSeconds', 'hostReportedStealPercent')),
                'Invalid exploratory CPU observation')
        when = utc_time(sample['capturedAtUtc'])
        require(utc_time(sdk['StartedAtUtc']) <= when
                <= utc_time(sdk['FinishedAtUtc']), 'CPU observation is outside the measured run')
        result['cpuSample'] = sample
    result['rawArtifactSha256'] = hashes
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--source-commit', required=True)
    args = parser.parse_args()
    print(json.dumps(verify(args.root, args.source_commit), indent=2))
