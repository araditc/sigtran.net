# Phase 41 Approved Commercial Run Publication Handoff

Phase 41 defines the governed approval path after filesystem-backed evidence has been verified. It does not create commercial evidence by itself. It records when a real evidence run is ready for approval, how reviewers approve it, and how the approved result is handed to release publication gates.

Public APIs use domain names such as `SigtranCommercialEvidenceApprovedRunTarget`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Approved Run Target

`SigtranCommercialEvidenceApprovedRunTarget` binds a filesystem-backed promotion execution to a reviewable commercial evidence run:

- Stable run id.
- Package version and source commit.
- Retained artifact root.
- Operator identity.
- UTC start and completion times.
- Ready filesystem-backed promotion execution.

This unit establishes the identity that later approval, audit, and publication handoff artifacts reference.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
