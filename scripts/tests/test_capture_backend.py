"""Offline capture backend/preflight regressions; no privileges or traffic required."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import unittest
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("inventory", SCRIPTS / "collect-host-inventory.py")
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)


class CaptureBackendTests(unittest.TestCase):
    def test_inventory_records_capture_independently_of_sudo(self):
        def command(args, **kwargs):
            return subprocess.CompletedProcess(args, 1 if args[0] == "sudo" else 0,
                                               stdout="10.0.401" if args[0] == "dotnet" else "", stderr="")
        with patch.object(inventory.subprocess, "run", side_effect=command):
            result = inventory.collect("sdk", "sdk")
        self.assertTrue(result["captureAvailable"])
        self.assertFalse(result["sudoAvailable"])

    def test_invalid_backend_is_rejected_even_in_plan_only_mode(self):
        env = {**os.environ, "PLAN_ONLY": "true", "SIGTRAN_CAPTURE_BACKEND": "unrecognized"}
        result = subprocess.run(["bash", str(SCRIPTS / "run-multi-host-qualification.sh")],
                                env=env, capture_output=True, text=True, timeout=10)
        self.assertEqual(result.returncode, 2)

    def test_dumpcap_plan_keeps_host_and_gate_boundaries(self):
        env = {**os.environ, "PLAN_ONLY": "true", "SIGTRAN_CAPTURE_BACKEND": "dumpcap"}
        result = subprocess.run(["bash", str(SCRIPTS / "run-multi-host-qualification.sh")],
                                env=env, capture_output=True, text=True, timeout=10, check=True)
        plan = json.loads(result.stdout)
        self.assertEqual(plan["captureBackend"], "dumpcap")
        self.assertTrue(plan["requiresDistinctHosts"])
        self.assertFalse(plan["stableGatePromotion"])


if __name__ == "__main__":
    unittest.main()
