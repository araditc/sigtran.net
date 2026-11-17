"""Synthetic resource-analysis fixtures must never become qualification evidence."""
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location('resource_analysis_test', pathlib.Path(__file__).resolve().parents[1] / 'analyze-endurance-resources.py')
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)


def row(elapsed=0., managed=50):
    return [elapsed, 100, 100, managed, 40, 0, 1000, 1.] + [0] * 18


class ResourceAnalysisTests(unittest.TestCase):
    def test_positive_rss_and_heap_cannot_reconstruct_missing_managed_values(self):
        result = analysis.scan([row(), row(1., -710056), row(2.)], dict(Concurrency=256, QueueCapacity=8192))
        self.assertEqual(result['negativeManagedObservations'], 1)
        self.assertFalse(result['historicalManagedReadingsComplete'])
        self.assertFalse(result['exactMissingManagedValuesReconstructable'])
        self.assertEqual(result['invalidManagedDetails'][0]['managedBytes'], -710056)
        self.assertEqual(result['peakWorkingSetBytes'], 100)

    def test_other_corruption_is_not_classified_as_a_managed_sampling_anomaly(self):
        bad = row(1.); bad[6] = -1; bad[21] = 1; bad[19] = 9000
        result = analysis.scan([row(), bad], dict(Concurrency=256, QueueCapacity=8192))
        self.assertEqual(result['otherNegativeFields'], 1)
        self.assertEqual(result['cumulativeOrTimeRollbackRows'], 1)
        self.assertEqual(result['unexpectedProtocolValues'], 1)
        self.assertEqual(result['observedBoundViolationRows'], 1)

    def test_nonfinite_and_empty_observations_are_rejected(self):
        with self.assertRaises(ValueError): analysis.scan([], dict(Concurrency=256, QueueCapacity=8192))
        with self.assertRaises(ValueError): analysis.scan([row(float('nan'))], dict(Concurrency=256, QueueCapacity=8192))


if __name__ == '__main__': unittest.main()
