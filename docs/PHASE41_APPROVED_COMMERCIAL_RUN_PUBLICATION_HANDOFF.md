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

## Unit 2 - Approval Checklist

`SigtranCommercialEvidenceRunApprovalChecklist` records required approval criteria before reviewer manifest creation:

- Run target identity is ready.
- Filesystem-backed promotion execution is ready.
- File verification report is verified.
- Retention ledger and integrity seal are ready.
- Publication attachments are ready.
- Trace-bearing artifacts have approved redaction state.
- Promotion gate and reviewer approval are present.

This unit turns commercial run approval into an explicit checklist with blocker identifiers rather than an implicit yes/no flag.

## Unit 3 - Reviewer Approval Manifest

`SigtranCommercialEvidenceRunApprovalManifest` records reviewer approval for a ready checklist:

- Deterministic checklist SHA-256 digest.
- Release reviewer approval.
- Security reviewer approval.
- Operations reviewer approval.
- UTC approval timestamps.
- Unique reviewer roles.

This unit makes commercial approval auditable and role-aware before any publication handoff is created.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
