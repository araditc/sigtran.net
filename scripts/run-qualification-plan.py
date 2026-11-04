#!/usr/bin/env python3
"""Launch the existing lab runner from a validated plan, without shell interpolation."""
import json
import os
from pathlib import Path
import subprocess
import sys

REQUIRED = {"RUN_ID", "QUALIFICATION_PROFILE", "FAULT_SCENARIO", "REMOTE_IP",
            "REMOTE_SCTP_PORT", "SDK_HOST_ID", "PEER_HOST_ID", "CAPTURE_INTERFACE"}
ALLOWED = REQUIRED | {"SIGTRAN_CAPTURE_BACKEND"}


def launch(plan_path, plan_only=False):
    """Protected SSH, source SHA and raw-evidence settings remain required by the runner."""
    plan = json.loads(Path(plan_path).read_text(encoding="utf-8-sig"))
    if not isinstance(plan, dict) or not REQUIRED.issubset(plan) or set(plan) - ALLOWED:
        raise ValueError("Plan must contain the required qualification inputs and no unknown keys")
    if any(not isinstance(v, str) or not v or "\n" in v or "\r" in v for v in plan.values()):
        raise ValueError("Invalid plan value")
    env = os.environ.copy()
    env.update(plan)
    env["PLAN_ONLY"] = "true" if plan_only else "false"
    runner = Path(__file__).resolve().with_name("run-multi-host-qualification.sh")
    return subprocess.run(["bash", str(runner)], env=env, check=False).returncode


if __name__ == "__main__":
    if len(sys.argv) not in (2, 3) or (len(sys.argv) == 3 and sys.argv[2] != "--plan-only"):
        raise SystemExit("Usage: run-qualification-plan.py <protected-plan.json> [--plan-only]")
    raise SystemExit(launch(sys.argv[1], len(sys.argv) == 3))
