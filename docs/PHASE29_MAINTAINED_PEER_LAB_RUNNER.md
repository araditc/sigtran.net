# Phase 29 Maintained Peer Lab Runner Materialization

Phase 29 turns the maintained peer lab automation contracts into runner materialization contracts. The goal is to make a real lab runner deterministic about directories, inputs, commands, retained outputs, and handoff checks without pretending that generated plans are the same as real peer evidence.

## Unit 1 - Runner Workspace

`SigtranMaintainedPeerLabRunnerWorkspace` now describes the filesystem workspace used by a maintained peer lab runner. It records:

- Runner workspace root.
- Script root.
- Artifact root.
- Config, log, PCAP, trace, comparison, and report directories.
- Required directory list.
- Materialization readiness based on executable manifests and deterministic artifact paths.

This gives lab automation a single path contract before rendering inputs or starting peer traffic.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
