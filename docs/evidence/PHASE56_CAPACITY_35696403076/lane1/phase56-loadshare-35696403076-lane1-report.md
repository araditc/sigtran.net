# Full-Stack Performance And Resilience Report

- Run id: `phase56-loadshare-35696403076-lane1`
- Completed UTC: `2026-09-22T06:47:56.7838953+00:00`
- Host: `runnervmlun5p`
- Runtime: `10.0.12`
- Processor count: `4`
- Peer: `independent-c-reference-peer-lane1`
- Execution passed: `True`
- Capacity qualified: `False`

## Stage Results

| Stage | Ops | Failed | Concurrency | TPS | P50 ms | P95 ms | P99 ms | Max ms | CPU avg/peak | RSS MB | Alloc B/op |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| warmup | 1000 | 0 | 16 | 5228.5 | 1.176 | 2.700 | 79.127 | 105.603 | 17.0/24.2 | 54 | 9814 |
| sustained | 20000 | 0 | 96 | 17349.8 | 5.192 | 9.856 | 13.934 | 23.415 | 28.5/36.4 | 72 | 9421 |
| peak | 20000 | 0 | 128 | 17151.5 | 7.083 | 13.342 | 16.327 | 22.435 | 29.1/45.0 | 74 | 9427 |
| recovery | 1000 | 0 | 32 | 6953.5 | 1.560 | 4.497 | 89.933 | 90.882 | 4.2/7.5 | 79 | 9450 |
| soak | 10000 | 0 | 96 | 19486.2 | 4.662 | 9.054 | 11.258 | 15.173 | 21.9/28.9 | 79 | 9361 |

## Qualification Targets

- Sustained throughput: `10000 TPS`
- Peak throughput: `20000 TPS`
- P95 latency: `20.0 ms`
- P99 latency: `50.0 ms`
- Peak CPU: `90.0%`
- Peak working set: `1024 MB`
- Allocation: `32768 B/op`

## Resilience

- Association recovery: `1681.9 ms`
- Traffic restoration: `1826.8 ms`
- Reconnect attempts: `1`
- Lost recovery operations: `0`

## Evidence Boundary

- Optional TCAP observation events dropped: `51872`

This runner measures the complete repository protocol profile over native Linux SCTP. A single-host or WSL result is a controlled baseline, not an operator-sized multi-host capacity claim.
