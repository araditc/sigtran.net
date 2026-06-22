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

## Unit 2 - Retained File Manifest

`SigtranCommercialEvidenceRetainedFileManifest` groups observed retained files against a promotion handoff:

- Every promotion-required handoff item must have an observed file.
- Retained paths must be unique.
- Every observed file must be verified.
- The handoff itself must be ready.

The manifest is the first aggregate proof that retained file observations match the digest-covered handoff from artifact intake.

## Unit 3 - File Verification Report

`SigtranCommercialEvidenceFileVerificationReport` evaluates the retained file manifest and produces explicit blocker reasons:

- Missing retained files.
- Empty retained files.
- Invalid SHA-256 values.
- Digest mismatches.
- Non-UTC observation times.
- Duplicate retained paths.
- Incomplete promotion handoff coverage.

The report keeps verification gates auditable. A commercial release decision can now distinguish "not verified" from the specific retained file condition that blocked promotion.

## Unit 4 - Retention Ledger

`SigtranCommercialEvidenceRetentionLedger` records how verified retained files are retained for commercial release evidence:

- One ledger entry per verified retained file.
- Reviewer identity.
- UTC retention start and expiry times.
- Minimum retention duration.
- Immutable retention marker.
- Digest coverage back to the verified retained file.

The ledger is ready only when the file verification report is verified, every verified file is represented, retained paths are unique, every entry is immutable, timestamps are UTC, and every retention window meets the configured minimum duration.

## Unit 5 - Integrity Seal

`SigtranCommercialEvidenceIntegritySeal` creates a deterministic SHA-256 aggregate digest over the retention ledger:

- Stable seal id derived from the evidence intake.
- Required `SHA-256` algorithm label.
- Aggregate digest computed from ledger entries.
- UTC seal time.
- Readiness check that recomputes the digest and compares it with the retained seal value.

This is an evidence integrity seal for the retained dossier. It does not replace trusted timestamped package signing or release provenance attestation.

## Unit 6 - Publication Attachments

`SigtranCommercialEvidencePublicationAttachmentManifest` prepares verified retained files for release dossier publication:

- Every sealed ledger entry must have a publication attachment.
- Attachment digests must remain valid SHA-256 values.
- Trace-bearing artifacts must require and carry redaction approval.
- Retained attachment paths must be unique.
- The final commercial readiness report must be included.

Publication attachment readiness does not publish anything. It only proves that the retained evidence set can be safely attached to a gated release decision.

## Unit 7 - Verified Promotion Gate

`SigtranCommercialEvidenceVerifiedPromotionGateResult` decides whether retained commercial evidence can move into the release publication decision:

- Publication attachments must be ready.
- Integrity seal must be ready.
- Retention ledger must be ready.
- File verification report must be verified.
- Commercial readiness report must be present.
- Explicit commercial evidence approval must be present.

The gate returns concrete blocker labels. It does not publish a package; it only allows the release workflow to consider the verified evidence set.

## Unit 8 - File Verification Command Plan

`SigtranCommercialEvidenceFileVerificationCommandPlan` defines the workflow-ready execution order:

1. Observe retained files.
2. Compute retained file digests.
3. Compare observed digests with promotion handoff digests.
4. Write the file verification report.
5. Write the retention ledger.
6. Create the integrity seal.
7. Create the publication attachment manifest.
8. Evaluate the promotion gate.

The plan is package-neutral and shell-neutral. It describes the expected workflow contract so CI, a CLI, or a release operator can materialize equivalent commands without changing SDK domain APIs.

## Unit 9 - File Verification Status

`SigtranCommercialEvidenceFileVerificationStatus` reports the current phase readiness:

- Completed file verification capabilities.
- Default commercial publication blockers.
- Foundation readiness from the default verification chain.
- Explicit separation between foundation readiness and real retained file evidence.
- Commercial publication remains blocked until real retained file evidence and final validation are complete.

At this point the status intentionally keeps `status-final-validation-pending` as a blocker.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
