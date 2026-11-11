"""Synthetic contract fixtures only; these are never published as measured benchmark evidence."""
import copy
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location('capacity_evidence', pathlib.Path(__file__).resolve().parents[1] / 'verify-capacity-evidence.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def fixture():
    def stage(seconds):
        count = seconds * 20000
        return {'Counts': {'StartedByOperation': [count // 5] * 5, 'MeasuredByOperation': [count // 5] * 5,
                           'LateByOperation': [0] * 5, 'Outcomes': [count] + [0] * 7,
                           'CompletedPerSecond': [20000] * seconds}, 'MeasuredSeconds': seconds,
                'ElapsedSeconds': seconds + .01, 'Aborted': False,
                'Latency': {'Count': count, 'ResolutionMilliseconds': .1, 'P50Milliseconds': 1,
                            'P95Milliseconds': 2, 'P99Milliseconds': 3, 'MaximumMilliseconds': 4},
                'Resources': dict.fromkeys(['AllocatedBytes', 'Gen0Collections', 'Gen1Collections', 'Gen2Collections',
                                           'AverageCpuPercent', 'PeakCpuPercent', 'PeakWorkingSetMegabytes'], 1)}
    source = 'a' * 40
    total = 1810 * 20000
    stack = dict.fromkeys(['Reconnects', 'Faults', 'Pending', 'Active', 'Malformed', 'DroppedEvents',
                          'ForeignTransfers', 'UnsolicitedInvokes', 'SccpReturns', 'OutboundQueue', 'InboundQueue'], 0)
    stack.update(dict.fromkeys(['Sent', 'Received', 'Opened', 'Closed'], total), ReadersHealthy=True)
    sdk = dict(SchemaVersion=1, SourceCommit=source, Workload=module.WORKLOAD, DurationSeconds=1800,
               WarmupSeconds=10, Warmup=stage(10), Measured=stage(1800), Stack=stack, TargetTps=20000,
               ExecutionPassed=True, NumericTargetMet=True, Diagnostic=False)
    sdk.update(StartedAtUtc='2026-01-01T00:00:00+00:00', FinishedAtUtc='2026-01-01T00:30:00+00:00')
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
        windows[0], windows[1] = 10000, 30000
        values[0]['NumericTargetMet'] = False
        result = module.evaluate(*values)
        self.assertEqual(result['completedTps'], 20000)
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

    def test_short_wall_clock_and_unverified_cleanup_are_rejected(self):
        values = list(fixture())
        values[0]['FinishedAtUtc'] = '2026-01-01T00:00:10+00:00'
        with self.assertRaises(ValueError): module.evaluate(*values)
        values = list(fixture())
        values[2]['cleanupVerified'] = False
        with self.assertRaises(ValueError): module.evaluate(*values)
        values = list(fixture())
        values[0]['Measured']['Latency']['P99Milliseconds'] = float('nan')
        with self.assertRaises(ValueError): module.evaluate(*values)


if __name__ == '__main__':
    unittest.main()
