# Phase 26 Commercial Roadmap Realignment

Phase 26 realigns commercial interoperability around package-neutral external SIGTRAN peer evidence.

The SDK source API must not depend on a specific peer package name. Product names such as OpenSS7/IPSS7 or Osmocom may appear in retained lab notes, configuration examples, and artifact evidence, but public SDK contracts use domain names such as external peer, maintained peer, interoperability evidence, and commercial readiness.

## Unit 1 - Package Neutral Source Contracts

The existing interop execution contracts were renamed from package-specific source names to package-neutral source names.

Renamed source surfaces:

- `SigtranExternalPeerInteropEnvironment`
- `SigtranExternalPeerInteropConfiguration`
- `SigtranExternalPeerInteropTraceExpectations`
- `SigtranExternalPeerInteropArtifactManifest`
- `SigtranExternalPeerInteropRunPlan`
- `SigtranExternalPeerInteropCommandSet`
- `SigtranExternalPeerInteropRunReport`
- `SigtranExternalPeerInteropEvidenceRegistry`
- `SigtranExternalPeerInteropReadiness`
- `SigtranExternalPeerInteropStatus`

Commercial evidence now uses `ExternalPeerInterop` as the evidence area. The retained OpenSS7/IPSS7 failure remains valid blocker evidence, but it is represented as one external peer trial rather than as the SDK's permanent interoperability gate.

## Direction

The next units build a maintained peer selection policy, neutral lab environment contract, artifact contract, run plan, comparison contract, and readiness aggregation. The default lab may use a maintained package, but the SDK API remains independent of that package.
