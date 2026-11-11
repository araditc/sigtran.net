# Completed MAP Capacity Measurement

Status: **NOT QUALIFIED**.

Tested source: `382336cb2668ea70ce0c7cb94dcc2f9e15e5bcbb`.
Topology: `two-host-shared-peer`. One SDK process, one C peer process.

| Observation | Measured value |
| --- | ---: |
| Measurement duration | 1800 s |
| In-window completed transactions | 21,844,522 |
| Average completed TPS | 12,135.85 |
| Minimum / maximum one-second TPS | 1,209 / 16,402 |
| Failed / unresolved | 0 / 0 |
| Successes during drain, excluded from TPS | 256 |
| P50 / P95 / P99 latency | 21.1 / 30.8 / 36.9 ms |
| Maximum successful latency | 1051.948 ms |
| Average / peak normalized process CPU | 82.77% / 99.53% |
| Peak SDK working set | 108 MiB |
| Process-wide allocated bytes per successful transaction, including drain | 16,053 |
| Peer total process CPU / peak RSS | 1204.510 s / 1920 KiB |

## Scope

The workload is the equal five-operation ITU MAP SMS mix over native
SCTP/M3UA/SCCP/TCAP. Warmup, drain, queued sends and errors do not raise
measured TPS. Latency quantiles are 0.1 ms bucket upper bounds, not an
open-loop arrival SLA. The bounded PCAP corroborates protocol correctness;
full SDK/peer counters, not extrapolated capture samples, establish totals.

## Remaining Gates

- `completed-tps-below-target`
- `hosts-not-both-dedicated`
- `diagnostic-execution`

The fixed capacity target remains 20,000 successful complete transactions
in every measured second for 1,800 seconds on two dedicated hosts.
A shared Kafka host is not dedicated evidence. No Kafka service or
configuration changes are authorized by this report. Unchanged process
identities do not prove absence of performance interference.

This is source-build workload evidence, not proof that a packed NuGet
candidate was deployed. It is not a signed release envelope, an operator
acceptance result, a production SLA or authorization to promote stable.

## Declared Hardware

- SDK host: 4 logical CPUs, 15988 MiB RAM, Linux `5.15.0-191-generic`.
- PEER host: 4 logical CPUs, 5924 MiB RAM, Linux `6.8.0-149-generic`.

The private peer was constrained to one CPU at nice 10. Declared logical
CPUs do not establish dedicated physical-core performance.

## Exploratory CPU Sample

A 2.002-second read-only sample observed 2.77 user CPU seconds and 3.93 kernel CPU seconds across SDK threads. CPU model: `Intel(R) Xeon(R) CPU E5-2620 v4 @ 2.10GHz`.

This sample cannot identify a syscall/allocation bottleneck or assign the
full-run limit solely to SDK code, kernel, network, GC or hardware.
