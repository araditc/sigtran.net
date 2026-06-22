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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
