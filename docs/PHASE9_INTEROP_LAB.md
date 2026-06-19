# Phase 9 Interoperability Lab

Phase 9 captures real interoperability evidence from native SCTP and peer-stack lab runs.

## Lab Scenarios

`SigtranInteropLabScenarios.GetScenarios()` exposes the required lab scenario catalog.

| Id | Peer | Purpose |
| --- | --- | --- |
| `linux-native-sctp-loopback` | `linux-kernel-sctp` | Validate native SCTP loopback socket, connect, accept, send, receive, and health |
| `openss7-m3ua-asp-to-sg` | `openss7-ipss7` | Validate M3UA ASP-to-SG lifecycle and DATA against OpenSS7/IPSS7 |
| `map-sms-trace-comparison` | `operator-or-simulator-peer` | Validate MAP SMS traces against a real peer or approved simulator profile |

Every scenario defines required artifacts such as PCAP captures, SDK traces, peer configuration, peer logs, and comparison reports.
