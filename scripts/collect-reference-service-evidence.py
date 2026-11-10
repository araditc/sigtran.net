"""Re-verify three source-bound lab runs and emit only their sanitized observations."""

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess


PROFILES = ("reference", "wrong-context", "maintained")


def validate_manifest(root, report, profile, source_commit):
    """Check the recorded run identity and the unchanged bytes of every listed artifact."""
    if report.get("result") != "pass" or report.get("profile") != profile or report.get("sourceCommit") != source_commit:
        raise ValueError(f"{profile}: expected one passing run from the selected source commit")
    digests = report["privateArtifactSha256"]
    if not isinstance(digests, dict) or "traffic.pcap" not in digests or "http-result.json" not in digests:
        raise ValueError(f"{profile}: incomplete evidence inventory")
    for name, digest in digests.items():
        if not re.fullmatch(r"[a-z0-9][a-z0-9_.-]{0,63}", name) or not re.fullmatch(r"[a-f0-9]{64}", digest):
            raise ValueError(f"{profile}: invalid artifact identity")
        path = root / name
        if path.is_symlink() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise ValueError(f"{profile}: artifact digest mismatch: {name}")


def collect(roots, source_commit):
    """Re-decode the original PCAPs; saved pass flags do not establish qualification."""
    if not re.fullmatch(r"[a-f0-9]{40}", source_commit) or set(roots) != set(PROFILES):
        raise ValueError("Expected one full source commit and all three profiles")
    if len({root.resolve() for root in roots.values()}) != len(PROFILES):
        raise ValueError("Profiles must use separate run directories")
    verifier_path = Path(__file__).with_name("verify-reference-service-evidence.py")
    spec = importlib.util.spec_from_file_location("reference_service_verifier", verifier_path)
    verifier = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(verifier)
    runs = []
    for profile in PROFILES:
        root = roots[profile]
        saved = json.loads((root / "report.json").read_text(encoding="utf-8"))
        validate_manifest(root, saved, profile, source_commit)
        verified = verifier.verify(root, profile)
        if verified != saved:
            raise ValueError(f"{profile}: reproduced observations differ from the retained report")
        runs.append(verified)
    return {"schemaVersion": 1, "evidenceKind": "itu-map-sms-reference-service",
            "testedRuntimeCommit": source_commit, "result": "pass", "runs": runs,
            "qualificationBoundary": "Isolated interoperability evidence, not stable candidate attestation or operator acceptance."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-commit", required=True)
    for profile in PROFILES:
        parser.add_argument(f"--{profile}", required=True, type=Path)
    args = parser.parse_args()
    try:
        roots = {profile: getattr(args, profile.replace("-", "_")) for profile in PROFILES}
        print(json.dumps(collect(roots, args.source_commit), indent=2))
    except (OSError, ValueError, KeyError, IndexError, TypeError, subprocess.SubprocessError) as error:
        parser.exit(1, f"Reference evidence collection rejected: {error}\n")


if __name__ == "__main__":
    main()
