# Phase 30 Maintained Peer Lab Runner Operationalization

Phase 30 turns the maintained peer lab runner materialization contracts into operational handoff contracts. The goal is to make a real runner reviewable around file creation, execution logs, command outcomes, artifact verification, provenance, failure handling, retry policy, and evidence packaging without claiming that planned artifacts are real lab evidence.

## Unit 1 - File Materialization

`SigtranMaintainedPeerLabRunnerFileMaterializationPlan` now describes the directories and input files a runner must create before executing peer traffic. It records:

- Required workspace directories.
- Environment and command script input files.
- Directory and input coverage checks.
- Shell script rendering for reviewable materialization.

The plan does not write files. It renders the deterministic operation that a real lab runner can execute or review.

## Unit 2 - Execution Log

`SigtranMaintainedPeerLabRunnerExecutionLog` now records timestamped runner events with package-neutral event kinds. It supports:

- Runner lifecycle events.
- Command start and completion events.
- Warning and error events.
- JSON Lines rendering.
- Markdown rendering.

The log contract gives real runner output a structured shape without implying that the SDK has executed peer traffic locally.

## Unit 3 - Command Outcomes

`SigtranMaintainedPeerLabRunnerCommandOutcomeReport` now aggregates execution logs into per-command outcomes. It records:

- Start and completion timestamps.
- Command-level error detection.
- Duration when timestamps are complete.
- Failed command kind inventory.
- Markdown rendering for retained run notes.

This makes a runner log reviewable by command before artifact verification starts.

## Unit 4 - Artifact Verification

`SigtranMaintainedPeerLabRunnerArtifactVerificationReport` now cross-checks retained runner artifacts against digest evidence. It records:

- Per-artifact retained state.
- Digest presence and SHA-256 validity.
- Missing required artifact paths.
- Missing retained artifact digest paths.
- Invalid retained artifact digest paths.
- Markdown rendering for operator review.

This keeps artifact handoff separate from execution status: a command can pass while the artifact package still fails verification.

## Unit 5 - Runner Provenance

`SigtranMaintainedPeerLabRunnerProvenanceReport` now records the runner evidence source identity. It includes:

- Run id.
- SDK name and version.
- Source repository and commit.
- Runner host identity.
- Workflow or runner plan name.
- Retained artifact root.
- UTC generation timestamp.
- Markdown rendering for retained review notes.

This report makes evidence packages traceable to source and host identity while keeping real lab execution evidence as a separate requirement.

## Unit 6 - Failure Classification

`SigtranMaintainedPeerLabRunnerFailureReport` now classifies runner blockers into stable categories. It covers:

- Preflight failures.
- Command execution failures.
- Artifact retention failures.
- Digest verification failures.
- Provenance failures.
- Comparison failures.
- Run report failures.
- Markdown rendering for failure triage.

This gives operator and CI handoff a deterministic diagnosis surface when a maintained peer run cannot be promoted.

## Unit 7 - Retry Policy

`SigtranMaintainedPeerLabRunnerRetryPolicy` now defines which classified failures can be retried. It records:

- Failure-kind retry rules.
- Maximum attempt counts.
- Retry delays.
- Retryable and non-retryable failure inventory.
- Markdown rendering for runner control flow.

The default policy treats command and selected comparison failures as potentially transient, while preflight, artifact, digest, provenance, and run-report failures require operator or environment correction.

## Unit 8 - Evidence Package Manifest

`SigtranMaintainedPeerLabRunnerEvidencePackageManifest` now gathers verified runner outputs into a reviewable handoff package. It records:

- Retained artifact package items.
- Digest manifest package item.
- Provenance report package item.
- Failure report package item.
- Package readiness gates.
- Markdown rendering for evidence handoff.

The package manifest only becomes ready when artifacts, digest coverage, provenance, comparison handoff, and failure classification are all clean.

## Unit 9 - Operator Handoff

`SigtranMaintainedPeerLabRunnerOperatorHandoffReport` now converts package and retry state into an operator-facing decision. It records:

- Operator review readiness.
- Commercial promotion readiness.
- Recommended action.
- Package readiness.
- Retry readiness.
- Markdown rendering for handoff notes.

The handoff recommends evidence promotion only when the package is ready, retry only when failures are transient, and blocker correction when evidence cannot be promoted or retried.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
