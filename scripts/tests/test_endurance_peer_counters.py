"""Synthetic counter ABI contracts are tests, never measured signalling evidence."""
import importlib.util
import pathlib
import struct
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('endurance_counters', pathlib.Path(__file__).resolve().parents[1] / 'endurance-peer-counters.py')
counters = importlib.util.module_from_spec(spec)
spec.loader.exec_module(counters)


class PeerCounterTests(unittest.TestCase):
    def test_native_c_counter_values_remain_64_bit(self):
        large = (1 << 32) + 5
        result = counters.decode(struct.pack('<8Q', large, large, large, 0, 0, 0, 0, 0))
        self.assertEqual(result['received'], large)
        self.assertEqual(result['byOperation'], [large, 0, 0, 0, 0])

    def test_incomplete_publication_requires_a_bounded_declared_cut(self):
        data = struct.pack('<8Q', 11, 10, 10, 0, 0, 0, 0, 0)
        with self.assertRaises(ValueError): counters.decode(data)
        self.assertEqual(counters.decode(data, 1)['maximumUncertainPublicationTail'], 1)
        for invalid in (b'', data + b'\0', struct.pack('<8Q', 10, 11, 10, 0, 0, 0, 0, 0)):
            with self.assertRaises(ValueError): counters.decode(invalid)
        with self.assertRaises(ValueError): counters.decode(data, 33)

    def test_reader_rejects_wrong_size_and_preserves_validation_failures(self):
        with tempfile.TemporaryDirectory() as root:
            path = pathlib.Path(root) / 'counts.bin'
            path.write_bytes(struct.pack('<8Q', 10, 10, 10, 0, 0, 0, 0, 2))
            self.assertEqual(counters.read(path)['validationFailures'], 2)
            path.write_bytes(b'wrong')
            with self.assertRaises(ValueError): counters.read(path)


if __name__ == '__main__':
    unittest.main()
