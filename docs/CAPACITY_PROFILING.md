# Completed MAP Capacity Profiling

This is the continuation of Phase 61, not a new capacity criterion. Preserve the
[original 30-minute baseline](CAPACITY_MEASUREMENT_20261111.md). Successful
protocol execution, numeric capacity and dedicated-host qualification remain
separate decisions. Kafka service, configuration and workload are outside scope.

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
