# Completed Endurance Attempt: Qualification Rejected

Review date: 2026-11-17. Phase 62 remains **not qualified**. The workload
completed the full duration and host restoration passed, but the original
independent verifier rejected invalid managed-memory observations. This is
not an interrupted load run, a passing resource replay or stable release GO.

The [sanitized audit](evidence/ENDURANCE_COMPLETED_REJECTED_20261117.json)
records separate accounting/wire/restoration checks and the original rejection.
It is deliberately not named or published as a passing verified summary.

## Measured Result

| Field | Original Result |
| --- | --- |
| Run | `map-endurance-full-20261113-a` |
| Source | `1579bad8b2c731819bfeb063260f23d10a0e427f` |
| Start UTC | `2026-11-13T07:18:35.937656+00:00` |
| Finish UTC, including drain | `2026-11-16T07:18:35.9746375+00:00` |
| Measured duration | 259,200 seconds, one SDK process, no stitched hours |
| Successful in-window transactions | 3,545,239,973 |
| Mean completed TPS | 13,677.62; includes fault downtime and recovery probes |
| Drain successes excluded from TPS | 256 |
| Total admissions / terminal successes | 3,545,242,531 / 3,545,240,229 |
| Declared injected-fault terminals | 2,302; 1,361 timeouts and 941 transport failures |
| Unexpected outcomes / unresolved work | 0 / 0 |
| Latency P50 / P95 / P99 | 18.5 / 26.5 / 33.2 ms |
| Maximum observed latency | 580.745837 ms; histogram resolution 0.1 ms |
| OS peak working set | 112,631,808 bytes, about 107.41 MiB |
| SDK / controller exit | 0 / 0 |
| Original supervision exit | 1; `Negative resource counter` |

These source/hardware/profile observations are not a general operator SLA,
per-second capacity qualification, or evidence for an untested NuGet package.
The run used .NET 10.0.12/server GC, four maximum workers, concurrency 256,
queue capacity 8192 and the explicit `lan-bounded-v1` native SCTP profile.

## Independently Checked Evidence

- All 94 original artifact hashes matched. A private local copy retains
  131,374,554 bytes under
  `artifacts/qualification/phase-62/completed-rejected-20261117/original`.
- All 4,403 journal frames passed digest/ordering/final-frame validation;
  SDK terminal accounting and independent crash-surviving peer counters agree.
- All nine scheduled incidents recovered; maximum recovery was 18.93 seconds,
  below the unchanged 90-second budget. Every incident has five fresh successes.
- Ten capture sets passed the existing wire verifier. Re-decoding matched
  3,554 complete MAP pairs, including all 45 fresh recovery pairs. Packet
  samples do not extrapolate or establish full-run throughput.
- Actual scoped SCTP partition drops/removal, process-fault receipts and
  supervisor cleanup were checked independently. Both temporary restart
  exclusions were removed. No lab workload was relaunched during review.

These subchecks do not bypass the original resource-replay failure. The
original supervisor remains failed and no `verified-summary.json` was created.

## Kafka Restoration

The host-local cleanup restored the approved peer maintenance window at
`2026-11-16T07:19:43.481010+00:00`, about 10:49 Tehran. A fresh read-only check
on 2026-11-17 confirmed broker/controller running and health timer waiting;
the originally inactive health service remained inactive. States, enablement
and unit-file hashes matched the original snapshots. The restoration fallback
timer is no longer active. Review did not change Kafka data or configuration.

## Why The Gate Remains Open

Seven of 259,201 raw observations have negative `ManagedBytes`, ranging from
-710,056 to -31,656 bytes. The first occurs at elapsed 32,430.261368219 seconds.
Every other recorded field was nonnegative. The original probe reads this
field directly from `GC.GetTotalMemory(false)`; the values are not a traffic
counter overflow or an altered artifact. The API reports managed heap bytes
excluding fragmentation; see the [official API contract](https://learn.microsoft.com/dotnet/api/system.gc.gettotalmemory).
The runtime-level cause of these invalid readings has not been established.

The online monitor aggregated maxima starting at zero and therefore did not
retain a violation for a negative reading. The stricter final verifier did
reject it. Positive peak aggregates and successful packet checks cannot make
the raw resource stream valid after the fact.

The follow-up changes reject invalid resource observations immediately, retain
the failed bound and raw signed value, and identify the field/value/time in
offline verification errors. Regression tests cover negative managed memory,
other invalid resource counters, non-finite CPU time and unchanged raw bytes.
No observation is clamped, removed, interpolated or retroactively regraded.
This detection correction does not itself fix the sampling anomaly.

## Remaining Exit Work

The subsequent [limited investigation](MANAGED_MEMORY_SAMPLING.md), requested
without a new 72-hour run, adds audited bounded retries and records the failed
independent reproduction attempt. It does not change this original rejection.
The steps below remain the no-exception qualification path, not an automatic
instruction to restart measurement or Kafka maintenance.

1. Establish and test a valid non-forcing-GC sampling correction, with explicit
   semantics for every retained observation; do not loosen the existing gate.
2. Freeze the corrected source, pass build/tests/API/pack, and verify a real
   diagnostic with fault recovery and original-state restoration.
3. Run a fresh continuous 72-hour attempt under an explicitly valid maintenance
   window. Preserve this failed attempt; do not reuse its hours or receipts.
4. Close unit 10 only after complete resource replay, accounting/wire checks,
   successful stopped supervision and restoration all pass together.

No new Kafka maintenance window or 72-hour workload was started in this review.
NuGet publication, stable tags and phases 63-65 remain outside this result.

The later owner-authorized [acceptance package](ENDURANCE_ACCEPTANCE.md) follows
a separate no-repeat administrative path. Its six implementation units are
delivered. A later explicit scoped review was authenticated and PR #38 was
merged; the owner authorized [administrative closure with recorded limitations](ENDURANCE_CLOSURE_20261117.md).
That decision does not edit this run's failed verdict
or qualify it under the original policy. The no-exception path above is unchanged.

## Follow-Up Verification

The failure-detection correction passed the complete local verification group
`phase-62/completion-audit/units/10`: Release build (zero warnings/errors),
553 SDK checks, 40 qualification checks, trace-analysis self-test, public API
comparison (2,308 members; zero additions/removals) and package creation.
The Python endurance suite ran 36 tests: 34 passed and two Linux-only tests
were skipped on Windows. These are engineering checks, not lab qualification.

Replaying the private original stream with the corrected error reporting
still rejected `ManagedBytes=-710056` at elapsed 32,430.261368219 seconds.
All 94 original hashes matched again after this read-only replay. The measured
source, policy, original resource values and failed supervision were not changed.
