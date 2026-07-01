# Phase 29 Reference Peer Lab Runner Materialization

Phase 29 turns the reference peer lab automation contracts into runner materialization contracts. The goal is to make a real lab runner deterministic about directories, inputs, commands, retained outputs, and handoff checks without pretending that generated plans are the same as real peer evidence.

## Unit 1 - Runner Workspace

`SigtranReferencePeerLabRunnerWorkspace` now describes the filesystem workspace used by a reference peer lab runner. It records:

- Runner workspace root.
- Script root.
- Artifact root.
- Config, log, PCAP, trace, comparison, and report directories.
- Required directory list.
- Materialization readiness based on executable manifests and deterministic artifact paths.

This gives lab automation a single path contract before rendering inputs or starting peer traffic.

## Unit 2 - Runner Input Bundle

`SigtranReferencePeerLabRunnerInputBundle` now joins the runner workspace, rendered environment file, and command script into the deterministic input files a lab runner should materialize. It records:

- Environment file path.
- Command script path.
- Rendered environment content.
- Rendered command script content.
- Materialization readiness based on run-id consistency and command-plan coverage.

The bundle still does not write files or claim evidence. It only defines the exact files a real runner should create before execution.

## Unit 3 - Output Artifact Materialization

`SigtranReferencePeerLabRunnerArtifactMaterializationPlan` now maps every expected retained artifact to the command expected to produce it. The plan checks:

- Required artifact coverage.
- Output paths under the artifact root.
- Producer command coverage.
- Required output path inventory.

This gives the runner a deterministic post-execution checklist before evidence collection and digest generation.

## Unit 4 - Runner Preflight

`SigtranReferencePeerLabRunnerPreflight` now evaluates the checks that must pass before real reference peer lab execution starts:

- Workspace materialization readiness.
- Input bundle materialization readiness.
- Output artifact materialization readiness.
- Configuration validity.
- Host prerequisite readiness.

The preflight report returns stable failed check identifiers so operators can fix the lab environment before opening SCTP traffic.

## Unit 5 - Command Manifest

`SigtranReferencePeerLabRunnerCommandManifest` now turns the command plan into an execution manifest. It records:

- One-based command sequence.
- Command kind, name, and command line.
- Expected output paths per command.
- Preflight dependency.
- Markdown rendering for retained run notes.

The manifest is execution-ready only when inputs, outputs, preflight checks, command sequencing, and expected artifact mappings are all valid.

## Unit 6 - Evidence Collection

`SigtranReferencePeerLabRunnerEvidenceCollection` now records which expected runner artifacts were retained after execution. It tracks:

- Artifact kind.
- Artifact path.
- Required/optional status.
- Retained/missing status.
- Missing required artifact paths.

The collection can convert retained artifacts into evidence artifacts, but it intentionally does not add digest coverage. Digest generation is a separate step.

## Unit 7 - Digest Generation

`SigtranReferencePeerLabRunnerDigestReport` now validates calculated SHA-256 values for retained artifacts and produces the reference peer lab digest manifest. It checks:

- Retained required artifact coverage.
- Missing digest paths.
- SHA-256 format validity through the digest manifest.
- Handoff readiness for digest-covered artifacts.

The SDK still does not invent digests. A real runner must calculate them from retained files and pass the values into this contract.

## Unit 8 - Comparison Handoff

`SigtranReferencePeerLabRunnerComparisonHandoff` now bridges runner output into the reference peer lab evidence bundle. It combines:

- Runner input bundle.
- Digest report.
- Comparison report.
- Run report.

The handoff is ready only when run ids are consistent, digest coverage is complete, comparison passed, and the run report passed. It can then create the evidence bundle used by commercial readiness gates.

## Unit 9 - Workflow Readiness

`SigtranReferencePeerLabRunnerWorkflowReadiness` now evaluates whether the manual self-hosted workflow template can safely execute a materialized runner command manifest. It checks:

- Manual dispatch and self-hosted Linux policy.
- Default CI safety.
- Runner materialization readiness.
- Artifact upload pattern coverage.

This keeps workflow availability separate from runner readiness and prevents planned YAML from being mistaken for a passing lab execution.

## Unit 10 - Runner Status

`SigtranReferencePeerLabRunnerStatus` now summarizes the completed runner materialization foundation. It reports ten completed capabilities:

- Runner workspace materialization.
- Runner input bundle.
- Artifact output materialization.
- Runner preflight.
- Runner command manifest.
- Runner evidence collection.
- Runner digest generation.
- Runner comparison handoff.
- Runner workflow readiness.
- Runner status reporting.

The foundation report is ready, but commercial readiness remains blocked until a real reference peer run produces retained runner evidence.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
