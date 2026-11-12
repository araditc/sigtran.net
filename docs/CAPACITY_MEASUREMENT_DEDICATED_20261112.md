# Completed MAP Capacity Measurement

Status: **PASS**.

Protocol/accounting execution: **PASS**.
Numeric capacity target: **MET**.
Capacity policy: `completed-map-10k-v1`.

Tested source: `947d69abd081449cc0e2c286409eeab69a659ab7`.
Topology: `two-host-declared-dedicated`. One SDK process, one C peer process.

| Observation | Measured value |
| --- | ---: |
| Measurement duration | 1800 s |
| In-window completed transactions | 26,599,474 |
| Average completed TPS | 14,777.49 |
| Minimum / maximum one-second TPS | 10,079 / 20,485 |
| Seconds below target / measured seconds | 0 / 1800 |
| Worst one-second window, zero-based offset | 556 s |
| Failed / unresolved | 0 / 0 |
| Successes during drain, excluded from TPS | 256 |
| P50 / P95 / P99 latency | 17.0 / 23.4 / 30.9 ms |
| Maximum successful latency | 66.255 ms |
| Average / peak normalized process CPU | 84.06% / 92.08% |
| Peak SDK working set | 103 MiB |
| Process-wide allocated bytes per successful transaction, including drain | 8,715 |
| Peer total process CPU / peak RSS | 1354.956 s / 1920 KiB |

## Scope

The workload is the equal five-operation ITU MAP SMS mix over native
SCTP/M3UA/SCCP/TCAP. Warmup, drain, queued sends and errors do not raise
measured TPS. Latency quantiles are 0.1 ms bucket upper bounds, not an
open-loop arrival SLA. The bounded PCAP corroborates protocol correctness;
full SDK/peer counters, not extrapolated capture samples, establish totals.

## Remaining Gates


The declared capacity target is 10,000 successful complete transactions
in every measured second for 1,800 seconds on two dedicated hosts.
A shared Kafka host is not dedicated evidence. This report grants no
service-change authorization. Unchanged process identities alone do not
prove absence of performance interference.

This is source-build workload evidence, not proof that a packed NuGet
candidate was deployed. It is not a signed release envelope, an operator
acceptance result, a production SLA or authorization to promote stable.

## Approved Maintenance Window

The owner explicitly approved temporary peer-host service quiescence.
The hashed audit verifies inactive selected units before traffic and
restoration of original active/inactive states, enablement and unit-file
hashes after collection. A remote fallback timer was armed before stop
and cancelled after verified restoration. No manual Kafka configuration
or topic-data edits were made. This is a dedicated VM workload window,
not proof of exclusive physical hypervisor resources.

## Declared Hardware

- SDK host: 4 logical CPUs, 15988 MiB RAM, Linux `5.15.0-191-generic`.
- PEER host: 4 logical CPUs, 5924 MiB RAM, Linux `6.8.0-149-generic`.

The private peer was constrained to one CPU at nice 0. Declared logical
CPUs do not establish dedicated physical-core performance.

## Observed Socket Profile

- SDK: `lan-bounded-v1`, effective send/receive buffers 425984 / 425984 bytes; RTO initial/minimum/maximum 300 / 200 / 1000 ms.
- PEER: `lan-bounded-v1`, effective send/receive buffers 425984 / 425984 bytes; RTO initial/minimum/maximum 300 / 200 / 1000 ms.

These are per-socket workload settings, not SDK defaults or a public-network
recommendation. Buffer requests may be clamped. The opt-in LAN profile
uses shorter recovery bounds and needs requalification on a different path.

## Runtime Observations

Retained 1802 timestamped observations; maximum sampling gap 1.004 s; cumulative GC pause 33083.882 ms.

Sample brackets may span adjacent seconds. Namespace counters and sampled queue maxima are not association-only traces or proof of stall causation.

## Application Runtime Profile

Observed worker minimum/maximum: 4 / 4.

Worker limits apply only to the workload application. The SDK library never
changes a consumer thread pool. Results are conditional on this declared
profile and do not establish an HTTP or mixed-service workload SLA.
