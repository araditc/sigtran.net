# Phase 27 Sigtran.NET Branding

Phase 27 starts by making `Sigtran.NET` the canonical SDK identity across source, packaging, documentation, workflows, and release evidence paths.

## Unit 1 - Canonical SDK Name

The project now uses `Sigtran.NET` consistently for:

- Source namespaces.
- Solution, project, and test project paths.
- NuGet package id and generated package names.
- README examples.
- CI and release workflow commands.
- Engineering scripts for SBOM, provenance, signing, benchmarks, and API baseline generation.
- Governance and evidence artifact names.

The repository folder itself may still be checked out under any local directory name, but SDK contracts and committed project paths use `Sigtran.NET`.

## Validation

This unit is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

The rename must also pass a repository sweep that rejects previous non-canonical SDK spellings in tracked source, docs, scripts, and workflows.

## Next Units

The maintained external peer lab continues in [Phase 27 Maintained External Peer Lab](PHASE27_MAINTAINED_EXTERNAL_PEER_LAB.md).
