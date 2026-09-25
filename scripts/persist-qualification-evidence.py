#!/usr/bin/env python3
"""Copy private stopped-run evidence; remove scratch only after full verification.

No network access. Destination is attempt-specific and must not already exist.
On any failure the original private scratch remains available for recovery.
"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import sys
import tempfile


def inventory(root: Path) -> dict[str, str]:
    result = {}
    for path in sorted(root.rglob("*")):
        mode = path.lstat().st_mode
        relative = path.relative_to(root).as_posix()
        if stat.S_ISLNK(mode) or not (stat.S_ISDIR(mode) or stat.S_ISREG(mode)):
            raise ValueError("Evidence must contain only directories and regular files")
        if stat.S_ISDIR(mode):
            result[relative + "/"] = "directory"
        else:
            digest = hashlib.sha256()
            with path.open("rb") as stream:
                for block in iter(lambda: stream.read(1024 * 1024), b""):
                    digest.update(block)
            result[relative] = digest.hexdigest()
    return result


def require_private_directory(path: Path) -> None:
    info = path.lstat()
    if not stat.S_ISDIR(info.st_mode) or info.st_uid != os.geteuid() or info.st_mode & 0o077:
        raise ValueError("Evidence roots must be owned private directories (0700)")


def persist(source: Path, destination: Path) -> None:
    source = source.absolute()
    destination = destination.absolute()
    require_private_directory(source)
    # Reject overlap before creating/changing anything, including symlink aliases.
    src_real, dst_real = source.resolve(), destination.resolve()
    if src_real == dst_real or src_real in dst_real.parents or dst_real in src_real.parents:
        raise ValueError("Scratch and persistent evidence must be disjoint")
    if destination.exists() or destination.is_symlink():
        raise FileExistsError("Attempt evidence already exists; never overwrite it")
    destination.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    require_private_directory(destination.parent)
    expected = inventory(source)
    if "raw/" not in expected or "safe/" not in expected:
        raise ValueError("Expected raw and safe evidence directories")
    staging = Path(tempfile.mkdtemp(prefix=".sigtran-evidence-", dir=destination.parent))
    try:
        shutil.copytree(source, staging, dirs_exist_ok=True, symlinks=True)
        # Restrict copied modes before exposing the final attempt directory.
        for path in staging.rglob("*"):
            if path.is_symlink():
                raise ValueError("Evidence changed to a symlink during persistence")
            path.chmod(0o700 if path.is_dir() else 0o600)
        if inventory(staging) != expected or inventory(source) != expected:
            raise ValueError("Evidence copy verification failed or source changed")
        # This private manifest covers raw + sanitized evidence and empty dirs.
        manifest = staging / "protected-evidence.sha256.json"
        with manifest.open("x", encoding="utf-8") as stream:
            json.dump(expected, stream, sort_keys=True, indent=2)
            stream.write("\n")
        manifest.chmod(0o600)
        if destination.exists() or destination.is_symlink():
            raise FileExistsError("Attempt evidence appeared during persistence")
        staging.rename(destination)
        # Verify the final tree as well; a failed final verification retains scratch.
        retained = inventory(destination)
        retained.pop("protected-evidence.sha256.json")
        if retained != expected:
            raise ValueError("Final evidence verification failed")
        shutil.rmtree(source)
    finally:
        if staging.exists():
            shutil.rmtree(staging)


if __name__ == "__main__":
    os.umask(0o077)
    if len(sys.argv) != 3:
        raise SystemExit("usage: persist-qualification-evidence.py PRIVATE_SCRATCH ATTEMPT_DESTINATION")
    try:
        persist(Path(sys.argv[1]), Path(sys.argv[2]))
    except Exception as error:
        # Do not print raw content or subscriber/topology values from the tree.
        print(f"Protected evidence persistence failed ({type(error).__name__}); private scratch retained.", file=sys.stderr)
        raise SystemExit(1)
