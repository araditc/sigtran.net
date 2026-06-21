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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
