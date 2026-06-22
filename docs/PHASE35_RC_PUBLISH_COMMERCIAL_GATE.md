# Phase 35 RC Publish And Commercial Gate

Phase 35 adds the release-candidate publication and final commercial gate foundation for Sigtran.NET. It covers dry-run release rehearsal, gated NuGet prerelease publication, final commercial readiness reporting, release notes, migration notes, and the RC-versus-stable decision.

The public APIs use domain names such as `SigtranReleaseDryRuns`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Dry-Run Release

`SigtranReleaseDryRuns` defines the dry-run release rehearsal plan:

- Pack the SDK with the release candidate version.
- Verify the generated NuGet package.
- Evaluate the dry-run release gate.
- Retain dry-run evidence under a versioned artifact root.
- Avoid every NuGet upload path.

The dry-run plan is release-rehearsal ready only when it includes package creation, package verification, retained evidence, and no `nuget push` command.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
