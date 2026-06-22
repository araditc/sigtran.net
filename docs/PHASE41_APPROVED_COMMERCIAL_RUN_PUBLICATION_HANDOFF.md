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

## Unit 4 - Approval Report Writer

`SigtranCommercialEvidenceRunApprovalReportWriters` renders and retains a Markdown approval report:

- Run id, package version, source commit, and artifact root.
- Checklist digest and manifest readiness.
- Reviewer approval roles.
- UTC write time.
- SHA-256 digest for the retained report.

This unit turns approval data into a retained report artifact that later promotion packages and publication handoffs can reference.

## Unit 5 - Evidence Promotion Package

`SigtranCommercialEvidenceApprovedRunPromotionPackage` collects the approved run artifacts required for publication handoff:

- Retained approval report.
- Integrity seal artifact reference.
- Publication attachment artifact reference.
- Promotion gate artifact reference.
- SHA-256 digest coverage for every required artifact.

This unit gives the publication handoff a single approved package contract instead of relying on loose references.

## Unit 6 - Publication Handoff

`SigtranCommercialEvidencePublicationHandoff` connects an approved run promotion package to publication channel intent:

- Requested publication channel.
- Requester identity.
- UTC handoff creation time.
- Explicit publish request flag.
- Channel acceptance of package version.
- Commercial readiness requirement visibility.

This unit keeps RC and stable publication boundaries explicit before a final publication gate evaluates the handoff.

## Unit 7 - Publication Handoff Gate

`SigtranCommercialEvidencePublicationHandoffGates` evaluates handoff blockers:

- Promotion package readiness.
- Explicit publish request.
- UTC handoff time.
- Channel acceptance of package version.
- Stable commercial readiness approval when required.

This unit gives release operators actionable blockers before package publication workflows are allowed to proceed.

## Unit 8 - Approval Audit Trail

`SigtranCommercialEvidenceApprovalAuditTrail` records digest-covered lifecycle events:

- Run target.
- Approval checklist.
- Reviewer approval manifest.
- Approval report.
- Promotion package.
- Publication handoff.
- Handoff gate.

This unit gives the approval path a retained audit chain that can be reviewed before package publication proceeds.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
