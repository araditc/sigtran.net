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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
