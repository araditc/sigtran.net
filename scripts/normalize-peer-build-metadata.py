#!/usr/bin/env python3
"""Validate and canonicalize an authorized peer build marker.

Input is read from stdin so the unvalidated remote marker is never written to
workspace or persistent evidence. Only the minimal validated identity fields are
emitted for protected raw evidence.
"""
import json
import re
import sys

MAX_BYTES = 16 * 1024
_ALLOWED_KEYS = {"schemaVersion", "implementation", "version", "buildDigest"}
_SHA256 = re.compile(r"sha256:[0-9a-fA-F]{64}")


def fail(message: str) -> "NoReturn":
    raise SystemExit(message)


def clean_text(value, name: str) -> str:
    if not isinstance(value, str):
        fail(f"{name} must be a string")
    value = value.strip()
    if not value or len(value) > 200:
        fail(f"{name} must contain 1-200 characters")
    if any(ord(ch) < 0x20 or ord(ch) == 0x7F for ch in value):
        fail(f"{name} contains control characters")
    return value


def main() -> None:
    raw = sys.stdin.buffer.read(MAX_BYTES + 1)
    if len(raw) > MAX_BYTES:
        fail("peer build marker exceeds 16 KiB")
    try:
        value = json.loads(raw.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        fail(f"peer build marker is not valid UTF-8 JSON: {exc}")

    if not isinstance(value, dict):
        fail("peer build marker must be a JSON object")
    unknown = set(value) - _ALLOWED_KEYS
    if unknown:
        fail("peer build marker contains unsupported fields: " + ", ".join(sorted(unknown)))
    if value.get("schemaVersion") != 1:
        fail("peer build marker schemaVersion must be 1")

    result = {
        "schemaVersion": 1,
        "implementation": clean_text(value.get("implementation"), "implementation"),
        "version": clean_text(value.get("version"), "version"),
    }
    digest = value.get("buildDigest")
    if digest is not None:
        if not isinstance(digest, str) or _SHA256.fullmatch(digest) is None:
            fail("buildDigest must be sha256:<64 hex characters>")
        result["buildDigest"] = digest.lower()

    json.dump(result, sys.stdout, indent=2, ensure_ascii=False)
    sys.stdout.write("\n")


if __name__ == "__main__":
    main()
