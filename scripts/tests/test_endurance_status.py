"""Discovery is read-only and never mistakes a management outage or old diagnostic for a new load job."""
import base64
import hashlib
import importlib.util
import json
import pathlib
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS = pathlib.Path(__file__).resolve().parents[1]


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, SCRIPTS / file)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


status = load('endurance_status_test', 'read-endurance-status.py')
inspection = load('endurance_inspect_test', 'inspect-endurance-lab.py')


def candidate(identity, date, state='inactive', diagnostic=False):
    return dict(runId=identity, measurement=dict(startedAtUtc=date, diagnostic=diagnostic, durationSeconds=360 if diagnostic else 259200),
                unit=dict(ActiveState=state))


class StatusTests(unittest.TestCase):
    def test_service_query_never_starts_or_stops_the_measured_unit(self):
        with patch.object(status.subprocess, 'run') as run:
            run.return_value.stdout = 'LoadState=loaded\nActiveState=active\nResult=success\n'
            self.assertEqual(status.unit_state('example-run')['ActiveState'], 'active')
            args = run.call_args.args[0]
            self.assertEqual(args[:2], ['systemctl', 'show'])
            self.assertNotIn('start', args)
            self.assertNotIn('restart', args)
            self.assertNotIn('stop', args)

    def test_active_full_run_wins_over_later_diagnostics_and_failed_history(self):
        active = candidate('active', '2026-01-02T00:00:00+00:00', 'active')
        values = [candidate('old', '2026-01-01T00:00:00+00:00', 'failed'), active,
                  candidate('diagnostic', '2026-01-03T00:00:00+00:00', 'active', True)]
        self.assertIs(status.choose(values), active)
        with self.assertRaises(ValueError): status.choose(values + [candidate('other', '2026-01-04T00:00:00+00:00', 'active')])

    def test_latest_terminal_run_is_found_without_restarting_anything(self):
        old = candidate('old', '2026-01-01T00:00:00+00:00', 'failed')
        final = candidate('final', '2026-01-02T00:00:00+00:00')
        self.assertIs(status.choose([old, final]), final)

    def test_unreachable_desktop_does_not_claim_failure_or_restart_workload(self):
        result = inspection.unreachable(TimeoutError())
        self.assertEqual(result['executionState'], 'unknown')
        self.assertFalse(result['workloadRestarted'])
        self.assertFalse(result['qualificationPassed'])

    def test_retained_start_binds_source_and_original_process(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory); target = root / 'work/sdk/checkpoints.jsonl'; target.parent.mkdir(parents=True)
            header = dict(Kind='start', SourceCommit='a' * 40, DurationSeconds=259200, Diagnostic=False,
                          ProcessId=123, Runtime='10.0.12', StartedAtUtc='2026-01-01T00:00:00.1234567+00:00')
            body = json.dumps(header).encode()
            frame = dict(Sequence=1, PreviousSha256='0' * 64, PayloadBase64=base64.b64encode(body).decode(),
                         Sha256=hashlib.sha256(bytes(32) + body).hexdigest())
            target.write_text(json.dumps(frame) + '\n')
            value = status.metadata(root)
            self.assertEqual(value['sdkProcessId'], 123)
            self.assertEqual(value['expectedMeasuredFinishUtc'], '2026-01-04T00:00:00.123456+00:00')
            frame['Sha256'] = 'f' * 64; target.write_text(json.dumps(frame) + '\n')
            with self.assertRaises(ValueError): status.metadata(root)


if __name__ == '__main__': unittest.main()
