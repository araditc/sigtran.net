# Phase 28 Maintained Peer Lab Automation

Phase 28 turns the maintained external peer lab foundation into automation and evidence handoff contracts. The goal is to prepare deterministic lab inputs and outputs without pretending that planned automation is the same as real retained evidence.

## Unit 1 - Run Manifest

`SigtranMaintainedPeerLabRunManifest` now aggregates the package-neutral maintained peer lab contracts into one executable manifest:

- Peer binding.
- Validated lab configuration.
- Retained artifact plan.
- Ordered command plan.
- Expected traffic vectors.
- Manual self-hosted CI policy.

The manifest is executable only when every foundation contract is present and internally valid. It does not claim commercial evidence readiness until a real lab run produces retained, digest-covered artifacts.

## Unit 2 - Environment File Renderer

`SigtranMaintainedPeerLabEnvironmentFiles` now renders a deterministic shell-compatible environment file from the run manifest. The rendered file includes:

- Peer binding variables.
- Local and remote SCTP endpoint values.
- M3UA routing context and traffic mode.
- SS7 OPC, DPC, network indicator, and service indicator.
- Artifact root and run id.

This gives real lab scripts a stable input file while keeping package-specific values in configuration rather than public type names.

## Unit 3 - Artifact Digest Manifest

`SigtranMaintainedPeerLabArtifactDigestManifest` now records SHA-256 digest entries for every planned retained artifact. Handoff is ready only when:

- Every required planned artifact has a digest entry.
- Every digest is a valid SHA-256 hex value.
- The digest manifest can be converted into retained evidence artifacts for promotion gates.

The manifest does not generate digests itself. Real lab automation must calculate the digests from retained files and pass them into this contract.

## Unit 4 - Command Script Renderer

`SigtranMaintainedPeerLabCommandScripts` now renders the ordered command plan as a shell script with:

- `#!/usr/bin/env bash`
- `set -euo pipefail`
- A sourced environment file.
- The ordered prepare, capture, peer, SDK, compare, and collect commands.

The renderer gives operators a deterministic script body while still allowing local lab automation to map package-neutral runner commands to site-specific scripts.

## Unit 5 - Comparison Report

`SigtranMaintainedPeerLabComparisonReports` now compares observed lab messages against the expected traffic vectors and renders a retained Markdown report. The report records:

- Run id.
- Comparison artifact path.
- Expected and actual message counts.
- Pass/fail state.
- Ordered mismatch details.

This report is designed to become the retained comparison artifact referenced by the evidence promotion gate.

## Unit 6 - Run Report

`SigtranMaintainedPeerLabRunReport` now records the execution outcome for each maintained peer lab command, the comparison result, and the retained run report path. The report records:

- Ordered command step status values.
- Optional step duration and retained log path metadata.
- Comparison pass/fail state.
- Overall run pass/fail state.
- Markdown output suitable for retention as the run report artifact.

This keeps lab execution evidence explicit: a passing comparison alone is not enough unless every command step also passed.

## Unit 7 - Evidence Bundle Handoff

`SigtranMaintainedPeerLabEvidenceBundle` now joins the executable manifest, rendered environment file, command script, comparison report, run report, and digest manifest into one handoff object. The bundle is handoff-ready only when:

- Every component references the same run id.
- The manifest is executable.
- The rendered script covers the command plan.
- Comparison and run reports are passing.
- Retained artifacts are digest-covered with valid SHA-256 values.

The bundle can produce the maintained peer lab evidence promotion report, but invalid digest manifests are intentionally blocked instead of being treated as commercial evidence.

## Unit 8 - Workflow Template

`SigtranMaintainedPeerLabWorkflowTemplate` now renders a guarded GitHub Actions workflow template for maintained peer lab execution. The template is intentionally:

- Manual-dispatch only.
- Self-hosted Linux runner only.
- Read-only for repository contents.
- Artifact-upload enabled for retained PCAP, logs, config, traces, comparison reports, and run reports.
- Not safe for default pull request CI.

The SDK exposes this as a renderable contract instead of adding an always-active workflow file, so maintainers can choose when a real lab runner is available.

## Unit 9 - Commercial Readiness Bridge

`SigtranMaintainedPeerLabCommercialBridge` now evaluates whether a maintained peer lab evidence bundle can support a commercial readiness claim. The bridge checks:

- Workflow template readiness.
- Bundle run-id consistency.
- Handoff readiness.
- Evidence promotion readiness.
- Maintained peer lab status readiness.

When evidence is incomplete or invalid, the report returns blocker identifiers instead of silently treating the bundle as production evidence.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
