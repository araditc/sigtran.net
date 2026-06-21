# Phase 19 Protocol Interop Vectors

Phase 19 adds the SCCP, TCAP, and MAP SMS protocol interoperability vector foundation for SIGTRAN.NET.

The goal is to make higher-layer protocol evidence executable and reviewable in the same way as the lower transport and external peer lab work. The SDK now exposes a catalog of required vectors, external references, artifact requirements, comparison rules, run plans, run reports, evidence registry, readiness, and CI metadata.

The public APIs use domain names such as `SigtranProtocolInteropStatus`; phase numbers are intentionally kept out of source type names.

## Vector Catalog

`SigtranProtocolInteropVectorCatalog.GetVectors()` defines required vectors for:

- SCCP UDT route-on-SSN.
- SCCP XUDT segmentation.
- TCAP Begin with dialogue and Invoke.
- TCAP ReturnResult in End.
- MAP MO-ForwardSM.
- MAP SendRoutingInfoForSM.

All vectors require external references before they can count as commercial interoperability evidence.

## References

`SigtranProtocolInteropReferences.GetReferences()` tracks the external protocol references used for validation:

- ITU-T Q.713 for SCCP formats and codes.
- ITU-T Q.773 for TCAP transaction and component encoding.
- 3GPP TS 29.002 for MAP SMS operation profile behavior.

Each reference requires trace validation so generated SDK vectors can be compared against reference encodings and decoded fields.

## Artifacts

`SigtranProtocolInteropArtifactManifest` requires:

- Reference vector.
- SDK generated vector.
- Comparison report.

Packet captures and operator profile notes are supported artifact kinds, but the minimum commercial evidence contract is reference vector, SDK vector, and comparison report.

## Comparison Rules

`SigtranProtocolInteropComparisonRules.CreateDefault()` requires:

- Byte-exact encoding comparison.
- Decoded field comparison.
- Trace order validation.

Operator-specific extensions are allowed, but they must be retained as explicit artifacts or comparison notes.

## Run Plan

`SigtranProtocolInteropRunPlans.CreateDefault()` combines the vector catalog, references, and comparison rules.

The run plan is executable when vectors require external references, references require trace validation, rules are commercial-validation ready, and external vectors are required.

Executable does not mean verified. Verification requires retained passing evidence.

## Commands

`SigtranProtocolInteropCommands.CreateDefault()` defines the command contract for a prepared protocol-vector lab.

The command set requires `SIGTRAN_PROTOCOL_VECTOR_ROOT` and comparison reports. A real runner should store external reference vectors and SDK-generated vectors under the configured vector root.

## Run Reports

`SigtranProtocolInteropRunReport` records a vector, artifact manifest, run status, start time, and completion time.

Only passed reports with a complete artifact manifest count as passing evidence.

## Evidence

`SigtranProtocolInteropEvidence.CreateCurrentRegistry()` currently returns an empty registry.

Real SCCP, TCAP, and MAP SMS vector evidence must be captured and promoted before commercial readiness can claim verified higher-layer protocol interoperability.

## Readiness

`SigtranProtocolInteropReadiness.GetReport()` separates foundation readiness from evidence readiness.

The foundation is ready. Verification remains false until complete passing evidence exists for every required vector.

## CI Profile

`SigtranProtocolInteropCi.CreateDefault()` defines the protocol interop vector CI profile.

The profile is enabled by `SIGTRAN_PROTOCOL_INTEROP` and uses `SIGTRAN_PROTOCOL_VECTOR_ROOT` to locate retained vector artifacts.

## Status

`SigtranProtocolInteropStatus.Describe()` summarizes the vector foundation and keeps verified status false until complete passing evidence exists.
