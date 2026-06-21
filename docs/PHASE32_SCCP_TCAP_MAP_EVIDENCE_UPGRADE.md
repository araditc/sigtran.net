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

## Unit 2 - SCCP Evidence Vectors

`SccpEvidenceVectors` now provides deterministic SCCP vectors and encoder validation for:

- UDT route-on-SSN MAP-to-MSC payload.
- XUDT with segmentation optional parameter.
- LUDT with 16-bit variable parameter pointers.
- UDTS with subsystem-failure return cause.

Each vector stores literal expected bytes and validates the current SDK encoder output through `SigtranProtocolEvidenceValidator`. The tests also decode every expected payload to verify that the evidence bytes remain parseable by the SDK decoder.

## Unit 3 - TCAP Evidence Vectors

`TcapEvidenceVectors` now provides deterministic TCAP vectors and encoder validation for:

- Begin transaction with originating transaction id, dialogue portion, and Invoke component.
- End transaction with destination transaction id and ReturnResult component.

Each vector stores literal BER expected bytes and validates the current TCAP transaction encoder output. Tests decode the transaction wrapper, dialogue portion, Invoke component, and ReturnResult component so byte evidence is tied to decoded field behavior.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
