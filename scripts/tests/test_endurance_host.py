"""Safety contracts for protected endurance supervision; no service, SSH or load is started."""
import datetime
import importlib.util
import pathlib
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('endurance_host_test', pathlib.Path(__file__).resolve().parents[1] / 'endurance-host.py')
host = importlib.util.module_from_spec(spec); spec.loader.exec_module(host)
driver = host.load_module('endurance_driver_test', 'run-endurance-lab.py')


def fixture():
    return dict(runId='example-run', sourceCommit='a' * 40, sdkIp='192.168.100.28', peerIp='192.168.100.43',
                port=33072, durationSeconds=259200, diagnostic=False, explicitMaintenanceApproval=True,
                expiresAtUtc=(datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=73)).isoformat(),
                peerSha256='b' * 64, librarySha256='c' * 64, peerCpu=3)


class HostTests(unittest.TestCase):
    def test_runtime_path_is_discovered_before_maintenance(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'dotnet'; path.touch()
            with patch.object(driver.shutil, 'which', return_value=str(path)):
                self.assertEqual(driver.executable('dotnet'), str(path.resolve()))
            with patch.object(driver.shutil, 'which', return_value=None):
                with self.assertRaises(ValueError): driver.executable('dotnet')

    def test_short_runs_approval_and_paths_fail_closed(self):
        host.validate(fixture())
        for key, value in [('runId', '../root'), ('durationSeconds', 360), ('explicitMaintenanceApproval', False),
                           ('sdkIp', '192.168.100.43'), ('port', 22), ('command', 'shell')]:
            plan = fixture(); plan[key] = value
            with self.assertRaises(ValueError): host.validate(plan)

    def test_diagnostic_flag_cannot_enable_unbounded_duration(self):
        plan = fixture(); plan.update(diagnostic=True, durationSeconds=360)
        host.validate(plan)
        plan['durationSeconds'] = 259200
        with self.assertRaises(ValueError): host.validate(plan)

    def test_restoration_preserves_original_inactive_services_and_definitions(self):
        before = {unit: dict(ActiveState='active', SubState='running', UnitFileState='enabled', fragmentSha256='a') for unit in host.UNITS}
        after = {unit: state.copy() for unit, state in before.items()}
        self.assertTrue(host.restored(before, after))
        after[host.UNITS[0]]['fragmentSha256'] = 'b'
        self.assertFalse(host.restored(before, after))

    def test_duplicate_remote_stop_does_not_kill_replacement_process(self):
        peer = object.__new__(host.Peer)
        peer.value = dict(diagnostic=True); peer.cache = {('stop', 1, 'a' * 32): dict(exitCode=-9)}
        self.assertEqual(peer.request(dict(action='stop', id=1, nonce='a' * 32)), dict(exitCode=-9))
        with self.assertRaises(ValueError): peer.request(dict(action='stop', id=3, nonce='a' * 32))
        with self.assertRaises(ValueError): peer.request(dict(action='arbitrary-shell'))

    def test_audit_atomic_replace_retains_complete_json(self):
        if not hasattr(__import__('os'), 'O_DIRECTORY'): self.skipTest('Linux directory fsync')
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'audit.json'
            host.atomic(path, dict(before=True)); host.atomic(path, dict(after=True))
            self.assertEqual(__import__('json').loads(path.read_text()), dict(after=True))
            self.assertFalse(path.with_name('audit.json.pending').exists())


if __name__ == '__main__': unittest.main()
