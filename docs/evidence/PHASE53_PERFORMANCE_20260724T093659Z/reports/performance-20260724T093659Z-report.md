# Full-Stack Performance And Resilience Report

- Run id: `performance-20260724T093659Z`
- Completed UTC: `2026-07-24T09:37:17.3277711+00:00`
- Host: `AmmarPC`
- Runtime: `10.0.9`
- Processor count: `8`
- Peer: `independent-c-reference-peer`
- Execution passed: `True`
- Capacity qualified: `False`

## Stage Results

| Stage | Ops | Failed | Concurrency | TPS | P50 ms | P95 ms | P99 ms | Max ms | CPU avg/peak | RSS MB | Alloc B/op |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| warmup | 1000 | 0 | 16 | 4913.2 | 1.112 | 2.352 | 92.041 | 120.867 | 16.0/31.9 | 51 | 10179 |
| sustained | 20000 | 0 | 64 | 13515.3 | 4.876 | 7.230 | 8.419 | 10.508 | 42.7/51.7 | 60 | 9627 |
| peak | 20000 | 0 | 128 | 13491.5 | 9.628 | 14.471 | 16.380 | 21.269 | 42.8/49.0 | 69 | 9585 |
| recovery | 1000 | 0 | 32 | 4333.8 | 3.343 | 13.255 | 89.833 | 90.296 | 18.5/31.1 | 70 | 9626 |
| soak | 20000 | 0 | 64 | 13582.4 | 4.765 | 6.911 | 8.058 | 10.039 | 31.0/42.1 | 70 | 9501 |

## Qualification Targets

- Sustained throughput: `10000 TPS`
- Peak throughput: `20000 TPS`
- P95 latency: `20.0 ms`
- P99 latency: `50.0 ms`
- Peak CPU: `90.0%`
- Peak working set: `1024 MB`
- Allocation: `32768 B/op`

## Resilience

- Association recovery: `1039.1 ms`
- Traffic restoration: `1270.4 ms`
- Reconnect attempts: `1`
- Lost recovery operations: `0`

## Evidence Boundary

- Optional TCAP observation events dropped: `61872`

This runner measures the complete repository protocol profile over native Linux SCTP. A single-host or WSL result is a controlled baseline, not an operator-sized multi-host capacity claim.
