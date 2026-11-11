"""Synthetic contract fixtures only; these are never published as measured benchmark evidence."""
import copy
import importlib.util
import pathlib
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('capacity_evidence', pathlib.Path(__file__).resolve().parents[1] / 'verify-capacity-evidence.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def fixture():
    def stage(seconds):
        count = seconds * 10000
        return {'Counts': {'StartedByOperation': [count // 5] * 5, 'MeasuredByOperation': [count // 5] * 5,
                           'LateByOperation': [0] * 5, 'Outcomes': [count] + [0] * 7,
                           'CompletedPerSecond': [10000] * seconds}, 'MeasuredSeconds': seconds,
                'ElapsedSeconds': seconds + .01, 'Aborted': False,
                'Latency': {'Count': count, 'ResolutionMilliseconds': .1, 'P50Milliseconds': 1,
                            'P95Milliseconds': 2, 'P99Milliseconds': 3, 'MaximumMilliseconds': 4},
                'Resources': dict.fromkeys(['AllocatedBytes', 'Gen0Collections', 'Gen1Collections', 'Gen2Collections',
                                           'AverageCpuPercent', 'PeakCpuPercent', 'PeakWorkingSetMegabytes'], 1)}
    source = 'a' * 40
    total = 1810 * 10000
    stack = dict.fromkeys(['Reconnects', 'Faults', 'Pending', 'Active', 'Malformed', 'DroppedEvents',
                          'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns', 'OutboundQueue', 'InboundQueue'], 0)
    stack.update(dict.fromkeys(['Sent', 'Received', 'Opened', 'Closed'], total), ReadersHealthy=True)
    sdk = dict(SchemaVersion=1, SourceCommit=source, Workload=module.WORKLOAD, DurationSeconds=1800,
               WarmupSeconds=10, Warmup=stage(10), Measured=stage(1800), Stack=stack, TargetTps=10000,
               CapacityPolicy=module.CAPACITY_POLICY,
               ExecutionPassed=True, NumericTargetMet=True, Diagnostic=False)
    sdk.update(StartedAtUtc='2026-01-01T00:00:00+00:00', FinishedAtUtc='2026-01-01T00:30:00+00:00')
    for item in (sdk['Warmup'], sdk['Measured']):
        item['Resources'].update(ServerGc=True, LogicalProcessors=4)
    peer = dict(schemaVersion=1, workload=module.WORKLOAD, passed=True, received=total, responses=total,
                byOperation=[total // 5] * 5, cpuSeconds=1, peakRssKiB=1)
    before = {'sdk': {'machineHash': '1' * 64, 'kafkaProcesses': []},
              'peer': {'machineHash': '2' * 64, 'kafkaProcesses': []}}
    inventory = dict(schemaVersion=1, sourceCommit=source, durationSeconds=1800, before=before,
                     after=copy.deepcopy(before), sdkExitCode=0, peerExitCode=0, captureExitCode=0,
                     completed=True, cleanupVerified=True, dedicatedDeclaration=True, sharedPeer=False, diagnostic=False)
    return sdk, peer, inventory, {'redecoded': True, 'pairedByOperation': [1] * 5}, source


class EvidenceTests(unittest.TestCase):
    def test_contract_accepts_only_complete_declared_target(self):
        self.assertTrue(module.evaluate(*fixture())['qualificationPassed'])

    def test_shared_kafka_host_never_becomes_dedicated_evidence(self):
        values = list(fixture())
        values[2]['sharedPeer'] = True
        self.assertFalse(module.evaluate(*values)['qualificationPassed'])

    def test_average_does_not_hide_a_stalled_second(self):
        values = list(fixture())
        windows = values[0]['Measured']['Counts']['CompletedPerSecond']
        windows[0], windows[1] = 5000, 15000
        values[0]['NumericTargetMet'] = False
        result = module.evaluate(*values)
        self.assertEqual(result['completedTps'], 10000)
        self.assertFalse(result['qualificationPassed'])

    def test_missing_and_tampered_completions_are_rejected(self):
        values = list(fixture())
        values[0]['Measured']['Counts']['Outcomes'][0] -= 1
        with self.assertRaises(ValueError): module.evaluate(*values)
        values = list(fixture())
        values[1]['byOperation'][0] -= 1
        with self.assertRaises(ValueError): module.evaluate(*values)

    def test_candidate_mismatch_and_nonfinite_latency_are_rejected(self):
        values = list(fixture())
        values[0]['SourceCommit'] = 'b' * 40
        with self.assertRaises(ValueError): module.evaluate(*values)
        values = list(fixture())
        values[0]['Measured']['Latency']['P99Milliseconds'] = float('nan')
        with self.assertRaises(ValueError): module.evaluate(*values)

    def test_short_wall_clock_and_unverified_cleanup_are_rejected(self):
        values = list(fixture())
        values[0]['FinishedAtUtc'] = '2026-01-01T00:00:10+00:00'
        with self.assertRaises(ValueError): module.evaluate(*values)
        values = list(fixture())
        values[2]['cleanupVerified'] = False
        with self.assertRaises(ValueError): module.evaluate(*values)

    def test_wire_sample_rejects_wrong_stream_and_control_context(self):
        with patch.object(module.wire, 'messages', return_value=[(1, False, 0, 4, 3, {6: (101).to_bytes(4, 'big')})]):
            with self.assertRaisesRegex(ValueError, 'activation context'):
                module.verify_sample('unused synthetic input', 33061)
        messages = [(1, False, 0, 4, 3, {6: (100).to_bytes(4, 'big')}),
                    (2, True, 0, 1, 1, {6: (100).to_bytes(4, 'big')})]
        with patch.object(module.wire, 'messages', return_value=messages):
            with self.assertRaisesRegex(ValueError, 'wrong stream/context'):
                module.verify_sample('unused synthetic input', 33061)

    def test_public_report_does_not_hide_a_shared_host_or_failure(self):
        summary_spec = importlib.util.spec_from_file_location('capacity_summary', pathlib.Path(__file__).resolve().parents[1] / 'publish-capacity-summary.py')
        summary = importlib.util.module_from_spec(summary_spec)
        summary_spec.loader.exec_module(summary)
        values = list(fixture())
        values[2]['sharedPeer'] = True
        report = module.evaluate(*values)
        text = summary.render(report)
        self.assertIn('NOT QUALIFIED', text)
        self.assertIn('hosts-not-both-dedicated', text)
        self.assertIn('not proof', text)

    def test_duplicate_properties_and_nonfinite_json_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'metadata.json'
            path.write_text('{"passed":false,"passed":true}')
            with self.assertRaisesRegex(ValueError, 'Duplicate JSON'):
                module.read_json(path)
            path.write_text('{"tps":NaN}')
            with self.assertRaisesRegex(ValueError, 'Non-finite JSON'):
                module.read_json(path)

    def test_dotnet_seven_digit_timestamps_are_portable_and_require_utc(self):
        parsed = module.utc_time('2026-11-11T08:11:33.8478911+00:00')
        self.assertEqual(parsed.microsecond, 847891)
        self.assertEqual(module.utc_time('2026-11-11T08:11:33Z').microsecond, 0)
        with self.assertRaises(ValueError): module.utc_time('2026-11-11T08:11:33')
        with self.assertRaises(ValueError): module.utc_time('2026-11-11T08:11:33+03:30')

    def test_prior_20k_target_is_not_silently_regraded_as_current_evidence(self):
        values = list(fixture())
        values[0]['TargetTps'] = 20000
        values[0].pop('CapacityPolicy')
        with self.assertRaisesRegex(ValueError, 'policy mismatch'): module.evaluate(*values)
        policy = module.read_json(pathlib.Path(__file__).resolve().parents[2] / 'eng/qualification/capacity-policy.json')
        self.assertEqual(policy['minimumCompletedTps'], module.TARGET_TPS)
        self.assertEqual(policy['id'], module.CAPACITY_POLICY)


if __name__ == '__main__':
    unittest.main()
