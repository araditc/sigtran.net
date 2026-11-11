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
