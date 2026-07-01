# Phase 17 Native SCTP Lab Verification

Phase 17 adds the native SCTP lab verification framework for SIGTRAN.NET.

This phase prepares the SDK to capture real Linux SCTP verification evidence. It does not claim native SCTP production verification by itself because this Windows development environment cannot prove Linux kernel SCTP behavior or peer traffic.

## Lab Scenarios

`SigtranNativeSctpLabScenarios.GetScenarios()` defines the native SCTP verification scenarios:

- Linux SCTP platform probe.
- Linux SCTP loopback association.
- Linux SCTP multi-stream payload.
- Linux SCTP peer M3UA traffic.

All scenarios require Linux. The peer traffic scenario also requires an external peer stack.

## Artifact Manifest

`SigtranNativeSctpLabArtifactManifest` tracks packet captures, SDK traces, platform reports, peer configuration, peer logs, and comparison reports.

The manifest must satisfy the required artifact names for each scenario before the run can be promoted as evidence.

## Run Plan

`SigtranNativeSctpLabRunPlans.CreateDefault()` defines the default verification plan.

The plan requires Linux SCTP capabilities and packet capture. It includes local loopback verification and external peer traffic verification.

## Command Set

`SigtranNativeSctpLabCommands.CreateDefault()` defines the command contract for a Linux runner:

- Capture kernel/platform metadata.
- Load or inspect SCTP kernel support.
- Build the solution.
- Run the tests with `SIGTRAN_NATIVE_SCTP_LAB=1`.

The command set assumes Linux and `lksctp-tools` or equivalent SCTP tooling.

## Run Reports

`SigtranNativeSctpLabRunReport` records scenario, manifest, status, kernel description, start time, and completion time.

Only passed reports with complete manifests count as passing evidence.

## Evidence Registry

`SigtranNativeSctpLabEvidenceRegistry` stores native SCTP lab evidence.

Production verification requires passing evidence for every required native SCTP lab scenario.

## Readiness

`SigtranNativeSctpLabReadiness.GetReport()` separates lab foundation readiness from production verification.

The current foundation is ready. Production readiness remains false until complete passing Linux SCTP lab evidence is captured.

## CI Profile

`SigtranNativeSctpLabCi.CreateDefault()` defines an opt-in Linux-only lab profile.

The profile is enabled by `SIGTRAN_NATIVE_SCTP_LAB` and stores artifacts under `SIGTRAN_NATIVE_SCTP_ARTIFACT_ROOT`.

## Production Gate

`SigtranNativeSctpLabProductionGate.Evaluate()` makes native SCTP production verification explicit.

The gate remains closed until complete passing evidence exists.

## Phase Status

`SigtranNativeSctpLabVerificationStatus.Describe()` summarizes the completed Phase 17 units and separates lab framework readiness from native SCTP production verification.
