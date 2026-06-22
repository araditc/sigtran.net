# Phase 36 Commercial Evidence Readiness Lockdown

Phase 36 locks the evidence-readiness prerequisites required before real lab execution and RC publication. The phase does not manufacture passing evidence. It makes the release target, secrets, artifact roots, checklists, preflight, and go/no-go decision explicit and testable before evidence-producing work begins.

The public APIs use domain names such as `SigtranCommercialReleaseTargetLocks`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Release Target Lock

`SigtranCommercialReleaseTargetLocks` defines the immutable release target used to bind retained evidence:

- Release-candidate package version.
- Pinned source commit.
- Source branch or tag reference.
- Release channel.
- Versioned retained artifact root.

The target is locked only when it is a release candidate, the commit is pinned, the channel is supported, and the artifact root is retained under `artifacts/` with the version in its path.

## Unit 2 - Required Secrets Readiness

`SigtranCommercialReleaseSecrets` defines the protected secret contract without exposing secret values:

- `NUGET_API_KEY` for package publication.
- `SIGNING_CERTIFICATE` for trusted package signing material.
- `SIGNING_CERTIFICATE_PASSWORD` for signing material unlock.
- `PROVENANCE_ATTESTATION_TOKEN` for provenance attestation upload when OIDC is not used.

The readiness evaluator accepts only secret names and reports missing requirements. This keeps release checks auditable while avoiding secret-value logging in build, test, and release output.

## Unit 3 - Evidence Artifact Retention Map

`SigtranCommercialEvidenceRetentionMaps` defines the retained artifact roots required for commercial evidence capture:

- Native SCTP evidence.
- External peer interoperability evidence.
- Protocol interoperability evidence.
- Performance and resilience evidence.
- Supply-chain evidence.
- Public API evidence.
- Release workflow evidence.
- Publication dossier evidence.

Every path must live under the release target artifact root, keep artifacts for at least one year, and require digest coverage. Floating paths such as `artifacts/latest/...` are intentionally rejected because they cannot prove which release target produced the evidence.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
