"""Synthetic diagnostic replay tests; fixtures are never measured lab evidence."""
import copy
import importlib.util
import json
import pathlib
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('local_resource_evidence_test', pathlib.Path(__file__).resolve().parents[1] / 'verify-local-resource-evidence.py')
verifier = importlib.util.module_from_spec(spec); spec.loader.exec_module(verifier)


class ContextTests(unittest.TestCase):
    def setUp(self):
        self.header = dict(StartedAtUtc='2026-01-01T00:00:00+00:00', ObservationFormat='SGENOBS2',
            ManagedMemorySampling=dict(Method='bounded-non-forcing-read-v1', MaximumAttempts=3, RetryDelayMilliseconds=1))
        self.row = [0., 100, 100, 50, 40, 0, 1000, 0.] + [0] * 18
        self.row[14:17] = [5, 5, 0]
        resources = dict(zip(verifier.evidence.RESOURCE_KEYS, self.row[1:14]))
        resources.update(ManagedMemory=dict(First=-1, Second=50, Third=0, Attempts=2),
            GcContext=dict(BeforeIndex=7, AfterIndex=6, Gen0Before=10, Gen0After=11))
        counts = dict(StartedByOperation=[1] * 5, SuccessfulByOperation=[1] * 5,
            Outcomes=[5] + [0] * 7, FaultWindowOutcomes=[0] * 8, Pending=0)
        keys = ('Active', 'Pending', 'OutboundQueue', 'InboundQueue', 'Malformed', 'DroppedEvents',
                'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns')
        self.value = dict(Sample=dict(StartedAtUtc=self.header['StartedAtUtc'], ElapsedSeconds=0.,
            Counts=counts, Resources=resources), Stack=dict.fromkeys(keys, 0))

    def replay(self, value, suffix=b'', prefix=None):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory); text, binary = root / 'contexts.jsonl', root / 'rows.bin'
            text.write_bytes(json.dumps(value).encode() + b'\n')
            data = verifier.evidence.OBSERVATION_WITH_READS.pack(*(self.row + [-1, 50, 0, 2]))
            binary.write_bytes(b'SGENOBS2' + (data if prefix is None else prefix) + suffix)
            return verifier.contexts(text, binary, self.header)

    def test_each_raw_attempt_is_compared_and_gc_index_is_not_assumed_monotonic(self):
        result = self.replay(self.value)
        self.assertEqual(result['observations'], 1)
        self.assertEqual(result['changedIndexBrackets'], 1)
        self.assertEqual(result['changedGen0Brackets'], 1)
        self.assertFalse(result['atomicSnapshot'])
        self.assertFalse(result['rootCauseProven'])

    def test_json_cannot_replace_binary_readings_or_skip_an_earlier_valid_read(self):
        for key, value in [('ManagedBytes', 51), ('ManagedMemory', dict(First=50, Second=50, Third=0, Attempts=2)),
                           ('ManagedMemory', dict(First=-1, Second=50, Third=False, Attempts=2))]:
            bad = copy.deepcopy(self.value); bad['Sample']['Resources'][key] = value
            with self.assertRaises(ValueError): self.replay(bad)

    def test_missing_rows_and_truncated_binary_are_rejected(self):
        with self.assertRaises(ValueError): self.replay(self.value, suffix=b'x')
        with self.assertRaises(ValueError): self.replay(self.value, prefix=b'x')

    def test_context_boolean_negative_and_count_rollback_are_rejected(self):
        for key, value in [('BeforeIndex', True), ('AfterIndex', -1), ('Gen0After', 9)]:
            bad = copy.deepcopy(self.value); bad['Sample']['Resources']['GcContext'][key] = value
            with self.assertRaises(ValueError): self.replay(bad)


class TraceWindowTests(unittest.TestCase):
    def test_actual_pid_exits_and_schedule_are_required_without_whole_run_coverage(self):
        item = dict(name='trace-001', file='trace-001.nettrace', reason='scheduled', sdkPid=42, exitCode=0,
            requestedSeconds=5, offsetFromSdkSpawnSeconds=2.1,
            startedAtUtc='2026-01-01T00:00:02+00:00', finishedAtUtc='2026-01-01T00:00:08+00:00')
        exits = dict(sdk=0, peer=0, capture=0, **{'trace-001': 0})
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory); (root / item['file']).write_bytes(b'synthetic-not-a-trace')
            hashes = {item['file']: 'a' * 64, 'trace-001.log': 'b' * 64}
            def replay(entry): return verifier.trace_inventory(root, dict(windows=[entry], negativeObservationAlerts=[]), 10, 42, exits, hashes)
            result = replay(item)
            self.assertFalse(result['fullIntervalCoverage'])
            self.assertFalse(result['independentlyDecoded'])
            for key, value in [('sdkPid', 43), ('exitCode', 1), ('offsetFromSdkSpawnSeconds', 33), ('requestedSeconds', 3600)]:
                with self.assertRaises(ValueError): replay(dict(item, **{key: value}))
            exits['unowned'] = 0
            with self.assertRaises(ValueError): replay(item)


if __name__ == '__main__': unittest.main()
