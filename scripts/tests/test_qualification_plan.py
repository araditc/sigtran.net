"""Offline launch checks: no SSH, packet capture, or fault injection."""
import importlib.util
import json
import pathlib
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("plan", pathlib.Path(__file__).resolve().parents[1] / "run-qualification-plan.py")
plan = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plan)


class LaunchTests(unittest.TestCase):
    def test_plan_only_is_explicit_and_secret_injection_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / "plan.json"
            values = dict.fromkeys(plan.ALLOWED, "safe")
            path.write_text(json.dumps(values))
            with patch.object(plan.subprocess, "run") as run:
                run.return_value.returncode = 0
                self.assertEqual(plan.launch(path, True), 0)
                self.assertEqual(run.call_args.kwargs["env"]["PLAN_ONLY"], "true")
                self.assertIsInstance(run.call_args.args[0], list)
            values["SIGNING_CERTIFICATE"] = "forbidden"
            path.write_text(json.dumps(values))
            with self.assertRaises(ValueError):
                plan.launch(path)


if __name__ == "__main__":
    unittest.main()
