#!/usr/bin/env python3
"""Offline regressions: synthetic data + command stubs; no SSH/systemd/traffic."""
import importlib.util
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("evidence", SCRIPTS / "persist-qualification-evidence.py")
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)

STUB = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
root = Path(os.environ["STUB_ROOT"])
name = Path(sys.argv[0]).name
args = sys.argv[1:]
with (root / "calls.jsonl").open("a") as stream:
    stream.write(json.dumps([name, *args]) + "\n")
if name == "systemd-run":
    if os.environ.get("ARM_FAIL") == "1":
        raise SystemExit(1)
    (root / "armed").touch()
    payload = args[args.index("/bin/bash"):]
    (root / "payload.json").write_text(json.dumps(payload))
elif name == "systemctl":
    if args[0] == "start":
        count_file = root / "starts"
        count = int(count_file.read_text()) if count_file.exists() else 0
        count += 1
        count_file.write_text(str(count))
        if count <= int(os.environ.get("START_FAILURES", "0")):
            raise SystemExit(1)
        if os.environ.get("VERIFY_FAIL") != "1":
            (root / "active").touch()
    elif args[0] == "is-active":
        raise SystemExit(0 if (root / "active").exists() else 3)
    elif args[0] == "stop":
        if args[1].endswith(".timer"):
            (root / "armed").unlink(missing_ok=True)
        else:
            (root / "active").unlink(missing_ok=True)
'''


class PeerRollbackTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.bin = self.root / "bin"
        self.bin.mkdir()
        for name in ("systemctl", "systemd-run", "sleep"):
            path = self.bin / name
            path.write_text(STUB)
            path.chmod(0o700)
        self.env = dict(os.environ, STUB_ROOT=str(self.root),
                        PATH=str(self.bin) + os.pathsep + os.environ["PATH"])
        self.unit = "sigtran-peer-rollback-synthetic-123-2"
        self.service = "synthetic-peer.service"

    def run_helper(self, mode, **env):
        return subprocess.run(["bash", str(SCRIPTS / "peer-outage-recovery.sh"),
                               mode, self.unit, self.service, "70"],
                              env=dict(self.env, **env), capture_output=True, timeout=5)

    def calls(self):
        return [json.loads(line) for line in (self.root / "calls.jsonl").read_text().splitlines()]

    def test_peer_rollback_is_armed_before_stop(self):
        self.assertEqual(self.run_helper("outage").returncode, 0)
        calls = self.calls()
        self.assertEqual(calls[0][0], "systemd-run")
        self.assertEqual(calls[1], ["systemctl", "stop", self.service])
        self.assertTrue((self.root / "armed").exists())

    def test_failed_arm_never_stops_peer(self):
        self.assertNotEqual(self.run_helper("outage", ARM_FAIL="1").returncode, 0)
        self.assertFalse(any(call[:2] == ["systemctl", "stop"] for call in self.calls()))

    def test_peer_timer_retries_without_sdk_or_ssh(self):
        self.assertEqual(self.run_helper("outage").returncode, 0)
        payload = json.loads((self.root / "payload.json").read_text())
        # Execute the saved peer-manager payload after the initiating process exited.
        run = subprocess.run(payload, env=dict(self.env, START_FAILURES="1"), timeout=5)
        self.assertEqual(run.returncode, 0)
        self.assertEqual((self.root / "starts").read_text(), "2")
        self.assertTrue((self.root / "active").exists())

    def test_normal_recovery_disarms_only_after_verification(self):
        self.assertEqual(self.run_helper("outage").returncode, 0)
        self.assertEqual(self.run_helper("recover").returncode, 0)
        self.assertEqual(self.calls()[-3:], [
            ["systemctl", "start", self.service],
            ["systemctl", "is-active", "--quiet", self.service],
            ["systemctl", "stop", self.unit + ".timer", self.unit + ".service"]])
        self.assertFalse((self.root / "armed").exists())

    def test_failed_start_or_verification_keeps_rollback(self):
        for env in ({"START_FAILURES": "99"}, {"VERIFY_FAIL": "1"}):
            with self.subTest(env=env):
                self.assertEqual(self.run_helper("outage").returncode, 0)
                self.assertNotEqual(self.run_helper("recover", **env).returncode, 0)
                self.assertTrue((self.root / "armed").exists())

    def test_invalid_service_is_rejected_before_commands(self):
        self.service = "peer.service'; touch /tmp/invalid; '"
        self.assertEqual(self.run_helper("outage").returncode, 2)
        self.assertFalse((self.root / "calls.jsonl").exists())


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.source = self.root / "scratch"
        self.source.mkdir(mode=0o700)
        (self.source / "raw").mkdir()
        (self.source / "safe").mkdir()
        (self.source / "raw/trace.txt").write_text("synthetic raw data")
        (self.source / "safe/summary.json").write_text('{"passed":false}')
        self.destination = self.root / "retained/attempt-2"

    def test_verified_persistence_removes_scratch_and_restricts_copy(self):
        expected = evidence.inventory(self.source)
        evidence.persist(self.source, self.destination)
        self.assertFalse(self.source.exists())
        manifest = json.loads((self.destination / "protected-evidence.sha256.json").read_text())
        self.assertEqual(manifest, expected)
        for path in [self.destination, *self.destination.rglob("*")]:
            self.assertEqual(stat.S_IMODE(path.stat().st_mode), 0o700 if path.is_dir() else 0o600)

    def test_corrupt_copy_preserves_private_original(self):
        original = shutil.copytree
        def corrupt(source, target, **kwargs):
            with patch.object(evidence.shutil, "copytree", original):
                result = original(source, target, **kwargs)
            (Path(target) / "raw/trace.txt").write_text("corruption")
            return result
        with patch.object(evidence.shutil, "copytree", side_effect=corrupt):
            with self.assertRaises(ValueError):
                evidence.persist(self.source, self.destination)
        self.assertTrue(self.source.exists())
        self.assertEqual(stat.S_IMODE(self.source.stat().st_mode), 0o700)
        self.assertFalse(self.destination.exists())

    def test_failed_copy_preserves_private_original(self):
        with patch.object(evidence.shutil, "copytree", side_effect=OSError("synthetic copy failure")):
            with self.assertRaises(OSError):
                evidence.persist(self.source, self.destination)
        self.assertTrue(self.source.exists())
        self.assertFalse(self.destination.exists())

    def test_existing_attempt_is_never_overwritten(self):
        self.destination.mkdir(parents=True)
        marker = self.destination / "original"
        marker.write_text("retain me")
        with self.assertRaises(FileExistsError):
            evidence.persist(self.source, self.destination)
        self.assertEqual(marker.read_text(), "retain me")
        self.assertTrue(self.source.exists())

    def test_symlinks_and_nonprivate_roots_are_rejected(self):
        (self.source / "raw/link").symlink_to(self.source / "raw/trace.txt")
        with self.assertRaises(ValueError):
            evidence.persist(self.source, self.destination)
        (self.source / "raw/link").unlink()
        self.source.chmod(0o755)
        with self.assertRaises(ValueError):
            evidence.persist(self.source, self.destination)
        self.assertTrue(self.source.exists())

    def test_overlapping_destination_is_rejected(self):
        with self.assertRaises(ValueError):
            evidence.persist(self.source, self.source / "retained")
        self.assertTrue(self.source.exists())


class PeerBuildMetadataTests(unittest.TestCase):
    def run_normalizer(self, value: bytes):
        return subprocess.run(
            [sys.executable, str(SCRIPTS / "normalize-peer-build-metadata.py")],
            input=value, capture_output=True, timeout=5)

    def test_valid_marker_is_minimized_and_canonicalized(self):
        digest = "sha256:" + "a" * 64
        value = {
            "schemaVersion": 1,
            "implementation": "  synthetic-lksctp-peer  ",
            "version": "  2.4.1  ",
            "buildDigest": digest,
        }
        run = self.run_normalizer(json.dumps(value).encode())
        self.assertEqual(run.returncode, 0, run.stderr)
        normalized = json.loads(run.stdout)
        self.assertEqual(normalized, {
            "schemaVersion": 1,
            "implementation": "synthetic-lksctp-peer",
            "version": "2.4.1",
            "buildDigest": digest,
        })

    def test_missing_identity_and_unknown_fields_fail_closed(self):
        for value in (
            {"schemaVersion": 1, "implementation": "peer"},
            {"schemaVersion": 1, "implementation": "peer", "version": "1",
             "topology": "private"},
        ):
            with self.subTest(value=value):
                run = self.run_normalizer(json.dumps(value).encode())
                self.assertNotEqual(run.returncode, 0)

    def test_invalid_digest_and_oversized_marker_fail_closed(self):
        invalid = {"schemaVersion": 1, "implementation": "peer", "version": "1",
                   "buildDigest": "sha256:not-a-digest"}
        self.assertNotEqual(
            self.run_normalizer(json.dumps(invalid).encode()).returncode, 0)
        self.assertNotEqual(
            self.run_normalizer(b"{" + b"x" * (16 * 1024) + b"}").returncode, 0)



class PeerEndpointVerificationTests(unittest.TestCase):
    def run_verifier(self, target: str, inventory: str):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "peer-addresses.txt"
            path.write_text(inventory)
            return subprocess.run(
                [sys.executable, str(SCRIPTS / "verify-peer-endpoint.py"), target, str(path)],
                capture_output=True, text=True, timeout=5)

    def test_authenticated_peer_must_own_remote_ip(self):
        inventory = (
            "2: eth0    inet 192.0.2.44/24 brd 192.0.2.255 scope global eth0\n"
            "3: eth1    inet 198.51.100.7/24 brd 198.51.100.255 scope global eth1\n"
        )
        run = self.run_verifier("198.51.100.7", inventory)
        self.assertEqual(run.returncode, 0, run.stderr)

    def test_mismatched_peer_endpoint_fails_closed(self):
        inventory = "2: eth0    inet 192.0.2.44/24 brd 192.0.2.255 scope global eth0\n"
        run = self.run_verifier("198.51.100.7", inventory)
        self.assertNotEqual(run.returncode, 0)
        self.assertIn("does not own REMOTE_IP", run.stderr)


class QualificationRunnerContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.runner = (SCRIPTS / "run-multi-host-qualification.sh").read_text()
        cls.performance_lab = (
            SCRIPTS.parent / "src" / "Sigtran.NET.PerformanceLab" / "Program.cs"
        ).read_text()

    def test_capture_starts_only_after_failover_ready_and_is_hard_bounded(self):
        ready = self.runner.index('test -s "$failover_ready"')
        invocation = self.runner.index("start_capture\nfault_started_utc", ready)
        self.assertGreater(invocation, ready)
        self.assertIn('capture_limit_mb=64', self.runner)
        self.assertIn('capture_file_count=4', self.runner)
        self.assertIn('-C "$capture_limit_mb" -W "$capture_file_count"', self.runner)
        self.assertNotIn('traffic.pcap', self.runner)

    def test_capture_stops_after_recovery_and_before_soak_is_released(self):
        fault_release = self.runner.index('touch "$failover_complete"')
        recovery_wait = self.runner.index('[[ -s "$recovery_complete" ]]', fault_release)
        stop = self.runner.index("stop_capture", recovery_wait)
        acknowledge = self.runner.index('touch "$capture_stopped"', stop)
        sdk_wait = self.runner.index('wait "$sdk_pid"', acknowledge)
        self.assertLess(fault_release, recovery_wait)
        self.assertLess(recovery_wait, stop)
        self.assertLess(stop, acknowledge)
        self.assertLess(acknowledge, sdk_wait)
        self.assertIn(
            'recovery_marker_timeout_seconds=$((failover_timeout_seconds + 300))',
            self.runner,
        )

        recovery_stage = self.performance_lab.index("stages.Add(recovery);")
        recovery_marker = self.performance_lab.index(
            "options.RecoveryCompletePath", recovery_stage
        )
        capture_ack = self.performance_lab.index(
            "options.CaptureStoppedPath", recovery_marker
        )
        soak = self.performance_lab.index(
            "stages.Add(options.SoakDuration", capture_ack
        )
        self.assertLess(recovery_stage, recovery_marker)
        self.assertLess(recovery_marker, capture_ack)
        self.assertLess(capture_ack, soak)
        self.assertIn(
            '"capture-stopped-acknowledged"',
            self.performance_lab[recovery_marker:soak],
        )

    def test_peer_label_and_authenticated_address_inventory_are_protected(self):
        self.assertIn('peer_addresses="$raw/peer-addresses.txt"', self.runner)
        self.assertIn(
            'python3 "$script_dir/verify-peer-endpoint.py" "$REMOTE_IP" "$peer_addresses"',
            self.runner)
        peer_block_start = self.runner.index('echo "label=$PEER_HOST_ID"')
        peer_block_end = self.runner.index('} >"$peer_host"', peer_block_start)
        self.assertLess(peer_block_start, peer_block_end)


class QualificationPlanTests(unittest.TestCase):
    def run_plan(self, duration: str):
        env = dict(
            os.environ,
            PLAN_ONLY="true",
            QUALIFICATION_PROFILE="smoke",
            FAULT_SCENARIO="peer-outage",
            FAULT_DURATION_SECONDS=duration,
        )
        return subprocess.run(
            ["bash", str(SCRIPTS / "run-multi-host-qualification.sh")],
            env=env, capture_output=True, text=True, timeout=5)

    @staticmethod
    def reconnect_delay_budget_seconds(attempts: int) -> float:
        delay = 0.1
        total = 0.0
        for _ in range(attempts):
            total += delay
            delay = min(delay * 2, 1.0)
        return total

    def test_zero_padded_fault_duration_is_canonical_decimal(self):
        for raw, expected in (("08", 8), ("09", 9)):
            with self.subTest(raw=raw):
                run = self.run_plan(raw)
                self.assertEqual(run.returncode, 0, run.stderr)
                value = json.loads(run.stdout)
                self.assertEqual(value["faultDurationSeconds"], expected)

    def test_maximum_fault_duration_has_bounded_retry_horizon(self):
        run = self.run_plan("60")
        self.assertEqual(run.returncode, 0, run.stderr)
        value = json.loads(run.stdout)
        self.assertEqual(value["faultDurationSeconds"], 60)
        self.assertEqual(value["failoverTimeoutSeconds"], 105)
        attempts = value["reconnectMaxAttempts"]
        self.assertGreaterEqual(attempts, 30)
        self.assertLessEqual(attempts, 180)
        self.assertGreaterEqual(
            self.reconnect_delay_budget_seconds(attempts),
            value["failoverTimeoutSeconds"] + 5,
        )


class SshMaterialTests(unittest.TestCase):
    def test_atomic_modes_and_cleanup_on_success_and_failure(self):
        for exit_code in (0, 7):
            with self.subTest(exit_code=exit_code), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                report = root / "observed.json"
                child = ('import json,os,pathlib,stat,sys; '
                         'p=pathlib.Path(os.environ["PEER_SSH_IDENTITY_FILE"]); '
                         'q=pathlib.Path(os.environ["PEER_SSH_KNOWN_HOSTS_FILE"]); '
                         'json.dump({"dir":str(p.parent),"modes":[stat.S_IMODE(x.stat().st_mode) for x in [p.parent,p,q]],'
                         '"rawSecretInherited":"PEER_SSH_PRIVATE_KEY" in os.environ},open(sys.argv[1],"w")); '
                         'sys.exit(int(sys.argv[2]))')
                env = dict(os.environ, RUNNER_TEMP=str(root),
                           PEER_SSH_PRIVATE_KEY="synthetic-not-a-private-key",
                           PEER_SSH_KNOWN_HOSTS="synthetic-known-hosts")
                # Start with permissive umask to expose the historical creation gap.
                run = subprocess.run(["bash", "-c", 'umask 000; exec bash "$@"', "bash",
                                      str(SCRIPTS / "run-with-peer-ssh.sh"), sys.executable,
                                      "-c", child, str(report), str(exit_code)], env=env,
                                     capture_output=True, timeout=5)
                self.assertEqual(run.returncode, exit_code, run.stderr)
                value = json.loads(report.read_text())
                self.assertEqual(value["modes"], [0o700, 0o600, 0o600])
                self.assertFalse(value["rawSecretInherited"])
                self.assertFalse(Path(value["dir"]).exists())


if __name__ == "__main__":
    unittest.main(verbosity=2)
