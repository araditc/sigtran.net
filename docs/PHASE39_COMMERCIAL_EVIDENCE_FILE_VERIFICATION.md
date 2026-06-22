# Phase 39 Commercial Evidence File Verification

Phase 39 turns digest-covered intake handoff records into retained file verification contracts. It does not create real commercial evidence by itself. It defines how retained artifact files are observed, matched to expected digests, checked for retention, sealed, and prepared for release evidence gates.

Public APIs use domain names such as `SigtranCommercialEvidenceRetainedFiles`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Retained File Evidence Item

`SigtranCommercialEvidenceRetainedFiles` records an observed retained file:

- Checklist artifact kind.
- Retained artifact path.
- Expected SHA-256 digest from the promotion handoff.
- Actual SHA-256 digest observed from the retained file.
- Observed file size.
- UTC observation time.
- File existence state.

A retained file is verified only when the file exists, is non-empty, carries valid SHA-256 values, has matching expected and actual digests, and uses a UTC observation time.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
