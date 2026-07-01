# SIGTRAN.NET SDK Roadmap

This roadmap is based on the current repository and the supplied SIGTRAN references:

- Tekelec EAGLE SS7-over-IP using SIGTRAN, rev B
- ETSI EG 202 360 V1.1.1 SIGTRAN scenarios
- Nokia MCAS 6.1.1 SS7/SIGTRAN User Guide
- M2PA Internet-Draft 13, with RFC 4165 used as the standards-track baseline
- RFC 4666 for M3UA and RFC 9260 for SCTP

## Current Assessment

The current repository has moved beyond proof-of-concept status into a foundation-complete SDK with governed release contracts, protocol evidence vectors, native SCTP readiness contracts, retained external C SCTP peer evidence, retained peer-traffic benchmark evidence, retained internal timestamped RC signing evidence, a passing protected release workflow dry-run, and a public NuGet prerelease package. Stable commercial publication is still blocked until approved stable package publication and stable NuGet publication evidence are complete.

For a concise map of every phase and its primary documents, see [Phase Index](PHASE_INDEX.md).

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

## Phase 7 - ProductionReadiness And Release Hardening

- Define commercial readiness gates and make blocked gates visible in public APIs.
- Capture native SCTP verification status and OS support limits.
- Track external interoperability evidence from real peer stacks and packet traces.
- Add release candidate manifests, package governance, security policy, compatibility policy, observability guidance, and deployment profiles.
- Finalize release automation and documentation for commercial adoption.

Status: SDK foundation is complete for commercial readiness gates, native SCTP support matrix, external interoperability evidence tracking, release candidate manifests, package governance, security policy, compatibility policy, observability profile, deployment profiles, and Phase 7 status reporting. Internal release readiness is available; commercial production readiness remains blocked until native SCTP verification, external interoperability evidence, package signing, and SBOM automation are complete.

## Phase 8 - Native SCTP Production Transport

- Probe Linux native SCTP socket creation using `SocketType.Stream` and IP protocol number `132`.
- Add native SCTP socket factory, connector, listener, send/receive path, lifecycle events, health snapshots, and reconnect integration.
- Add Linux-focused integration test hooks that can run when SCTP kernel support is available.
- Keep Windows and macOS contract-only until a verified provider is selected.

Status: SDK foundation is complete for Linux native SCTP platform probing, socket creation, endpoint planning, socket adaptation, client connect, server listen/accept, lab profile, readiness reporting, and commercial gate integration. Production readiness remains blocked until Linux SCTP lab verification passes with real kernel SCTP support and peer traffic.

## Phase 9 - Real Interoperability Lab

- Define required lab scenarios for Linux native SCTP, external peer M3UA ASP-to-SG, and MAP SMS trace comparison.
- Capture PCAPs, SDK traces, peer configuration, peer logs, and comparison reports.
- Convert passing lab runs into release evidence that can unlock commercial readiness gates.
- Keep evidence pending until artifacts are real and reviewable.

Status: Phase 9 is foundation-ready for scenario catalog, artifact manifests, run reports, external peer lab template, trace comparison, evidence promotion, opt-in CI profile, readiness reporting, and commercial gate integration. Production readiness remains blocked until real external lab artifacts are captured and promoted.

## Phase 10 - Release Automation And Supply Chain

- Define deterministic release automation steps for restore, build, test, pack, validation, and publish.
- Track package artifacts, checksums, SBOM requirements, signing requirements, and provenance.
- Add release channel rules, version/release-note requirements, and publish gates.
- Keep commercial release blocked until signing, SBOM, provenance, native SCTP verification, and external interoperability evidence are complete.

Status: Phase 10 is foundation-ready for release automation plan, artifact manifest, SBOM plan, package signing plan, provenance tracking, release notes validation, publish channels, release gate evaluation, release CI profile, and phase documentation. Stable commercial publication remains blocked until real signing, SBOM generation, native SCTP verification, and external interoperability evidence are complete.

## Phase 11 - Developer Experience And Adoption

- Add quickstarts, sample inventories, configuration profiles, troubleshooting guidance, and adoption gates.
- Make the shortest M3UA ASP-to-SG path clear for new users.
- Keep production claims tied to readiness and interoperability evidence.

Status: Phase 11 is foundation-ready for capability catalog, M3UA quickstart, sample templates, configuration profiles, troubleshooting index, API reference index, adoption gates, documentation readiness, developer experience CI profile, and phase documentation. Enterprise production adoption remains blocked until commercial readiness is complete.

## Phase 12 - Production Operations And Support

- Add operational runbook, incident, health, recovery, and support foundations.
- Keep operations readiness separate from commercial production readiness.
- Make production support expectations visible to enterprise adopters.

Status: Phase 12 is foundation-ready for operations capability catalog, runbook catalog, incident response targets, health check matrix, rollback plan, maintenance policy, support handbook, operations readiness, operations CI profile, and phase documentation. Production operations remain blocked until commercial readiness is complete.

## Phase 13 - Compliance And Audit Readiness

- Add compliance capability, audit-event, evidence-retention, license, data-handling, and lawful-use foundations.
- Keep compliance foundation readiness separate from enterprise production compliance claims.
- Make audit and governance expectations visible to open-source and commercial adopters.

Status: Phase 13 is foundation-ready for compliance capability catalog, audit event catalog, evidence retention policy, license compliance policy, data handling classification, export-control policy, compliance readiness, compliance CI profile, commercial compliance gate, and phase documentation. Enterprise compliance claims remain blocked until commercial readiness is complete and adopters complete their own legal, regulatory, export-control, privacy, and operator-authorization reviews.

## Phase 14 - Performance Capacity And Benchmark Readiness

- Add performance capability, benchmark-scenario, capacity, throughput, latency, load-test, and resource-budget foundations.
- Keep performance foundation readiness separate from production throughput, latency, and capacity claims.
- Make benchmark evidence expectations visible to enterprise adopters.

Status: Phase 14 is foundation-ready for performance capability catalog, benchmark scenario catalog, capacity profile, throughput targets, latency budgets, load-test plan, resource budget, performance readiness, performance CI profile, and phase documentation. Production performance claims remain blocked until representative native SCTP and external-peer benchmark evidence is captured and retained.

## Phase 15 - API Stability Deprecation And Migration Readiness

- Add public API surface catalog, stability contracts, version-line matrix, deprecation policy, migration guide catalog, breaking-change review, and API baseline foundations.
- Keep API lifecycle foundation readiness separate from stable API lifecycle claims.
- Make API-shaping changes visible and reviewable for open-source and commercial adopters.

Status: Phase 15 is foundation-ready for API surface catalog, stability contracts, version matrix, deprecation policy, migration guide catalog, breaking-change review policy, public API baseline, API lifecycle readiness, API lifecycle CI profile, and phase documentation. Stable API lifecycle claims remain blocked until wider commercial readiness is complete and protocol surfaces have the required validation evidence.

## Phase 16 - Configuration Policy And Environment Readiness

- Add configuration schema, validation, environment matrix, secret policy, transport configuration, routing configuration, readiness, and CI foundations.
- Keep configuration foundation readiness separate from production configuration claims.
- Make production secret, routing, transport, and evidence expectations visible to enterprise adopters.

Status: Phase 16 is foundation-ready for configuration schema, validation helpers, environment matrix, secret policy, transport configuration, routing configuration, configuration readiness, configuration CI profile, commercial configuration gate, and phase documentation. Production configuration claims remain blocked until wider commercial readiness is complete and deployment-specific review is performed.

## Phase 17 - Native SCTP Lab Verification

- Add native SCTP lab scenarios, artifact manifests, run plan, command set, run reports, evidence registry, readiness, and CI profile.
- Keep lab framework readiness separate from native SCTP production verification.
- Make Linux SCTP evidence requirements explicit before commercial production claims.

Status: Phase 17 is foundation-ready for native SCTP lab scenario catalog, artifact manifest, run plan, command set, run report, evidence registry, lab readiness, lab CI profile, commercial gate, and phase documentation. Native SCTP production verification remains blocked until complete passing Linux SCTP lab evidence is captured.

## Phase 18 - External Peer Interop Execution

- Add external peer environment, ASP-to-SG configuration, trace expectations, artifact manifest, run plan, command set, run reports, evidence registry, readiness, and CI metadata.
- Keep execution foundation readiness separate from verified external peer evidence.
- Make required external peer artifacts explicit before commercial interoperability claims.

Status: Phase 18 is foundation-ready and has RC-grade external peer evidence from `commercial-external-peer-20260627T111932Z`, including PCAP, SDK trace, peer log, configuration, TShark decode, comparison output, run report, and digests. Stable publication still requires the protected release workflow to attach reviewed evidence to the final release run.

## Phase 19 - SCCP TCAP MAP Interop Vectors

- Add SCCP, TCAP, and MAP SMS protocol vector catalog, external references, artifact manifest, comparison rules, run plan, command set, run reports, evidence registry, readiness, and CI metadata.
- Keep vector foundation readiness separate from verified higher-layer protocol evidence.
- Make required reference vectors, SDK vectors, and comparison reports explicit before commercial SCCP, TCAP, or MAP SMS interoperability claims.

Status: Phase 19 is foundation-ready for SCCP, TCAP, and MAP SMS protocol interoperability vectors. Verification remains blocked until real external reference vectors, SDK-generated vectors, and reviewed comparison reports are captured and promoted for every required vector.

## Phase 20 - Production Evidence Dossier

- Add commercial evidence requirements, artifact contract, manifest, bundle, gate, readiness report, CI metadata, and status reporting.
- Consolidate native SCTP, external peer, protocol-vector, release provenance, package, SBOM, and signing evidence into one release dossier contract.
- Normalize source status capability labels so source-level metadata uses domain names instead of roadmap phase labels.

Status: Phase 20 is foundation-ready for commercial evidence dossier assembly. Production evidence readiness remains blocked until real retained artifacts, digest coverage, native SCTP verification, external peer verification, protocol vector verification, and release governance are complete.

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

Status: Phase 24 is foundation-ready for package publication readiness. Public RC NuGet publication is complete for `Sigtran.NET` version `1.0.0-rc.1`; stable NuGet publication remains blocked until retained stable commercial evidence, trusted stable signing, provenance, and protected stable publish approval are available.

## Phase 25 - Production Release Execution And Evidence

- Retain real Linux SCTP, external peer, packet capture, trace, comparison, SBOM, signing, provenance, benchmark, public API baseline, workflow, dry-run, and publication gate evidence.
- Keep blocker evidence explicit instead of manufacturing passing artifacts.
- Promote only when all retained evidence areas are passing and digest-covered.

Status: Phase 25 has execution evidence in place. Linux SCTP loopback evidence is retained from a real Ubuntu 22.04 VM, external C SCTP peer evidence is retained for the RC gate, peer-traffic benchmark evidence is retained, internal timestamped RC signing evidence is retained, protected release workflow dry-run `28289987418` passed with artifact upload and `publish=false`, and protected prerelease publication workflow `28290586511` published `Sigtran.NET` version `1.0.0-rc.1` to NuGet.org. The legacy OpenSS7/IPSS7 attempt remains retained blocker evidence for Linux 5.15 `open_softirq` compatibility. Stable commercial publication remains blocked on stable package publication approval, public/stable signing policy execution, and stable NuGet publication evidence.

## Phase 26 - API Naming Alignment

- Replace package-specific SDK source contracts with package-neutral external SIGTRAN peer contracts.
- Keep legacy OpenSS7/IPSS7 evidence as retained blocker evidence, not as the permanent commercial gate.
- Add reference peer selection, lab environment, artifact, run, comparison, and readiness contracts without naming public APIs after a peer package.
- Document any selected peer package only in lab profile notes, configuration examples, and retained evidence.

Status: Phase 26 is foundation-complete for package-neutral API naming alignment. `SigtranApiNamingAlignmentStatus` now gates source naming, public labels, external peer readiness, reference peer selection, and commercial release gate alignment. Production release remains blocked until a reference external SIGTRAN peer run produces passing PCAP, peer logs, SDK traces, configuration, comparison evidence, and digest-covered release artifacts.

## Phase 27 - Reference External Peer Lab

- Canonicalize the SDK name as `Sigtran.NET` across source namespaces, project paths, package id, scripts, CI workflows, docs, and release evidence names.
- Bind a reference external SIGTRAN peer package through package-neutral configuration and environment variables.
- Define host prerequisites, peer config, artifact naming, command scripts, traffic vectors, evidence promotion, readiness, and commercial gate alignment.
- Keep selected package details outside public SDK type names.

Status: Phase 27 is foundation-complete. It has canonical `Sigtran.NET` naming, a package-neutral reference peer lab binding catalog, host prerequisite readiness modeling, validated lab configuration contracts, deterministic retained artifact planning, an ordered command plan, reference peer traffic vectors, a digest-covered evidence promotion gate, manual self-hosted CI policy, and final status reporting. Production release remains blocked until the reference peer lab produces passing digest-covered evidence.

## Phase 28 - Reference Peer Lab Automation And Evidence Handoff

- Aggregate reference peer lab contracts into an executable run manifest.
- Render environment files, command scripts, workflow templates, comparison reports, and evidence handoff bundles.
- Keep automation package-neutral and separate planned contracts from retained commercial evidence.

Status: Phase 28 is foundation-complete. It has a run manifest that aggregates binding, configuration, artifact, command, traffic vector, and CI contracts, deterministic environment file rendering, command script rendering, reference peer comparison reporting, run reporting, artifact digest manifests, evidence bundle handoff for promotion reports, a manual self-hosted workflow template, a commercial readiness bridge, and automation status reporting. Production readiness still requires a real reference peer lab execution with digest-covered retained artifacts.

## Phase 29 - Reference Peer Lab Runner Materialization

- Materialize reference peer lab runner workspace directories, inputs, commands, expected outputs, evidence collection, and handoff checks.
- Keep runner materialization package-neutral and separate from real retained lab evidence.
- Prepare the SDK for a real reference external peer lab run without manufacturing passing artifacts.

Status: Phase 29 is foundation-complete. It has deterministic reference peer lab runner workspace, execution input bundle, output artifact materialization, preflight, command manifest, evidence collection, digest generation, comparison handoff, workflow readiness, and status reporting contracts. Production readiness still requires a real reference peer lab execution with retained digest-covered runner evidence.

## Phase 30 - Reference Peer Lab Runner Operationalization

- Operationalize runner materialization with reviewable file creation, execution logs, command outcomes, artifact verification, provenance, failure handling, retry policy, evidence packaging, and operator handoff.
- Keep runner operations package-neutral and separate from real retained lab evidence.
- Prepare real reference peer lab execution without manufacturing passing artifacts.

Status: Phase 30 is foundation-complete. It has reviewable file materialization plan rendering, execution log, command outcome, artifact verification, runner provenance, failure classification, retry policy, evidence package manifest, operator handoff, and operations status contracts. Production readiness still requires real reference peer execution with retained digest-covered evidence and operator handoff artifacts.

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

## Phase 35 - RC Publish And Production Gate

- Rehearse releases with a dry-run plan that cannot upload to NuGet.
- Gate NuGet prerelease publication separately from stable publication.
- Produce final commercial readiness, release notes, and migration notes artifacts.
- Decide RC versus stable based on retained release evidence and commercial readiness.

Status: Phase 35 is foundation-complete. The dry-run release rehearsal plan is available and requires package creation, package verification, retained evidence, and no NuGet upload command. Gated prerelease publication requires an RC/prerelease version, explicit publish request, NuGet API key availability, dry-run success, and supply-chain release readiness; stable versions are rejected by this gate. Retained release notes artifacts require versioned Markdown, digest coverage, publishable content, breaking-change section, and migration notes link. Retained migration notes artifacts require versioned Markdown, digest coverage, migration entries, code-sample requirement, and experimental SCCP/TCAP/MAP boundary statements. Final commercial readiness reporting separates RC prerelease readiness from stable commercial readiness and retains current blockers. RC/stable decisioning recommends `Blocked`, `Prerelease`, or `Stable` from retained readiness evidence. RC publication evidence requires package, symbols, dry-run, notes, migration, readiness, decision, and digest artifacts before upload. The release workflow has explicit `dry-run`, `prerelease`, and `stable` channel wiring with retained dry-run evidence and RC publication gating. The retained prerelease workflow run `28290586511` published `Sigtran.NET` version `1.0.0-rc.1` to NuGet.org. Stable publication remains blocked until commercial evidence, trusted stable signing, and protected stable approval are complete.

## Phase 36 - Production Evidence Readiness Lockdown

- Lock the release-candidate target before evidence-producing work starts.
- Validate required secrets, artifact roots, evidence checklists, preflight inputs, protected environments, and dossier handoff.
- Produce a go/no-go decision that blocks lab execution and RC publication when readiness prerequisites are missing.
- Keep stable publication blocked until commercial evidence is complete.

Status: Phase 36 is foundation-complete. The release target lock binds an RC version to a pinned source commit, release channel, and versioned artifact root. Secret readiness defines publish, signing, and provenance requirements without exposing secret values. Evidence retention mapping binds all commercial artifact areas to the target artifact root with one-year retention and digest coverage. The commercial evidence checklist requires packet capture, logs, traces, configuration, comparison, SBOM, signing, provenance, benchmark, API, workflow, publication, and readiness-report artifacts. Release preflight aggregates target, secrets, retention, and checklist blockers before execution starts. Protected release environments separate dry-run, prerelease, and stable publication with approval and protected-ref rules. Evidence dossier handoff maps checklist items to retained paths, reviewer roles, digest verification, and redaction review. The go/no-go gate separates no-go, evidence execution, RC publication, and stable publication decisions. Status reporting exposes completed capabilities and keeps publication blockers explicit. Final validation is complete. RC publication is complete; stable publication remains blocked until retained stable commercial release evidence is complete.

## Phase 37 - Production Evidence Execution Orchestration

- Create a governed execution run identity for evidence-producing work.
- Define execution stages, operator commands, environment contracts, artifact collection, digest and redaction verification, blocker handling, retry/resume behavior, and execution status.
- Keep the phase separate from real passing evidence: orchestration can prepare a run, but publication remains blocked until retained artifacts prove execution success.

Status: Phase 37 is foundation-complete. Evidence execution run identity binds a locked release target to a stable run id, operator identity, UTC start time, and run-scoped artifact root. The stage catalog covers readiness preflight, native SCTP lab, external peer interoperability, protocol validation, performance benchmark, supply-chain evidence, release workflow dry-run, and dossier assembly with run-scoped artifact roots. The operator command plan maps every stage to an ordered run-id-aware command and requires protected approval for supply-chain, workflow, and dossier assembly execution. The environment contract binds run identity, lab inputs, and protected secrets while preventing fixed secret values from being stored. Artifact collection maps all checklist artifacts to known stage roots and retained output paths. Digest and redaction verification requires digest coverage for every artifact and redaction review for trace-bearing evidence. Blocker classification categorizes readiness, environment, command, native SCTP, external peer, artifact, digest, redaction, and approval failures with retryability guidance. Retry/resume applies bounded retries and manual-correction gates for non-retryable failures. Status reporting exposes completed capabilities, orchestration readiness, retained evidence readiness, publication readiness, and current blockers. Final validation is complete. Production publication remains blocked until real retained execution artifacts are produced, verified, redacted where needed, and approved.

## Phase 38 - Production Evidence Artifact Intake

- Receive real execution artifacts into a run-scoped commercial dossier.
- Register artifact sources, digests, redaction reviews, completeness results, dossier reports, promotion handoff, and execution-to-dossier bridging.
- Keep intake foundation separate from commercial publication: intake can prove that artifacts are ready for review, but publication remains blocked until all retained evidence is complete and approved.

Status: Phase 38 is foundation-complete. Artifact intake target identity binds a stable intake id, reviewer identity, UTC receipt time, and run-scoped dossier root to a governed commercial evidence execution run. Artifact source registration maps every required expected execution artifact to a concrete source path and unique retained dossier path while rejecting floating `artifacts/latest` aliases. Digest coverage records SHA-256 values for every retained source and blocks invalid digest values. Redaction review requires approved reviewer records for trace-bearing retained artifacts. Completeness evaluation reports explicit source, digest, and redaction blockers. Dossier reporting renders a retained Markdown summary with run, intake, reviewer, counts, completion state, and blockers. Promotion handoff includes all digest-covered retained artifacts and the dossier intake report. Execution-to-dossier bridge assembles the intake pipeline from a governed execution run. Status reporting exposes completed capabilities, foundation readiness, real artifact evidence readiness, publication readiness, and current blockers. Final validation is complete. Production publication remains blocked until real artifact files are retained, digest-calculated, redaction-reviewed, and approved.

## Phase 39 - Production Evidence File Verification

- Verify retained commercial evidence files against promotion handoff digests.
- Track file existence, size, observation time, digest match, retention, integrity sealing, publication attachment, promotion gate, and operator command contracts.
- Keep file verification separate from real lab execution: verification contracts can evaluate retained files, but publication remains blocked until real files are present and approved.

Status: Phase 39 is foundation-complete. Retained file evidence item verification checks existence, non-empty size, SHA-256 validity, digest match, and UTC observation time. Retained file manifest coverage verifies that every promotion-required handoff item has a unique verified retained file. File verification reporting exposes missing, empty, invalid digest, digest mismatch, non-UTC observation, duplicate path, and incomplete handoff blockers. Retention ledger modeling binds verified files to reviewer identity, immutable retention, UTC retention windows, and minimum duration checks. Integrity sealing computes and validates a deterministic aggregate SHA-256 digest over the ledger. Publication attachment planning covers sealed ledger entries, validates attachment digests, includes the commercial readiness report, and blocks trace-bearing attachments without redaction approval. Verified promotion gating requires ready attachments, ready integrity seal, ready retention ledger, verified file report, commercial readiness report presence, and explicit approval before evidence can move into release publication decisions. Command planning orders observation, digest computation, comparison, report, ledger, seal, attachment, and promotion-gate work for workflow materialization. Status reporting separates foundation readiness from real retained file evidence and commercial publication readiness. Production publication remains blocked until real retained file evidence is captured and approved.

## Phase 40 - Production Evidence Filesystem Execution

- Execute retained evidence file observation against the local filesystem.
- Build verification manifests, reports, retained artifacts, ledger, seal, attachments, promotion gate, and command materialization from observed files.
- Keep filesystem execution separate from commercial publication: helpers can verify real files, but publication remains blocked until retained evidence comes from an approved commercial run.

Status: Phase 40 is foundation-complete. Filesystem observation reads retained files from disk, computes real SHA-256 digests, records file existence and size, and maps observations into the retained file verification model. Filesystem manifest execution observes every promotion handoff item, supports retained-path-to-local-path overrides, and builds retained file manifests from real observations. Filesystem verification report execution evaluates those manifests and exposes retained file blockers from real files. Verification artifact writing retains a Markdown report and tab-separated observation manifest on disk. Retention ledger execution creates ledger entries from filesystem-backed verification reports and written artifacts. Integrity seal execution seals filesystem-backed ledgers with deterministic aggregate SHA-256 digests. Publication attachment execution creates release dossier attachments from the filesystem-backed seal and requires approved redaction state for trace-bearing artifacts. Promotion execution evaluates filesystem-backed attachments through reviewer approval, UTC evaluation, and explicit blockers. Command materialization writes the ordered execution plan to a retained shell script. Status reporting now tracks ten completed capabilities including documentation and clears final validation blockers. Production publication remains blocked until a real approved commercial run is retained and approved.

## Phase 41 - Approved Production Run Publication Handoff

- Bind a filesystem-backed promotion execution to a reviewable commercial evidence run.
- Record approval checklist, reviewer approvals, retained reports, promotion package, publication handoff, blocker gates, and audit trail.
- Keep approval handoff separate from commercial publication: the SDK can prepare approval records, but publication remains blocked until a real approved run is retained.

Status: Phase 41 is foundation-complete. Approved run target identity binds package version, source commit, operator identity, UTC run timing, retained artifact root, and filesystem-backed promotion execution. Approval checklist requires verified filesystem promotion, ready report/ledger/seal/attachments, approved trace redaction, promotion approval, and reviewer approval records. Reviewer approval manifest records release, security, and operations approvals with UTC timestamps and a deterministic checklist SHA-256 digest. Approval report writing renders retained Markdown reports with run identity, checklist digest, reviewer roles, UTC write time, and report SHA-256 digest coverage. Approved run promotion package collects approval report, integrity seal, publication attachment, and promotion gate artifact references with required digest coverage. Publication handoff binds the approved package to requested channel, requester identity, UTC handoff time, explicit publish intent, and channel version policy. Publication handoff gate reports blockers for package readiness, publish intent, UTC timing, channel/version policy, and stable commercial readiness approval. Approval audit trail records digest-covered lifecycle events for run target, checklist, manifest, report, package, handoff, and gate. Command materialization writes the ordered approval workflow to a retained shell script. Status reporting now tracks ten completed capabilities including documentation and clears final validation blockers. RC publication evidence is retained; stable package publication remains blocked until a real approved stable commercial run is retained and approved.

## Phase 42 - Production Package Publication Gate Integration

- Connect approved commercial evidence handoff records to package publication gate execution.
- Bind package artifacts, credentials, publication evidence, publish guard, channel policy, dry-run rehearsal, guarded publish commands, and final status.
- Keep live publication blocked until retained release evidence and a protected approved publication run exist.

Status: Phase 42 is foundation-complete. Package publication requests derive from approved handoff gates and preserve package version, channel, requester, run id, promotion package id, and UTC request time. Publication artifacts bind nupkg/snupkg paths, retained sizes, SHA-256 digests, version-matched paths, and package integrity manifest projection. Credential readiness evaluates NuGet and signing secret names without storing secret values. Publication evidence assembly creates the final evidence manifest from package integrity, supply-chain readiness, and approved commercial evidence readiness. Publish guard and channel policy bridges enforce manual dispatch, publish intent, version tags, NuGet API key availability, prerelease channel rules, and stable commercial readiness. Final gate execution aggregates credentials, evidence, metadata, layout, guard, and channel blockers. Dry-run rehearsal writes retained non-publishing Markdown output, guarded command materialization writes a release script using environment-based `NUGET_API_KEY`, and status reporting tracks ten completed capabilities. RC package publication is complete; stable package publication remains blocked until retained stable release evidence and protected stable approval exist.

## Phase 43 - Stable Production Release Gate

- Lock the stable release target and bind it to a matching `v{version}` tag.
- Map the complete stable commercial dossier and require reviewed readiness before decisioning.
- Gate stable tag creation, protected publication authorization, guarded publish execution, final report retention, audit trail, and final status.
- Keep the gate foundation separate from real stable publication evidence.

Status: Phase 43 is foundation-complete. The SDK can model the stable release target, retained dossier evidence, approved readiness checklist, stable decision, stable tag commands, protected publication authorization, guarded stable publish execution plan, final commercial report, audit trail, and final stable gate status. Stable commercial release remains blocked until real retained stable release evidence is verified, a protected stable publication run completes, and actual NuGet publication evidence is retained and verified.

## Recommended First Deliverable

The first useful SDK release should be an alpha package focused on M3UA over a transport abstraction:

- Correct M3UA binary parser/writer
- ASP state-machine API
- Protocol Data send/receive
- Routing context and network appearance support
- Structured diagnostics and test vectors
- Clear experimental labels for SCCP, TCAP, and MAP until their encodings are replaced with standards-based implementations
