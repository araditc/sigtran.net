# Phase 30 Maintained Peer Lab Runner Operationalization

Phase 30 turns the maintained peer lab runner materialization contracts into operational handoff contracts. The goal is to make a real runner reviewable around file creation, execution logs, command outcomes, artifact verification, provenance, failure handling, retry policy, and evidence packaging without claiming that planned artifacts are real lab evidence.

## Unit 1 - File Materialization

`SigtranMaintainedPeerLabRunnerFileMaterializationPlan` now describes the directories and input files a runner must create before executing peer traffic. It records:

- Required workspace directories.
- Environment and command script input files.
- Directory and input coverage checks.
- Shell script rendering for reviewable materialization.

The plan does not write files. It renders the deterministic operation that a real lab runner can execute or review.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
