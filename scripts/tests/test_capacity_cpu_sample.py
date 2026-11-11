"""Test CPU attribution arithmetic without reading another process or waiting."""
import copy
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location('cpu_sample', pathlib.Path(__file__).resolve().parents[1] / 'sample-capacity-process.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CpuTests(unittest.TestCase):
    def setUp(self):
        self.before = {'startTicks': 1, 'userTicks': 0, 'kernelTicks': 0, 'hostCpuTicks': [0] * 10}
        self.after = {'startTicks': 1, 'userTicks': 274, 'kernelTicks': 397,
                      'hostCpuTicks': [274, 0, 397, 129, 0, 0, 0, 0, 0, 0]}

    def test_multi_core_process_cpu_is_not_capped_to_wall_seconds(self):
        result = module.summarize(self.before, self.after, 2, 100)
        self.assertEqual(result['sdkUserCpuSeconds'], 2.74)
        self.assertEqual(result['sdkKernelCpuSeconds'], 3.97)
        self.assertEqual(result['hostReportedStealPercent'], 0)

    def test_guest_ticks_are_not_double_counted_as_steal(self):
        self.after['hostCpuTicks'] = [40, 0, 0, 0, 0, 0, 0, 60, 30, 0]
        self.assertEqual(module.summarize(self.before, self.after, 2, 100)['hostReportedStealPercent'], 60)

    def test_reused_pid_and_regressing_counters_are_rejected(self):
        self.after['startTicks'] = 2
        with self.assertRaises(ValueError): module.summarize(self.before, self.after, 2, 100)
        self.after['startTicks'] = 1
        self.after['userTicks'] = -1
        with self.assertRaises(ValueError): module.summarize(self.before, self.after, 2, 100)


if __name__ == '__main__':
    unittest.main()
