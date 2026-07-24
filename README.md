# SIGTRAN.NET

**SIGTRAN.NET is the first open-source .NET 10 SDK dedicated to SIGTRAN and SS7-over-IP protocol engineering.**

The project brings telecom signaling infrastructure to the modern .NET ecosystem, with an engineering focus on M3UA, SCTP, SCCP, TCAP, MAP, byte-level protocol validation, interoperability evidence, and production-oriented release governance.

SIGTRAN.NET is open source under the Apache-2.0 license and welcomes contributors from the telecom, protocol engineering, Linux networking, and .NET communities.

Repository: <https://github.com/araditc/sigtran.net>

---

## Status

SIGTRAN.NET is currently in a **public release-candidate** track. Version
`1.0.0-rc.1` has been published and restored from NuGet.org.

Native Linux SCTP, external SCTP/M3UA, and cross-implementation full-stack MAP
SMS traffic evidence are retained. Stateful M3UA, M2PA, SCCP, TCAP, and MAP SMS
runtimes are available. Full stable-product readiness remains gated on
operator/vendor profile interoperability, independent M2PA evidence,
20K peak and operator-sized multi-host performance evidence, representative
Kubernetes SCTP validation, trusted signing, and stable release execution.

For release history and governance, see:

- [Changelog](CHANGELOG.md)
- [v0.1.0-alpha release notes](docs/releases/v0.1.0-alpha.md)
- [Production readiness report](docs/COMMERCIAL_READINESS_REPORT.md)

> RC notice: use the package for controlled integration and lab traffic. Do not
> claim a complete operator-grade SS7 stack until the remaining runtime, protocol,
> capacity, and stable-release gates are closed.

---

## Why SIGTRAN.NET?

SS7 and SIGTRAN remain important in telecom signaling, mobile messaging, roaming, interconnection, and carrier-grade integration.

The .NET ecosystem has historically had limited native, open-source tooling for this domain. SIGTRAN.NET is designed to close that gap by providing a standards-oriented, testable, and maintainable SDK for SS7-over-IP protocol engineering in C# and .NET 10.

---

## Current Engineering Focus

The current RC engineering track focuses on:

- M3UA framing, parsing, routing, diagnostics, and ASP state handling.
- Transport abstraction for SIGTRAN workloads.
- Official layer contracts from SCTP through MAP SMS for dependency-injected applications.
- Linux native SCTP production transport with retained stream/PPID metadata,
  reconnect, metrics, and graceful-shutdown evidence.
- External SCTP/M3UA peer validation with PCAP, SDK trace, peer log, and TShark
  comparison evidence.
- Cross-implementation MAP SMS traffic through TCAP, SCCP, M3UA, and native SCTP
  with retained PCAP, traces, field comparison, and digest evidence.
- Full-stack performance and resilience tooling with a retained 62,000-operation
  baseline, latency/resource metrics, SCTP failover, and zero-loss recovery.
- Executable health probes, OpenTelemetry-compatible tracing and metrics,
  structured JSONL events, validated node configuration, and a containerized
  M3UA operations host with Kubernetes probes.
- Stateful SCCP, TCAP, and MAP SMS service layers.
- M2PA as a parallel MTP2 path.
- Byte-level tests and protocol validation.
- Wireshark-friendly diagnostics and trace-oriented tooling.
- Interoperability lab planning and external peer validation.
- NuGet/package readiness, documentation, and release governance.

---

## Protocol Scope

| Area | Current Direction |
| --- | --- |
| M3UA | Codec, routing, ASP state, long-running `IMtp3Network` runtime, bounded queues, heartbeat supervision, reconnect/failover hooks, diagnostics, and external peer evidence are available. |
| SCTP | Native Linux SCTP evidence validates stream id, PPID, receive metadata, reconnect, metrics, graceful shutdown, and external peer traffic. |
| M2PA | RFC 4165 codec and stateful `IMtp2Link` runtime provide alignment, proving, 24-bit sequencing, acknowledgement, retrieval retention, congestion handling, processor-outage recovery, metrics, and transport replacement. Independent peer evidence remains. |
| SCCP | Stateful `ISccpService` supports UDT/XUDT/LUDT, GT translation, routing, bounded segmentation/reassembly, UDTS return policy, metrics, cancellation, and MTP3 receive ownership. Independent C-peer UDT evidence is retained; operator/vendor profile evidence remains. |
| TCAP | Concurrent `TcapDialogueManager` provides transaction correlation, Begin/Continue/End/Abort, tracked invokes, Result/Error/Reject outcomes, shared timeout scanning, bounded queues, cleanup, snapshots, and metrics. Independent C-peer transaction evidence is retained; operator/vendor profile evidence remains. |
| MAP | Stateful client/server SMS workflows cover SRI-SM, MO/MT ForwardSM, ReportSM-DeliveryStatus, and AlertServiceCentre with operation profiles, result/error correlation, typed dispatch, cancellation, and metrics. All five operations have cross-implementation repository-profile evidence; operator/vendor profile evidence remains. |
| Performance | The retained WSL baseline completed 62,000 full-stack operations without protocol loss at about 13.5K sustained/peak TPS. The 20K peak and multi-host operator qualification gates remain open. |
| Operations | Runtime health probes, BCL telemetry, structured events, validated configuration, live/ready/metrics endpoints, container manifests, and recovery/upgrade runbooks are available. |
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
- [Phase 47 M3UA runtime](docs/PHASE47_M3UA_RUNTIME.md)
- [M2PA implementation notes](docs/M2PA.md)
- [Phase 48 M2PA production path](docs/PHASE48_M2PA_PRODUCTION_PATH.md)
- [SCTP transport](docs/SCTP_TRANSPORT.md)
- [Phase 45 native SCTP production transport](docs/PHASE45_NATIVE_SCTP_PRODUCTION_TRANSPORT.md)
- [Phase 46 evidence and readiness reconciliation](docs/PHASE46_EVIDENCE_READINESS_RECONCILIATION.md)
- [SCCP](docs/SCCP.md)
- [Phase 49 SCCP stateful service](docs/PHASE49_SCCP_STATEFUL_SERVICE.md)
- [TCAP](docs/TCAP.md)
- [Phase 50 TCAP dialogue manager](docs/PHASE50_TCAP_DIALOGUE_MANAGER.md)
- [MAP SMS service](docs/MAP.md)
- [Phase 51 MAP SMS service](docs/PHASE51_MAP_SMS_SERVICE.md)
- [Phase 52 end-to-end SS7 traffic lab](docs/PHASE52_END_TO_END_SS7_TRAFFIC_LAB.md)
- [Phase 53 performance and resilience](docs/PHASE53_OPERATOR_PERFORMANCE_RESILIENCE.md)
- [Phase 54 production operations package](docs/PHASE54_PRODUCTION_OPERATIONS_PACKAGE.md)
- [Runtime operations](docs/OPERATIONS_RUNTIME.md)
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
- Independent M2PA peer evidence.
- Stateful SCCP, TCAP, and MAP SMS service validation.
- Operator/vendor-profile end-to-end protocol trace validation.
- Operator-sized capacity and resilience evidence.
- Trusted package signing and provenance.
- Stable package publication evidence.
- Stable API lifecycle validation.
- Security, release, compliance, and operations review.

The Linux SCTP, external M3UA, repository-profile full-stack traffic, and
controlled performance execution gates are complete. Until the remaining gates
are complete, the package should be treated as release-candidate infrastructure
for controlled integrations.

---

## License

SIGTRAN.NET is licensed under the [Apache License 2.0](LICENSE).
