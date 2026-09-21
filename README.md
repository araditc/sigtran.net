# SIGTRAN.NET

**SIGTRAN.NET is the first open-source .NET 10 SDK dedicated to SIGTRAN and SS7-over-IP protocol engineering.**

SIGTRAN.NET brings telecom signaling infrastructure to the modern .NET ecosystem, with a focus on native Linux SCTP, M3UA, M2PA, MTP3, SCCP, TCAP, MAP SMS, byte-level protocol validation, interoperability evidence, and production-oriented release governance.

Repository: <https://github.com/araditc/sigtran.net>

---

## What it is

SIGTRAN.NET is a standards-oriented C#/.NET SDK for building and testing SS7-over-IP and SIGTRAN signaling applications.

The project is designed for telecom engineers, protocol specialists, .NET infrastructure developers, and teams working with signaling gateways, SS7/SIGTRAN interop, GSM/MAP SMS flows, M3UA, SCTP, SCCP, TCAP, or MAP.

---

## Current status

SIGTRAN.NET is currently in a **public release-candidate** track.

- Latest public NuGet prerelease: `Sigtran.NET` `1.0.0-rc.1`.
- Current source candidate: unpublished `1.0.0-rc.2`.
- Target framework: `.NET 10` / `net10.0`.
- License: Apache-2.0.
- GitHub Releases/tags: pending; the repository currently retains release evidence and release notes, but the GitHub release/tag must still be created from an approved commit.

The SDK has retained evidence for native Linux SCTP, external SCTP/M3UA traffic, cross-implementation full-stack MAP SMS traffic, RC publication, package restore, operations-host smoke execution, and performance/resilience runs.

It is **not yet a fully stable operator-grade SS7/SIGTRAN stack**. Stable production claims remain gated on independent M2PA evidence, operator/vendor-profile interoperability evidence, representative multi-host capacity/soak evidence, Kubernetes SCTP validation, trusted signing, and final stable release execution.

Recommended release/status documents:

- [Changelog](CHANGELOG.md)
- [v1.0.0-rc.2 release notes draft](docs/releases/v1.0.0-rc.2.md)
- [Production readiness report](docs/COMMERCIAL_READINESS_REPORT.md)
- [Stable release execution](docs/PHASE55_STABLE_RELEASE_EXECUTION.md)
- [Community response templates](docs/community/COMMUNITY_RESPONSES.md)

> RC notice: use the SDK for controlled integrations, lab traffic, protocol review, and contributor validation. Do not represent it as a complete stable operator-grade SS7 stack until the remaining stable gates are closed.

---

## Install

The latest public prerelease package is `1.0.0-rc.1`:

```powershell
dotnet add package Sigtran.NET --version 1.0.0-rc.1
```

Source builds currently produce the unpublished `1.0.0-rc.2` candidate by default to avoid accidental stable `1.0.0` package creation.

---

## Why SIGTRAN.NET?

SS7 and SIGTRAN remain important in telecom signaling, mobile messaging, roaming, interconnection, carrier-grade integration, and legacy-to-modern telecom infrastructure.

The .NET ecosystem has historically had limited native open-source tooling for this domain. SIGTRAN.NET is intended to close that gap by providing a testable, maintainable, evidence-driven SDK for SS7-over-IP protocol engineering in C# and .NET 10.

---

## Current engineering focus

The current RC engineering track focuses on:

- M3UA codec, routing, ASP state, diagnostics, and long-running runtime behavior.
- Native Linux SCTP direction with stream id, PPID, receive metadata, reconnect, metrics, and graceful shutdown evidence.
- M2PA as a parallel MTP2 path with independent peer evidence still pending.
- Stateful SCCP service support for UDT/XUDT/LUDT, GT translation, routing, segmentation/reassembly, UDTS return policy, metrics, cancellation, and MTP3 receive ownership.
- TCAP dialogue management with Begin/Continue/End/Abort, invoke tracking, result/error/reject handling, timeout scanning, cleanup, snapshots, and metrics.
- MAP SMS workflows for SRI-SM, MO/MT ForwardSM, ReportSM-DeliveryStatus, and AlertServiceCentre.
- Cross-implementation MAP SMS traffic through TCAP, SCCP, M3UA, and native SCTP.
- Runtime health probes, OpenTelemetry-compatible tracing and metrics, structured JSONL events, validated node configuration, container manifests, Kubernetes manifests, and operations runbooks.
- Byte-level tests, Wireshark-friendly diagnostics, trace comparison, and retained interoperability evidence.
- NuGet/package readiness, public API governance, and guarded stable release workflow execution.

---

## Protocol scope

| Area | Current direction |
| --- | --- |
| M3UA | Codec, routing, ASP state, runtime, bounded queues, heartbeat supervision, reconnect/failover hooks, diagnostics, and external peer evidence. |
| SCTP | Native Linux SCTP validation for stream id, PPID, receive metadata, reconnect, metrics, graceful shutdown, and external peer traffic. |
| M2PA | RFC 4165 codec and stateful runtime with alignment, proving, 24-bit sequencing, acknowledgement, retrieval retention, congestion handling, processor-outage recovery, metrics, and transport replacement. Independent peer evidence remains. |
| SCCP | Stateful service for UDT/XUDT/LUDT, global-title translation, routing, bounded segmentation/reassembly, UDTS return policy, metrics, cancellation, and MTP3 receive ownership. Operator/vendor profile evidence remains. |
| TCAP | Dialogue manager for transaction correlation, Begin/Continue/End/Abort, tracked invokes, Result/Error/Reject outcomes, timeout scanning, cleanup, snapshots, and metrics. Operator/vendor profile evidence remains. |
| MAP SMS | Client/server SMS workflows for SRI-SM, MO/MT ForwardSM, ReportSM-DeliveryStatus, and AlertServiceCentre. Repository-profile evidence exists; operator/vendor profile evidence remains. |
| Performance | Retained full-stack performance evidence exists, but representative multi-host capacity and long-duration soak evidence remain stable blockers. |
| Operations | Health probes, BCL telemetry, structured events, validated configuration, live/ready/metrics endpoints, container manifests, Kubernetes manifests, and runbooks. |

---

## Requirements

- .NET 10 SDK
- Git
- Windows or Linux for development
- Linux for native SCTP validation and production-oriented transport testing

---

## Build and test

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

Public API XML comments are required. Missing public documentation is treated as a build-quality issue so generated packages remain usable by downstream developers.

---

## M3UA example

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
- [M2PA implementation notes](docs/M2PA.md)
- [SCTP transport](docs/SCTP_TRANSPORT.md)
- [SCCP](docs/SCCP.md)
- [TCAP](docs/TCAP.md)
- [MAP SMS service](docs/MAP.md)
- [Runtime operations](docs/OPERATIONS_RUNTIME.md)
- [Public API baseline](docs/PUBLIC_API_BASELINE.md)
- [Interoperability and tooling](docs/INTEROPERABILITY.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Quality and contribution rules](docs/QUALITY.md)
- [API naming policy](docs/API_NAMING.md)
- [Security policy](SECURITY.md)

Release and governance:

- [Changelog](CHANGELOG.md)
- [v1.0.0-rc.2 release notes draft](docs/releases/v1.0.0-rc.2.md)
- [RC.1 to RC.2 migration](docs/migrations/1.0.0-rc.1-to-rc.2.md)
- [Production readiness report](docs/COMMERCIAL_READINESS_REPORT.md)
- [Phase 55 stable release execution](docs/PHASE55_STABLE_RELEASE_EXECUTION.md)
- [Phase 55 stable assessment evidence](docs/evidence/PHASE55_STABLE_ASSESSMENT_20260724T110519Z.json)

---

## Contributing

Contributions are welcome.

Useful contribution areas include:

- Protocol review.
- M3UA validation.
- SCTP testing on Linux.
- Independent M2PA peer validation.
- SCCP, TCAP, and MAP standards/profile alignment.
- ASN.1 BER validation.
- Wireshark trace comparison.
- Interoperability lab results.
- Operator/vendor profile validation.
- Documentation and examples.
- Performance and memory-allocation review.
- Issue reports and design discussions.

Good first contribution areas include documentation improvements, protocol test vectors, validation reports, issue triage, and focused M3UA/MAP SMS examples.

Please open an issue or pull request if you want to help. Telecom protocol expertise, .NET infrastructure experience, Linux SCTP experience, and real-world signaling validation are especially valuable.

For ready-to-use outreach copy, see [Community response templates](docs/community/COMMUNITY_RESPONSES.md).

---

## Production-readiness policy

SIGTRAN.NET is being developed with a conservative production-readiness model.

Stable production support requires:

- Retained Linux SCTP verification evidence.
- External SIGTRAN peer interoperability evidence.
- Independent M2PA peer evidence.
- Stateful SCCP, TCAP, and MAP SMS service validation.
- Operator/vendor-profile end-to-end protocol trace validation.
- Operator-sized capacity and resilience evidence.
- Representative Kubernetes SCTP/CNI deployment evidence.
- Trusted package signing and provenance.
- Stable package publication evidence.
- Stable API lifecycle validation.
- A machine-evaluated `GO` stable release decision.
- Security, release, compliance, and operations review.

Until those gates are complete, the package should be treated as release-candidate infrastructure for controlled integrations.

---

## License

SIGTRAN.NET is licensed under the [Apache License 2.0](LICENSE).
