# Phase 38 Commercial Evidence Artifact Intake

Phase 38 turns governed execution outputs into a retained, reviewable commercial evidence dossier. It does not manufacture passing evidence. It defines how real artifacts from an execution run are received, checked, digested, redaction-reviewed, completed, and handed off for promotion.

Public APIs use domain names such as `SigtranCommercialEvidenceArtifactIntakes`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Artifact Intake Target

`SigtranCommercialEvidenceArtifactIntakes` creates an intake target bound to:

- The execution run that produced the artifacts.
- A stable intake identifier.
- The reviewer or automation identity performing intake.
- UTC artifact receipt time.
- A run-scoped dossier root under the execution run artifact root.

Floating dossier roots such as `artifacts/latest/...` are rejected because commercial evidence must prove exactly which execution run produced the retained artifacts.

## Unit 2 - Artifact Source Manifest

`SigtranCommercialEvidenceArtifactSources` registers received artifacts against the expected execution artifact manifest:

- Every required expected artifact must have a source entry.
- Source paths must be concrete and must not use floating `artifacts/latest` aliases.
- Retained paths must be unique.
- Retained paths must live under the intake dossier root.

The source manifest is the handoff from execution outputs into dossier intake. It prepares the next step, digest generation, without claiming that files are already verified.

## Unit 3 - Artifact Digest Manifest

`SigtranCommercialEvidenceArtifactDigests` records SHA-256 coverage for retained sources:

- Every registered source must have a digest entry.
- Each digest entry keeps the stage id, artifact kind, source path, retained path, and SHA-256 value together.
- SHA-256 values must be 64-character hexadecimal values.
- Retained paths must remain unique.

The digest manifest is required before redaction review and promotion handoff because commercial evidence must remain tamper-evident after intake.

## Unit 4 - Redaction Review Manifest

`SigtranCommercialEvidenceRedactionReviews` records reviewer approval for trace-bearing artifacts:

- Packet captures.
- Peer logs.
- SDK traces.
- Configuration.
- Comparison reports.
- Benchmark reports.

Each required review records the retained path, artifact kind, reviewer identity, UTC review time, approval state, and notes. Rejected or missing reviews block the intake from moving into completeness evaluation.

## Unit 5 - Artifact Completeness Evaluation

`SigtranCommercialEvidenceArtifactCompleteness` evaluates whether intake can move into dossier reporting:

- Source registration must be complete.
- Digest coverage must be complete.
- Redaction review must be complete.

The evaluator returns explicit blocker codes such as `artifact-source-registration-incomplete`, `artifact-digest-coverage-incomplete`, and `redaction-review-incomplete` so release operators can correct the right evidence area without guessing.

## Unit 6 - Dossier Intake Report

`SigtranCommercialEvidenceDossierIntakeReports` creates a retained Markdown report for the intake:

- Execution run identifier.
- Intake identifier.
- Reviewer identity.
- Dossier root.
- Source, digest, and redaction review counts.
- Completion state and blocker count.

The report path must be under the intake dossier root. This gives release operators a single retained summary before promotion handoff.

## Unit 7 - Promotion Handoff

`SigtranCommercialEvidencePromotionHandoffs` prepares intake output for commercial evidence promotion:

- Every digest-covered retained artifact is included.
- The dossier intake report is included.
- Every handoff item requires a valid SHA-256 digest.
- Handoff creation time is normalized to UTC.

The handoff does not publish a package. It creates a verified package of intake evidence for the existing commercial evidence and release gates to evaluate.

## Unit 8 - Execution-To-Dossier Bridge

`SigtranCommercialEvidenceDossierIntakeBridge` assembles the default intake pipeline from a governed execution run:

- Expected execution artifact manifest.
- Intake target.
- Source manifest.
- Digest manifest.
- Redaction review manifest.
- Completeness result.
- Dossier intake report.
- Promotion handoff.

The bridge is intentionally a contract builder, not a file executor. Real artifact copying, digest calculation, and redaction review still happen in the lab or release workflow and provide the values passed into the bridge.

## Unit 9 - Artifact Intake Status

`SigtranCommercialEvidenceArtifactIntakeStatus` exposes the current artifact intake state:

- Completed intake capabilities.
- Default commercial publication blockers.
- Artifact intake foundation readiness.
- Real artifact evidence readiness.
- Commercial publication readiness.

The status keeps foundation readiness separate from commercial publication readiness. Real file evidence remains a blocker until retained artifacts are captured, reviewed, and approved.

## Unit 10 - Final Validation

Final validation closes the artifact intake foundation:

- Build, test, and package validation are run together.
- `final-validation` is included in the public status capability list.
- The temporary final-validation blocker is removed.
- The real artifact file evidence blocker remains active.

This keeps the phase complete as an SDK foundation while preserving the commercial release gate until real artifact files are retained and reviewed.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
