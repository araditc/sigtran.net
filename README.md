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
- [Continuous Integration](docs/CI.md)
- [SCTP Transport](docs/SCTP_TRANSPORT.md)
- [References](docs/REFERENCES.md)
- [Quality and contribution rules](docs/QUALITY.md)

## Project Direction

The roadmap is intentionally conservative:

1. Finish remaining M3UA protocol coverage and API polish.
2. Add a production SCTP transport story.
3. Harden SCCP with external interoperability vectors and trace validation.
4. Harden TCAP with external interoperability vectors and MAP profile validation.
5. Harden MAP SMS with external interoperability vectors and operator-profile validation.
6. Use completed interoperability tooling to run external lab validation, native SCTP verification, and release automation hardening.
7. Complete commercial readiness gates and publish governed release candidates.

## License

This project is licensed under the Apache License 2.0.
