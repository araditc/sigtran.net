# Phase 40 Commercial Evidence Filesystem Execution

Phase 40 turns the file verification contracts from Phase 39 into filesystem-backed execution helpers. It still does not manufacture commercial evidence. It reads retained files from disk, computes real SHA-256 digests, and feeds the existing verification, retention, sealing, attachment, and promotion-gate contracts.

Public APIs use domain names such as `SigtranCommercialEvidenceFileSystemObserver`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Filesystem Observation

`SigtranCommercialEvidenceFileSystemObserver` observes a promotion handoff item against the local filesystem:

- Reads the retained file path or an explicit local path override.
- Reports existence and file size.
- Computes the actual SHA-256 digest for existing files.
- Uses a deterministic missing-file digest marker for absent files.
- Produces a `SigtranCommercialEvidenceRetainedFile` so the Phase 39 verification pipeline can evaluate the observation.

The observer is the first executable bridge from retained dossier paths to real file verification.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
