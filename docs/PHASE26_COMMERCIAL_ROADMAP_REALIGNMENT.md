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

## Unit 2 - External Peer Profile Model

`SigtranInteropPeerProfile` now records `SupportModel` so release evidence can distinguish maintained peer stacks, legacy references, operator-provided peers, and simulators.

The default profile is package-neutral:

- Stable id: `external-sigtran-sg`.
- Product label: maintained SIGTRAN peer.
- Transport: `SCTP/M3UA`.
- Support model: maintained peer stack.

`IsMaintainedCommercialCandidate` is true only for a maintained Signalling Gateway profile with M3UA transport. This keeps simulator and legacy evidence useful for development without allowing it to satisfy the commercial peer gate.

## Unit 3 - Maintained Peer Selection Policy

`SigtranMaintainedPeerSelectionPolicy` defines package-neutral criteria that a lab package must satisfy before it can back commercial external peer evidence:

- Maintained upstream or active distribution packaging.
- Current Linux support without legacy kernel requirements.
- Native SCTP support.
- M3UA ASP-to-SG lifecycle and DATA coverage.
- Retained PCAP, peer log, SDK trace, configuration, and comparison artifacts.
- License isolation from the SDK package.

The policy evaluates a `SigtranInteropPeerProfile` plus explicitly satisfied criterion ids. This keeps the package choice outside the SDK contract while still making the commercial gate deterministic.

## Unit 4 - External Peer Lab Environment Contract

`SigtranExternalPeerInteropEnvironment` now records the lab evidence contract:

- Linux runner.
- Native SCTP support.
- External SIGTRAN peer.
- Packet capture.
- SDK trace capture.
- Peer configuration capture.
- Peer log capture.
- Required tools such as `dotnet`, `tcpdump`, `tshark`, and SCTP tooling.
- Stable artifact root under `artifacts/external-peer`.

`CanProduceCommercialArtifacts` is true only when the environment can produce the artifact classes needed by commercial review. This keeps a simple transport smoke test separate from a commercial interoperability lab.

## Unit 5 - Digest Covered External Peer Artifacts

`SigtranExternalPeerInteropArtifactManifest` now separates three states:

- `IsComplete`: every required artifact kind is present.
- `AllArtifactsHaveDigests`: every retained artifact has a SHA-256 digest.
- `IsReviewReady`: the manifest is complete and digest-covered.

The required artifact kinds are packet capture, SDK trace, peer configuration, peer log, and comparison report. `SigtranExternalPeerInteropRunReport` and `SigtranExternalPeerInteropEvidenceRegistry` now expose commercial-review-ready evidence in addition to passing evidence.

## Unit 6 - External Peer Run Commands

`SigtranExternalPeerInteropCommandSet` now models a package-neutral lab command contract. The default commands use environment variables instead of embedding a peer package name:

- `SIGTRAN_EXTERNAL_PEER_ID`
- `SIGTRAN_EXTERNAL_PEER_PACKAGE`
- `SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT`

The command set requires external peer execution, packet capture, SDK trace capture, and comparison report generation. `SigtranExternalPeerInteropRunPlan` now includes the command set and is executable only when the environment, configuration, trace expectations, and commercial lab commands are all present.

## Unit 7 - Commercial Readiness Aggregation

`SigtranExternalPeerCommercialReadiness` aggregates maintained peer selection, lab environment capability, executable run plan, and commercial-review-ready evidence.

The current report remains blocked because no real maintained peer package has been selected and no passing digest-covered external peer evidence is retained. A commercial claim requires all of the following:

- Maintained peer selection policy passes.
- Lab environment can produce commercial artifacts.
- Run plan is executable.
- Evidence registry contains passing, digest-covered artifacts.

## Unit 8 - Commercial Release Gate Alignment

Commercial evidence and release execution readiness now use `externalPeerInteropVerified` and `external-peer-interop` instead of package-specific gate names.

`SigtranCommercialReleaseExecutionReadiness` now consumes `SigtranExternalPeerCommercialReadiness.CreateCurrent()` so the release decision is based on maintained peer selection, lab environment, run plan, and review-ready evidence. Retained legacy blocker notes remain evidence context, not public gate names.

## Unit 9 - Migration Notes And Public Labels

`docs/EXTERNAL_PEER_INTEROP_MIGRATION.md` records the naming rule for future contributors.

Public labels, CI variables, status capability names, and README roadmap text now use external peer terminology. Legacy OpenSS7/IPSS7 wording is retained only where the repository is documenting historical blocker evidence or reference material.

## Direction

The next units build a maintained peer selection policy, neutral lab environment contract, artifact contract, run plan, comparison contract, and readiness aggregation. The default lab may use a maintained package, but the SDK API remains independent of that package.
