"""Safety contracts for a short local SDK diagnostic; no service or traffic is launched here."""
import copy
import importlib.util
import pathlib
import subprocess
import unittest
from unittest.mock import Mock
from unittest.mock import MagicMock

path = pathlib.Path(__file__).resolve().parents[1] / 'run-local-resource-diagnostic.py'
spec = importlib.util.spec_from_file_location('local_resource_driver_test', path)
driver = importlib.util.module_from_spec(spec); spec.loader.exec_module(driver)


class LocalResourceTests(unittest.TestCase):
    def test_duration_source_and_input_paths_are_fenced(self):
        value = dict(sourceCommit='a' * 40, durationSeconds=3600, port=33462, inputs={'sdk-build/app.dll': 'b' * 64})
        self.assertEqual(driver.validate(value)['durationSeconds'], 3600)
        for key, invalid in [('durationSeconds', 3601), ('durationSeconds', True), ('durationSeconds', 259200),
                             ('port', 80), ('sourceCommit', 'main'), ('inputs', {'../outside': 'b' * 64})]:
            bad = copy.deepcopy(value); bad[key] = invalid
            with self.assertRaises(ValueError): driver.validate(bad)

    def test_cleanup_only_signals_its_owned_child(self):
        child = Mock(); child.poll.return_value = None; child.wait.return_value = 0
        self.assertEqual(driver.stop(child), 0)
        child.send_signal.assert_called_once_with(driver.signal.SIGINT)
        child.kill.assert_not_called()
        child.wait.side_effect = [subprocess.TimeoutExpired('owned', 10), -9]
        self.assertEqual(driver.stop(child), -9)
        child.kill.assert_called_once()

    def test_driver_has_no_remote_control_firewall_or_kafka_commands(self):
        text = path.read_text()
        self.assertIn("'127.0.0.1'", text)
        for command in ("'ssh'", "'sudo'", "'iptables'", "'systemctl'", "'kafka'"):
            self.assertNotIn(command, text)

    def test_launcher_status_is_explicit_and_does_not_change_gc_memory_limits(self):
        launcher = path.with_name('start-local-resource-diagnostic.py').read_text()
        self.assertIn("if args.status:", launcher)
        self.assertIn("'--property=Restart=no'", launcher)
        self.assertIn("'--property=KillMode=control-group'", launcher)
        self.assertNotIn('MemoryMax=', launcher)
        self.assertNotIn('SIGTRAN_LAB_PEER_PASSWORD', launcher)

    def test_sftp_upload_requires_write_and_exclusive_creation(self):
        spec = importlib.util.spec_from_file_location('local_resource_launcher_test', path.with_name('start-local-resource-diagnostic.py'))
        launcher = importlib.util.module_from_spec(spec); spec.loader.exec_module(launcher)
        sftp = MagicMock()
        launcher.upload_new(sftp, '/private/new.bin', b'input')
        sftp.open.assert_called_once_with('/private/new.bin', 'wx')
        sftp.open.return_value.__enter__.return_value.write.assert_called_once_with(b'input')


if __name__ == '__main__': unittest.main()
