# Phase 18 External Peer Interop Execution

Phase 18 adds the external SIGTRAN peer interoperability execution framework for SIGTRAN.NET.

The earlier lab template identified the need for a real M3UA ASP-to-SG peer. This phase turns that template into an executable contract with environment requirements, ASP-to-SG configuration, trace expectations, artifact capture, run reports, evidence registry, readiness, and CI metadata.

## Environment

`SigtranExternalPeerInteropEnvironments.CreateDefault()` defines the external peer lab environment.

The environment requires Linux, native SCTP, a configured SIGTRAN peer, and packet capture.

## Configuration

`SigtranExternalPeerInteropConfigurations.CreateDefaultAspToSg()` defines default ASP-to-SG configuration values:

- Association name.
- Application server name.
- Routing Context.
- Traffic mode.

These values are planning defaults and must be adapted to the actual peer host configuration.

## Trace Expectations

`SigtranExternalPeerInteropTraceExpectationsCatalog.CreateAspToSg()` defines the expected M3UA message sequence.

The sequence covers ASP Up, ASP Active, Heartbeat, DATA, ASP Inactive, and ASP Down.

## Artifacts

`SigtranExternalPeerInteropArtifactManifest` requires:

- Packet capture.
- SDK trace.
- Peer configuration.
- Peer log.
- Comparison report.

All artifact kinds must be present before a run can count as passing evidence.

## Run Plan

`SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg()` combines the external peer template, environment, configuration, and trace expectations.

The plan is executable when all prerequisite contracts are present. Executable does not mean verified; verification requires real captured artifacts.

## Commands

`SigtranExternalPeerInteropCommands.CreateDefault()` defines the command contract for a prepared lab host.

The commands assume peer configuration tooling, packet capture, and an opt-in test run with `SIGTRAN_INTEROP_PEER=external-sigtran-peer`.

## Run Reports

`SigtranExternalPeerInteropRunReport` records the plan, artifact manifest, status, start time, and completion time.

Only passed runs with a complete artifact manifest count as passing evidence.

## Evidence

`SigtranExternalPeerInteropEvidence.CreateCurrentRegistry()` currently returns an empty registry.

Real external peer artifacts must be captured and promoted before commercial readiness can use this evidence.

## Readiness

`SigtranExternalPeerInteropReadiness.GetReport()` separates execution foundation readiness from verified interop evidence.

The execution foundation is ready. Verification remains false until real passing evidence is retained.

## CI Profile

`SigtranExternalPeerInteropCi.CreateDefault()` defines an opt-in external peer CI profile.

The profile is enabled with `SIGTRAN_EXTERNAL_PEER_INTEROP` and stores artifacts under `SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT`.

## Status

`SigtranExternalPeerInteropStatus.Describe()` summarizes the external peer execution foundation and keeps verified status false until passing evidence exists.
