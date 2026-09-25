#!/usr/bin/env python3
"""Offline tests for Kubernetes SCTP evidence validation."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parents[1]
VALIDATOR = SCRIPTS / "validate-kubernetes-sctp-evidence.py"
RENDERER = SCRIPTS / "render-kubernetes-deployment.py"
WORKFLOW = SCRIPTS.parent / ".github" / "workflows" / "phase56-kubernetes-sctp.yml"
DOCKERFILE = SCRIPTS.parent / "deploy" / "Dockerfile"
SOURCE_SHA = "1" * 40
IMAGE = "ghcr.io/araditc/sigtran-net-operations-host@sha256:" + "a" * 64


class KubernetesSctpEvidenceTests(unittest.TestCase):
    def make_evidence(self, root: Path, *, host_network=False, associations=True):
        raw = root / "raw"
        safe = root / "safe"
        raw.mkdir()
        safe.mkdir()

        (raw / "kube-version.json").write_text(json.dumps({
            "serverVersion": {"gitVersion": "v1.34.1"}
        }))
        (raw / "nodes.json").write_text(json.dumps({
            "items": [
                {
                    "metadata": {"name": "worker-a"},
                    "status": {"nodeInfo": {
                        "operatingSystem": "linux",
                        "kernelVersion": "6.8.0-test",
                        "osImage": "Synthetic Linux",
                    }},
                },
                {
                    "metadata": {"name": "worker-b"},
                    "status": {"nodeInfo": {
                        "operatingSystem": "linux",
                        "kernelVersion": "6.8.0-test",
                        "osImage": "Synthetic Linux",
                    }},
                },
            ]
        }))
        (raw / "cni-daemonset.json").write_text(json.dumps({
            "metadata": {"name": "synthetic-cni"},
            "spec": {"template": {"spec": {"containers": [
                {"name": "cni", "image": "registry.example/cni:v1.2.3"}
            ]}}},
        }))
        (raw / "deployment.json").write_text(json.dumps({
            "spec": {"template": {"spec": {"hostNetwork": host_network}}}
        }))

        digest = "sha256:" + "a" * 64
        for name, uid, node in (
            ("pod-initial.json", "uid-initial", "worker-a"),
            ("pod-final.json", "uid-final", "worker-b"),
        ):
            (raw / name).write_text(json.dumps({
                "metadata": {"uid": uid},
                "spec": {
                    "nodeName": node,
                    "containers": [{"name": "sigtran-node", "image": IMAGE}],
                },
                "status": {
                    "containerStatuses": [{
                        "name": "sigtran-node",
                        "imageID": "docker-pullable://sigtran@" + digest,
                    }]
                },
            }))

        healthy = json.dumps({"status": "Healthy"})
        for name in ("live.json", "ready.json", "live-final.json", "ready-final.json"):
            (raw / name).write_text(healthy)

        assoc = (
            "ASSOC SOCK STY SST ST HBKT ASSOC-ID TX_QUEUE RX_QUEUE UID INODE LPORT RPORT\n"
            "1 2 2 1 1 1 42 0 0 1000 12345 2905 2906\n"
            if associations
            else
            "ASSOC SOCK STY SST ST HBKT ASSOC-ID TX_QUEUE RX_QUEUE UID INODE LPORT RPORT\n"
        )
        (raw / "sctp-assocs.txt").write_text(assoc)
        (raw / "sctp-assocs-final.txt").write_text(assoc)
        (raw / "image-source-revision.txt").write_text(SOURCE_SHA + "\n")
        return raw, safe

    def write_matrix(self, raw: Path, *, passed=True, source_sha=SOURCE_SHA):
        for filename in (
            "service-exposure.json",
            "network-policy.json",
            "graceful-termination.json",
            "pdb.json",
            "rollout-rollback.json",
            "node-drain.json",
        ):
            (raw / filename).write_text(json.dumps({
                "schemaVersion": 1,
                "sourceSha": source_sha,
                "passed": passed,
            }))

    def run_validator(self, raw: Path, safe: Path, *, profile="cni", image=IMAGE):
        return subprocess.run(
            [
                sys.executable,
                str(VALIDATOR),
                "--raw-root", str(raw),
                "--safe-root", str(safe),
                "--source-sha", SOURCE_SHA,
                "--run-id", "synthetic-k8s-1",
                "--network-profile", profile,
                "--image", image,
            ],
            capture_output=True,
            text=True,
            timeout=5,
        )

    def test_core_evidence_can_pass_without_falsely_closing_gate(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 0, run.stderr)
            value = json.loads((safe / "summary.json").read_text())
            self.assertTrue(value["executionPassed"])
            self.assertFalse(value["gateEligible"])
            self.assertFalse(value["passed"])
            self.assertEqual(value["initialSctpAssociationCount"], 1)
            self.assertEqual(value["finalSctpAssociationCount"], 1)
            self.assertEqual(
                {v["detail"] for v in value["matrix"].values()},
                {"not executed"},
            )

    def test_complete_source_bound_matrix_becomes_gate_eligible(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            self.write_matrix(raw)
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 0, run.stderr)
            value = json.loads((safe / "summary.json").read_text())
            self.assertTrue(value["executionPassed"])
            self.assertTrue(value["gateEligible"])
            self.assertTrue(value["passed"])

    def test_sctp_header_without_association_fails_core_execution(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory), associations=False)
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 1)
            value = json.loads((safe / "summary.json").read_text())
            self.assertFalse(value["checks"]["initialSctpAssociationObserved"])
            self.assertFalse(value["checks"]["finalSctpAssociationObserved"])
            self.assertFalse(value["executionPassed"])

    def test_image_revision_must_match_source_sha(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            (raw / "image-source-revision.txt").write_text("2" * 40 + "\n")
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 1)
            value = json.loads((safe / "summary.json").read_text())
            self.assertFalse(value["checks"]["imageRevisionMatchesSource"])

    def test_network_profile_must_match_deployment(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory), host_network=False)
            run = self.run_validator(raw, safe, profile="hostNetwork")
            self.assertEqual(run.returncode, 1)
            value = json.loads((safe / "summary.json").read_text())
            self.assertFalse(value["checks"]["deploymentProfileMatches"])

    def test_mutable_image_tag_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            run = self.run_validator(
                raw,
                safe,
                image="ghcr.io/araditc/sigtran-net-operations-host:latest",
            )
            self.assertNotEqual(run.returncode, 0)
            self.assertIn("pinned by sha256 digest", run.stderr)

    def test_matrix_source_mismatch_cannot_be_gate_eligible(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            self.write_matrix(raw, source_sha="2" * 40)
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 0, run.stderr)
            value = json.loads((safe / "summary.json").read_text())
            self.assertTrue(value["executionPassed"])
            self.assertFalse(value["gateEligible"])
            self.assertEqual(
                {v["detail"] for v in value["matrix"].values()},
                {"source SHA mismatch"},
            )


    def test_public_evidence_redacts_private_topology_and_registry_paths(self):
        with tempfile.TemporaryDirectory() as directory:
            raw, safe = self.make_evidence(Path(directory))
            run = self.run_validator(raw, safe)
            self.assertEqual(run.returncode, 0, run.stderr)

            summary_text = (safe / "summary.json").read_text()
            report_text = (safe / "report.md").read_text()
            value = json.loads(summary_text)

            self.assertEqual(value["initialPodNode"], "node-1")
            self.assertEqual(value["finalPodNode"], "node-2")
            self.assertEqual(value["cniImages"], ["cni:v1.2.3"])
            for private_value in ("worker-a", "worker-b", "registry.example"):
                self.assertNotIn(private_value, summary_text)
                self.assertNotIn(private_value, report_text)


class KubernetesWorkflowSafetyTests(unittest.TestCase):
    def test_operations_image_embeds_immutable_revision_metadata(self):
        dockerfile = DOCKERFILE.read_text()
        self.assertIn("ARG SOURCE_REVISION=unknown", dockerfile)
        self.assertIn("LABEL org.opencontainers.image.revision=$SOURCE_REVISION", dockerfile)
        self.assertIn("ENV SIGTRAN_IMAGE_REVISION=$SOURCE_REVISION", dockerfile)

    def test_workflow_requires_runtime_image_revision_to_match_source(self):
        workflow = WORKFLOW.read_text()
        self.assertIn("image-source-revision.txt", workflow)
        self.assertIn(
            r'printf "%s\n" "$SIGTRAN_IMAGE_REVISION"',
            workflow,
        )
        self.assertNotIn(
            r'printf "%s\\n" "$SIGTRAN_IMAGE_REVISION"',
            workflow,
        )
        self.assertIn(
            'IFS= read -r observed_revision < "$raw/image-source-revision.txt"',
            workflow,
        )
        self.assertIn('test "$observed_revision" = "$SOURCE_SHA"', workflow)

    def test_digest_image_renderer_accepts_expected_reference(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template = root / "deployment.yaml"
            output = root / "rendered.yaml"
            placeholder = "ghcr.io/araditc/sigtran-net-operations-host:VERSION"
            template.write_text(f"image: {placeholder}\n")
            run = subprocess.run(
                [
                    sys.executable,
                    str(RENDERER),
                    "--image", IMAGE,
                    "--template", str(template),
                    "--output", str(output),
                ],
                capture_output=True,
                text=True,
                timeout=5,
            )
            self.assertEqual(run.returncode, 0, run.stderr)
            self.assertEqual(output.read_text(), f"image: {IMAGE}\n")

    def test_multiline_or_sed_metacharacter_image_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template = root / "deployment.yaml"
            output = root / "rendered.yaml"
            template.write_text(
                "image: ghcr.io/araditc/sigtran-net-operations-host:VERSION\n"
            )
            malicious = (
                "ghcr.io/araditc/image; touch /tmp/sigtran-injected #|e\n"
                "@sha256:" + "a" * 64
            )
            run = subprocess.run(
                [
                    sys.executable,
                    str(RENDERER),
                    "--image", malicious,
                    "--template", str(template),
                    "--output", str(output),
                ],
                capture_output=True,
                text=True,
                timeout=5,
            )
            self.assertNotEqual(run.returncode, 0)
            self.assertFalse(output.exists())

    def test_workflow_never_interpolates_image_into_sed_program(self):
        workflow = WORKFLOW.read_text()
        self.assertNotIn(
            "sed 's|ghcr.io/araditc/sigtran-net-operations-host:VERSION|",
            workflow,
        )
        self.assertIn(
            "python3 scripts/render-kubernetes-deployment.py",
            workflow,
        )

    def test_failed_validator_is_persisted_before_status_is_returned(self):
        workflow = WORKFLOW.read_text()
        finalize = workflow.index("- name: Finalize and persist evidence")
        validator = workflow.index("validator_status=0", finalize)
        persist = workflow.index(
            'python3 scripts/persist-qualification-evidence.py',
            validator,
        )
        returned = workflow.index('exit "$validator_status"', persist)
        self.assertLess(validator, persist)
        self.assertLess(persist, returned)
        self.assertIn(
            "Protected evidence persistence: PASS",
            workflow[validator:returned],
        )

    def test_capture_stage_failure_still_reaches_evidence_finalizer(self):
        workflow = WORKFLOW.read_text()
        finalize = workflow.index("- name: Finalize and persist evidence")
        cleanup = workflow.index("- name: Cleanup private kubectl material", finalize)
        block = workflow[finalize:cleanup]
        self.assertIn(
            "if: always() && steps.identity.outcome == 'success'",
            block,
        )
        for step_id in (
            "cluster",
            "deploy",
            "initial",
            "rollout",
            "final_capture",
        ):
            self.assertIn(f"steps.{step_id}.outcome", block)
        self.assertIn("workflow_steps_ok=true", block)
        self.assertIn(
            'python3 scripts/persist-qualification-evidence.py',
            block,
        )
        self.assertIn(
            'if [[ "$workflow_steps_ok" != "true" ]]; then',
            block,
        )

    def test_checksum_failures_block_gate_and_finalizer_success(self):
        workflow = WORKFLOW.read_text()
        finalize = workflow.index("- name: Finalize and persist evidence")
        cleanup = workflow.index("- name: Cleanup private kubectl material", finalize)
        block = workflow[finalize:cleanup]
        self.assertIn("raw_checksum_status=0", block)
        self.assertIn("safe_checksum_status=0", block)
        self.assertIn("checksums_ok=false", block)
        self.assertIn('&& "$checksums_ok" == "true"', block)
        self.assertIn('if [[ "$checksums_ok" != "true" ]]; then', block)

    def test_job_summary_uses_literal_backticks_and_real_newlines(self):
        workflow = WORKFLOW.read_text()
        finalize = workflow.index("- name: Finalize and persist evidence")
        cleanup = workflow.index("- name: Cleanup private kubectl material", finalize)
        block = workflow[finalize:cleanup]
        self.assertNotIn('echo "- Source: `', block)
        self.assertNotIn('echo "- Network profile: `', block)
        for label in (
            "Source",
            "Network profile",
            "Capture stages complete",
            "Stable gate eligible",
        ):
            self.assertIn(f"printf -- '- {label}: `%s`\\n'", block)
            self.assertNotIn(f"printf -- '- {label}: `%s`\\\\n'", block)

    def test_public_evidence_requires_successful_qualification_job(self):
        workflow = WORKFLOW.read_text()
        retain = workflow.index("  retain-evidence:")
        condition = workflow.index(
            "if: needs.qualify.result == 'success' "
            "&& needs.qualify.outputs.gate_eligible == 'true'",
            retain,
        )
        self.assertGreater(condition, retain)


if __name__ == "__main__":
    unittest.main(verbosity=2)
