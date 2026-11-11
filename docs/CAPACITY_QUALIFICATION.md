# Completed MAP Transaction Capacity

Phase 61 evaluates one Linux SDK process, native SCTP/M3UA, connectionless
SCCP, TCAP dialogue negotiation and the five typed MAP SMS operations.
The fixed target is 20,000 successful **completed** transactions per second
for 1,800 consecutive measured seconds on two declared dedicated hosts.
That is at least 36,000,000 valid correlated results, not queue admissions,
SCTP sends, M3UA echoes or HTTP requests. The workload is an equal round-robin
mix of SRI-SM, MO-ForwardSM, MT-ForwardSM, ReportSMDeliveryStatus and
AlertServiceCentre, using synthetic subscriber data and three-byte TPDUs.

**Current result: NOT QUALIFIED.** The [verified 30-minute measurement](CAPACITY_MEASUREMENT_20261111.md)
completed 21,844,522 valid transactions with zero failures/unresolved work, but
averaged 12,135.85 TPS on the authorized shared-peer topology. Implementation
delivery is not capacity-gate completion. The [sanitized JSON](evidence/COMPLETED_MAP_CAPACITY_20261111.json)
retains tested source, original artifact hashes, wire comparison and resources.

## Ten Units

| Unit | Implementation and acceptance | Status |
| --- | --- | --- |
| 1 | Fixed target, bounded options and diagnostic separation | Implemented; unit verification retained locally |
| 2 | Measured completion ledger and per-second windows | Implemented; boundary and reconciliation tests |
| 3 | Fixed workers, warmup exclusion and complete drain accounting | Implemented; drain/cancel/failure tests |
| 4 | Bounded latency and resource observations | Implemented; quantile and overflow tests |
| 5 | Native full-stack session with generation-fenced sends | Implemented; MAP outcome classification tests |
| 6 | Independent C peer counters and machine-readable summary | Implemented; independent fixtures compiled in CI |
| 7 | Two-host inventory, execution and shared-host safety | Implemented; alias/shared-host/endpoint tests |
| 8 | Source-bound report verification and tamper tests | Implemented; source/counter/latency/window rejection tests |
| 9 | Real Linux execution and mismatch correction | Two-host smoke verified; explicit lab GC/timeout/capture/cleanup corrections |
| 10 | Final evidence, CI and remaining-gate report | Implemented; real 1800-second evidence retained; target remains NOT QUALIFIED |

Each unit runs build, SDK/qualification tests, public API compatibility and
pack before its own commit/push. Names in runtime code describe the domain.

## Qualification Boundary

Warmup and drain are outside measured TPS. Only validated MAP results count.
Error, Reject, timeout, cancellation and transport failure remain visible.
No automatic retry is permitted. Every admitted invoke must have exactly one
terminal accounting entry. Late successes must not inflate measured TPS.
Every complete one-second window must meet the target; an average alone does
not hide stalls. Latency percentiles and CPU/memory/allocation report the
declared workload and hardware, not an unconditional performance promise.

Short, local or shared-host runs are diagnostic and cannot close the dedicated
two-host gate. Historical aggregate/single-host capacity remains separate.
Kafka services, configuration and ports must never be changed by this runner.
The measured interval is half-open: a result at exactly 1,800 seconds belongs
to drain. Accounting arrays are fixed by the five operations, eight terminal
outcomes and measured seconds, independent of the number of transactions.
The stable evaluator and release policy are not automatically promoted.

## Execution Model

The new capacity path is separate from historical PerformanceLab stages.
It uses bounded closed-loop concurrency: one Task per worker, one outstanding
MAP invoke per worker, and no retry. This measures saturation capacity rather
than an open-loop arrival SLA; it must not claim coordinated-omission-free
latency under overload. Warmup uses a separate ledger. Admission stops using
the monotonic clock, while the in-flight set drains for at most ten seconds.
External cancellation and drain cancellation remain counted failures.
The capacity path uses 100,001 latency counters, not one retained sample per
transaction. P50/P95/P99 are bucket upper bounds at 0.1 ms resolution up to
10 seconds; overflow quantiles conservatively use the observed maximum.
Only successful results inside measurement enter latency. CPU is process CPU
normalized to available logical processors; memory is sampled working set.
Process-wide allocations and GC counts include the bounded drain, and are not
presented as isolated codec allocations or a managed-heap-only measurement.

## Native Workload

`Sigtran.NET.PerformanceLab capacity` opens one native association: ordered
M3UA DATA stream 1, PPID 3, eight negotiated streams, ITU PCs 1/2, NI 2,
SCCP SI 3 and Routing Context 100. It invokes public `IMapSmsService`
correlated operations directly, not the HTTP ingress or a hand-built TCAP
loop. All TCAP events, unsolicited invokes and SCCP returns have owned readers.
Unexpected work, malformed input, event drops, reconnects and faults prevent
execution PASS. The final stack totals must match every admitted dialogue.
`sdk.json` is created once and retains warmup and measurement separately.
Numeric target and protocol execution are distinct; this file alone cannot
prove dedicated topology, peer counts, wire validity or release qualification.

The independent C executable accepts `IP PORT 0 capacity /private/peer.json`.
It validates the protocol envelopes, MAP arguments, ordered stream 1, network
byte-order PPID 3, Routing Context 100 and declared point codes. It counts
each accepted request/response by operation, without per-request logging.
After orderly SDK closure it creates a new JSON summary with PID, process CPU
seconds and peak RSS. Signal termination, invalid metadata or partial input
cannot emit a passing summary. These counters corroborate SDK totals; peer
send acceptance alone is not proof of SDK application completion.

## Two-Host Controller

`scripts/run-capacity-qualification.py` needs Python/Paramiko on the controller,
pinned SSH known hosts, a key for the SDK VM and either a peer key or the
process-only `SIGTRAN_CAPACITY_PEER_PASSWORD` environment variable. No credential
is written into artifacts. The controller archives the clean source commit,
builds on the SDK VM and transfers the C executable plus its local SCTP library
to a private peer directory. It uses no sudo, package installation, daemon,
firewall change, service stop or machine-wide tuning.

Both machine hashes, address ownership and non-local data routing must match.
Declare hosts dedicated only after excluding competing workloads; inventory
cannot prove an operator declaration by itself. A shared peer is accepted only
with `--shared-peer`, which forces diagnostic/non-qualifying status. The C peer
runs at nice 10 on one declared CPU. Capture is bounded by 8,192 packets,
20 seconds and 16 MiB; it samples protocol validity, not every transaction.
Private run roots, child-only timeouts, before/after inventory and SHA256s
are retained even on failed execution. Kafka process IDs/start times can be
compared; this does not assert absence of performance interference.

## Evidence Verification

On Linux with TShark, run:

```bash
python3 scripts/verify-capacity-evidence.py RUN_ROOT \
  --source-commit FULL_TESTED_COMMIT
```

The verifier rehashes original artifacts,
checks the git archive source ID, re-decodes the PCAP and independently matches
transaction/invoke identities, dialogue contexts, stream/PPID and the five MAP
results. A bounded prefix sample may end with unpaired invokes; only complete
pairs corroborate wire validity, and sample counts are never multiplied into TPS.
Complete SDK/peer totals account for the entire measured interval.

Warmup, measured successes, late results, terminal failures, per-second totals,
stack dialogue counts and peer per-operation counts must reconcile. A 20K
average cannot mask a low second. Source mismatch, changed Kafka process
identities or conflicting pass flags fail verification. The resulting report
separates execution correctness, numeric target and dedicated-host eligibility.
It is not a signed release envelope and never promotes stable by itself.

## Initial Live Observations

Two authorized shared-peer smoke measurements completed on different Linux
VMs: 51,816 in-window MAP results in 5 seconds at concurrency 64, and 116,584
in 10 seconds at concurrency 256. Both had zero failures/unresolved invokes.
The second PCAP independently matched 3,403 complete pairs spanning all five
operations; peer and SDK full-run counts reconciled. These are diagnostics,
not qualification evidence or an extrapolation to thirty minutes.

The initial workstation-GC run allocated about 16 KiB per transaction and had
frequent collections. The workload executable now explicitly selects server GC
and records the observed runtime mode; the SDK library never changes its
consumer's GC policy. This is a measured comparison candidate, not a claimed
speedup. See Microsoft's [GC configuration](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector)
and [server/workstation discussion](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc).
Every MAP operation explicitly uses a five-second timeout within the ten-second
drain budget. Capture must open before load starts, cleanup must be verified,
and UTC duration must corroborate the monotonic interval.

## Reproduction And Publication

Use a clean committed checkout and pre-authorized pinned SSH identities.
The SDK VM needs .NET 10, GCC/lksctp headers, Python, iproute2, dumpcap with
interface permission and TShark. The peer needs Linux kernel SCTP, Python,
iproute2, timeout, nice and taskset; it does not need .NET, GCC or root access.
The transferred lksctp binary/library must be ABI-compatible with its libc.

```bash
python3 scripts/run-capacity-qualification.py \
  --sdk-host SDK_IPV4 --peer-host PEER_IPV4 --user LAB_USER \
  --sdk-key /private/sdk-key --peer-key /private/peer-key \
  --dedicated-hosts --duration 1800 --concurrency 256 --peer-cpu 3 \
  --output /private/new-capacity-run
```

Choose an available peer CPU index from inventory. An explicitly approved
shared-peer measurement must use `--shared-peer`, never `--dedicated-hosts`
as a substitute for actually dedicating the host. Short diagnostics additionally
require `--diagnostic --duration N`. Keep credentials out of shell history and
logs; the controller also accepts a process-only peer password environment.
The archive and output roots are unique and retained, not recursively removed.

Generate public summaries from original protected raw evidence on Linux:

```bash
python3 scripts/publish-capacity-summary.py /private/run \
  --source-commit FULL_TESTED_COMMIT \
  --json-output /review/new-capacity-summary.json \
  --markdown-output /review/new-capacity-summary.md
```

Publication re-verifies hashes, counters and wire data first. Both output files
are created exclusively; a saved PASS flag is not accepted as input. Public
output contains counts and digests, not topology addresses, SSH identities,
subscriber traces or private directories. Raw evidence must remain accessible
to an authorized reviewer; a public hash without retained bytes is not sufficient.
Queue maxima are one-second sampled observations, not exact kernel/high-water
marks. This capacity run does not establish 72-hour leak freedom or resilience.

For exploratory CPU attribution, `sample-capacity-process.py RUN_ROOT` reads a
two-second procfs sample from the run-owned capacity child. It does not attach
a profiler or mutate process settings. User/kernel seconds are process CPU
across all threads, not percentages of one core. The reported host steal
counter is platform-dependent; zero does not exonerate a hypervisor or prove
dedicated physical resources. A short sample cannot identify which syscall,
allocation site or scheduling decision caused the full-run throughput limit.
Use a separate, explicitly profiled run for causal CPU/allocation diagnosis.

## Remaining Work Within This Phase

The target is unchanged. Average throughput needs about 65% improvement on the
observed configuration, and the worst one-second dip (1,209 TPS) needs separate
investigation. The 1,051.948 ms maximum latency is not hidden by a 36.9 ms P99.
About 16,053 allocated bytes per successful transaction and the exploratory
user/kernel split identify investigation areas, not a proven root cause.

1. Run a separate bounded CPU/allocation profile of this exact five-operation
   workload. Correlate kernel/syscall/wakeup cost, managed allocation sites,
   queue scheduling, SCTP retransmission counters and hypervisor contention.
   Profiler overhead must not be mixed into an unprofiled qualification claim.
2. Optimize only demonstrated hot paths, preserving buffer ownership, dialogue
   correlation, generation-fenced sends, cancellation and independent wire
   validation. Increasing concurrency alone did not close the observed gap.
3. Repeat the fixed 30-minute target on two genuinely dedicated hosts. Shared
   diagnostics remain useful but cannot replace that acceptance criterion.

Do not close Phase 61, promote stable, or begin claiming an operator SLA from
this result. Phase 62's 72-hour resilience criterion remains separate.
