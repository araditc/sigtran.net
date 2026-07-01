# SIGTRAN.NET

**SIGTRAN.NET is the first open-source .NET 10 SDK dedicated to SIGTRAN and SS7-over-IP protocol engineering.**

The project brings telecom signaling infrastructure to the modern .NET ecosystem, with an engineering focus on M3UA, SCTP, SCCP, TCAP, MAP, byte-level protocol validation, interoperability evidence, and production-oriented release governance.

SIGTRAN.NET is open source under the Apache-2.0 license and welcomes contributors from the telecom, protocol engineering, Linux networking, and .NET communities.

Repository: <https://github.com/araditc/sigtran.net>

---

## Status

SIGTRAN.NET is currently in an **alpha / preview** track.

The first production-oriented milestone is **M3UA over a transport abstraction**. SCTP, SCCP, TCAP, and MAP foundations are present in the project roadmap and implementation work, but production claims remain gated until retained interoperability evidence, Linux SCTP validation, external peer testing, and stable release governance are complete.

For the current public alpha preparation, see:

- [Changelog](CHANGELOG.md)
- [v0.1.0-alpha release notes](docs/releases/v0.1.0-alpha.md)
- [Publish v0.1.0-alpha preview release checklist](https://github.com/araditc/sigtran.net/issues/1)

> Alpha notice: SIGTRAN.NET is not yet a production-ready telecom signaling stack. Use it for evaluation, protocol review, experimentation, testing, and contribution until production evidence is complete.

---

## Why SIGTRAN.NET?

SS7 and SIGTRAN remain important in telecom signaling, mobile messaging, roaming, interconnection, and carrier-grade integration.

The .NET ecosystem has historically had limited native, open-source tooling for this domain. SIGTRAN.NET is designed to close that gap by providing a standards-oriented, testable, and maintainable SDK for SS7-over-IP protocol engineering in C# and .NET 10.

---

## Current Engineering Focus

The current alpha track focuses on:

- M3UA framing, parsing, routing, diagnostics, and ASP state handling.
- Transport abstraction for SIGTRAN workloads.
- Official layer contracts from SCTP through MAP SMS for dependency-injected applications.
- SCTP direction, including Linux native SCTP validation.
- SCCP, TCAP, and MAP foundations for future standards-oriented layers.
- Byte-level tests and protocol validation.
- Wireshark-friendly diagnostics and trace-oriented tooling.
- Interoperability lab planning and external peer validation.
- NuGet/package readiness, documentation, and release governance.

---

## Protocol Scope

| Area | Current Direction |
| --- | --- |
| M3UA | First production-oriented milestone; framing, parsing, routing, ASP state, diagnostics, and management flows are the main focus. |
| SCTP | Transport abstraction is in place; Linux native SCTP validation is part of the production-readiness path. |
| SCCP | Foundation work exists and is moving toward standards-oriented validation. |
| TCAP | Foundation work exists and requires retained interoperability and MAP profile validation. |
| MAP | SMS-oriented MAP foundations are part of the roadmap and require external validation before production claims. |
| Tooling | Byte-level tests, protocol diagnostics, trace comparison, and interoperability evidence are core project principles. |

---

## Requirements

- .NET 10 SDK
- Git
- Windows or Linux for development
- Linux for native SCTP validation and production-oriented transport testing

---

## Build and Test

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

Public API XML comments are required. Missing public documentation is treated as a build-quality issue so that generated packages remain usable by downstream developers.

---

## M3UA Example

```csharp
using Sigtran.NET.Layers.M3UA;

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

---

## Documentation

Start here:

- [SDK roadmap](docs/SDK_ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Layer contracts](docs/LAYER_CONTRACTS.md)
- [M3UA implementation notes](docs/M3UA.md)
- [SCTP transport](docs/SCTP_TRANSPORT.md)
- [SCCP](docs/SCCP.md)
- [TCAP](docs/TCAP.md)
- [MAP SMS profile](docs/MAP.md)
- [Interoperability and tooling](docs/INTEROPERABILITY.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Quality and contribution rules](docs/QUALITY.md)
- [API naming policy](docs/API_NAMING.md)
- [Security policy](SECURITY.md)

Release and governance:

- [Changelog](CHANGELOG.md)
- [v0.1.0-alpha release notes](docs/releases/v0.1.0-alpha.md)
- [Alpha release checklist](docs/ALPHA_RELEASE.md)
- [Production readiness report](docs/COMMERCIAL_READINESS_REPORT.md)

---

## Contributing

Contributions are welcome.

You can contribute through:

- Code changes.
- Protocol review.
- M3UA validation.
- SCTP testing on Linux.
- SCCP, TCAP, and MAP standards alignment.
- ASN.1 BER validation.
- Wireshark trace comparison.
- Interoperability lab results.
- Documentation and examples.
- Performance and memory-allocation review.
- Issue reports and design discussions.

Good first contribution areas include documentation improvements, protocol test vectors, validation reports, issue triage, and small focused M3UA test cases.

Please open an issue or pull request if you want to help. Telecom protocol expertise, .NET infrastructure experience, and real-world signaling validation are especially valuable.

---

## Production-Readiness Policy

SIGTRAN.NET is being developed with a conservative production-readiness model.

Stable production support requires:

- Retained Linux SCTP verification evidence.
- External SIGTRAN peer interoperability evidence.
- Protocol trace validation.
- Stable package publication evidence.
- Stable API lifecycle validation.
- Security, release, compliance, and operations review.

Until those gates are complete, the project should be treated as alpha / preview infrastructure.

---

## License

SIGTRAN.NET is licensed under the [Apache License 2.0](LICENSE).
