# Phase 53 - Operator-Sized Performance And Resilience

Phase 53 replaces smoke timing with an executable full-stack benchmark over
native Linux SCTP and an independent C peer.

## Completed Units

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | Full-stack performance lab executable | Complete |
| 2 | Configurable warmup and sustained stages | Complete |
| 3 | Configurable peak and soak stages | Complete |
| 4 | P50, P95, P99, and maximum latency | Complete |
| 5 | CPU, working set, allocation, and Gen2 metrics | Complete |
| 6 | Association failover and reconnect orchestration | Complete |
| 7 | Recovery traffic and loss validation | Complete |
| 8 | PCAP, trace, peer logs, metrics, and comparison artifacts | Complete |
| 9 | Hot-path allocation and TCAP queue corrections | Complete |
| 10 | Retained baseline, docs, build, test, and pack | Complete |

## Run The Benchmark

The runner requires Linux kernel SCTP, lksctp headers, GCC, .NET 10,
`tcpdump`, `tshark`, and `gzip`.

```bash
SIGTRAN_ARTIFACT_ROOT="$HOME/sigtran-lab/artifacts" \
SIGTRAN_WARMUP_OPERATIONS=1000 \
SIGTRAN_SUSTAINED_OPERATIONS=20000 \
SIGTRAN_PEAK_OPERATIONS=20000 \
SIGTRAN_RECOVERY_OPERATIONS=1000 \
SIGTRAN_SOAK_OPERATIONS=20000 \
bash scripts/run-full-stack-performance-lab.sh
```

Counts, concurrency, addresses, point codes, and peer identity are environment
configurable. Publication targets remain fixed in the lab report unless the
executable is explicitly given alternative qualification arguments.

## Runtime Corrections

The first measured run exposed two production bottlenecks:

- Native SCTP receive allocated a buffer as large as the maximum M3UA PDU for
  every message, and send copied an already array-backed payload.
- The optional TCAP dialogue observation queue could block correlated result
  processing when no observation consumer was present.

The native path now uses array-backed memory directly and falls back to
`ArrayPool<byte>` only when required. `SCTP_NODELAY` is enabled by default
through `NativeSctpTransportOptions.EnableNoDelay`.

TCAP correlated processing no longer waits for the optional dialogue event
consumer. A full observation queue drops the event and increments
`TcapDialogueManagerMetrics.DroppedDialogueEvents`; inbound application Invoke
components retain bounded backpressure.

## Retained Baseline

Run `performance-20260724T093659Z` completed on WSL2 kernel
`6.18.33.2-microsoft-standard-WSL2` with .NET `10.0.9`, eight visible
processors, native SCTP, packet capture, and the independent C peer.

| Stage | Operations | Failed | TPS | P95 | P99 | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Warmup | 1,000 | 0 | 4,913.2 | 2.352 ms | 92.041 ms | 10,179 B/op |
| Sustained | 20,000 | 0 | 13,515.3 | 7.230 ms | 8.419 ms | 9,627 B/op |
| Peak | 20,000 | 0 | 13,491.5 | 14.471 ms | 16.380 ms | 9,585 B/op |
| Recovery | 1,000 | 0 | 4,333.8 | 13.255 ms | 89.833 ms | 9,626 B/op |
| Soak | 20,000 | 0 | 13,582.4 | 6.911 ms | 8.058 ms | 9,501 B/op |

All 62,000 requests and responses were counted at M3UA, SCCP, and TCAP.
Association recovery completed in `1,039.1 ms`, recovery traffic completed in
`1,270.4 ms`, and no recovery operations were lost.

The retained bundle under
`docs/evidence/PHASE53_PERFORMANCE_20260724T093659Z/` includes compressed PCAP
and TShark output, peer logs, host profile, SDK trace, structured metrics,
configuration, comparison, report, and SHA-256 manifest.

## Qualification Decision

The controlled execution passed, including sustained load, failover, recovery,
and soak. It did not satisfy the `20,000 TPS` peak target: observed peak
throughput was `13,491.5 TPS`.

This result is also single-host WSL loopback. It cannot support an
operator-sized multi-host capacity claim. That claim requires a dedicated
Linux host or VM pair, representative CPU limits, production network latency,
long-duration soak, and a separately operated peer. The runner is ready for
that environment without code changes.
