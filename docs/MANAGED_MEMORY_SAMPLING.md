# Managed-Memory Sampling Investigation

Date: 2026-11-17. Scope: limited instrumentation investigation and defensive
correction, **not** a fresh endurance run, acceptance exception or Phase 62 pass.
The owner requested that the 72-hour test not be repeated during this work.
No Kafka maintenance, SCTP traffic or original-evidence modification occurred.

## Original Evidence

The [completed rejected run](ENDURANCE_COMPLETION_20261117.md) retained seven
negative `ManagedBytes` values among 259,201 observations. The probe read this
field directly from `GC.GetTotalMemory(false)`. The new
[offline resource analysis](evidence/ENDURANCE_RESOURCE_ANALYSIS_20261117.json)
rechecked every observation and all 94 original hashes before/after analysis.

The seven invalid values are the only negative fields. No cumulative/time
rollback, unexpected-protocol value or observed numeric bound violation was
found by the diagnostic scan. Maximum observation gap was 1.26005 seconds;
OS peak RSS was 107.41 MiB, maximum known nonnegative managed reading 24.10 MiB,
and maximum last-GC heap 8.86 MiB. These distinct measurements do not fill the
seven unknown current-managed values. The strict original replay still fails.

Allocation deltas and neighbouring values are recorded as context, not as
certified upper bounds for the missing samples. Reads are not one atomic
snapshot and the allocated-byte counter is approximate. Neither interpolation
nor substituting last-GC heap/RSS can recover the exact historical readings.

## Runtime Investigation

The [API contract](https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalmemory?view=net-10.0)
describes managed heap bytes excluding fragmentation. The
[.NET 10.0.12 implementation](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/gc/gc.cpp#L47805)
contains approximate heap-size subtraction. That is a possible investigation
direction, **not proof** of the cause of this run's negative values. No confirmed
upstream runtime defect or instrumentation-only root cause is claimed.

Three standalone diagnostics ran on the same SDK host with .NET 10.0.12/server
GC and four visible processors. They reference no Sigtran.NET SDK package,
create no sockets, do not control services and never explicitly collect GC.
Source/binary snapshot hashes and private original outputs are retained in the
[diagnostic audit](evidence/MANAGED_MEMORY_DIAGNOSTICS_20261117.json).

| Diagnostic | Allocation Pattern | Duration | Readings | Naturally Negative / Exhausted |
| --- | --- | --- | --- | --- |
| A | Three allocation threads, small ring, burst allocation | 60 seconds | 1,721,938 | 0 / 0 |
| B | Three threads, 2,048 retained slots, 20 ms batch delay | 300 seconds | 71,076,197 | 0 / 0 |
| C | Three async/ThreadPool workers, 64 slots, 20 ms delay | 120 seconds | 27,472,896 | 0 / 0 |

Total: **100,271,031 readings in eight requested minutes**. None reproduced the
anomaly. Accelerated read count is not equivalent to three days of production
heap ageing; these runs cannot prove that the original cause is fixed. In
particular, the negative retry path was exercised by deterministic injected
tests, not by a naturally negative value during these live diagnostics.

Final read-only checks found no running probe processes, no original hash
mismatch and the original failed endurance supervision unchanged. On the peer,
all four Kafka-related states/enablement/unit-file hashes still matched the
already restored snapshot (2026-11-17 09:08 UTC). No new stop/start was issued.

## Defensive Correction

`ManagedMemorySampler` reads the same non-forcing API. A valid nonnegative
reading, including one above the memory limit, is returned immediately.
Only a negative reading causes another attempt: at most three calls, separated
by at most two requested 1 ms sleeps. Actual scheduling delay is not guaranteed.
There is no forced GC, zero clamp, interpolation or alternate-metric fallback.

Every attempted value and attempt count is retained. If all three reads are
negative, the accepted field stays negative and the existing online/offline
invalid-observation checks reject it. The monitor counts retried observations
and negative raw attempts, including successfully recovered observations.
This is bounded defensive handling, not a demonstrated root-cause fix.

### Observation Formats

- `SGENOBS1` remains the strict 208-byte legacy format. Its negative values are
  still invalid; no retry information is invented for historical files.
- New `SGENOBS2` records keep the original 208-byte prefix and append four
  signed 64-bit fields: first, second and third raw managed reads, then attempts.
  Total width is 240 bytes. Unused attempt slots must be zero.
- A source-bound start frame declares `SGENOBS2`, method
  `bounded-non-forcing-read-v1`, maximum attempts three and requested delay 1 ms.
  The verifier rejects a different/missing declaration, skipped valid earlier
  readings, accepted/raw mismatches, exhausted negative readings, altered
  checkpoint attempts or incorrect aggregate retry counts.
- The full record count still fits the original 64 MiB observation budget.
  Duration, TPS, memory, queue, recovery and unexpected-failure limits are
  unchanged. Status inspection supports both widths without starting load.

The on-disk sampling protocol is explicit; it does not retroactively change
the old frozen source or grant acceptance to its rejected stream.

## Repeatable Commands

Offline analysis uses a private copy of the original retained root:

```text
python scripts/analyze-endurance-resources.py PRIVATE_ORIGINAL_ROOT --source-commit 1579bad8b2c731819bfeb063260f23d10a0e427f --output NEW_ANALYSIS.json
```

The standalone allocation diagnostic is bounded to 1..1800 seconds and creates
only a new output directory. Do not run it on an active measured lab host.

```text
dotnet run --project eng/Sigtran.NET.ResourceProbe -c Release -- --seconds 120 --allocation-mode threadpool --allocation-delay-ms 20 --retention-slots 64 --output NEW_PROBE_DIRECTORY
```

The `threadpool` mode changes limits only inside its own diagnostic process.
It does not change the SDK consumer or host configuration. Private artifact
roots are under `artifacts/qualification/phase-62/resource-probe-20261117-*`.
The diagnostic is intentionally independent of the SDK and makes no network
throughput, leak-free production or operator-readiness claim.

## Acceptance Decision

The limited correction can be tested without another 72-hour run, and that
engineering work has been done. The original seven values remain unknown and
the root cause was not reproduced independently, so the available evidence
does **not** justify a full pass under the unchanged original acceptance gate.

To avoid repeating the full measurement, an explicitly approved, independently
reviewed acceptance exception would have to cover those historical missing
managed observations. None was requested, inferred or granted here. Otherwise
new qualifying evidence remains necessary after the sampling investigation
and integration diagnostic; this is not authorization to start another run.

The defensible current status is: retained 72-hour traffic/recovery/restoration
evidence, corrected future sampling protocol with regression coverage, and an
open managed-memory evidence gap. Phase 62 and stable promotion remain open.

## Engineering Verification

The `phase-62/sampling-review/units/10` verification passed Release build,
554 SDK checks, 40 qualification checks, trace-analysis self-test, the unchanged
2,308-member public API baseline and package creation. The standalone diagnostic
project also built with zero warnings/errors. The Python endurance suite ran
43 tests on Windows: 41 passed, two Linux-only tests skipped. CI includes those
tests and compilation of the standalone probe. None of these checks grants an
acceptance exception or qualifies the historical/new package candidate.
