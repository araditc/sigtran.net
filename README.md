# SIGTRAN.NET

SIGTRAN.NET is a .NET 10 SDK for building SS7-over-IP applications. The project is being shaped from an early proof of concept into a standards-oriented, open-source SDK with stable public APIs, documented protocol behavior, and byte-level tests.

The first production milestone is M3UA over a transport abstraction. SCCP, TCAP, and MAP code still exist in the repository, but those layers are currently experimental until their simplified encodings are replaced with standards-based implementations.

## Current Status

| Area | Status |
| --- | --- |
| SDK packaging | .NET 10 library, NuGet metadata, XML documentation, package validation, symbols |
| Alpha readiness | M3UA release label, verification command count, readiness report, and packet validation gates |
| M3UA common framing | Protocol metadata, header preview/parser, message length guards, message parameter count, TLV reader/writer, parameter presence/lookup, padding handling |
| M3UA capability discovery | Protocol metadata, capability snapshots, and typed parser support/require checks by message class and type |
| M3UA transfer | DATA builders/parsers with Network Appearance, Routing Context, Protocol Data, and Correlation Id |
| M3UA DATA routing | Mutable Payload route table with selector matching, descriptions, validation, inspection, add-or-replace, snapshots, and name lookup |
| M3UA inbound processing | Decode, typed dispatch, ASP ACK state updates, and DATA route resolution |
| M3UA outbound processing | State-aware builders with association defaults for ASP lifecycle, RKM, and typed DATA |
| M3UA transport session | Async receive/send/wait facade with typed DATA send, health snapshots, Heartbeat acknowledgement, transport-loss notification, and resettable counters |
| M3UA ASP client | ASP startup/reset option validation, heartbeat, and shutdown helpers using transport wait APIs |
| M3UA ASPSM | ASP Up/Down, Heartbeat, acknowledgements, typed parsing |
| M3UA ASPTM | ASP Active/Inactive, acknowledgements, typed parsing |
| M3UA management | Error and Notify builders/parsers plus transport send helpers |
| M3UA SSNM | DUNA, DAVA, DAUD, DRST, DUPU, and SCON builders/parsers plus transport send helpers |
| M3UA RKM | REG/DEREG builders, parsers, strict client helpers, assigned-context lookup, and response status utilities |
| M3UA diagnostics | ASP session summaries, alpha packet validation, header summaries, typed message summaries, parameter inventories, and offset-based hex dump formatting |
| ASP state | Local acknowledgement-driven ASP session state machine with reset and Routing Context inspection |
| MTP3 | Service information octet and ITU-style routing label primitives for SCCP payload routing |
| SCTP | Phase 2 foundation complete: packet transport contract, connection options, reconnect policy, health snapshots, stream selection, SIGTRAN PPID helpers, optional stream/PPID metadata, association lifecycle models, and metadata-aware development TCP adapter |
| SCTP readiness | Foundation readiness report with capability count; native SCTP implementation and interoperability verification remain the production gate |
| SCCP | Phase 3 foundation complete: MTP3 boundary, route-on-SSN/GT APIs, connectionless message types, protocol class primitives, party address indicators, SSN, point code, TBCD global title support, pointer-based UDT, XUDT hop-counter/segmentation encode-decode, LUDT long-payload support, and UDTS return-cause messages |
| SCCP readiness | Foundation readiness report with capability count; external interoperability vectors and trace validation remain the production gate |
| TCAP | Phase 4 foundation complete: ASN.1 BER TLV primitives, package tags, transaction identifiers, BER Invoke/ReturnResult/ReturnError/Reject components, transaction message envelopes, dialogue portion application-context support, dialogue state/timeout control, identifier allocation helpers, and Begin/End session builders |
| TCAP readiness | Foundation readiness report with capability count; external TCAP interoperability vectors and MAP profile validation remain the production gate |
| MAP | Phase 5 foundation complete: SMS operation metadata, BER context-specific parameter containers, TBCD address primitives, MO/MT-ForwardSM codecs, SendRoutingInfoForSM, ReportSM-DeliveryStatus, AlertServiceCentre, error mapping, extension containers, and TCAP client facade |
| MAP readiness | Foundation readiness report with capability count; external MAP SMS interoperability vectors and operator-profile validation remain the production gate |
| Interoperability tooling | Phase 6 foundation-ready: trace frames, Wireshark-friendly hex dumps, conformance vectors, built-in M3UA/MAP vectors, simulator scripts, MAP SMS flow builders, local TCP samples, sample catalog, CI profile, and readiness report; production gate is external interoperability lab evidence |
| Commercial readiness | Phase 7 started: internal release readiness is available; commercial production readiness requires native SCTP verification, external interoperability evidence, and release governance |
| Native SCTP support | Phase 8 foundation is available for Linux native SCTP; Linux still requires lab verification before production support, while Windows and macOS remain contract-only |
| External interoperability evidence | Phase 7 registry added for peer-stack lab results and packet trace references; current evidence inventory is empty until real lab artifacts are captured |
| Release candidates | Phase 7 manifest added for package version, source commit, internal release gates, and commercial promotion gates |
| Release automation | Phase 10 foundation-ready: deterministic release plan, artifact manifest with digest tracking, SBOM plan, package signing plan, provenance tracking, release notes validation, publish channels, release gate evaluator, release CI profile, and status report added; stable commercial publication still requires real signing, SBOM generation, native SCTP evidence, and external interoperability evidence |
| Developer experience | Phase 11 foundation-ready: capability catalog, M3UA ASP-to-SG quickstart, sample templates, configuration profiles, troubleshooting index, API reference index, adoption gates, documentation readiness report, DX CI profile, and status report added; enterprise production adoption still requires commercial readiness |
| Operations | Phase 12 started: production operations capability catalog, runbook catalog, and incident response targets added |
| Package governance | Phase 7 policy added: current package metadata is tracked; commercial target still requires package signing and SBOM automation |
| Security governance | Phase 7 security policy added with private disclosure and severity response targets |
| Compatibility policy | Phase 7 policy added: net10.0 target, SemVer, pre-stable breaking-change allowance, and stable major-version rule |
| Observability | Phase 7 profile added for commercial metrics, trace categories, and health signals |
| Deployment profiles | Phase 7 profiles added for commercial Linux and local development use |
| Phase 7 status | Commercialization foundation complete; commercial production remains blocked on native SCTP verification, external lab evidence, signing, and SBOM |
| Native SCTP implementation | Phase 8 foundation-ready: Linux SCTP probe, socket factory, endpoint planner, native adapter, connector, listener, lab profile, readiness report, and commercial gate integration; production readiness still requires Linux SCTP lab verification |
| Interoperability lab | Phase 9 foundation-ready: lab scenario catalog, artifact manifests, run reports, OpenSS7/IPSS7 M3UA ASP-to-SG template, trace comparison, evidence promotion, opt-in CI profile, readiness report, and commercial gate integration added; production readiness still requires passing external lab evidence |

## Requirements

- .NET 10 SDK
- Windows or Linux development environment
- Git for source control

## Build And Test

```powershell
dotnet build src\sigtran.net.sln
dotnet run --project src\sigtran.net.Tests\sigtran.net.Tests.csproj
dotnet pack src\sigtran.net\sigtran.net.csproj -c Release
```

Public API XML comments are required. The library treats missing public documentation (`CS1591`) as an error so that generated NuGet packages remain usable by downstream developers.

## M3UA Example

```csharp
using sigtran.net.Layers.M3UA;

Span<byte> buffer = stackalloc byte[256];
M3uaAffectedPointCode[] affected =
[
    new(mask: 0, pointCode: 0x00112233)
];

bool built = M3uaMessageBuilder.BuildSignallingCongestion(
    buffer,
    networkAppearance: 7,
    routingContexts: [0x55],
    affectedPointCodes: affected,
    concernedDestination: new M3uaAffectedPointCode(0, 0x0000AAAA),
    congestionLevel: 2,
    infoString: "scon"u8,
    out int written,
    out string? error);

if (!built)
{
    throw new InvalidOperationException(error);
}

M3uaMessage message = new();
if (!message.TryDecode(buffer[..written], out error))
{
    throw new InvalidOperationException(error);
}

if (!M3uaTypedMessageParser.TryParseSignallingCongestion(
        message,
        out M3uaSignallingCongestionMessage? scon,
        out error))
{
    throw new InvalidOperationException(error);
}
```

## Documentation

- [SDK roadmap](docs/SDK_ROADMAP.md)
- [Alpha release checklist](docs/ALPHA_RELEASE.md)
- [Architecture](docs/ARCHITECTURE.md)
- [M3UA implementation notes](docs/M3UA.md)
- [M3UA typed dispatcher](docs/DISPATCHER.md)
- [M3UA Payload Data](docs/DATA.md)
- [M3UA Diagnostics](docs/DIAGNOSTICS.md)
- [M3UA Payload Routing](docs/ROUTING.md)
- [M3UA Inbound Processing](docs/PROCESSING.md)
- [M3UA Outbound Processing](docs/OUTBOUND.md)
- [M3UA Transport Session](docs/TRANSPORT_SESSION.md)
- [M3UA ASP Client](docs/ASP_CLIENT.md)
- [M3UA Management Messages](docs/MANAGEMENT.md)
- [M3UA Signalling Network Management](docs/SSNM.md)
- [M3UA Routing Key Management](docs/RKM.md)
- [MTP3 Routing](docs/MTP3.md)
- [SCCP](docs/SCCP.md)
- [TCAP](docs/TCAP.md)
- [MAP SMS Profile](docs/MAP.md)
- [Interoperability and Tooling](docs/INTEROPERABILITY.md)
- [Phase 6 Summary](docs/PHASE6_SUMMARY.md)
- [Phase 7 Commercialization](docs/PHASE7_COMMERCIALIZATION.md)
- [Phase 7 Summary](docs/PHASE7_SUMMARY.md)
- [Phase 8 Native SCTP](docs/PHASE8_NATIVE_SCTP.md)
- [Phase 8 Summary](docs/PHASE8_SUMMARY.md)
- [Phase 9 Interoperability Lab](docs/PHASE9_INTEROP_LAB.md)
- [Phase 9 Summary](docs/PHASE9_SUMMARY.md)
- [Phase 10 Release Automation](docs/PHASE10_RELEASE_AUTOMATION.md)
- [Phase 10 Summary](docs/PHASE10_SUMMARY.md)
- [Phase 11 Developer Experience](docs/PHASE11_DEVELOPER_EXPERIENCE.md)
- [Phase 11 Summary](docs/PHASE11_SUMMARY.md)
- [Phase 12 Operations](docs/PHASE12_OPERATIONS.md)
- [Continuous Integration](docs/CI.md)
- [SCTP Transport](docs/SCTP_TRANSPORT.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Observability](docs/OBSERVABILITY.md)
- [Deployment profiles](docs/DEPLOYMENT.md)
- [References](docs/REFERENCES.md)
- [Quality and contribution rules](docs/QUALITY.md)
- [Security policy](SECURITY.md)

## Project Direction

The roadmap is intentionally conservative:

1. Finish remaining M3UA protocol coverage and API polish.
2. Add a production SCTP transport story.
3. Harden SCCP with external interoperability vectors and trace validation.
4. Harden TCAP with external interoperability vectors and MAP profile validation.
5. Harden MAP SMS with external interoperability vectors and operator-profile validation.
6. Use completed interoperability tooling to run external lab validation, native SCTP verification, and release automation hardening.
7. Complete commercial readiness gates and publish governed release candidates.
8. Complete Linux native SCTP transport and verification.
9. Capture real interoperability lab evidence and promote passing artifacts into commercial readiness.
10. Complete release automation and supply-chain hardening for governed package publication.
11. Complete developer experience and enterprise adoption guidance.
12. Complete production operations and support readiness.

## License

This project is licensed under the Apache License 2.0.
