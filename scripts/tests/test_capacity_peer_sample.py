"""Owned-peer procfs parsing tests without opening any VM connection."""
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location('peer_sample', pathlib.Path(__file__).resolve().parents[1] / 'sample-capacity-peer.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PeerSampleTests(unittest.TestCase):
    def test_stat_uses_field_numbers_after_comm_parenthesis(self):
        fields = ['0'] * 50
        fields[0], fields[11], fields[12], fields[16], fields[19], fields[9] = 'R', '10', '20', '10', '99', '3'
        item = module.parse_stat('123 (name (with) spaces) ' + ' '.join(fields))
        self.assertEqual(item, dict(state='R', userTicks=10, kernelTicks=20, nice=10, startTicks=99, majorFaults=3))


if __name__ == '__main__':
    unittest.main()
