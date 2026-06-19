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

## Artifact Manifests

`SigtranInteropLabArtifactManifest` records the artifacts captured for one lab scenario.

```csharp
SigtranInteropLabArtifactManifest manifest = new("openss7-m3ua-asp-to-sg");
manifest.Add(new SigtranInteropLabArtifact(
    SigtranInteropLabArtifactKind.PacketCapture,
    "artifacts/openss7/pcap/m3ua-asp.pcapng"));
```

A manifest satisfies a scenario only when every required artifact name is present in the captured artifact paths.

## Run Reports

`SigtranInteropLabRunReport` records the scenario, artifact manifest, status, start time, completion time, and operator notes for one lab run.

`HasPassingEvidence` is true only when the run status is `Passed` and the manifest satisfies the scenario artifact requirements.
