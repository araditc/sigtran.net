# Completed MAP Capacity Measurement

Status: **NOT QUALIFIED**.

Protocol/accounting execution: **PASS**.
Numeric capacity target: **NOT MET**.
Capacity policy: `completed-map-10k-v1`.

Tested source: `67db5bc7a317a4c1fb27d17280e1b23280110cdb`.
Topology: `two-host-shared-peer`. One SDK process, one C peer process.

| Observation | Measured value |
| --- | ---: |
| Measurement duration | 1800 s |
| In-window completed transactions | 22,241,973 |
| Average completed TPS | 12,356.65 |
| Minimum / maximum one-second TPS | 1,769 / 16,488 |
| Seconds below target / measured seconds | 29 / 1800 |
| Worst one-second window, zero-based offset | 1781 s |
| Failed / unresolved | 0 / 0 |
| Successes during drain, excluded from TPS | 256 |
| P50 / P95 / P99 latency | 20.5 / 32.3 / 38.2 ms |
| Maximum successful latency | 1044.874 ms |
| Average / peak normalized process CPU | 80.95% / 99.55% |
| Peak SDK working set | 103 MiB |
| Process-wide allocated bytes per successful transaction, including drain | 10,176 |
| Peer total process CPU / peak RSS | 1232.151 s / 1920 KiB |

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

The declared capacity target is 10,000 successful complete transactions
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
