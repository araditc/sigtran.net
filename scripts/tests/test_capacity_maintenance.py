"""Offline maintenance safety contracts; never connect to a host or stop a real unit."""
import copy
import importlib.util
import io
import json
import pathlib
import shlex
import sys
import tempfile
import types
import unittest
from unittest.mock import patch


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, pathlib.Path(__file__).resolve().parents[1] / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


maintenance = load('maintenance', 'run-capacity-maintenance.py')
verifier = load('maintenance_verifier', 'verify-capacity-evidence.py')
publisher = load('maintenance_publisher', 'publish-capacity-summary.py')


def audit_fixture():
    units = ['example-health.timer', 'example-health.service', 'example@broker.service', 'example@controller.service']
    before = {unit: dict(ActiveState='active', SubState='running', UnitFileState='enabled',
                         fragmentSha256='a' * 64, MainPID='100') for unit in units}
    before[units[0]].update(SubState='waiting', MainPID='0')
    before[units[1]].update(ActiveState='inactive', SubState='dead', MainPID='0')
    quiet = copy.deepcopy(before)
    for state in quiet.values():
        state.update(ActiveState='inactive', SubState='dead', MainPID='0')
    after = copy.deepcopy(before)
    after[units[2]]['MainPID'] = '200'
    audit = dict(schemaVersion=1, explicitlyAuthorized=True, restorationVerified=True,
                 unitFilesUnchanged=True, fallbackTimerArmed=True, fallbackTimerCancelled=True,
                 restorationErrors=[], stoppedUnits=units, before=before, quiesced=quiet, after=after,
                 restoreOrder=[units[3], units[2], units[0]],
                 startedAtUtc='2026-01-01T00:00:00Z', quiescedAtUtc='2026-01-01T00:00:05Z',
                 restoredAtUtc='2026-01-01T00:31:00Z')
    sdk = dict(StartedAtUtc='2026-01-01T00:00:10Z', FinishedAtUtc='2026-01-01T00:30:10Z')
    return audit, sdk


class MaintenanceTests(unittest.TestCase):
    def test_only_bounded_explicit_nonessential_units_are_accepted(self):
        units = audit_fixture()[0]['stoppedUnits']
        self.assertEqual(maintenance.validate_units(units), units)
        for invalid in ([], units * 2, ['../example.service'], ['ssh.service'], ['networking.service'],
                        ['systemd-journald.service'], ['example.service; reboot'], ['example.target']):
            with self.assertRaises(ValueError): maintenance.validate_units(invalid)

    def test_restore_order_excludes_originally_inactive_health_service(self):
        audit, _ = audit_fixture()
        self.assertEqual(maintenance.restoration_order(audit['stoppedUnits'], audit['before']), audit['restoreOrder'])
        audit['before'][audit['stoppedUnits'][0]]['ActiveState'] = 'activating'
        with self.assertRaises(ValueError): maintenance.restoration_order(audit['stoppedUnits'], audit['before'])

    def test_new_pids_are_expected_but_state_enablement_and_hash_changes_are_not(self):
        audit, _ = audit_fixture()
        self.assertTrue(maintenance.restored(audit['before'], audit['after']))
        for key in ('ActiveState', 'SubState', 'UnitFileState', 'fragmentSha256'):
            changed = copy.deepcopy(audit['after'])
            changed[audit['stoppedUnits'][0]][key] = 'changed'
            self.assertFalse(maintenance.restored(audit['before'], changed))

    def test_remote_fallback_attempts_every_unit_and_retains_failure(self):
        script = maintenance.restoration_script(audit_fixture()[0]['restoreOrder'])
        self.assertIn('|| status=1; done', script)
        self.assertIn('exit "$status"', script)

    def test_verified_maintenance_summary_contains_no_host_identity(self):
        audit, sdk = audit_fixture()
        audit['peerHost'] = '192.0.2.2'
        result = verifier.host_maintenance(audit, sdk)
        self.assertTrue(result['restorationVerified'])
        self.assertEqual(result['stoppedUnitCount'], 4)
        self.assertEqual(result['restoredActiveUnitCount'], 3)
        self.assertNotIn('peerHost', result)

    def test_systemd_timers_have_no_main_pid_but_services_must_report_zero(self):
        audit, sdk = audit_fixture()
        for states in (audit['before'], audit['quiesced'], audit['after']):
            del states[audit['stoppedUnits'][0]]['MainPID']
        self.assertTrue(verifier.host_maintenance(audit, sdk)['quiescenceVerified'])
        del audit['quiesced'][audit['stoppedUnits'][2]]['MainPID']
        with self.assertRaisesRegex(ValueError, 'not quiesced'):
            verifier.host_maintenance(audit, sdk)

    def test_unapproved_unrestored_or_uncancelled_maintenance_is_rejected(self):
        for key in ('explicitlyAuthorized', 'restorationVerified', 'unitFilesUnchanged',
                    'fallbackTimerArmed', 'fallbackTimerCancelled'):
            audit, sdk = audit_fixture()
            audit[key] = False
            with self.assertRaises(ValueError): verifier.host_maintenance(audit, sdk)
        audit, sdk = audit_fixture()
        audit['restorationErrors'] = ['example:TimeoutError']
        with self.assertRaises(ValueError): verifier.host_maintenance(audit, sdk)

    def test_nonquiesced_changed_units_wrong_order_or_timing_are_rejected(self):
        for mutation in (
            lambda a: a['quiesced'][a['stoppedUnits'][2]].update(MainPID='100'),
            lambda a: a['after'][a['stoppedUnits'][0]].update(UnitFileState='disabled'),
            lambda a: a['quiesced'][a['stoppedUnits'][0]].update(fragmentSha256='b' * 64),
            lambda a: a['restoreOrder'].append(a['stoppedUnits'][1]),
            lambda a: a.update(restoredAtUtc='2026-01-01T00:20:00Z'),
            lambda a: a.update(quiescedAtUtc='2026-01-01T00:10:00Z')):
            audit, sdk = audit_fixture()
            mutation(audit)
            with self.assertRaises(ValueError): verifier.host_maintenance(audit, sdk)

    def test_runner_exception_still_restores_services_and_cancels_remote_guard(self):
        audit, _ = audit_fixture()
        states = copy.deepcopy(audit['before'])
        operations = []

        class Output(io.BytesIO):
            def __init__(self, data=b''):
                super().__init__(data)
                self.channel = types.SimpleNamespace(recv_exit_status=lambda: 0)

        class Client:
            def load_host_keys(self, *_): pass
            def set_missing_host_key_policy(self, *_): pass
            def connect(self, *_, **__): pass
            def close(self): pass
            def get_transport(self):
                return types.SimpleNamespace(is_active=lambda: True, set_keepalive=lambda _: None)

            def exec_command(self, command, **_):
                args = shlex.split(command)[4:]
                operations.append(args)
                if args[0] == 'systemctl' and args[1] in ('stop', 'start'):
                    unit = args[-1]
                    if unit in states:
                        if args[1] == 'stop':
                            states[unit].update(ActiveState='inactive', SubState='dead', MainPID='0')
                        else:
                            states[unit] = copy.deepcopy(audit['before'][unit])
                value = json.dumps(states).encode() if args[0] == 'python3' else b''
                stdin = io.StringIO()
                stdin.channel = types.SimpleNamespace(shutdown_write=lambda: None)
                return stdin, Output(value), Output()

        fake = types.SimpleNamespace(SSHClient=Client, RejectPolicy=lambda: None)
        with tempfile.TemporaryDirectory() as directory:
            args = types.SimpleNamespace(allow_temporary_unit_stop=True, shared_peer=False,
                diagnostic=False, dedicated_hosts=True, duration=1800, maintenance_unit=audit['stoppedUnits'],
                restoration_guard_seconds=3300, output=str(pathlib.Path(directory) / 'new-run'),
                peer_host='192.0.2.2', known_hosts='unused', user='lab', peer_key=None)
            with patch.dict(sys.modules, {'paramiko': fake}), patch.dict('os.environ', {
                'SIGTRAN_MAINTENANCE_SUDO_PASSWORD': 'test-only', 'SIGTRAN_CAPACITY_PEER_PASSWORD': 'test-only'}), \
                    patch.object(maintenance.runner, 'run', side_effect=RuntimeError('synthetic controller failure')):
                with self.assertRaisesRegex(RuntimeError, 'synthetic controller failure'):
                    maintenance.run(args)
            saved = json.loads((pathlib.Path(args.output + '-maintenance') / 'host-maintenance.json').read_text())
        self.assertTrue(saved['restorationVerified'] and saved['fallbackTimerCancelled'])
        starts = [operation[-1] for operation in operations if operation[:2] == ['systemctl', 'start']]
        self.assertEqual(starts, audit['restoreOrder'])
        self.assertTrue(maintenance.restored(audit['before'], states))


if __name__ == '__main__':
    unittest.main()
