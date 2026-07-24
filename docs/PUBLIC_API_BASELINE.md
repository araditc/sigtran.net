# Public API Baseline

The stable-candidate API baseline is
`eng/api/Sigtran.NET.1.0.public-api.txt`.

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

## Generation

```powershell
dotnet build src\Sigtran.NET\Sigtran.NET.csproj -c Release
powershell -ExecutionPolicy Bypass -File eng\generate-public-api-baseline.ps1 `
  -OutputPath eng\api\Sigtran.NET.1.0.public-api.txt
```

The generator runs `eng/Sigtran.NET.ApiSurface`, a .NET 10 reflection tool. The
tool avoids the previous error of treating every XML-documented internal member
as a public API.

## Comparison

```powershell
powershell -ExecutionPolicy Bypass -File eng\generate-public-api-baseline.ps1 `
  -OutputPath artifacts\api\Sigtran.NET-current.public-api.txt
powershell -ExecutionPolicy Bypass -File eng\compare-public-api.ps1 `
  -CurrentPath artifacts\api\Sigtran.NET-current.public-api.txt `
  -FailOnBreaking
```

The release workflow runs this comparison before package publication.
Prerelease runs reject removed or changed baseline lines and report additions.
The initial stable channel rejects every addition, removal, or changed line so
the reviewed 1.0 baseline is exactly the package contract.

The NuGet package's built-in package validation remains enabled as a second,
independent check.

## RC Compatibility

`eng/api/Sigtran.NET.1.0.0-rc.1.public-api.txt` preserves the public surface of
the already published RC.1 package. RC.2 intentionally removes repository
governance types from the package contract and adds the runtime APIs delivered
after RC.1. This is a prerelease compatibility break and is documented in the
RC.2 migration guide. No stable 1.x compatibility promise has yet begun.
