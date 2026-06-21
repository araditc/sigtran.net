# Phase 32 - SCCP TCAP MAP Evidence Upgrade

Phase 32 upgrades SCCP, TCAP, and MAP SMS from foundation-ready protocol surfaces toward evidence-backed behavior. The phase focuses on deterministic vectors, trace validation, mismatch reporting, and correcting any byte-level or decoded-field mismatches found by those checks.

The phase does not replace the need for retained external interoperability evidence. It creates SDK-side evidence contracts and validation reports that can be compared against external vectors, packet captures, peer logs, SDK traces, and commercial release artifacts.

## Unit 1 - Shared Protocol Evidence Contract

`SigtranProtocolEvidenceVector` and `SigtranProtocolEvidenceValidator` now provide a shared byte-level evidence contract for SCCP, TCAP, and MAP SMS. They provide:

- Stable vector ids.
- Protocol surface labels.
- Expected encoded bytes.
- Source references.
- Uppercase hex rendering.
- Byte offset mismatch reporting.
- Length mismatch reporting.
- Pass/fail validation summaries.

This gives the next units a common validation language before adding SCCP, TCAP, and MAP-specific vector suites.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
