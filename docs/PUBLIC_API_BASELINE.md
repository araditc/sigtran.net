# Public API Baseline

The stable-candidate API contract is composed from:

- `eng/api/Sigtran.NET.1.0.public-api.txt` — the reviewed RC.2-era stable-candidate snapshot;
- `eng/api/Sigtran.NET.1.0.accepted-additions.txt` — explicitly admitted pre-stable additions that are part of the intended 1.0 contract.

The comparison tool treats the two files as one canonical candidate surface. The additions file is intentionally small and reviewable; it must not be used as a wildcard or compatibility bypass. Before or at stable 1.0 publication the contract may be consolidated into a single generated snapshot, but until then both files are retained and digest-covered by the stable evidence manifest.

## Scope

The baseline is generated from `Assembly.GetExportedTypes()` and public
constructors, methods, properties, fields, and events. It records signatures,
type kind, inheritance, implemented interfaces, accessor visibility, and
selected modifiers.

It intentionally excludes internal release planning, evidence orchestration,
readiness catalogs, and repository governance models. Those types support this
repository's verification process but are not SDK contracts for an application.

The supported public surface is concentrated in:

- `Sigtran.NET.Core.Interfaces`
- `Sigtran.NET.Core.Profiles`
- `Sigtran.NET.Layers.SCTP`
- `Sigtran.NET.Layers.MTP2`
- `Sigtran.NET.Layers.MTP3`
- `Sigtran.NET.Layers.M3UA`
- `Sigtran.NET.Layers.SCCP`
- `Sigtran.NET.Layers.TCAP`
- `Sigtran.NET.Layers.MAP`
- `Sigtran.NET.Operations`
- the small conformance, simulation, and trace subset under
  `Sigtran.NET.Core.Utilities`

The `Sigtran.NET.Core.Profiles` surface was intentionally admitted before stable 1.0 as part of the approved Operator Profile Framework work. Its admission does not promote the separate `operator-profile` interoperability gate: real operator/vendor acceptance evidence is still required for that gate.

## Generation

```powershell
dotnet build src\Sigtran.NET\Sigtran.NET.csproj -c Release
powershell -ExecutionPolicy Bypass -File eng\generate-public-api-baseline.ps1 `
  -OutputPath artifacts\api\Sigtran.NET-current.public-api.txt
```

The generator runs `eng/Sigtran.NET.ApiSurface`, a .NET 10 reflection tool. The
tool avoids the previous error of treating every XML-documented internal member
as a public API.

New public members are never admitted merely by generation. A proposed addition must be reviewed and then listed explicitly in `eng/api/Sigtran.NET.1.0.accepted-additions.txt` while the 1.0 surface remains pre-stable.

## Comparison

```powershell
powershell -ExecutionPolicy Bypass -File eng\generate-public-api-baseline.ps1 `
  -OutputPath artifacts\api\Sigtran.NET-current.public-api.txt
powershell -ExecutionPolicy Bypass -File eng\compare-public-api.ps1 `
  -CurrentPath artifacts\api\Sigtran.NET-current.public-api.txt `
  -FailOnAnyChange
```

The comparison script forms the reference set from the base snapshot plus the explicitly accepted additions. Prerelease runs can still use `-FailOnBreaking` when additions are being reviewed; the stable channel uses `-FailOnAnyChange` so any current member outside the admitted contract, or any admitted member missing from the assembly, fails the release workflow.

The stable evaluator also requires the accepted-additions file to exist and the `public-api-baseline` gate records both files as digest-covered evidence.

The NuGet package's built-in package validation remains enabled as a second,
independent check.

## RC Compatibility

`eng/api/Sigtran.NET.1.0.0-rc.1.public-api.txt` preserves the public surface of
the already published RC.1 package. RC.2 intentionally removes repository
governance types from the package contract and adds the runtime APIs delivered
after RC.1. This is a prerelease compatibility break and is documented in the
RC.2 migration guide. The Operator Profile Framework is an additional intentional pre-stable 1.0 candidate admission. No stable 1.x compatibility promise has yet begun.
