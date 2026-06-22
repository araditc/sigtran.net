# Phase 36 Commercial Evidence Readiness Lockdown

Phase 36 locks the evidence-readiness prerequisites required before real lab execution and RC publication. The phase does not manufacture passing evidence. It makes the release target, secrets, artifact roots, checklists, preflight, and go/no-go decision explicit and testable before evidence-producing work begins.

The public APIs use domain names such as `SigtranCommercialReleaseTargetLocks`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Release Target Lock

`SigtranCommercialReleaseTargetLocks` defines the immutable release target used to bind retained evidence:

- Release-candidate package version.
- Pinned source commit.
- Source branch or tag reference.
- Release channel.
- Versioned retained artifact root.

The target is locked only when it is a release candidate, the commit is pinned, the channel is supported, and the artifact root is retained under `artifacts/` with the version in its path.

## Unit 2 - Required Secrets Readiness

`SigtranCommercialReleaseSecrets` defines the protected secret contract without exposing secret values:

- `NUGET_API_KEY` for package publication.
- `SIGNING_CERTIFICATE` for trusted package signing material.
- `SIGNING_CERTIFICATE_PASSWORD` for signing material unlock.
- `PROVENANCE_ATTESTATION_TOKEN` for provenance attestation upload when OIDC is not used.

The readiness evaluator accepts only secret names and reports missing requirements. This keeps release checks auditable while avoiding secret-value logging in build, test, and release output.

## Unit 3 - Evidence Artifact Retention Map

`SigtranCommercialEvidenceRetentionMaps` defines the retained artifact roots required for commercial evidence capture:

- Native SCTP evidence.
- External peer interoperability evidence.
- Protocol interoperability evidence.
- Performance and resilience evidence.
- Supply-chain evidence.
- Public API evidence.
- Release workflow evidence.
- Publication dossier evidence.

Every path must live under the release target artifact root, keep artifacts for at least one year, and require digest coverage. Floating paths such as `artifacts/latest/...` are intentionally rejected because they cannot prove which release target produced the evidence.

## Unit 4 - Commercial Evidence Checklist

`SigtranCommercialEvidenceChecklists` defines the mandatory checklist for evidence-producing execution:

- Native SCTP packet capture.
- External peer log, SDK trace, and configuration.
- Protocol comparison report.
- Final SBOM, signing verification, and provenance attestation.
- Peer benchmark report.
- Public API diff.
- Release workflow run evidence.
- Release notes, migration notes, and final commercial readiness report.

The checklist validates unique item identifiers, mandatory coverage for every retention area, and mandatory coverage for every essential artifact kind. This is a pre-execution checklist, not a claim that the artifacts already exist.

## Unit 5 - Release Preflight Inputs

`SigtranCommercialReleasePreflightChecks` aggregates the lockdown inputs before evidence-producing work starts:

- Release target lock.
- Protected release secret readiness.
- Evidence retention map.
- Commercial evidence checklist.

The preflight report returns stable blocker identifiers for unlocked targets, missing secrets, incomplete retention maps, target mismatches between the release lock and retention map, and incomplete checklists. A passing preflight means the execution inputs are ready; it does not mean lab evidence or publication evidence already passed.

## Unit 6 - Protected Release Environment Profile

`SigtranProtectedReleaseEnvironments` defines the protected release environments expected by the release workflow:

- `release-dry-run` cannot publish packages.
- `release-prerelease` can publish RC packages only from protected refs with reviewer approval.
- `release-stable` can publish stable packages only from protected refs with stronger reviewer approval.

The profile validates channel coverage, dry-run non-publication, stable publication protection, and per-channel approval strength. This keeps publish rights separated from ordinary build/test/pack execution.

## Unit 7 - Evidence Dossier Handoff Plan

`SigtranEvidenceDossierHandoffs` maps checklist items into a reviewer-ready dossier:

- Every checklist item receives an expected retained path under the release target root.
- Telecom protocol, supply-chain, performance, release management, and security reviewer roles are represented.
- Every item requires digest verification.
- Trace-bearing evidence requires redaction review.
- The handoff requires a digest manifest and comparison summary.

The handoff plan is still a readiness artifact. It defines how the evidence will be reviewed once real retained artifacts exist.

## Unit 8 - Commercial Go/No-Go Gate

`SigtranCommercialGoNoGoGates` separates three decisions:

- `NoGo` when lockdown inputs are incomplete.
- `EvidenceExecutionOnly` when lockdown inputs are ready but real commercial evidence is still incomplete.
- `ReleaseCandidate` or `Stable` only when retained commercial release evidence is complete.

This intentionally prevents the SDK from treating readiness foundation as publication evidence. With the current retained evidence state, the gate can allow evidence-producing execution but continues to block RC and stable publication until the commercial evidence dossier is complete.

## Unit 9 - Readiness Lockdown Status

`SigtranCommercialEvidenceReadinessLockdownStatus` exposes domain-based status reporting for the completed readiness-lockdown capabilities:

- Release target lock.
- Release secret readiness.
- Evidence retention map.
- Commercial evidence checklist.
- Release preflight.
- Protected release environments.
- Evidence dossier handoff.
- Commercial go/no-go gate.
- Documentation and status reporting.

The current status is ready to start evidence-producing execution, but it continues to block RC and stable publication because commercial release evidence is incomplete.

## Unit 10 - Final Validation

The readiness lockdown is complete when:

- Status reporting includes final validation.
- Publication blockers no longer include validation-pending work.
- Build, test, and package validation pass.
- README, roadmap, phase index, and phase summary all describe the same commercial position.

Phase 36 completion does not mean commercial publication is ready. It means the release target, secrets, retention, checklist, preflight, protected environments, dossier handoff, go/no-go decisioning, status reporting, and validation are all ready to support the real evidence-producing work.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
