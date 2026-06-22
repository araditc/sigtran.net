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

Status: SDK foundation is complete for MAP SMS operation metadata, typed SMS parameters, TCAP client builders, errors, and extensions. Production claims still require external MAP SMS interoperability vectors and operator-profile validation.

## Phase 6 - Interoperability And Tooling

- Build Wireshark-friendly trace logging and hex dump helpers.
- Add conformance vectors from RFC examples and vendor configuration scenarios.
- Add simulator components for SG, ASP, and MAP SMS test flows.
- Provide samples for ASP-to-SG, IPSP, SCCP/MAP SMS, and local TCP test transport.
- Add CI for build, formatting, package validation, and protocol golden tests.

Status: SDK foundation is complete for trace formatting, conformance vector inventory, built-in M3UA/MAP vectors, simulator scripts, MAP SMS flows, local TCP sample scenarios, sample catalog, CI verification profile, and interoperability readiness reporting. Production claims still require external interoperability lab evidence and native SCTP verification.

## Phase 7 - Commercialization And Release Hardening

- Define commercial readiness gates and make blocked gates visible in public APIs.
- Capture native SCTP verification status and OS support limits.
- Track external interoperability evidence from real peer stacks and packet traces.
- Add release candidate manifests, package governance, security policy, compatibility policy, observability guidance, and deployment profiles.
- Finalize release automation and documentation for commercial adoption.

Status: SDK foundation is complete for commercial readiness gates, native SCTP support matrix, external interoperability evidence tracking, release candidate manifests, package governance, security policy, compatibility policy, observability profile, deployment profiles, and Phase 7 status reporting. Internal release readiness is available; commercial production readiness remains blocked until native SCTP verification, external interoperability evidence, package signing, and SBOM automation are complete.

## Native SCTP Production Transport

- Probe Linux native SCTP socket creation using `SocketType.Seqpacket` and IP protocol number `132`.
- Add native SCTP socket factory, connector, listener, send/receive path, lifecycle events, health snapshots, and reconnect integration.
- Add Linux-focused integration test hooks that can run when SCTP kernel support is available.
- Keep Windows and macOS contract-only until a verified provider is selected.

Status: SDK foundation is complete for Linux native SCTP platform probing, socket creation, endpoint planning, socket adaptation, client connect, server listen/accept, lab profile, readiness reporting, and commercial gate integration. Production readiness remains blocked until Linux SCTP lab verification passes with real kernel SCTP support and peer traffic.

## Real Interoperability Lab

- Define required lab scenarios for Linux native SCTP, external peer M3UA ASP-to-SG, and MAP SMS trace comparison.
- Capture PCAPs, SDK traces, peer configuration, peer logs, and comparison reports.
- Convert passing lab runs into release evidence that can unlock commercial readiness gates.
- Keep evidence pending until artifacts are real and reviewable.

Status: Phase 9 is foundation-ready for scenario catalog, artifact manifests, run reports, external peer lab template, trace comparison, evidence promotion, opt-in CI profile, readiness reporting, and commercial gate integration. Production readiness remains blocked until real external lab artifacts are captured and promoted.

## Release Automation And Supply Chain

- Define deterministic release automation steps for restore, build, test, pack, validation, and publish.
- Track package artifacts, checksums, SBOM requirements, signing requirements, and provenance.
- Add release channel rules, version/release-note requirements, and publish gates.
- Keep commercial release blocked until signing, SBOM, provenance, native SCTP verification, and external interoperability evidence are complete.

Status: Phase 10 is foundation-ready for release automation plan, artifact manifest, SBOM plan, package signing plan, provenance tracking, release notes validation, publish channels, release gate evaluation, release CI profile, and phase documentation. Stable commercial publication remains blocked until real signing, SBOM generation, native SCTP verification, and external interoperability evidence are complete.

## Developer Experience And Adoption

- Add quickstarts, sample inventories, configuration profiles, troubleshooting guidance, and adoption gates.
- Make the shortest M3UA ASP-to-SG path clear for new users.
- Keep production claims tied to readiness and interoperability evidence.

Status: Phase 11 is foundation-ready for capability catalog, M3UA quickstart, sample templates, configuration profiles, troubleshooting index, API reference index, adoption gates, documentation readiness, developer experience CI profile, and phase documentation. Enterprise production adoption remains blocked until commercial readiness is complete.

## Production Operations And Support

- Add operational runbook, incident, health, recovery, and support foundations.
- Keep operations readiness separate from commercial production readiness.
- Make production support expectations visible to enterprise adopters.

Status: Phase 12 is foundation-ready for operations capability catalog, runbook catalog, incident response targets, health check matrix, rollback plan, maintenance policy, support handbook, operations readiness, operations CI profile, and phase documentation. Production operations remain blocked until commercial readiness is complete.

## Compliance And Audit Readiness

- Add compliance capability, audit-event, evidence-retention, license, data-handling, and lawful-use foundations.
- Keep compliance foundation readiness separate from enterprise production compliance claims.
- Make audit and governance expectations visible to open-source and commercial adopters.

Status: Phase 13 is foundation-ready for compliance capability catalog, audit event catalog, evidence retention policy, license compliance policy, data handling classification, export-control policy, compliance readiness, compliance CI profile, commercial compliance gate, and phase documentation. Enterprise compliance claims remain blocked until commercial readiness is complete and adopters complete their own legal, regulatory, export-control, privacy, and operator-authorization reviews.

## Performance Capacity And Benchmark Readiness

- Add performance capability, benchmark-scenario, capacity, throughput, latency, load-test, and resource-budget foundations.
- Keep performance foundation readiness separate from production throughput, latency, and capacity claims.
- Make benchmark evidence expectations visible to enterprise adopters.

Status: Phase 14 is foundation-ready for performance capability catalog, benchmark scenario catalog, capacity profile, throughput targets, latency budgets, load-test plan, resource budget, performance readiness, performance CI profile, and phase documentation. Production performance claims remain blocked until representative native SCTP and external-peer benchmark evidence is captured and retained.

## API Stability Deprecation And Migration Readiness

- Add public API surface catalog, stability contracts, version-line matrix, deprecation policy, migration guide catalog, breaking-change review, and API baseline foundations.
- Keep API lifecycle foundation readiness separate from stable API lifecycle claims.
- Make API-shaping changes visible and reviewable for open-source and commercial adopters.

Status: Phase 15 is foundation-ready for API surface catalog, stability contracts, version matrix, deprecation policy, migration guide catalog, breaking-change review policy, public API baseline, API lifecycle readiness, API lifecycle CI profile, and phase documentation. Stable API lifecycle claims remain blocked until wider commercial readiness is complete and protocol surfaces have the required validation evidence.

## Configuration Policy And Environment Readiness

- Add configuration schema, validation, environment matrix, secret policy, transport configuration, routing configuration, readiness, and CI foundations.
- Keep configuration foundation readiness separate from production configuration claims.
- Make production secret, routing, transport, and evidence expectations visible to enterprise adopters.

Status: Phase 16 is foundation-ready for configuration schema, validation helpers, environment matrix, secret policy, transport configuration, routing configuration, configuration readiness, configuration CI profile, commercial configuration gate, and phase documentation. Production configuration claims remain blocked until wider commercial readiness is complete and deployment-specific review is performed.

## Native SCTP Lab Verification

- Add native SCTP lab scenarios, artifact manifests, run plan, command set, run reports, evidence registry, readiness, and CI profile.
- Keep lab framework readiness separate from native SCTP production verification.
- Make Linux SCTP evidence requirements explicit before commercial production claims.

Status: Phase 17 is foundation-ready for native SCTP lab scenario catalog, artifact manifest, run plan, command set, run report, evidence registry, lab readiness, lab CI profile, commercial gate, and phase documentation. Native SCTP production verification remains blocked until complete passing Linux SCTP lab evidence is captured.

## Phase 18 - External Peer Interop Execution

- Add external peer environment, ASP-to-SG configuration, trace expectations, artifact manifest, run plan, command set, run reports, evidence registry, readiness, and CI metadata.
- Keep execution foundation readiness separate from verified external peer evidence.
- Make required external peer artifacts explicit before commercial interoperability claims.

Status: Phase 18 is foundation-ready for external peer interoperability execution. Verification remains blocked until real external peer packet captures, SDK traces, peer configuration, peer logs, and comparison reports are captured and promoted.

## Phase 19 - SCCP TCAP MAP Interop Vectors

- Add SCCP, TCAP, and MAP SMS protocol vector catalog, external references, artifact manifest, comparison rules, run plan, command set, run reports, evidence registry, readiness, and CI metadata.
- Keep vector foundation readiness separate from verified higher-layer protocol evidence.
- Make required reference vectors, SDK vectors, and comparison reports explicit before commercial SCCP, TCAP, or MAP SMS interoperability claims.

Status: Phase 19 is foundation-ready for SCCP, TCAP, and MAP SMS protocol interoperability vectors. Verification remains blocked until real external reference vectors, SDK-generated vectors, and reviewed comparison reports are captured and promoted for every required vector.

## Phase 20 - Commercial Evidence Dossier

- Add commercial evidence requirements, artifact contract, manifest, bundle, gate, readiness report, CI metadata, and status reporting.
- Consolidate native SCTP, external peer, protocol-vector, release provenance, package, SBOM, and signing evidence into one release dossier contract.
- Normalize source status capability labels so source-level metadata uses domain names instead of roadmap phase labels.

Status: Phase 20 is foundation-ready for commercial evidence dossier assembly. Commercial evidence readiness remains blocked until real retained artifacts, digest coverage, native SCTP verification, external peer verification, protocol vector verification, and release governance are complete.

## Phase 21 - Supply Chain Automation

- Add supply-chain automation plan, SBOM generation contract, package signing contract, signature verification contract, provenance attestation contract, artifact manifest, gate, readiness report, CI profile, and status reporting.
- Connect SBOM and package-signing policies to ordered release-security commands.
- Keep supply-chain foundation readiness separate from release promotion readiness.

Status: Phase 21 is foundation-ready for supply-chain automation. Promotion readiness remains blocked until real SBOMs, package signatures, timestamp receipts, provenance attestations, verification reports, signing secrets, and commercial evidence are retained.

## Phase 22 - Release Workflow Orchestration

- Add release workflow trigger, stage, secret, supply-chain, commercial-evidence, and publish contracts.
- Keep workflow contract readiness separate from a concrete workflow file.
- Split the workflow work into smaller committed parts so each part can be tested, documented, packed, committed, and pushed independently.

Status: Phase 22 Part 1 is contract-ready for release workflow orchestration. Full orchestration remains blocked until a concrete release workflow file is added and validated.

## Phase 23 - Release Workflow Completion

- Add the concrete release workflow file, YAML validation, publish guard, artifact retention, permission policy, concurrency policy, environment contract, promotion gate, and final status alignment.
- Keep release workflow orchestration readiness separate from commercial release promotion.
- Require real commercial evidence and supply-chain promotion evidence before a package can be promoted.

Status: Phase 23 is foundation-ready for release workflow orchestration. Release promotion remains blocked until real evidence, signing, SBOM, provenance, and publish credentials are available.

## Phase 24 - Package Publication Readiness

- Add release version and tag policy, NuGet metadata contract, package layout, dry-run publish plan, credential policy, channel policy, package integrity manifest, publication evidence manifest, publication gate, readiness status, and documentation.
- Keep NuGet publication separate from publication foundation readiness.
- Require live commercial evidence, supply-chain artifacts, signing material, provenance, and NuGet credentials before any real package upload.

Status: Phase 24 is foundation-ready for package publication readiness. Real NuGet publication remains blocked until retained commercial evidence, signed supply-chain artifacts, provenance, and live publish credentials are available.

## Phase 25 - Commercial Release Execution And Evidence

- Retain real Linux SCTP, external peer, packet capture, trace, comparison, SBOM, signing, provenance, benchmark, public API baseline, workflow, dry-run, and publication gate evidence.
- Keep blocker evidence explicit instead of manufacturing passing artifacts.
- Promote only when all retained evidence areas are passing and digest-covered.

Status: Phase 25 has execution evidence in place. Linux SCTP loopback evidence is retained from a real Ubuntu 22.04 VM; external peer interoperability remains blocked until a maintained SIGTRAN peer run is captured. The legacy OpenSS7/IPSS7 attempt remains retained blocker evidence for Linux 5.15 `open_softirq` compatibility.

## Phase 26 - Commercial Roadmap Realignment

- Replace package-specific SDK source contracts with package-neutral external SIGTRAN peer contracts.
- Keep legacy OpenSS7/IPSS7 evidence as retained blocker evidence, not as the permanent commercial gate.
- Add maintained peer selection, lab environment, artifact, run, comparison, and readiness contracts without naming public APIs after a peer package.
- Document any selected peer package only in lab profile notes, configuration examples, and retained evidence.

Status: Phase 26 is foundation-complete for package-neutral commercial roadmap realignment. `SigtranCommercialRoadmapRealignmentStatus` now gates source naming, public labels, external peer readiness, maintained peer selection, and commercial release gate alignment. Commercial release remains blocked until a maintained external SIGTRAN peer run produces passing PCAP, peer logs, SDK traces, configuration, comparison evidence, and digest-covered release artifacts.

## Phase 27 - Maintained External Peer Lab

- Canonicalize the SDK name as `Sigtran.NET` across source namespaces, project paths, package id, scripts, CI workflows, docs, and release evidence names.
- Bind a maintained external SIGTRAN peer package through package-neutral configuration and environment variables.
- Define host prerequisites, peer config, artifact naming, command scripts, traffic vectors, evidence promotion, readiness, and commercial gate alignment.
- Keep selected package details outside public SDK type names.

Status: Phase 27 is foundation-complete. It has canonical `Sigtran.NET` naming, a package-neutral maintained peer lab binding catalog, host prerequisite readiness modeling, validated lab configuration contracts, deterministic retained artifact planning, an ordered command plan, maintained peer traffic vectors, a digest-covered evidence promotion gate, manual self-hosted CI policy, and final status reporting. Commercial release remains blocked until the maintained peer lab produces passing digest-covered evidence.

## Phase 28 - Maintained Peer Lab Automation And Evidence Handoff

- Aggregate maintained peer lab contracts into an executable run manifest.
- Render environment files, command scripts, workflow templates, comparison reports, and evidence handoff bundles.
- Keep automation package-neutral and separate planned contracts from retained commercial evidence.

Status: Phase 28 is foundation-complete. It has a run manifest that aggregates binding, configuration, artifact, command, traffic vector, and CI contracts, deterministic environment file rendering, command script rendering, maintained peer comparison reporting, run reporting, artifact digest manifests, evidence bundle handoff for promotion reports, a manual self-hosted workflow template, a commercial readiness bridge, and automation status reporting. Commercial readiness still requires a real maintained peer lab execution with digest-covered retained artifacts.

## Phase 29 - Maintained Peer Lab Runner Materialization

- Materialize maintained peer lab runner workspace directories, inputs, commands, expected outputs, evidence collection, and handoff checks.
- Keep runner materialization package-neutral and separate from real retained lab evidence.
- Prepare the SDK for a real maintained external peer lab run without manufacturing passing artifacts.

Status: Phase 29 is foundation-complete. It has deterministic maintained peer lab runner workspace, execution input bundle, output artifact materialization, preflight, command manifest, evidence collection, digest generation, comparison handoff, workflow readiness, and status reporting contracts. Commercial readiness still requires a real maintained peer lab execution with retained digest-covered runner evidence.

## Phase 30 - Maintained Peer Lab Runner Operationalization

- Operationalize runner materialization with reviewable file creation, execution logs, command outcomes, artifact verification, provenance, failure handling, retry policy, evidence packaging, and operator handoff.
- Keep runner operations package-neutral and separate from real retained lab evidence.
- Prepare real maintained peer lab execution without manufacturing passing artifacts.

Status: Phase 30 is foundation-complete. It has reviewable file materialization plan rendering, execution log, command outcome, artifact verification, runner provenance, failure classification, retry policy, evidence package manifest, operator handoff, and operations status contracts. Commercial readiness still requires real maintained peer execution with retained digest-covered evidence and operator handoff artifacts.

## Phase 31 - Native SCTP Production Hardening

- Harden native SCTP stream and PPID framing before send.
- Add reconnect orchestration, backpressure policy, cancellation contracts, multi-homing readiness, association lifecycle journaling, fault classification, and recovery decisions.
- Keep production readiness blocked until retained Linux SCTP and external peer evidence prove the hardened contracts against real traffic.

Status: Phase 31 is foundation-complete. The outbound stream and PPID framing contract, association lifecycle journal, reconnect schedule, send backpressure policy, cancellation/timeout policy, multi-homing readiness checks, fault recovery decisions, transport diagnostics snapshots, production hardening readiness gate, and status report are available. Production readiness remains blocked until retained Linux SCTP and external peer evidence are complete.

## Phase 32 - SCCP TCAP MAP Evidence Upgrade

- Add byte-level evidence vectors for SCCP, TCAP, and MAP SMS.
- Validate SDK encoders and decoders against deterministic expected bytes and trace-order expectations.
- Report and correct mismatches before upgrading readiness claims from foundation-only to evidence-backed.
- Keep external interoperability evidence as a commercial promotion gate.

Status: Phase 32 is complete for SDK evidence-backed behavior. The shared protocol evidence vector and byte-level mismatch validation contract is available. SCCP has deterministic UDT, XUDT, LUDT, and UDTS evidence vectors. TCAP has deterministic Begin/Invoke/Dialogue and End/ReturnResult evidence vectors. MAP SMS has deterministic MO-ForwardSM, MT-ForwardSM, SendRoutingInfoForSM, ReportSM-DeliveryStatus, and AlertServiceCentre evidence vectors. A cross-layer evidence bundle aggregates vector counts, duplicate-id checks, and validation pass/fail status. Ordered trace validation compares `SigtranTraceFrame` sequences against those vectors, mismatch classification recommends whether correction belongs to protocol labels, codec/vector bytes, missing capture frames, or extra artifact mapping, readiness gates separate SDK evidence-backed status from production evidence claims, status reporting summarizes completed capabilities, and final sweeps validated naming/package-neutrality. Production evidence remains blocked until retained external interoperability artifacts exist.

## Phase 33 - Performance And Resilience Evidence

- Capture real peer-traffic benchmark evidence with warmup, sustained, and peak stages.
- Track latency P95/P99, throughput, message loss, CPU, memory, allocation, and failover behavior.
- Produce a publishable performance report with retained artifact references.
- Keep production performance claims blocked until retained benchmark evidence is complete and reviewed.

Status: Phase 33 is foundation-complete. Peer-traffic benchmark workload evidence maps the commercial load-test plan into warmup, sustained, and peak stages with target/actual message-rate checks and message-loss validation. Retained artifact manifests and run plans require digest-covered PCAP, SDK trace, peer logs/configuration, metrics, latency profile, resource profile, resilience log, and benchmark report artifacts. Latency percentile evidence evaluates P95/P99 measurements against SDK latency budgets, resource evidence evaluates CPU, working set, and allocation measurements against commercial resource budgets, resilience evidence gates failover on event coverage, recovery time, and zero message loss, publishable Markdown reports aggregate all gates, production performance evidence gates connect publishable reports to wider commercial readiness, manual self-hosted runner/CI handoff metadata defines real execution commands and artifact upload patterns, status reporting documents current blockers, and final sweeps validated naming/package-neutrality. Production performance claims remain blocked until retained real peer benchmark evidence and commercial readiness are complete.

## Phase 34 - Supply Chain Release Execution

- Generate and retain the final versioned SBOM artifact.
- Require trusted timestamped package signing and verification evidence.
- Produce provenance attestation evidence for package, SBOM, source, and workflow identity.
- Retain public API diff artifacts before publication.
- Upload release artifacts from the workflow with digest coverage and promotion gates.

Status: Phase 34 is foundation-complete. The final SBOM artifact contract is available and requires SPDX JSON, package/version alignment, workflow outputs, and digest coverage. Trusted timestamped signing evidence now requires certificate identity, HTTPS timestamp authority, retained timestamp receipt, verification report, and digest coverage. Provenance attestation now links package and SBOM subjects to source commit, release workflow identity, OIDC issuer, and retained digests. Public API diff artifacts now retain baseline/current paths, diff digest, member change counts, and breaking-change approval state. Release artifact upload now covers package, symbols, SBOM, signing, timestamp, provenance, API diff, and digest artifacts with 90-day retention. The ordered command plan defines SBOM, signing, verification, provenance, API diff, digest, and upload execution. The release gate aggregates those contracts with commercial evidence readiness. The concrete GitHub Actions release workflow now performs SBOM generation, signing, verification, GitHub attestations, public API diff retention, digest creation, and artifact upload. Status reporting and final validation keep promotion blocked until retained release-run artifacts and commercial evidence are available.

## Phase 35 - RC Publish And Commercial Gate

- Rehearse releases with a dry-run plan that cannot upload to NuGet.
- Gate NuGet prerelease publication separately from stable publication.
- Produce final commercial readiness, release notes, and migration notes artifacts.
- Decide RC versus stable based on retained release evidence and commercial readiness.

Status: Phase 35 is in progress. The dry-run release rehearsal plan is available and requires package creation, package verification, retained evidence, and no NuGet upload command. Gated prerelease publication now requires an RC/prerelease version, explicit publish request, NuGet API key availability, dry-run success, and supply-chain release readiness; stable versions are rejected by this gate. Retained release notes artifacts now require versioned Markdown, digest coverage, publishable content, breaking-change section, and migration notes link. Retained migration notes artifacts now require versioned Markdown, digest coverage, migration entries, code-sample requirement, and experimental SCCP/TCAP/MAP boundary statements. Final commercial readiness reporting now separates RC prerelease readiness from stable commercial readiness and retains current blockers. RC/stable decisioning now recommends `Blocked`, `ReleaseCandidate`, or `Stable` from retained readiness evidence. RC publication evidence now requires package, symbols, dry-run, notes, migration, readiness, decision, and digest artifacts before upload. The release workflow now has explicit `dry-run`, `prerelease`, and `stable` channel wiring with retained dry-run evidence and RC publication gating. Final status reporting and validation remain in progress.

## Recommended First Deliverable

The first useful SDK release should be an alpha package focused on M3UA over a transport abstraction:

- Correct M3UA binary parser/writer
- ASP state-machine API
- Protocol Data send/receive
- Routing context and network appearance support
- Structured diagnostics and test vectors
- Clear experimental labels for SCCP, TCAP, and MAP until their encodings are replaced with standards-based implementations
