#!/usr/bin/env python3
"""Validate a digest-pinned OCI image reference and render the Kubernetes template.

The image value is treated only as data. It is never interpolated into a shell,
sed, or other executable program.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import re
import sys

_PLACEHOLDER = "ghcr.io/araditc/sigtran-net-operations-host:VERSION"
_POD_LABEL = "\n    metadata:\n      labels:\n        app.kubernetes.io/name: sigtran-node\n"
_RUN_RE = re.compile(r"[a-z0-9](?:[-a-z0-9]{0,60}[a-z0-9])?")
_IMAGE_RE = re.compile(
    r"[a-z0-9]+(?:[.-][a-z0-9]+)*(?::[1-9][0-9]{0,4})?"
    r"(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)+"
    r"@sha256:[0-9a-f]{64}"
)


def validate_image_reference(value: str) -> str:
    if len(value) > 512 or _IMAGE_RE.fullmatch(value) is None:
        raise ValueError(
            "image must be a bounded lowercase OCI repository reference "
            "pinned by sha256 digest"
        )
    return value


def render(template: Path, output: Path, image: str, run_id: str | None = None) -> None:
    """Render the checked template and optionally mark only its pod template."""
    image = validate_image_reference(image)
    content = template.read_text(encoding="utf-8")
    count = content.count(_PLACEHOLDER)
    if count != 1:
        raise ValueError(
            f"deployment template must contain exactly one image placeholder; observed {count}"
        )
    if run_id is not None:
        if not _RUN_RE.fullmatch(run_id) or content.count(_POD_LABEL) != 1:
            raise ValueError("A valid qualification run ID and unique pod label section are required")
        content = content.replace(_POD_LABEL, _POD_LABEL + f'        sigtran.net/qualification-run: "{run_id}"\n')
    output.write_text(content.replace(_PLACEHOLDER, image), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", required=True)
    parser.add_argument("--template")
    parser.add_argument("--output")
    parser.add_argument("--run-id")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    image = validate_image_reference(args.image)
    if (args.template is None) != (args.output is None) or (args.run_id is not None and args.template is None):
        raise ValueError("--template and --output must be provided together")
    if args.template is not None:
        render(Path(args.template), Path(args.output), image, args.run_id)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        raise SystemExit(2)
