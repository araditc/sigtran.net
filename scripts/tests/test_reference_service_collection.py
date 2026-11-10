"""Exercise source and artifact integrity before re-reading reference-service PCAPs."""

import hashlib
import importlib.util
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest


spec = importlib.util.spec_from_file_location("collector",
    Path(__file__).resolve().parents[1] / "collect-reference-service-evidence.py")
collector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(collector)
SOURCE = "a" * 40


class ReferenceCollectionTests(unittest.TestCase):
    def test_matching_source_and_digests_pass(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            digests = {}
            for name in ("traffic.pcap", "http-result.json"):
                (root / name).write_bytes(name.encode())
                digests[name] = hashlib.sha256(name.encode()).hexdigest()
            report = {"result": "pass", "profile": "reference", "sourceCommit": SOURCE, "privateArtifactSha256": digests}
            collector.validate_manifest(root, report, "reference", SOURCE)
            with self.assertRaisesRegex(ValueError, "selected source commit"):
                collector.validate_manifest(root, report, "reference", "b" * 40)
            (root / "traffic.pcap").write_bytes(b"altered")
            with self.assertRaisesRegex(ValueError, "artifact digest mismatch"):
                collector.validate_manifest(root, report, "reference", SOURCE)

    def test_partial_and_aliased_evidence_fail(self):
        root = Path(".")
        with self.assertRaisesRegex(ValueError, "all three profiles"):
            collector.collect({"reference": root}, SOURCE)
        with self.assertRaisesRegex(ValueError, "separate run directories"):
            collector.collect(dict.fromkeys(collector.PROFILES, root), SOURCE)


if __name__ == "__main__":
    unittest.main()
