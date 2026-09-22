# Full-Stack Performance And Resilience Report

- Run id: `phase56-loadshare-35696403076-lane2`
- Completed UTC: `2026-09-22T06:47:56.6632885+00:00`
- Host: `runnervmlun5p`
- Runtime: `10.0.12`
- Processor count: `4`
- Peer: `independent-c-reference-peer-lane2`
- Execution passed: `True`
- Capacity qualified: `False`

## Stage Results

| Stage | Ops | Failed | Concurrency | TPS | P50 ms | P95 ms | P99 ms | Max ms | CPU avg/peak | RSS MB | Alloc B/op |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| warmup | 1000 | 0 | 16 | 5516.8 | 1.144 | 2.661 | 66.880 | 97.121 | 20.1/27.5 | 55 | 9801 |
| sustained | 20000 | 0 | 96 | 18584.1 | 4.927 | 9.243 | 11.254 | 16.190 | 27.9/34.9 | 72 | 9428 |
| peak | 20000 | 0 | 128 | 17377.1 | 6.956 | 13.443 | 16.081 | 22.501 | 31.6/46.3 | 76 | 9432 |
| recovery | 1000 | 0 | 32 | 7039.1 | 0.999 | 5.035 | 99.669 | 99.855 | 1.5/1.9 | 82 | 9475 |
| soak | 10000 | 0 | 96 | 26345.1 | 3.371 | 6.577 | 8.542 | 11.318 | 33.0/51.9 | 82 | 9348 |

## Qualification Targets

- Sustained throughput: `10000 TPS`
- Peak throughput: `20000 TPS`
- P95 latency: `20.0 ms`
- P99 latency: `50.0 ms`
- Peak CPU: `90.0%`
- Peak working set: `1024 MB`
- Allocation: `32768 B/op`

## Resilience

- Association recovery: `1760.5 ms`
- Traffic restoration: `1903.3 ms`
- Reconnect attempts: `1`
- Lost recovery operations: `0`

## Evidence Boundary

- Optional TCAP observation events dropped: `51872`

This runner measures the complete repository protocol profile over native Linux SCTP. A single-host or WSL result is a controlled baseline, not an operator-sized multi-host capacity claim.
