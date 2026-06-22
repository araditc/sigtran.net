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
| Operations | Phase 12 foundation-ready: production operations capability catalog, runbook catalog, incident response targets, health check matrix, rollback plan, maintenance policy, support handbook, readiness report, operations CI profile, and status report added; production operations still require commercial readiness |
| Compliance and audit | Phase 13 foundation-ready: compliance capability catalog, audit event catalog, evidence retention policy, license compliance policy, data handling classification, export-control policy, readiness report, compliance CI profile, commercial compliance gate, and status report added; enterprise compliance claims still require commercial readiness and adopter-specific legal/regulatory review |
| Performance and capacity | Phase 14 foundation-ready: performance capability catalog, benchmark scenario catalog, capacity profile, throughput targets, latency budgets, load-test plan, resource budget, readiness report, performance CI profile, and status report added; production performance claims still require representative benchmark evidence |
| API lifecycle | Phase 15 foundation-ready: API surface catalog, stability contracts, version matrix, deprecation policy, migration guide catalog, breaking-change review policy, public API baseline, readiness report, API lifecycle CI profile, and status report added; stable API lifecycle claims still require commercial readiness and validation evidence |
| Configuration readiness | Phase 16 foundation-ready: configuration schema, validation helpers, environment matrix, secret policy, transport configuration, routing configuration, readiness report, configuration CI profile, commercial configuration gate, and status report added; production configuration still requires commercial readiness and deployment review |
| Native SCTP lab verification | Phase 17 foundation-ready: Linux SCTP lab scenario catalog, artifact manifest, run plan, command set, run reports, evidence registry, readiness report, lab CI profile, commercial gate, and status report added; production verification still requires complete passing Linux SCTP evidence |
| External peer interop execution | Phase 18 foundation-ready: package-neutral external peer environment, ASP-to-SG configuration, trace expectations, artifact manifest, run plan, command set, run reports, evidence registry, readiness report, CI profile, and status report added; verification still requires real retained lab evidence |
| Protocol interop vectors | Phase 19 foundation-ready: SCCP, TCAP, and MAP SMS vector catalog, external references, artifact manifest, comparison rules, run plan, command set, run reports, evidence registry, readiness report, CI profile, and status report added; verification still requires real reference vectors, SDK vectors, and comparison reports |
| Commercial evidence dossier | Phase 20 foundation-ready: commercial evidence requirements, artifact contract, manifest, bundle, gate, readiness report, CI profile, status report, and source status naming normalization added; commercial evidence readiness still requires real retained artifacts and verification gates |
| Supply chain automation | Phase 21 foundation-ready: supply-chain automation plan, SBOM generation contract, package signing contract, signature verification contract, provenance attestation contract, artifact manifest, gate, readiness report, CI profile, and status report added; promotion still requires real signed artifacts and commercial evidence |
| Release workflow orchestration | Phase 23 foundation-ready: release workflow triggers, stages, required secrets, supply-chain integration, commercial evidence verification, publish contract, concrete workflow file, YAML validation, publish guard, artifact retention, least-privilege permissions, concurrency policy, environment contract, promotion gate, readiness report, and status report added; promotion still requires real release evidence |
| Package publication readiness | Phase 24 foundation-ready: release version policy, NuGet metadata contract, package output layout, dry-run publish plan, credential policy, channel policy, package integrity manifest, publication evidence manifest, publication gate, readiness status, and documentation added; real NuGet publication remains blocked until retained evidence, signing, SBOM, provenance, and live credentials are available |
| Commercial release execution | Phase 25 execution foundation complete: retained execution evidence manifest, real Ubuntu VM Linux SCTP smoke capture evidence, structured external peer blocker evidence, artifact dossier, executable SBOM generation evidence, package signing execution evidence, provenance attestation evidence, smoke benchmark evidence, public API baseline evidence, and final readiness report added; commercial release remains blocked on external peer interop artifacts, trusted timestamped signing, and production benchmark evidence |
| Commercial roadmap realignment | Phase 26 complete: package-specific source API names were replaced with package-neutral external SIGTRAN peer contracts; commercial release gates now use external peer readiness instead of package-specific interop names |
| Package governance | Phase 7 policy added: current package metadata is tracked; commercial target still requires package signing and SBOM automation |
| Security governance | Phase 7 security policy added with private disclosure and severity response targets |
| Compatibility policy | Phase 7 policy added: net10.0 target, SemVer, pre-stable breaking-change allowance, and stable major-version rule |
| Observability | Phase 7 profile added for commercial metrics, trace categories, and health signals |
| Deployment profiles | Phase 7 profiles added for commercial Linux and local development use |
| Phase 7 status | Commercialization foundation complete; commercial production remains blocked on native SCTP verification, external lab evidence, signing, and SBOM |
| Native SCTP implementation | Phase 8 foundation-ready: Linux SCTP probe, socket factory, endpoint planner, native adapter, connector, listener, lab profile, readiness report, and commercial gate integration; production readiness still requires Linux SCTP lab verification |
| Interoperability lab | Phase 9 foundation-ready: lab scenario catalog, artifact manifests, run reports, external peer M3UA ASP-to-SG template, trace comparison, evidence promotion, opt-in CI profile, readiness report, and commercial gate integration added; production readiness still requires passing external lab evidence |
| Maintained peer lab automation | Phase 28 foundation-ready: executable run manifest, deterministic environment file rendering, command script rendering, artifact digest manifests, comparison report, run report, evidence bundle, manual self-hosted workflow template, commercial readiness bridge, and automation status reporting are available; commercial release still requires real retained maintained-peer evidence |
| Maintained peer lab runner | Phase 29 foundation-ready: runner workspace, execution input bundle, output artifact materialization, preflight, command manifest, evidence collection, digest generation, comparison handoff, workflow readiness, and status reporting contracts are available; commercial release still requires execution against a real maintained external peer |
| Maintained peer lab runner operations | Phase 30 foundation-ready: file materialization plan rendering, execution log, command outcome, artifact verification, runner provenance, failure classification, retry policy, evidence package manifest, operator handoff, and operations status contracts are available; commercial release still requires execution against a real maintained external peer |
| Native SCTP production hardening | Phase 31 foundation-complete: outbound stream/PPID framing, association lifecycle journal, reconnect schedule, send backpressure, cancellation/timeout, multi-homing readiness, fault recovery, transport diagnostics, production hardening readiness gate, and status reporting are available; production readiness still requires retained Linux SCTP and external peer evidence |
| SCCP/TCAP/MAP evidence upgrade | Phase 32 SDK evidence-backed: shared protocol evidence vectors, SCCP/TCAP/MAP SMS evidence suites, cross-layer evidence bundle, ordered trace validation, actionable mismatch classification, SDK evidence-backed readiness gates, status reporting, and final validation are available; production evidence remains blocked until retained external interoperability artifacts exist |
| Performance and resilience evidence | Phase 33 foundation-complete: peer-traffic workload evidence, digest-covered artifacts, latency P95/P99, CPU/memory/allocation, failover recovery, publishable Markdown report, production performance evidence gates, manual self-hosted runner/CI handoff, status reporting, and final validation are available; production claims still require retained real peer benchmark evidence and commercial readiness |
| Supply-chain release execution | Phase 34 foundation-complete: final versioned SBOM, trusted timestamped package signing, provenance attestation, public API diff, artifact upload, ordered command-plan, release promotion gate, concrete workflow execution, status reporting, and final validation are available; retained release-run artifacts and commercial evidence are still required |
| RC publish and commercial gate | Phase 35 complete: dry-run release rehearsal, gated NuGet prerelease publication, retained release notes, retained migration notes, final commercial readiness reporting, RC/stable decisioning, RC publication evidence manifest, release workflow channel wiring, status reporting, and final commercial gate report are available; stable publication remains blocked until commercial evidence is complete |
| Commercial evidence readiness lockdown | Phase 36 foundation-complete: release target locking, protected secret readiness, evidence retention mapping, commercial evidence checklist, release preflight, protected release environments, dossier handoff, go/no-go gating, status reporting, and final validation are available; RC and stable publication remain blocked until retained commercial release evidence is complete |
| Commercial evidence execution orchestration | Phase 37 foundation-complete: evidence execution run identity, stage catalog, operator command plan, execution environment contract, artifact collection manifest, digest/redaction verification, blocker classification, retry/resume policy, status reporting, and final validation are available; real retained execution artifacts are still required before commercial publication |
| Commercial evidence artifact intake | Phase 38 in progress: artifact intake target identity, artifact source registration, SHA-256 digest coverage, redaction review, completeness evaluation, dossier reporting, promotion handoff, and execution-to-dossier bridge are available, binding a stable intake id, reviewer identity, UTC receipt time, run-scoped dossier root, concrete source paths, unique retained dossier paths, retained digests, trace-bearing artifact approvals, explicit intake blockers, retained Markdown summary, digest-covered handoff, and end-to-end intake assembly to a governed execution run; status reporting and final validation remain |

## Requirements

- .NET 10 SDK
- Windows or Linux development environment
- Git for source control

## Build And Test

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

Public API XML comments are required. The library treats missing public documentation (`CS1591`) as an error so that generated NuGet packages remain usable by downstream developers.

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

## Documentation

Start here:

- [SDK roadmap](docs/SDK_ROADMAP.md)
- [Complete phase index](docs/PHASE_INDEX.md)
- [Commercial readiness report](docs/COMMERCIAL_READINESS_REPORT.md)
- [Phase 35 commercial gate report](docs/PHASE35_COMMERCIAL_GATE_REPORT.md)
- [Alpha release checklist](docs/ALPHA_RELEASE.md)

Architecture and protocol docs:

- [Architecture](docs/ARCHITECTURE.md)
- [Phases 0 to 5 foundation map](docs/PHASE0_TO_5_FOUNDATION.md)
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
- [SCTP Transport](docs/SCTP_TRANSPORT.md)
- [MTP3 Routing](docs/MTP3.md)
- [SCCP](docs/SCCP.md)
- [TCAP](docs/TCAP.md)
- [MAP SMS Profile](docs/MAP.md)

Release, operations, and governance docs:

- [Interoperability and tooling](docs/INTEROPERABILITY.md)
- [External peer interop migration](docs/EXTERNAL_PEER_INTEROP_MIGRATION.md)
- [Continuous integration](docs/CI.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Observability](docs/OBSERVABILITY.md)
- [Deployment profiles](docs/DEPLOYMENT.md)
- [References](docs/REFERENCES.md)
- [Quality and contribution rules](docs/QUALITY.md)
- [Security policy](SECURITY.md)

## Project Direction

The roadmap is intentionally conservative. The SDK can move toward a governed RC, but stable commercial publication remains blocked until retained evidence is complete.

1. Run the `dry-run` release workflow for the intended RC version and review uploaded artifacts.
2. Execute the maintained external peer lab against real peer traffic and retain PCAP, peer logs, SDK traces, configuration, comparison report, run report, and digests.
3. Re-run native SCTP verification on Linux with retained peer traffic evidence covering stream/PPID, lifecycle, reconnect, backpressure, cancellation, multi-homing readiness, and fault recovery.
4. Promote SCCP, TCAP, and MAP from SDK evidence-backed status to production evidence-backed status using retained external interoperability artifacts.
5. Replace smoke performance evidence with retained peer/load benchmarks covering warmup, sustained, peak, latency P95/P99, CPU, memory, allocation, and failover behavior.
6. Execute release supply-chain automation for the final release commit: SBOM, trusted timestamped signing, signature verification, provenance, public API diff, digest manifest, and artifact upload.
7. Publish a gated NuGet prerelease only after the RC decision report, release notes, migration notes, package artifacts, dry-run evidence, and digests are retained.
8. Keep stable publication blocked until the commercial evidence dossier passes without blockers.
9. After the RC soak period, review API lifecycle, migration notes, operational readiness, compliance posture, and support expectations.
10. Promote to stable only when commercial evidence, release evidence, performance evidence, and supply-chain evidence are complete and reviewable.

## License

This project is licensed under the Apache License 2.0.
