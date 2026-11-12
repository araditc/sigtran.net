"""Offline evidence contracts; synthetic fixtures never become measured benchmark evidence."""
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
        report = module.evaluate(*fixture())
        self.assertTrue(report['qualificationPassed'])
        self.assertEqual(report['secondsBelowTarget'], 0)
        self.assertEqual(report['worstSecondOffset'], 0)

    def test_shared_kafka_host_never_becomes_dedicated_evidence(self):
        values = list(fixture())
        values[2]['sharedPeer'] = True
        report = module.evaluate(*values)
        self.assertTrue(report['executionValid'])
        self.assertTrue(report['numericTargetMet'])
        self.assertFalse(report['qualificationPassed'])

    def test_average_does_not_hide_a_stalled_second(self):
        values = list(fixture())
        windows = values[0]['Measured']['Counts']['CompletedPerSecond']
        windows[0], windows[1] = 5000, 15000
        values[0]['NumericTargetMet'] = False
        result = module.evaluate(*values)
        self.assertEqual(result['completedTps'], 10000)
        self.assertFalse(result['qualificationPassed'])
        self.assertEqual(result['secondsBelowTarget'], 1)
        self.assertEqual(result['worstSecondOffset'], 0)

    def test_window_diagnostics_keep_exact_threshold_and_first_tied_minimum(self):
        values = list(fixture())
        windows = values[0]['Measured']['Counts']['CompletedPerSecond']
        windows[3], windows[4], windows[5] = 9999, 9999, 10002
        values[0]['NumericTargetMet'] = False
        result = module.evaluate(*values)
        self.assertEqual(result['secondsBelowTarget'], 2)
        self.assertEqual(result['worstSecondOffset'], 3)

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
        self.assertIn('Protocol/accounting execution: **PASS**', text)
        self.assertIn('Numeric capacity target: **MET**', text)
        self.assertIn('completed-map-10k-v1', text)
        self.assertIn('Seconds below target / measured seconds | 0 / 1800', text)
        self.assertIn('hosts-not-both-dedicated', text)
        self.assertIn('not proof', text)
        values[0]['Measured']['Counts']['CompletedPerSecond'][:2] = [9999, 10001]
        values[0]['NumericTargetMet'] = False
        text = summary.render(module.evaluate(*values))
        self.assertIn('Numeric capacity target: **NOT MET**', text)
        self.assertIn('Seconds below target / measured seconds | 1 / 1800', text)

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

    def test_runtime_diagnostics_bracket_actual_times_not_sample_indices(self):
        stage = fixture()[0]['Measured']
        stage['Counts']['CompletedPerSecond'] = [12000, 9000, 12000]
        def observation(t, pause, retransmits):
            return dict(ElapsedSeconds=t, GcPauseMilliseconds=pause, ProcessCpuSeconds=t,
                        Gen0=0, Gen1=0, Gen2=0, ThreadPoolThreads=4, ThreadPoolPending=1,
                        ThreadPoolCompleted=0, HostSctp={'SctpT3Retransmits': retransmits})
        stage['RuntimeObservations'] = [observation(0, 0, 0), observation(.9, 2, 0), observation(2.2, 5, 1), observation(3, 6, 1)]
        result = module.runtime_diagnostics(stage)
        bracket = result['lowestSubTargetWindows'][0]
        self.assertEqual(bracket['secondOffset'], 1)
        self.assertEqual(bracket['sampleStartSeconds'], .9)
        self.assertEqual(bracket['sampleEndSeconds'], 2.2)
        self.assertTrue(bracket['coversCompleteWindow'])
        self.assertEqual(bracket['gcPauseMilliseconds'], 3)
        self.assertEqual(bracket['namespaceSctpDelta']['SctpT3Retransmits'], 1)
        stage['RuntimeObservations'][2]['HostSctp'] = None
        self.assertNotIn('namespaceSctpDelta', module.runtime_diagnostics(stage)['lowestSubTargetWindows'][0])
        stage['RuntimeObservations'][2]['ElapsedSeconds'] = .8
        with self.assertRaisesRegex(ValueError, 'not increasing'): module.runtime_diagnostics(stage)

    def test_runtime_diagnostics_reject_invalid_or_unbounded_samples(self):
        stage = fixture()[0]['Measured']
        stage['RuntimeObservations'] = []
        with self.assertRaisesRegex(ValueError, 'missing runtime'): module.runtime_diagnostics(stage)
        stage['RuntimeObservations'] = [None] * 1815
        with self.assertRaisesRegex(ValueError, 'Unbounded'): module.runtime_diagnostics(stage)

    def test_worker_profile_must_match_actual_runtime_limits(self):
        limits = dict(MinimumWorkers=4, MaximumWorkers=4, MinimumIoThreads=1, MaximumIoThreads=1000)
        self.assertEqual(module.runtime_worker_limits({'ThreadPoolLimits': limits}, {'maximumWorkerThreads': 4}), limits)
        with self.assertRaisesRegex(ValueError, 'not observed'):
            module.runtime_worker_limits({'ThreadPoolLimits': limits}, {'maximumWorkerThreads': 8})
        with self.assertRaisesRegex(ValueError, 'lacks observed'):
            module.runtime_worker_limits({}, {'maximumWorkerThreads': 4})
        self.assertIsNone(module.runtime_worker_limits({}, {}))

    def test_worker_spin_experiment_is_explicit_and_not_an_inferred_runtime_state(self):
        value = {'WorkerSpinLimitEnvironment': '0'}
        self.assertEqual(module.runtime_tuning({'RuntimeTuning': value}, {'disableWorkerSpin': True}), value)
        with self.assertRaisesRegex(ValueError, 'missing its process'):
            module.runtime_tuning({}, {'disableWorkerSpin': True})
        self.assertIsNone(module.runtime_tuning({}, {}))

    def test_retained_numeric_pass_does_not_close_dedicated_qualification(self):
        # Metadata consistency only: CI cannot replace original private hash/wire verification.
        report = module.read_json(pathlib.Path(__file__).resolve().parents[2]
            / 'docs/evidence/COMPLETED_MAP_CAPACITY_10K_20261112.json')
        self.assertEqual(report['testedSourceCommit'], '324f1932cc7820335599c2e7b70e60ada32de4d9')
        self.assertEqual(report['capacityPolicy'], module.CAPACITY_POLICY)
        self.assertTrue(report['executionValid'] and report['numericTargetMet'])
        self.assertEqual(report['durationSeconds'], 1800)
        self.assertEqual(report['secondsBelowTarget'], 0)
        self.assertGreaterEqual(report['minimumOneSecondTps'], module.TARGET_TPS)
        self.assertFalse(report['qualificationPassed'] or report['stablePromotion'])
        self.assertIn('hosts-not-both-dedicated', report['reasons'])
        self.assertEqual(report['threadPoolLimits']['MaximumWorkers'], 4)
        self.assertEqual(report['peerNice'], 0)
        self.assertIsNone(report['runtimeTuning']['WorkerSpinLimitEnvironment'])


if __name__ == '__main__':
    unittest.main()
