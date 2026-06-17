# SIGTRAN.NET

SIGTRAN.NET is a .NET 10 SDK for building SS7-over-IP applications. The project is being shaped from an early proof of concept into a standards-oriented, open-source SDK with stable public APIs, documented protocol behavior, and byte-level tests.

The first production milestone is M3UA over a transport abstraction. SCCP, TCAP, and MAP code still exist in the repository, but those layers are currently experimental until their simplified encodings are replaced with standards-based implementations.

## Current Status

| Area | Status |
| --- | --- |
| SDK packaging | .NET 10 library, NuGet metadata, XML documentation generation, package validation |
| M3UA common framing | Header parser, TLV reader/writer, padding handling |
| M3UA transfer | DATA builders/parsers with Network Appearance, Routing Context, Protocol Data, and Correlation Id |
| M3UA DATA routing | Payload route table for matching DATA by Network Appearance, Routing Context, DPC, and SI |
| M3UA inbound processing | Decode, typed dispatch, ASP ACK state updates, and DATA route resolution |
| M3UA outbound processing | State-aware builders with association defaults for ASP lifecycle, RKM, and DATA |
| M3UA transport session | Async receive/send facade over `ISctpSocket` using inbound/outbound processors |
| M3UA ASP client | ASP startup, heartbeat, and shutdown helpers over the transport session |
| M3UA ASPSM | ASP Up/Down, Heartbeat, acknowledgements, typed parsing |
| M3UA ASPTM | ASP Active/Inactive, acknowledgements, typed parsing |
| M3UA management | Error and Notify builders/parsers plus transport send helpers |
| M3UA SSNM | DUNA, DAVA, DAUD, DRST, DUPU, and SCON builders/parsers |
| M3UA RKM | REG REQ, REG RSP, DEREG REQ, and DEREG RSP builders/parsers plus client helpers |
| ASP state | Local acknowledgement-driven ASP session state machine |
| SCTP | Transport contract plus a development TCP adapter; production SCTP is planned |
| SCCP/TCAP/MAP | Experimental proof-of-concept code; not yet interoperable |

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
- [Architecture](docs/ARCHITECTURE.md)
- [M3UA implementation notes](docs/M3UA.md)
- [M3UA typed dispatcher](docs/DISPATCHER.md)
- [M3UA Payload Data](docs/DATA.md)
- [M3UA Payload Routing](docs/ROUTING.md)
- [M3UA Inbound Processing](docs/PROCESSING.md)
- [M3UA Outbound Processing](docs/OUTBOUND.md)
- [M3UA Transport Session](docs/TRANSPORT_SESSION.md)
- [M3UA ASP Client](docs/ASP_CLIENT.md)
- [M3UA Management Messages](docs/MANAGEMENT.md)
- [M3UA Routing Key Management](docs/RKM.md)
- [References](docs/REFERENCES.md)
- [Quality and contribution rules](docs/QUALITY.md)

## Project Direction

The roadmap is intentionally conservative:

1. Finish remaining M3UA protocol coverage and API polish.
2. Add a production SCTP transport story.
3. Replace simplified SCCP with standards-based SCCP.
4. Replace simplified TCAP with ASN.1 BER based TCAP.
5. Add MAP SMS profiles and high-level client APIs.
6. Add interoperability tooling, simulators, CI, and release automation.

## License

This project is licensed under the Apache License 2.0.
