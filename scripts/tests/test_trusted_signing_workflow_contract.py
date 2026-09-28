#!/usr/bin/env python3
"""Static safety contracts for trusted signing and release workflows."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
PREFLIGHT = ROOT / ".github" / "workflows" / "phase56-trusted-signing-preflight.yml"
RELEASE = ROOT / ".github" / "workflows" / "release.yml"


def between(text: str, start: str, end: str) -> str:
    start_index = text.index(start)
    end_index = text.index(end, start_index)
    return text[start_index:end_index]


class TrustedSigningPreflightContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.preflight = PREFLIGHT.read_text()
        cls.release = RELEASE.read_text()

    def test_preflight_permissions_are_read_only_and_environment_protected(self):
        permissions = between(
            self.preflight,
            "permissions:",
            "concurrency:",
        )
        self.assertIn("contents: read", permissions)
        self.assertNotIn("contents: write", permissions)
        self.assertNotIn("pull-requests: write", permissions)
        self.assertIn("environment:\n      name: nuget-stable", self.preflight)

    def test_preflight_pfx_is_private_from_first_write(self):
        block = between(
            self.preflight,
            "- name: Discover organization signing certificate identity",
            "- name: Validate protected certificate fingerprint",
        )
        self.assertIn("umask 077", block)
        self.assertIn('pfx="$RUNNER_TEMP/stable-signing.pfx"', block)
        self.assertIn('base64 -d > "$pfx"', block)
        self.assertIn('chmod 600 "$pfx"', block)
        self.assertIn("stat -c '%a' \"$pfx\"", block)
        self.assertIn("-passin env:SIGNING_CERTIFICATE_PASSWORD", block)
        self.assertNotIn('pass:$SIGNING_CERTIFICATE_PASSWORD', block)
        self.assertNotIn("artifacts/signing/stable-signing.pfx", block)

    def test_preflight_cleanup_runs_even_after_failure(self):
        block = between(
            self.preflight,
            "- name: Cleanup private signing material",
            "- name: Write preflight summary",
        )
        self.assertIn("if: always()", block)
        self.assertIn("rm -f --", block)
        self.assertIn('"$RUNNER_TEMP/stable-signing.pfx"', block)

    def test_preflight_summary_does_not_use_shell_backtick_substitution(self):
        block = between(
            self.preflight,
            "- name: Write preflight summary",
            "- name: Upload signing preflight evidence",
        )
        self.assertNotIn("<<EOF", block)
        self.assertIn("printf -- '- Workflow run: `%s`\\n'", block)
        self.assertIn("printf -- '- Stable candidate: `Sigtran.NET %s`\\n'", block)

    def test_release_pfx_is_private_and_ephemeral(self):
        sign = between(
            self.release,
            "- name: Sign Package",
            "- name: Retain Unsigned Prerelease Package Evidence",
        )
        self.assertIn("set -euo pipefail", sign)
        self.assertIn("umask 077", sign)
        self.assertIn(
            'signing_certificate_path="$RUNNER_TEMP/sigtran-release-signing.pfx"',
            sign,
        )
        self.assertIn('chmod 600 "$signing_certificate_path"', sign)
        self.assertIn("stat -c '%a' \"$signing_certificate_path\"", sign)
        self.assertNotIn('pass:$SIGNING_CERTIFICATE_PASSWORD', self.release)

        trust = between(
            self.release,
            "- name: Trust Dry-Run Signing Certificate",
            "- name: Verify Signature And Timestamp",
        )
        self.assertIn("-passin env:SIGNING_CERTIFICATE_PASSWORD", trust)
        self.assertNotIn('-passin pass:"$SIGNING_CERTIFICATE_PASSWORD"', trust)

        cleanup = between(
            self.release,
            "- name: Cleanup private signing material",
            "- name: Attest Package Provenance",
        )
        self.assertIn("if: always()", cleanup)
        self.assertIn('"$RUNNER_TEMP/sigtran-release-signing.pfx"', cleanup)

    def test_certificate_verifier_keeps_password_out_of_argv(self):
        verifier = (ROOT / "eng" / "verify-signing-certificate.sh").read_text()
        self.assertIn("-passin env:SIGNING_CERTIFICATE_PASSWORD", verifier)
        self.assertNotIn('pass:${PASSWORD}', verifier)
        self.assertNotIn('pass:$SIGNING_CERTIFICATE_PASSWORD', verifier)

    def test_dry_run_trust_consumes_key_before_cleanup(self):
        sign_index = self.release.index("- name: Sign Package")
        trust_index = self.release.index("- name: Trust Dry-Run Signing Certificate")
        verify_index = self.release.index("- name: Verify Signature And Timestamp")
        cleanup_index = self.release.index(
            "- name: Cleanup private signing material",
            verify_index,
        )
        self.assertLess(sign_index, trust_index)
        self.assertLess(trust_index, verify_index)
        self.assertLess(verify_index, cleanup_index)


if __name__ == "__main__":
    unittest.main(verbosity=2)
