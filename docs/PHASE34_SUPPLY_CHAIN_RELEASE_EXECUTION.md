# Phase 34 Supply Chain Release Execution

Phase 34 turns the supply-chain foundation into release execution contracts for final SBOM, trusted timestamped signing, provenance attestation, public API diff artifacts, and workflow artifact upload.

The public APIs use domain names such as `SigtranFinalSbom`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Final SBOM Artifact

`SigtranFinalSbom` defines the final SBOM artifact retained for a release:

- Package id and version.
- Release package path covered by the SBOM.
- Versioned SPDX JSON output path.
- SBOM generation tool identity.
- Retained SHA-256 digest.
- Workflow output names for SBOM path, digest, and format.

The final SBOM contract does not manufacture evidence. A release is considered complete only when the retained SBOM is versioned, SPDX JSON, package-aligned, and digest-covered.

## Unit 2 - Trusted Timestamped Signing

`SigtranTrustedPackageSigning` defines the release signing evidence required for commercial promotion:

- Signed package path and SHA-256 digest.
- Signing certificate subject and thumbprint.
- HTTPS timestamp authority URL.
- Retained timestamp receipt artifact.
- Retained package verification report.
- Digest coverage for package, timestamp receipt, and verification report.

The evidence only supports release promotion when verification passes, timestamping is retained as a `.tsr` artifact, the timestamp authority uses HTTPS, the certificate thumbprint is present, and all required artifacts have SHA-256 digests.

## Unit 3 - Provenance Attestation

`SigtranProvenanceAttestations` defines the retained release provenance attestation:

- Versioned `.intoto.jsonl` attestation artifact.
- Attestation digest.
- Source repository and source commit.
- Release workflow name and workflow run identity.
- OIDC issuer used by the workflow.
- Package and SBOM subjects with SHA-256 digests.

The attestation can support release promotion only when it links package and SBOM subjects to a GitHub source repository, records a release workflow identity, uses an HTTPS OIDC issuer, and all retained subjects have complete digests.

## Unit 4 - Public API Diff Artifact

`SigtranPublicApiDiff` defines the retained public API diff artifact:

- Baseline API path.
- Current release API path.
- Versioned Markdown diff path.
- Diff SHA-256 digest.
- Added, removed, and changed public member counts.
- Explicit approval flag for breaking changes.

The artifact supports release promotion when it is digest-covered and either has no breaking changes or has explicit breaking-change approval. Removed or changed public members without approval remain a release blocker.

## Unit 5 - Release Artifact Upload Manifest

`SigtranReleaseArtifactUploads` defines the artifact upload manifest for the release workflow:

- NuGet package.
- NuGet symbol package.
- Final SBOM.
- Signing evidence.
- Timestamp receipt.
- Provenance attestation.
- Public API diff.
- Digest manifest.

Promotion-required artifacts must be present and retained for at least 90 days so operators can review the exact package, signing, provenance, API, and digest evidence that backed a release decision.

## Unit 6 - Supply Chain Release Command Plan

`SigtranSupplyChainReleaseCommands` defines the ordered command plan used by the release workflow:

- Generate final SBOM.
- Sign the NuGet package.
- Verify the package signature.
- Create provenance attestation.
- Create public API diff.
- Create digest manifest.
- Upload release artifacts.

The command plan records which command requires signing secrets and keeps upload as the final step so incomplete SBOM, signing, provenance, API diff, or digest artifacts cannot be promoted as a finished release bundle.

## Unit 7 - Supply Chain Release Gate

`SigtranSupplyChainReleaseGate` aggregates the release execution evidence into one promotion decision:

- Final SBOM artifact.
- Trusted timestamped signing evidence.
- Provenance attestation.
- Public API diff artifact.
- Release artifact upload manifest.
- Supply-chain release command plan.
- Commercial evidence readiness.

The gate keeps supply-chain completeness separate from commercial release readiness. Even complete SBOM, signing, provenance, API diff, and upload evidence cannot be promoted unless commercial evidence is also ready.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
