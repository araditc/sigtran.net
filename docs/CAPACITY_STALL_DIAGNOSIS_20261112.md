# Completed MAP Stall Diagnosis

## Decision

The [verified final measurement](CAPACITY_MEASUREMENT_10K_20261112.md) met the
unchanged numerical criterion: at least 10,000 validated completed MAP
transactions in **every one-second window for 1,800 seconds**, with zero failed
or unresolved work. All 25,590,881 in-window results reconciled with full peer
and SDK counters. Average TPS was 14,217.16, minimum 10,026, P99 31.6 ms and
maximum successful latency 63.205 ms. All four durable process exits were zero.
Independent PCAP decoding matched 3,319 complete pairs across all five operations.

This is **not dedicated-host qualification**. The peer VM still runs Kafka;
the test was explicitly diagnostic. The minimum margin is only 26 TPS (0.26%).
Do not sell this as an unconditional SLA, a packaged-candidate deployment,
signed release evidence or a stable promotion decision. No Kafka service,
configuration, affinity or task was changed or profiled. Before/after Kafka
process identities were unchanged; that does not prove absence of interference.

## What Was Found

The original 29 low seconds cannot be assigned individual causes after the
fact: the old PCAP covers only a bounded prefix, and the original run had no
per-second GC/peer observations. The prior 1,044.874 ms outlier is not proof
of a GC pause, SCTP RTO or hardware fault. It would be incorrect to claim that
all 29 seconds were caused by one SDK defect or by Kafka.

New diagnostics reproduced smaller dips without new SDK-side retransmissions,
Gen2 or long GC pauses in the enclosing sample brackets. Native profiling had
identified syscall/scheduling/yield cost, and allocation sampling identified
TCAP encoding/decoding costs. Measured changes and execution profiles were:

- Linux native receive waits now use non-consuming async socket readiness after
  would-block instead of repeated 2 ms polling timers. Native code still owns
  payload/metadata reads; cancellation, deadlines and record boundaries remain.
- TCAP OIDs no longer round-trip through numeric strings and split/parse
  arrays. BER shape checks and the existing bounded arc codec remain; immutable
  formatting is cached per instance, not in an unbounded global map.
- Four application workers improved measured scheduling headroom on the
  declared four-processor SDK VM. The library never sets consumer thread limits.
- The private C peer was tested at normal nice 0 instead of nice 10, on the
  same single CPU. This is a peer test-profile change, not a Kafka change or a
  guarantee that priority caused all historical dips.
- A separately observed M2PA Busy Ended race was fixed: restored state and
  cleared congestion become visible before blocked senders resume. This
  correctness defect was not a cause of M3UA workload capacity dips.
- SSH control loss exposed a collection defect. Keepalives, atomic owned-child
  exit receipts, bounded reconnects and partial-evidence retention now prevent
  unknown exit status from being treated as success.

Before/after shared-host runs do not isolate every causal contribution.
The validated conclusion is that the **combined declared profile met the
numeric target**, not that any single change guarantees that capacity elsewhere.

## Retained Experiments

The [diagnostic comparison](evidence/CAPACITY_STALL_DIAGNOSTICS_20261112.json)
records source IDs, raw SDK digests, counts and limitations. It is not a release
qualification envelope; reproduce its raw verification on an authorized host.

| Run | Seconds | Average TPS | Minimum TPS | Seconds below 10K |
| --- | ---: | ---: | ---: | ---: |
| Original 10K-policy baseline | 1800 | 12,356.65 | 1,769 | 29 |
| Runtime diagnosis | 300 | 11,708.81 | 9,599 | 6 |
| Async readiness | 300 | 12,172.90 | 9,982 | 2 |
| OID decoding | 300 | 11,627.29 | 8,940 | 5 |
| Four workers, short | 300 | 14,451.46 | 10,280 | 0 |
| Four workers, interrupted collection | 1800 | 14,530.76 | 9,782 | 3 |
| Spin-zero experiment, rejected | 360 | 10,801.24 | 4,150 | 10 |
| Normal-priority peer, short | 360 | 14,735.90 | 11,223 | 0 |
| Normal-priority peer, final verified | 1800 | 14,217.16 | 10,026 | 0 |

The interrupted run completed traffic but lost control-channel exit statuses;
its recovered originals remain separate, explicitly incomplete and non-passing.
The spin-zero experiment worsened throughput and reproduced a long tail; it is
**not** part of the final profile. No negative result was rewritten as a pass.

## Final Profile And Reproduction

Tested source: `324f1932cc7820335599c2e7b70e60ada32de4d9`. Linux native SCTP,
.NET 10.0.12/server GC, four SDK logical processors, four minimum/maximum worker
threads, concurrency 256, ten-second warmup, 1,800-second measurement and bounded
drain. Traffic remains one ordered DATA stream 1, PPID 3, RC 100, ITU PCs 1/2,
NI 2, SCCP SI 3 and the equal five-operation MAP SMS mix. There is no retry.
The independent C peer is single-threaded, pinned to one CPU at nice 0; the
owned-peer observer runs at nice 19. Worker spinning uses the runtime default.

```bash
python3 scripts/run-capacity-qualification.py \
  --sdk-host SDK_IPV4 --peer-host PEER_IPV4 --user LAB_USER \
  --sdk-key /private/sdk-key --peer-key /private/peer-key \
  --shared-peer --diagnostic --duration 1800 --concurrency 256 \
  --maximum-workers 4 --peer-cpu 3 --peer-nice 0 \
  --output /private/new-capacity-run
```

Select an available CPU index from the peer inventory. Shared mode is explicit,
never a substitute for a dedicated declaration. No profiler or concurrent build
ran on the SDK host during the measurement. Resource allocation was about
9,290 bytes/success including drain, versus 10,176 in the original 10K-policy
baseline; this observed reduction is not a causal throughput claim.

## Remaining Gate

The numerical regression is closed **for this source/profile/run**. Re-run on
two genuinely dedicated hosts before closing the complete Phase 61 qualification
gate. Preserve current source/package/scope binding and release trust policy;
these source-build reports alone do not meet that policy. Phase 62's 72-hour
resilience, deployment, operator acceptance and release gates remain separate.
20K optimization is deferred, not a current completion condition.
