# Phases 0 To 5 Foundation

This document maps the early SDK phases to the protocol documentation that now carries their implementation details. These phases were completed before the later phase-by-phase Markdown convention was introduced.

## Phase 0 - SDK Foundation

Primary documents:

- [Architecture](ARCHITECTURE.md)
- [Quality and contribution rules](QUALITY.md)
- [Compatibility policy](COMPATIBILITY.md)
- [Alpha release checklist](ALPHA_RELEASE.md)

Status: foundation complete for .NET 10 packaging, test execution, XML documentation, public API rules, package metadata, and project-level quality gates.

## Phase 1 - M3UA Core

Primary documents:

- [M3UA implementation notes](M3UA.md)
- [M3UA Payload Data](DATA.md)
- [M3UA typed dispatcher](DISPATCHER.md)
- [M3UA Diagnostics](DIAGNOSTICS.md)
- [M3UA Payload Routing](ROUTING.md)
- [M3UA Inbound Processing](PROCESSING.md)
- [M3UA Outbound Processing](OUTBOUND.md)
- [M3UA Transport Session](TRANSPORT_SESSION.md)
- [M3UA ASP Client](ASP_CLIENT.md)
- [M3UA Management Messages](MANAGEMENT.md)
- [M3UA Signalling Network Management](SSNM.md)
- [M3UA Routing Key Management](RKM.md)

Status: foundation complete for M3UA framing, TLV parsing/writing, ASP lifecycle, DATA handling, routing, typed message helpers, diagnostics, and transport-session APIs. Production promotion still depends on retained external peer evidence.

## Phase 2 - SCTP Transport

Primary documents:

- [SCTP Transport](SCTP_TRANSPORT.md)
- [Phase 8 Native SCTP](PHASE8_NATIVE_SCTP.md)
- [Phase 31 Native SCTP Production Hardening](PHASE31_NATIVE_SCTP_PRODUCTION_HARDENING.md)

Status: foundation complete for transport abstractions, development TCP adapter, stream/PPID metadata, reconnect policy, association lifecycle modeling, and native SCTP readiness contracts. Production support still requires retained Linux SCTP and peer-traffic evidence.

## Phase 3 - MTP3 And SCCP

Primary documents:

- [MTP3 Routing](MTP3.md)
- [SCCP](SCCP.md)
- [Phase 32 SCCP TCAP MAP Evidence Upgrade](PHASE32_SCCP_TCAP_MAP_EVIDENCE_UPGRADE.md)

Status: foundation complete for MTP3 routing labels and SCCP connectionless message models. SDK evidence vectors exist, but production SCCP claims still require retained external interoperability artifacts.

## Phase 4 - TCAP

Primary documents:

- [TCAP](TCAP.md)
- [Phase 32 SCCP TCAP MAP Evidence Upgrade](PHASE32_SCCP_TCAP_MAP_EVIDENCE_UPGRADE.md)

Status: foundation complete for BER primitives, transaction envelopes, dialogue portions, component codecs, session builders, and deterministic SDK evidence vectors. Production TCAP claims still require retained external interoperability artifacts.

## Phase 5 - MAP SMS Profile

Primary documents:

- [MAP SMS Profile](MAP.md)
- [Phase 32 SCCP TCAP MAP Evidence Upgrade](PHASE32_SCCP_TCAP_MAP_EVIDENCE_UPGRADE.md)

Status: foundation complete for MAP SMS operation models and deterministic SDK evidence vectors. Production MAP SMS claims still require retained external interoperability artifacts and operator-profile validation.
