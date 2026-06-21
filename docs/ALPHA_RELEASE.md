# Alpha Release Checklist

SIGTRAN.NET alpha releases are M3UA-focused. SCCP, TCAP, and MAP remain experimental until their simplified encodings are replaced with standards-based implementations.

## Required Verification

Run the same checks used during development before publishing a package:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

`M3uaAlphaReadiness.RequiredVerificationCommandCount` is `3` and mirrors this checklist. `M3uaAlphaReadiness.ReleaseLabel` is `M3UA alpha`.

## Package Metadata

The package includes Apache-2.0 licensing, repository URLs, package tags, release notes, XML documentation, package validation, and symbol package generation.

## Public API Gate

Public API additions must include XML comments. The library treats missing public XML documentation as `CS1591` errors.

## Supported Message Discovery

Use `M3uaTypedMessageParser.IsSupported` when an application wants to check whether a message class and type can be dispatched into a typed SDK model before attempting full parsing.

Use `M3uaDiagnostics.TryValidateSupportedPacket` for a fast packet-level alpha gate: common framing, TLV walkability, and typed-dispatcher support.

## Readiness Report

`M3uaAlphaReadiness.GetReport()` returns a framework-neutral alpha readiness report covering package metadata, XML documentation enforcement, M3UA protocol coverage, transport abstraction availability, and the experimental status of SCCP/TCAP/MAP.

```csharp
M3uaAlphaReadinessReport report = M3uaAlphaReadiness.GetReport();
if (!report.IsReady)
{
    throw new InvalidOperationException(report.Describe());
}
```
