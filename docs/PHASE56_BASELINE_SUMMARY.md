# Qualification Baseline Summary

The 2026-11-04 approved ten-unit baseline is implemented outside the SDK API:
scope/defects, topology, observed Linux inventory, protected runner plans,
artifact integrity, measured product gates, source/package/scope provenance,
stable/CI integration, and end-to-end validator rehearsal.

This work preserves `main@981113c` improvements, historical Phase 56 evidence,
the public immutable RC.2, and the `1.0.0-rc.3-dev` development identity.

The original artifact retains the single-host, pre-capture state. On
2026-11-10, fresh inventories from two Linux guests and
[bounded two-host SCTP smoke](evidence/SEPARATE_HOST_LAB_SMOKE_20261110.json)
were verified. This is not external operator, failover or endurance evidence.
The stable evaluator retains four legacy blockers and adds the mandatory
candidate qualification blocker. The schema-2 candidate validator separately
reports fifteen missing exact-candidate evidence envelopes.

Verification passed on Windows and a real Linux VM: 499 core tests, all seven
SDK/HA test projects, 35 Windows / 36 Linux qualification tests, and 60 Linux
offline Python regressions. Build has zero warnings/errors; all ten unit
build/test/pack runs passed. The 72-hour tier was validated in plan-only mode.
See the [sanitized retained observations](evidence/QUALIFICATION_BASELINE_20261104.json).

See [the unit ledger and exit decision](PHASE56_QUALIFICATION_BASELINE.md),
[tool commands](QUALIFICATION_TOOLING.md), and
[the approved continuation](RELEASE_QUALIFICATION_ROADMAP.md).
