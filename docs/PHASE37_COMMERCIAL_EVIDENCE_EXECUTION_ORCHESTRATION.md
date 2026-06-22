# Phase 37 Commercial Evidence Execution Orchestration

Phase 37 turns the readiness lockdown from Phase 36 into a governed execution package for real evidence-producing work. It does not create fake passing artifacts. It defines the run identity, stages, commands, environment contracts, artifact collection, verification, blocker handling, retry/resume behavior, and status reporting needed to run a real commercial evidence cycle.

Public APIs use domain names such as `SigtranCommercialEvidenceExecutionRuns`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Evidence Execution Run Identity

`SigtranCommercialEvidenceExecutionRuns` creates a release-candidate evidence execution run bound to:

- Locked release target.
- Stable run identifier.
- Operator or automation identity.
- UTC start time.
- Run-scoped artifact root under the target artifact root.

Floating roots such as `artifacts/latest/...` are rejected because retained evidence must prove exactly which package version, source commit, and run produced it.

## Unit 2 - Evidence Execution Stage Catalog

`SigtranCommercialEvidenceExecutionStages` defines the required execution stages:

- Readiness preflight.
- Native SCTP lab.
- External peer interoperability.
- Protocol validation.
- Performance benchmark.
- Supply-chain evidence.
- Release workflow dry-run.
- Dossier assembly.

The stage catalog validates required stage coverage, unique stage identifiers and order values, and run-scoped artifact roots for every stage.

## Unit 3 - Operator Command Plan

`SigtranCommercialEvidenceExecutionCommands` maps each execution stage to one operator-facing command:

- Commands follow the deterministic stage order.
- Every command carries the execution run identifier.
- Evidence-producing stages are marked as artifact-producing.
- Supply-chain, release workflow, and dossier assembly commands require protected approval.

The command plan is a runbook contract. It defines what should be run and how artifacts stay tied to the run; it does not execute the commands by itself.

## Unit 4 - Execution Environment Contract

`SigtranCommercialEvidenceExecutionEnvironments` defines the variables required by a governed evidence execution:

- Run identity: `SIGTRAN_RUN_ID`, `SIGTRAN_ARTIFACT_ROOT`, `SIGTRAN_RELEASE_VERSION`, and `SIGTRAN_SOURCE_COMMIT`.
- Lab inputs: `SIGTRAN_PEER_CONFIG` and `SIGTRAN_CAPTURE_INTERFACE`.
- Protected secrets: `NUGET_API_KEY`, `SIGNING_CERTIFICATE`, `SIGNING_CERTIFICATE_PASSWORD`, and `PROVENANCE_ATTESTATION_TOKEN`.

The contract validates run identity values, reports missing or mismatched variables, and prevents fixed secret values from being stored in the contract.

## Unit 5 - Artifact Collection Manifest

`SigtranCommercialEvidenceExecutionArtifacts` defines the retained outputs expected from the run:

- Packet capture.
- Peer logs, SDK traces, and configuration.
- Comparison report.
- SBOM, signing verification, provenance attestation, and public API diff.
- Benchmark report.
- Release workflow run record.
- Publication notes and commercial readiness report.

The manifest validates checklist coverage, known stage ownership, unique paths, and stage-scoped artifact roots.

## Unit 6 - Digest And Redaction Verification Plan

`SigtranCommercialEvidenceExecutionVerifications` defines review requirements for each retained artifact:

- Every artifact requires digest verification.
- Packet captures, logs, SDK traces, configurations, comparison reports, and benchmark reports require redaction review.
- Every verification item must map back to an artifact manifest path.

This plan keeps evidence review auditable and prevents sensitive telecom traces from entering the public dossier without redaction review.

## Unit 7 - Execution Blocker Classifier

`SigtranCommercialEvidenceExecutionBlockers` classifies execution failures into stable categories:

- Readiness preflight.
- Environment.
- Command failure.
- Native SCTP.
- External peer.
- Artifact retention.
- Digest verification.
- Redaction review.
- Protected approval.

Each blocker declares whether it is retryable after correction. Unknown blockers are not retried automatically and require manual triage.

## Unit 8 - Retry And Resume Policy

`SigtranCommercialEvidenceExecutionRetryPolicies` maps blocker kinds to retry decisions:

- Environment, command, external peer, artifact, digest, and redaction blockers can be retried within bounded attempt counts.
- Native SCTP host blockers, protected approval blockers, unknown blockers, and failed preflight blockers require manual correction.
- Retry decisions include the stage identifier where execution should resume.

The policy prevents endless retries and keeps host capability, approval, and unknown failures under operator control.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
