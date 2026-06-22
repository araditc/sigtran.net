# Phase 37 Commercial Evidence Execution Orchestration

Phase 37 turns the readiness lockdown from Phase 36 into a governed execution package for real evidence-producing work. It does not create fake passing artifacts. It defines the run identity, stages, commands, environment contracts, artifact collection, verification, blocker handling, retry/resume behavior, and status reporting needed to run a real commercial evidence cycle.

Public APIs use domain names such as `SigtranCommercialEvidenceExecutionRuns`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Evidence Execution Run Identity

`SigtranCommercialEvidenceExecutionRuns` creates a release-candidate evidence execution run bound to:

- Locked release target.
- Stable run identifier.
- Operator or automation identity.
- UTC start time.
- Run-scoped artifact root under the target artifact root.

Floating roots such as `artifacts/latest/...` are rejected because retained evidence must prove exactly which package version, source commit, and run produced it.

## Unit 2 - Evidence Execution Stage Catalog

`SigtranCommercialEvidenceExecutionStages` defines the required execution stages:

- Readiness preflight.
- Native SCTP lab.
- External peer interoperability.
- Protocol validation.
- Performance benchmark.
- Supply-chain evidence.
- Release workflow dry-run.
- Dossier assembly.

The stage catalog validates required stage coverage, unique stage identifiers and order values, and run-scoped artifact roots for every stage.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
