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

## Unit 4 - MAP SMS Evidence Vectors

`MapSmsEvidenceVectors` now provides deterministic MAP SMS vectors and encoder validation for:

- MO-ForwardSM.
- MT-ForwardSM.
- SendRoutingInfoForSM.
- ReportSM-DeliveryStatus.
- AlertServiceCentre.

Each vector stores literal BER-shaped parameter bytes and validates the current MAP SMS encoder output. Tests decode every vector back into its operation model to tie byte evidence to decoded field behavior.

## Unit 5 - Cross-Layer Evidence Bundle

`SigtranProtocolEvidenceBundle` now aggregates SCCP, TCAP, and MAP SMS vectors into one validation report. It provides:

- Required cross-layer vector count.
- Surface-specific vector counts.
- Duplicate vector id detection.
- Aggregate validation pass count.
- Complete and evidence-backed summary flags.

This gives release and readiness gates one deterministic SDK-side evidence report before trace validation and external artifact comparison are applied.

## Unit 6 - Evidence Trace Validation

`SigtranProtocolEvidenceTraceValidator` now validates ordered `SigtranTraceFrame` sequences against SCCP, TCAP, and MAP SMS evidence vectors. It provides:

- Protocol label checks.
- Ordered vector-to-frame pairing.
- Byte-level payload validation per frame.
- Missing expected vector reporting.
- Unexpected extra frame counting.
- Full trace pass/fail summaries.

This connects deterministic SDK vectors to captured trace output so later lab artifacts can be validated as ordered protocol evidence rather than isolated payload blobs.

## Unit 7 - Mismatch Classification And Fix Guidance

`SigtranProtocolEvidenceMismatchClassifier` now converts trace validation failures into actionable mismatch findings. It classifies:

- Protocol label mismatches.
- Byte-level payload mismatches.
- Missing expected trace frames.
- Unexpected extra trace frames.

Each finding includes a stable recommended action token such as `fix-trace-protocol-label`, `fix-codec-or-reference-vector`, `capture-missing-trace-frame`, or `map-or-trim-unexpected-trace-frame`. This gives lab comparison reports a deterministic way to decide whether the next correction belongs in the SDK codec, the reference vector, the trace capture, or the artifact mapping.

## Unit 8 - Evidence-Backed Readiness Gates

`SigtranProtocolEvidenceReadiness` now reports three distinct readiness levels:

- `FoundationReady`: SCCP, TCAP, and MAP SMS foundations are present.
- `SdkEvidenceBacked`: deterministic SDK vectors, cross-layer validation, trace validation, and mismatch classification are all clean.
- `ProductionEvidenceReady`: SDK evidence is backed by retained external interoperability evidence.

The default report marks SDK evidence as backed while keeping production evidence blocked by `external-protocol-interoperability-evidence-required`. This prevents the SDK from treating deterministic internal vectors as a substitute for real external PCAP, peer logs, SDK traces, and comparison artifacts.

## Unit 9 - Evidence Status And Summary

`SigtranProtocolEvidenceStatus` now provides a domain-level status report for the evidence upgrade. It exposes:

- Completed capability ids.
- Completed unit count.
- Foundation readiness.
- SDK evidence-backed readiness.
- Production evidence readiness.
- Current blocker identifiers.

`docs/PHASE32_SUMMARY.md` summarizes the evidence position for adopters and keeps the commercial gate explicit: internal deterministic vectors support SDK behavior claims, while production promotion still requires retained external interoperability artifacts.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
