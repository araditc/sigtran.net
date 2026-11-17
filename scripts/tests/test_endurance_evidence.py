"""Synthetic corruption tests only; no fixture here is published as measured endurance evidence."""
import base64
import copy
import hashlib
import importlib.util
import json
import pathlib
import struct
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('endurance_evidence', pathlib.Path(__file__).resolve().parents[1] / 'verify-endurance-evidence.py')
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)


def ledger(total=5):
    return dict(StartedByOperation=[total // 5] * 5, SuccessfulByOperation=[total // 5] * 5,
                Outcomes=[total] + [0] * 7, FaultWindowOutcomes=[0] * 8, Pending=0)


def frame(sequence, previous, payload):
    body = json.dumps(payload).encode()
    digest = hashlib.sha256(bytes.fromhex(previous) + body).hexdigest()
    return dict(Sequence=sequence, PreviousSha256=previous, PayloadBase64=base64.b64encode(body).decode(), Sha256=digest)


class EvidenceTests(unittest.TestCase):
    def test_restart_protection_requires_exact_scope_and_verified_removal(self):
        value = dict(schemaVersion=1, runId='fixture', role='sdk', unit='sigtran-endurance-sdk-fixture.service',
                     installedAtUtc='2026-01-01T00:00:00+00:00', expiresAtUtc='2026-01-04T00:30:00+00:00',
                     lastCheckedAtUtc='2026-01-04T00:01:00+00:00', automaticRebootEnabled=False,
                     unrelatedRestartPolicyChanged=False, available=True, protected=True,
                     effectiveSelectionVerified=True, removed=True, policyAbsent=True)
        value['snippetSha256'] = hashlib.sha256(evidence.service_policy.content('fixture', 'sdk', value['expiresAtUtc'])).hexdigest()
        run = dict(StartedAtUtc='2026-01-01T00:00:01+00:00', FinishedAtUtc='2026-01-04T00:00:01+00:00')
        evidence.restart_policy(value, 'sdk', run)
        for key in ('policyAbsent', 'removed', 'effectiveSelectionVerified', 'protected'):
            with self.assertRaises(ValueError): evidence.restart_policy(dict(value, **{key: False}), 'sdk', run)
        with self.assertRaises(ValueError): evidence.restart_policy(dict(value, unrelatedRestartPolicyChanged=True), 'sdk', run)

    def test_successful_child_cannot_substitute_for_supervisor_cleanup(self):
        good = dict(verified=True, observedAtUtc='2026-01-01T00:00:00+00:00')
        evidence.supervisor_cleanup(good, dict(supervisorCleanupVerified=True))
        for receipt, inventory in ((good, {}), (dict(good, verified=False), dict(supervisorCleanupVerified=True)),
                                   (good, dict(supervisorCleanupVerified=False))):
            with self.assertRaises(ValueError): evidence.supervisor_cleanup(receipt, inventory)

    def test_journal_requires_exact_payload_order_and_final_receipt(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'journal'
            first = frame(1, '0' * 64, {'Kind': 'start'})
            last = frame(2, first['Sha256'], {'Kind': 'finish'})
            def save(rows): path.write_bytes(b''.join(json.dumps(row).encode() + b'\n' for row in rows))
            save([first, last]); self.assertEqual(len(evidence.chain(path)), 2)
            for rows in ([first], [last, first], [first, first, last]):
                save(rows)
                with self.assertRaises(ValueError): evidence.chain(path)
            corrupt = copy.deepcopy(last); corrupt['PayloadBase64'] = base64.b64encode(b'{"Kind":"failed"}').decode()
            save([first, corrupt])
            with self.assertRaises(ValueError): evidence.chain(path)

    def test_diagnostic_never_relabels_short_duration_as_full_run(self):
        header = dict(DurationSeconds=360, Diagnostic=True)
        header['FaultPlan'] = [dict(Id=i + 1, Kind=kind, OffsetSeconds=offset, HoldSeconds=10)
                               for i, (kind, offset) in enumerate(zip(evidence.KINDS, (30, 120, 210)))]
        self.assertEqual(len(evidence.fault_plan(header)), 3)
        header['Diagnostic'] = False
        with self.assertRaises(ValueError): evidence.fault_plan(header)
        header['DurationSeconds'] = 259200
        with self.assertRaises(ValueError): evidence.fault_plan(header)

    def test_64bit_terminal_counts_and_protocol_failures_remain_exact(self):
        value = ledger(5 * (1 << 32))
        self.assertEqual(evidence.counts(value)['successes'], 5 * (1 << 32))
        value['Outcomes'][2] = 1; value['StartedByOperation'][0] += 1
        self.assertEqual(evidence.counts(value)['unexpected'], 1)
        value['FaultWindowOutcomes'][0] = 1
        with self.assertRaises(ValueError): evidence.counts(value)

    def test_duplicate_properties_negative_values_and_path_escape_are_rejected(self):
        for data in (b'{"id":1,"id":2}', b'{"id":NaN}'):
            with self.assertRaises(ValueError): evidence.json_bytes(data)
        with self.assertRaises(ValueError): evidence.bounded_int(True)
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory).resolve()
            for name in ('../outside', '/absolute', 'folder/../other', 'C:/path'):
                with self.assertRaises(ValueError): evidence.relative_file(root, name)

    def test_partial_observations_and_counter_rollback_fail(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'observations'
            path.write_bytes(b'SGENOBS1' + b'x')
            with self.assertRaises(ValueError): evidence.replay_resources(path, {}, [], {})
            row = [0.0, 100, 100, 50, 40, 0, 1000, 0.0] + [0] * 18
            row[14:17] = [5, 5, 0]
            second = row.copy(); second[0] = 1; second[15] = 4
            path.write_bytes(b'SGENOBS1' + struct.pack('<d6qd18q', *row) + struct.pack('<d6qd18q', *second))
            with self.assertRaisesRegex(ValueError, 'decreased'):
                evidence.replay_resources(path, dict(DurationSeconds=360, Diagnostic=True, Concurrency=256, QueueCapacity=8192), [], {})

    def test_event_logs_are_bounded_before_decoding(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'events'
            path.write_bytes(b'{}\n' * 257)
            with self.assertRaises(ValueError): evidence.event_log(path)

    def test_negative_observation_identifies_the_field_without_regrading_it(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'observations'
            row = [32430.261368219, 100, 100, -710056, 40, 0, 1000, 1.0] + [0] * 18
            original = b'SGENOBS1' + evidence.OBSERVATION.pack(*row)
            path.write_bytes(original)
            with self.assertRaisesRegex(ValueError, r'Negative resource counter: ManagedBytes=-710056 at elapsedSeconds=32430'):
                evidence.replay_resources(path, dict(DurationSeconds=259200, Diagnostic=False), [], {})
            self.assertEqual(path.read_bytes(), original)
            self.assertEqual(len(evidence.OBSERVATION_KEYS), len(row))

    def test_audited_retries_cannot_skip_valid_reads_clamp_or_exceed_three_attempts(self):
        self.assertEqual(evidence.managed_reading(50, (-710056, 50, 0, 2))['First'], -710056)
        self.assertEqual(evidence.managed_reading(50, (-1, -2, 50, 3))['Attempts'], 3)
        for value, raw in ((50, (100, 50, 0, 2)), (0, (-1, -2, -3, 3)), (50, (-1, 50, 0, 4)),
                           (50, (50, 1, 0, 1)), (50, (-1, 49, 0, 2))):
            with self.assertRaises(ValueError): evidence.managed_reading(value, raw)

    def test_new_format_requires_explicit_sampling_identity_and_retains_legacy_rejection(self):
        self.assertIs(evidence.observation_schema(b'SGENOBS1', {}), evidence.OBSERVATION)
        with self.assertRaises(ValueError): evidence.observation_schema(b'SGENOBS2', {})
        header = dict(ObservationFormat='SGENOBS2', ManagedMemorySampling=dict(
            Method='bounded-non-forcing-read-v1', MaximumAttempts=3, RetryDelayMilliseconds=1))
        self.assertIs(evidence.observation_schema(b'SGENOBS2', header), evidence.OBSERVATION_WITH_READS)
        with self.assertRaises(ValueError): evidence.observation_schema(b'SGENOBS1', header)
        header['ManagedMemorySampling']['RetryDelayMilliseconds'] = True
        with self.assertRaises(ValueError): evidence.observation_schema(b'SGENOBS2', header)

    def test_full_resource_replay_checks_all_retry_values_and_aggregate_counts(self):
        header = dict(DurationSeconds=360, Diagnostic=True, Concurrency=256, QueueCapacity=8192,
                      ObservationFormat='SGENOBS2', ManagedMemorySampling=dict(
                          Method='bounded-non-forcing-read-v1', MaximumAttempts=3, RetryDelayMilliseconds=1))
        rows, samples = [], []
        for i, elapsed in enumerate((0., 60., 61.)):
            row = [elapsed, 100, 100, 50, 40, 0, 1000 + i, float(i)] + [0] * 18
            row[14:17] = [5 * (i + 1), 5 * (i + 1), 0]
            raw = [-710056, 50, 0, 2] if i == 1 else [50, 0, 0, 1]
            resources = dict(zip(evidence.RESOURCE_KEYS, row[1:14]))
            resources['ManagedMemory'] = evidence.managed_reading(row[3], raw)
            samples.append(dict(ElapsedSeconds=elapsed, Resources=resources, Counts=ledger(5 * (i + 1))))
            rows.append(row + raw)
        summary = dict(Observations=3, LastElapsedSeconds=61., PeakWorkingSetBytes=100,
            MaximumObservedManagedBytes=50, MaximumObservedHeapBytes=40, BaselineWorkingSetBytes=100,
            BaselineAfterSeconds=60., MaximumResidentGrowthBytes=0, MaximumObservationGapSeconds=60.,
            MaximumNoProgressSeconds=0., PeakPending=0, PeakActive=0, PeakOutboundQueue=0, PeakInboundQueue=0,
            WithinBounds=True, Violations=[], RetriedManagedObservations=1, NegativeManagedReadings=1)
        final = dict(ResourceSummary=summary, Run=dict(Counts=ledger(15)))
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'observations'
            path.write_bytes(b'SGENOBS2' + b''.join(evidence.OBSERVATION_WITH_READS.pack(*row) for row in rows))
            replay = evidence.replay_resources(path, header, samples, final)
            self.assertEqual(replay['retriedManagedObservations'], 1)
            summary['NegativeManagedReadings'] = 0
            with self.assertRaisesRegex(ValueError, 'aggregates differ'):
                evidence.replay_resources(path, header, samples, final)
            summary['NegativeManagedReadings'] = 1
            samples[0]['Resources']['ManagedMemory']['Second'] = False
            with self.assertRaisesRegex(ValueError, 'Checkpoint managed-memory attempts differ'):
                evidence.replay_resources(path, header, samples, final)


if __name__ == '__main__':
    unittest.main()
