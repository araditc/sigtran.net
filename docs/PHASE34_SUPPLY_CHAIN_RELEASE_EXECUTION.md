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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
