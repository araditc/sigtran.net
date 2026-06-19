# Phase 18 OpenSS7/IPSS7 Interop Execution

Phase 18 adds the OpenSS7/IPSS7 interoperability execution framework for SIGTRAN.NET.

The earlier lab template identified OpenSS7/IPSS7 as a peer reference. This phase turns that template into an executable contract with environment requirements, ASP-to-SG configuration, trace expectations, artifact capture, run reports, evidence registry, readiness, and CI metadata.

## Environment

`SigtranOpenSs7InteropEnvironments.CreateDefault()` defines the OpenSS7/IPSS7 lab environment.

The environment requires Linux, native SCTP, an OpenSS7/IPSS7 peer, and packet capture.

## Configuration

`SigtranOpenSs7InteropConfigurations.CreateDefaultAspToSg()` defines default ASP-to-SG configuration values:

- Association name.
- Application server name.
- Routing Context.
- Traffic mode.

These values are planning defaults and must be adapted to the actual OpenSS7/IPSS7 host configuration.

## Trace Expectations

`SigtranOpenSs7InteropTraceExpectationsCatalog.CreateAspToSg()` defines the expected M3UA message sequence.

The sequence covers ASP Up, ASP Active, Heartbeat, DATA, ASP Inactive, and ASP Down.

## Artifacts

`SigtranOpenSs7InteropArtifactManifest` requires:

- Packet capture.
- SDK trace.
- Peer configuration.
- Peer log.
- Comparison report.

All artifact kinds must be present before a run can count as passing evidence.

## Run Plan

`SigtranOpenSs7InteropRunPlans.CreateDefaultAspToSg()` combines the OpenSS7/IPSS7 template, environment, configuration, and trace expectations.

The plan is executable when all prerequisite contracts are present. Executable does not mean verified; verification requires real captured artifacts.

## Commands

`SigtranOpenSs7InteropCommands.CreateDefault()` defines the command contract for a prepared lab host.

The commands assume OpenSS7/IPSS7 configuration tooling, packet capture, and an opt-in test run with `SIGTRAN_INTEROP_PEER=openss7-ipss7`.

## Run Reports

`SigtranOpenSs7InteropRunReport` records the plan, artifact manifest, status, start time, and completion time.

Only passed runs with a complete artifact manifest count as passing evidence.

## Evidence

`SigtranOpenSs7InteropEvidence.CreateCurrentRegistry()` currently returns an empty registry.

Real OpenSS7/IPSS7 artifacts must be captured and promoted before commercial readiness can use this evidence.

## Readiness

`SigtranOpenSs7InteropReadiness.GetReport()` separates execution foundation readiness from verified interop evidence.

The execution foundation is ready. Verification remains false until real passing evidence is retained.

## CI Profile

`SigtranOpenSs7InteropCi.CreateDefault()` defines an opt-in OpenSS7/IPSS7 CI profile.

The profile is enabled with `SIGTRAN_OPENSS7_INTEROP` and stores artifacts under `SIGTRAN_OPENSS7_ARTIFACT_ROOT`.

## Status

`SigtranOpenSs7InteropStatus.Describe()` summarizes the OpenSS7/IPSS7 execution foundation and keeps verified status false until passing evidence exists.
