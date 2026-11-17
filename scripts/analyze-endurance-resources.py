#!/usr/bin/env python3
"""Diagnose retained resource observations without repairing bytes, granting exceptions or qualifying a run."""
import argparse
import importlib.util
import json
import math
import pathlib
import re

spec = importlib.util.spec_from_file_location('resource_analysis_verifier', pathlib.Path(__file__).with_name('verify-endurance-evidence.py'))
verifier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verifier)


def scan(rows, header):
    """Separate missing managed readings from independently observable resource facts; never reconstruct them."""
    previous = None
    observations = negative_managed = nonmanaged_negative = rollbacks = protocol = bounds = 0
    peak_rss = peak_known_managed = peak_heap = maximum_gap = 0
    invalid = []
    for row in rows:
        verifier.require(len(row) >= 26 and math.isfinite(row[0]) and math.isfinite(row[7]), 'Invalid observation')
        observations += 1
        if row[3] < 0:
            negative_managed += 1
            if len(invalid) < 1024:
                invalid.append(dict(elapsedSeconds=row[0],managedBytes=row[3],lastGcHeapBytes=row[4],
                    allocatedBytesSincePrevious=None if previous is None else row[6] - previous[6],
                    previousManagedBytes=None if previous is None else previous[3]))
        else: peak_known_managed = max(peak_known_managed, row[3])
        nonmanaged_negative += sum(value < 0 for i, value in enumerate(row[:26]) if i != 3)
        protocol += sum(value != 0 for value in row[21:26])
        bounds += int(row[2] < row[1] or row[2] > 512 * 1048576 or row[3] > 256 * 1048576
                      or row[4] > 256 * 1048576 or row[16] > header['Concurrency']
                      or max(row[17:21]) > header['QueueCapacity'] or row[15] > row[14])
        if previous is not None:
            maximum_gap = max(maximum_gap, row[0] - previous[0])
            rollbacks += int(row[0] <= previous[0] or any(row[i] < previous[i] for i in (2, 6, 7, 8, 9, 10, 13, 14, 15)))
        peak_rss = max(peak_rss, row[2]); peak_heap = max(peak_heap, row[4]); previous = row
    verifier.require(observations > 0, 'Empty observation stream')
    return dict(observations=observations, negativeManagedObservations=negative_managed,
        invalidManagedDetails=invalid, invalidDetailListTruncated=negative_managed > len(invalid),
        otherNegativeFields=nonmanaged_negative, cumulativeOrTimeRollbackRows=rollbacks,
        unexpectedProtocolValues=protocol, observedBoundViolationRows=bounds,
        maximumObservationGapSeconds=maximum_gap, peakWorkingSetBytes=peak_rss,
        maximumKnownNonnegativeManagedBytes=peak_known_managed, maximumLastGcHeapBytes=peak_heap,
        historicalManagedReadingsComplete=negative_managed == 0,
        exactMissingManagedValuesReconstructable=False if negative_managed else None)


def analyze(root, source):
    """Bind analysis to hashed originals and retain the unchanged strict verifier outcome."""
    root = root.resolve()
    hashes = verifier.read_json(root / 'sha256.json')
    verifier.require(1 <= len(hashes) <= 512, 'Invalid artifact inventory')
    for name, digest in hashes.items():
        verifier.require(isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Invalid artifact digest')
        verifier.require(verifier.capacity.digest(verifier.relative_file(root, name)) == digest, 'Original hash differs: ' + name)
    sdk = verifier.read_json(root / 'sdk/sdk.json')
    frames = verifier.chain(root / 'sdk/checkpoints.jsonl')
    header, samples, _, _, _ = verifier.journal_contract(frames, sdk, source)
    path = root / 'sdk/resource-observations.bin'
    with path.open('rb') as stream:
        record = verifier.observation_schema(stream.read(8), header)
        verifier.require(8 < path.stat().st_size <= 64 * 1048576 and (path.stat().st_size - 8) % record.size == 0,
                         'Truncated or oversized observation stream')
        def rows():
            while data := stream.read(record.size): yield record.unpack(data)
        result = scan(rows(), header)
    try:
        verifier.replay_resources(path, header, samples, sdk)
        replay_passed, rejection = True, None
    except ValueError as error:
        replay_passed, rejection = False, str(error)
    for name, digest in hashes.items():
        verifier.require(verifier.capacity.digest(verifier.relative_file(root, name)) == digest, 'Original changed during analysis: ' + name)
    return dict(schemaVersion=1, evidenceKind='resource-observation-analysis', testedSourceCommit=source,
        qualificationPassed=False, stablePromotion=False, originalArtifactCount=len(hashes), originalHashesUnchanged=True,
        originalManifestSha256=verifier.capacity.digest(root / 'sha256.json'),
        strictResourceReplayPassed=replay_passed, strictReplayRejection=rejection,
        diagnosticFindings=result, missingValuesSubstituted=False, acceptanceExceptionGranted=False,
        conclusion='Historical missing managed readings are not reconstructed; RSS and last-GC heap are distinct measurements.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--output', required=True, type=pathlib.Path)
    args = parser.parse_args()
    if args.root.resolve() == args.output.resolve() or args.root.resolve() in args.output.resolve().parents:
        raise ValueError('Analysis output cannot enter the original artifact root')
    report = analyze(args.root, args.source_commit)
    with args.output.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')
    print(json.dumps(report, indent=2))
