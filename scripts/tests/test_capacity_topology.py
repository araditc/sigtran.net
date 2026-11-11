"""Offline host admission tests; never open SSH or start load."""
import copy
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location('capacity', pathlib.Path(__file__).resolve().parents[1] / 'run-capacity-qualification.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TopologyTests(unittest.TestCase):
    def setUp(self):
        self.sdk = {'machineHash': 'a' * 64, 'hostname': 'sdk', 'kafkaProcesses': [],
                    'addresses': [{'addr_info': [{'local': '192.0.2.1'}]}]}
        self.peer = copy.deepcopy(self.sdk)
        self.peer.update(machineHash='b' * 64, hostname='peer',
                         addresses=[{'addr_info': [{'local': '192.0.2.2'}]}])

    def test_separate_hosts(self):
        self.assertTrue(module.validate_hosts(self.sdk, self.peer, '192.0.2.2', False))

    def test_kafka_requires_explicit_nonqualifying_mode(self):
        self.peer['kafkaProcesses'] = [{'pid': 1}]
        with self.assertRaises(ValueError):
            module.validate_hosts(self.sdk, self.peer, '192.0.2.2', False)
        self.assertFalse(module.validate_hosts(self.sdk, self.peer, '192.0.2.2', True))

    def test_alias_and_wrong_endpoint(self):
        self.peer['machineHash'] = self.sdk['machineHash']
        with self.assertRaises(ValueError):
            module.validate_hosts(self.sdk, self.peer, '192.0.2.2', False)
        self.peer['machineHash'] = 'b' * 64
        with self.assertRaises(ValueError):
            module.validate_hosts(self.sdk, self.peer, '192.0.2.3', False)


if __name__ == '__main__':
    unittest.main()
