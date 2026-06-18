# SIGTRAN.NET SDK Roadmap

This roadmap is based on the current repository and the supplied SIGTRAN references:

- Tekelec EAGLE SS7-over-IP using SIGTRAN, rev B
- ETSI EG 202 360 V1.1.1 SIGTRAN scenarios
- Nokia MCAS 6.1.1 SS7/SIGTRAN User Guide
- M2PA Internet-Draft 13, with RFC 4165 used as the standards-track baseline
- RFC 4666 for M3UA and RFC 9260 for SCTP

## Current Assessment

The current repository is a useful proof of concept, but it is not yet an interoperable SDK. SCCP and TCAP use simplified internal encodings, SCTP is represented by a TCP framing adapter, and the M3UA layer only supports a narrow Payload Data path. The first production milestone should therefore focus on binary correctness, role/session state, and testable public APIs before adding more telecom features.

## Phase 0 - SDK Foundation

- Target .NET 10 and prepare NuGet package metadata.
- Add a repeatable test project and binary golden-vector tests.
- Introduce strict network-byte-order helpers and reject malformed TLVs.
- Separate demo/testing transports from production SCTP transport contracts.
- Define package namespaces, public API rules, diagnostics, logging, and versioning.

## Phase 1 - M3UA Core

- Implement RFC 4666 common header, TLV parser/writer, padding, and error handling.
- Model message classes and types: Management, Transfer, SSNM, ASPTM, ASP, RKM.
- Add Protocol Data, Network Appearance, Routing Context, Traffic Mode, Error Code, Status, ASP Identifier, Heartbeat Data, and related parameters.
- Implement ASP state machines for ASP Up, ASP Active, Notify, Heartbeat, and Error.
- Support SG/ASP/IPSP roles, routing contexts, traffic modes, and correlation IDs.

## Phase 2 - SCTP Transport

- Keep the TCP adapter as a development-only transport.
- Add a production SCTP abstraction over platform SCTP where available.
- Support SCTP streams, PPID selection, association lifecycle events, reconnect, heartbeats, multi-homing-ready configuration, and cancellation-safe async I/O.
- Document OS support limits clearly for Windows and Linux deployments.

## Phase 3 - MTP3 And SCCP

- Implement MTP3 routing label encoding and service information octet handling.
- Replace simplified SCCP UDT encoding with ITU-T Q.713-style message structures.
- Add SCCP UDT/XUDT/LUDT parsing, called/calling party address indicators, SSN, global title formats, segmentation/reassembly, return cause, and protocol classes.
- Add route-on-SSN and route-on-GT APIs suitable for SMS/MAP users.

Status: SDK foundation is complete for internal APIs and byte-level tests. Production claims still require external SCCP interoperability vectors and trace validation.

## Phase 4 - TCAP

- Replace simplified TCAP encoding with ASN.1 BER.
- Implement dialogue portions, transaction IDs, Begin, Continue, End, Abort, Unidirectional, Invoke, ReturnResult, ReturnError, and Reject.
- Add dialogue state, invoke timers, duplicate detection, abort/error propagation, and allocation policies for transaction IDs.

Status: SDK foundation is complete for BER primitives, transaction envelopes, component codecs, dialogue portions, state controls, allocation helpers, and session builders. Production claims still require external TCAP interoperability vectors and MAP profile validation.

## Phase 5 - MAP SMS Profile

- Add MAP operation models and ASN.1 bindings for common SMS flows.
- Prioritize MO-ForwardSM, MT-ForwardSM, SRI-SM, ReportSM-DeliveryStatus, AlertServiceCentre, error mapping, and extension containers.
- Provide high-level client APIs that hide SCCP/TCAP plumbing without blocking access to lower-level protocol objects.

## Phase 6 - Interoperability And Tooling

- Build Wireshark-friendly trace logging and hex dump helpers.
- Add conformance vectors from RFC examples and vendor configuration scenarios.
- Add simulator components for SG, ASP, and MAP SMS test flows.
- Provide samples for ASP-to-SG, IPSP, SCCP/MAP SMS, and local TCP test transport.
- Add CI for build, formatting, package validation, and protocol golden tests.

## Recommended First Deliverable

The first useful SDK release should be an alpha package focused on M3UA over a transport abstraction:

- Correct M3UA binary parser/writer
- ASP state-machine API
- Protocol Data send/receive
- Routing context and network appearance support
- Structured diagnostics and test vectors
- Clear experimental labels for SCCP, TCAP, and MAP until their encodings are replaced with standards-based implementations
