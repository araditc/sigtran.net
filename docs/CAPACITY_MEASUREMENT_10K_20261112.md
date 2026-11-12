# Completed MAP Capacity Measurement

Status: **NOT QUALIFIED**.

Protocol/accounting execution: **PASS**.
Numeric capacity target: **MET**.
Capacity policy: `completed-map-10k-v1`.

Tested source: `324f1932cc7820335599c2e7b70e60ada32de4d9`.
Topology: `two-host-shared-peer`. One SDK process, one C peer process.

| Observation | Measured value |
| --- | ---: |
| Measurement duration | 1800 s |
| In-window completed transactions | 25,590,881 |
| Average completed TPS | 14,217.16 |
| Minimum / maximum one-second TPS | 10,026 / 19,695 |
| Seconds below target / measured seconds | 0 / 1800 |
| Worst one-second window, zero-based offset | 122 s |
| Failed / unresolved | 0 / 0 |
| Successes during drain, excluded from TPS | 256 |
| P50 / P95 / P99 latency | 17.7 / 24.4 / 31.6 ms |
| Maximum successful latency | 63.205 ms |
| Average / peak normalized process CPU | 83.69% / 98.29% |
| Peak SDK working set | 96 MiB |
| Process-wide allocated bytes per successful transaction, including drain | 9,290 |
| Peer total process CPU / peak RSS | 1379.968 s / 1920 KiB |

## Scope

The workload is the equal five-operation ITU MAP SMS mix over native
SCTP/M3UA/SCCP/TCAP. Warmup, drain, queued sends and errors do not raise
measured TPS. Latency quantiles are 0.1 ms bucket upper bounds, not an
open-loop arrival SLA. The bounded PCAP corroborates protocol correctness;
full SDK/peer counters, not extrapolated capture samples, establish totals.

## Remaining Gates

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

The private peer was constrained to one CPU at nice 0. Declared logical
CPUs do not establish dedicated physical-core performance.

## Runtime Observations

Retained 1802 timestamped observations; maximum sampling gap 1.005 s; cumulative GC pause 35015.776 ms.

Sample brackets may span adjacent seconds. Namespace counters and sampled queue maxima are not association-only traces or proof of stall causation.

## Application Runtime Profile

Observed worker minimum/maximum: 4 / 4.

Worker limits apply only to the workload application. The SDK library never
changes a consumer thread pool. Results are conditional on this declared
profile and do not establish an HTTP or mixed-service workload SLA.
