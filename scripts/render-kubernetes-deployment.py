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


def render(template: Path, output: Path, image: str) -> None:
    image = validate_image_reference(image)
    content = template.read_text(encoding="utf-8")
    count = content.count(_PLACEHOLDER)
    if count != 1:
        raise ValueError(
            f"deployment template must contain exactly one image placeholder; observed {count}"
        )
    output.write_text(content.replace(_PLACEHOLDER, image), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", required=True)
    parser.add_argument("--template")
    parser.add_argument("--output")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    image = validate_image_reference(args.image)
    if (args.template is None) != (args.output is None):
        raise ValueError("--template and --output must be provided together")
    if args.template is not None:
        render(Path(args.template), Path(args.output), image)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        raise SystemExit(2)
