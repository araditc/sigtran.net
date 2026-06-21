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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
