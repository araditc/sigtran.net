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

## OpenSS7/IPSS7 Template

`SigtranInteropPeerProfiles.CreateOpenSs7M3uaAspToSgTemplate()` creates the repeatable M3UA ASP-to-SG lab template for the OpenSS7/IPSS7 peer.

The template references `http://www.openss7.org/ipss7_man.html`, expects SCTP/M3UA transport, and captures the ordered lifecycle from `ASPUP` through `ASPDN_ACK`.

## Trace Comparison

`SigtranTraceComparison.Compare()` compares ordered expected message names with actual trace message names and returns a deterministic `SigtranTraceComparisonReport`.

Use this report to attach a machine-readable pass/fail summary beside PCAP, SDK trace, and peer-log artifacts.

## Evidence Promotion

`SigtranInteropEvidencePromotion.Promote()` converts a passing lab run with a complete artifact manifest into a `SigtranInteropEvidenceItem`.

Failed runs, pending runs, or runs with incomplete manifests are rejected and must not unlock commercial readiness.

## Opt-In CI Profile

`SigtranInteropLabCiProfiles.CreateDefault()` defines the external lab variables and verification commands.

Required variables:

- `SIGTRAN_INTEROP_LAB`
- `SIGTRAN_INTEROP_LAB_ARTIFACT_ROOT`
- `SIGTRAN_INTEROP_PEER`

Native SCTP lab runs can additionally use `SIGTRAN_NATIVE_SCTP_LAB`.

## Readiness Report

`SigtranInteropLabReadiness.GetReport()` separates lab foundation readiness from production readiness.

Foundation readiness is true when the SDK has scenario catalog, manifests, run reports, peer profiles, trace comparison, evidence promotion, and CI profile support.

Production readiness remains false until passing external evidence is captured and registered.

## Commercial Gate

`SigtranCommercialReadiness.GetReport()` uses the Phase 9 lab production gate for external interoperability evidence.

This keeps commercial readiness blocked until the lab foundation is ready and at least one passing external evidence set is promoted.
