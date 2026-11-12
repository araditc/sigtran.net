# Completed MAP Capacity Profiling

This is the continuation of Phase 61, not a new capacity criterion. Preserve the
[original 30-minute baseline](CAPACITY_MEASUREMENT_20261111.md). Successful
protocol execution, numeric capacity and dedicated-host qualification remain
separate decisions. Kafka service, configuration and workload are outside scope.

The owner [approved a 10K current v1 target](CAPACITY_TARGET_DECISION.md) on
2026-11-11; 20K is now future optimization. Stop speculative optimization once
the current measured profile is met; keep correctness and all other gates.
The SDK test executable supports `--filter NAME` for focused Linux diagnosis;
the default still executes the complete suite and an empty selection fails.

## Collection And Attribution

Collect separate bounded diagnostics before changing code. The pinned private
tool is `dotnet-trace 10.0.750501`; the offline engineering parser references
`Microsoft.Diagnostics.Tracing.TraceEvent 3.2.8`. No parser/tool package becomes
a dependency of the SDK NuGet package. Raw traces contain stacks and topology
and belong only in protected lab storage.

```bash
dotnet-trace collect --process-id OWNED_SDK_PID \
  --profile dotnet-sampled-thread-time,gc-verbose \
  --duration 00:00:00:15 --buffersize 64 --output /private/baseline.nettrace
dotnet-trace report /private/baseline.nettrace topN --inclusive --number 30
dotnet run --project eng/Sigtran.NET.TraceAnalysis -c Release -- /private/baseline.nettrace
```

The managed thread-time profile includes waiting threads; its topN percentages
are **not** on-CPU attribution. Use bounded native CPU/syscall diagnostics for
kernel cost. GC allocation ticks are sampled and weighted, not an exact object
census. Type/site byte estimates guide investigation; inclusive method estimates
overlap. Actual bytes per transaction come from the unprofiled process counter.
The parser fails if no allocation observations exist or events were lost.
See [Microsoft's tracing documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
and the [TraceEvent parser](https://github.com/microsoft/perfview/blob/main/src/TraceEvent/Parsers/ClrTraceEventParser.cs.base).

## Baseline Observations

A separate 90-second native MAP workload at the unchanged baseline source
was sampled for 15 seconds. It completed correctly but is not a capacity run.
The trace retained 14,512 allocation ticks without lost events. Weighted samples
put byte arrays first, followed by strings, ASN.1 readers and async state.
ASN.1 writer buffer growth and repeated TCAP dialogue encode/decode occur on
these stacks. This identifies candidates for measurement, not a proven speedup.

## Verification And Evidence

Every change retains wire conformance, five-operation completion accounting,
generation fencing, cancellation and buffer ownership. Unit verification uses
`eng/verify-qualification-unit.ps1 -Phase 61 -RunGroup tuning -Unit N` so prior
logs/packages are never overwritten. Re-run the full 1,800-second unprofiled
workload only after short real cross-host trials demonstrate the intended
effect. A shared-peer run remains non-dedicated even if numeric throughput passes.

## Encoding Changes

The first measured targets are TCAP's per-invoke 2 KiB scratch array and the
standard ASN.1 writer's repeated buffer growth in MAP/TCAP encoders. Transactions
now allocate their exact final TLV size. Length boundaries and long-form lengths
are compared against an independent `AsnWriter`, including values above 2 KiB.
Small BER writers are borrowed synchronously per thread and reset on return;
nested calls get distinct writers, failed/unbalanced scopes cannot poison reuse,
and encodings above 4 KiB are not retained. Encoded output remains a fresh owned
copy, never pooled memory. No public API or wire format changes are intended.
See the standard [writer reset contract](https://learn.microsoft.com/en-us/dotnet/api/system.formats.asn1.asnwriter.reset?view=net-10.0).

The first real encoding trial reduced allocation to about 10,305 bytes per
transaction but did not improve observed TPS. Native CPU sampling identified
syscall entry, scheduling/context-switch and yield costs; syscall summaries
also observed a poll before almost every send/receive. These are separate
diagnostic observations, not timed-capacity evidence or exact managed CPU costs.

## Nonblocking Native I/O

The Linux metadata adapter already forces its socket nonblocking. It now tries
that native operation first and waits for readiness only after would-block.
Caller cancellation and the operation deadline are still checked before the
first attempt; fragmented records, notifications and oversize-tail draining
retain their existing state machine. Legacy/non-Linux paths retain pre-waiting.
Linux integration tests cover pre-cancelled sends, pre-cancelled receives that
must not consume queued data, metadata integrity and idle receive expiry.

The first real nonblocking trial exposed an existing native ABI defect hidden
by most pre-polling paths: lksctp `sctp_sendmsg` and `sctp_recvmsg` return C
`int`, not `ssize_t`. Both imports now preserve signed 32-bit returns, so x64
would-block is handled as EAGAIN rather than a positive overflowing length.
A platform-independent signature test and the real Linux idle/cancellation
tests guard this boundary. The failed trial is retained, not relabeled PASS.

## Current 10K Preflight

The corrected native candidate `104eac346ba37b839aa30136c30e6f757e422add`
completed a 30-second unprofiled trial at 12,505.1 completed TPS, minimum
one-second throughput 10,509, zero failed/unresolved work and 79.68% normalized
SDK CPU. This is a short preflight, not a substituted 30-minute pass.
The real Linux SDK suite also passed after making its concurrently invoked
test event observer thread-safe; focused test selection cannot silently pass
when it matches no tests. The final benchmark must run without profiler or
concurrent build activity on the SDK VM.

## Native CPU And Syscalls

A separate 45-second baseline workload was sampled with SDK-PID-only native
`perf` for ten seconds and `strace -f -c` for three seconds. The retained
CPU-clock profile lost no samples. Self percentages included syscall entry
8.79%, task-switch completion 8.29%, user `sched_yield` 6.37% and kernel
scheduling 5.59%. Some managed symbols remained unresolved; this is not a
complete managed CPU attribution. The syscall observation recorded 8,756 poll,
4,202 sendmsg and 4,449 recvmsg calls. Its elapsed-time percentages include
blocking and profiler overhead and must not be called on-CPU percentages.

Tools were installed/extracted privately on the SDK host. The owned process
was targeted explicitly; no global perf policy, kernel/sysctl, Kafka process,
service or host-wide scheduling configuration was changed. Short profiler runs are
diagnostic only. [Profile metadata and digests](evidence/COMPLETED_MAP_PROFILE_20261111.json)
identify the retained private originals without publishing stack traces.

## Full New-Policy Result

The [1,800-second 10K-target run](CAPACITY_MEASUREMENT_10K_20261111.md)
used frozen source `67db5bc7a317a4c1fb27d17280e1b23280110cdb`, no attached
profiler and no concurrent build on the SDK host. It completed 22,241,973
in-window transactions with zero failed/unresolved work. All full-run peer
counts reconciled; 256 drain successes were excluded from TPS. The separately
bounded PCAP matched 3,556 pairs across the five operations. Cleanup and
before/after Kafka process identities were verified; this does not prove
absence of shared-host interference.

| Full-run observation | Historical baseline | New 10K-policy run |
| --- | ---: | ---: |
| Average completed TPS | 12,135.85 | 12,356.65 |
| Minimum one-second TPS | 1,209 | 1,769 |
| Allocated bytes per success including drain | 16,053 | 10,176 |
| Average normalized SDK CPU | 82.77% | 80.95% |
| Peak SDK working set | 108 MiB | 103 MiB |
| P99 successful latency | 36.9 ms | 38.2 ms |

Allocation per success is 36.6% lower. Throughput and CPU observations are not
a controlled causal speedup claim: source, time and host contention differ.
There were 29 measured seconds below 10K, with a maximum successful latency of
1,044.874 ms. The average exceeds 10K, but the unchanged per-second and
dedicated-host gates remain open. The raw prefix PCAP cannot explain a stall
at measured offset 1,781 seconds. Keep host/scheduler, GC and retransmission
correlation as unresolved diagnosis; do not infer that more hardware alone
will fix it or continue optimizing toward the deferred 20K target.

Seven reviewable tuning units cover analysis tooling, encoding allocation,
nonblocking I/O, the signed native ABI fix, the owner-approved policy,
fail-closed CI checks and this final verified report. Each retains separate
build/test/pack/API logs before its commit/push. Runtime public API is unchanged.

## Per-Second Stall Diagnosis

The 29 sub-target windows were not identical: most clustered at offsets
1,338-1,390 seconds, while the worst was offset 1,781. Historical raw counters
alone cannot assign a GC, scheduler or retransmission cause to those windows.
New runs retain bounded `RuntimeObservations` with actual monotonic offsets,
cumulative GC pause/collection counts, ThreadPool threads/queued/completed work,
process CPU and Linux host SCTP/CPU counters. Delayed sampling is visible rather
than mislabeled as an exact second. SCTP counters are namespace-wide, CPU ticks
are host-wide, and an absent counter is not zero. Guest ticks are not counted
twice. This changes diagnostics, never completion accounting or acceptance.
See the [.NET runtime metric definitions](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime).

Linux metadata receive now waits through the .NET async socket engine after
native would-block rather than scheduling repeated 2 ms polling timers. The
zero-byte read is non-consuming; native `sctp_recvmsg` remains the only consumer
of payload, notification and ancillary metadata. A linked cancellation token
owns each wait until completion, including timeout, so no abandoned async reader
can interfere with a later call. Send/connect and legacy paths are unchanged.
Real Linux tests cover cancellation during readiness, subsequent data/PPID/stream
integrity and idle expiry. The standard runtime's
[zero-byte receive implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SocketPal.Unix.cs)
uses a one-byte peek; this is a measured optimization candidate, not proof of
the cause of every historical low window.

The complete Linux suite also exposed a pre-existing M2PA Busy Ended race:
the availability signal was released before `InService` was visible. Recovery
now publishes congestion/state atomically before waking senders, with observers
outside the state lock. A 32-cycle native-independent test exercises recovery
and verifies payload/acknowledgement accounting. This is not a claimed cause of
M3UA capacity stalls. The SCCP metrics test now waits for its documented
point-in-time counter to catch up with channel publication instead of assuming
the producer continuation has already run when the consumer returns.

The five-minute before/after receive-readiness comparison retained six versus
two sub-target windows (minimum 9,599 versus 9,982 TPS). The before-run low-window
brackets had no new SCTP retransmissions, only about 60-72 ms cumulative GC pause
across approximately two-second brackets, and no large sampling delay. This
does not reproduce or explain the historical one-second extreme stall.
Readiness alone did not close the target. Dialogue OID decoding now uses the
standard ASN.1 reader/decoder for BER shape plus the existing bounded OID arc
codec directly, avoiding temporary numeric strings and split/parse arrays.
The fixed dialogue syntax is compared by canonical value bytes. Immutable OID
formatting is cached per instance, never in an attacker-expandable global map.
Tests retain large-arc, malformed/truncated/non-minimal arc and exact wire checks.

The raw-evidence verifier validates optional runtime sample bounds, finite
numbers, monotonic time and cumulative counters. Sanitized output includes up
to 20 lowest sub-target windows with actual enclosing sample timestamps,
GC pause/Gen2 deltas, observed ThreadPool queue maximum and available namespace
SCTP deltas. Missing counters stay absent, and `coversCompleteWindow` explicitly
marks incomplete bracketing. This evidence supports diagnosis, not an automatic
root-cause label or qualification waiver.
