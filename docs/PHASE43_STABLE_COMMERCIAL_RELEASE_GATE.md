# Phase 43 - Stable Commercial Release Gate

Phase 43 is the final stable commercial release gate. It does not claim that a stable publication has happened. It creates the final gate contracts that decide whether stable tag and NuGet publication can proceed from retained evidence.

## Gap Assessment Before Phase 43

The previous phase map is still directionally correct, but the SDK is not commercially releasable yet. The remaining gaps are evidence and protected execution gaps, not naming or foundation gaps:

- A passing maintained external peer run still needs retained PCAP, peer logs, SDK traces, configuration, comparison report, run report, and digests.
- Native SCTP production hardening still needs retained Linux SCTP and external peer evidence that proves stream, PPID, lifecycle, reconnect, backpressure, cancellation, multi-homing readiness, and recovery behavior.
- SCCP, TCAP, and MAP evidence are SDK evidence-backed, but production evidence still needs retained external interoperability artifacts.
- Performance evidence is still smoke-level and needs sustained peer/load benchmark evidence with P95/P99 latency, resource metrics, and failover behavior.
- Supply-chain evidence needs a trusted timestamped signing run, verified package signature, SBOM, provenance, API diff, digest manifest, and uploaded artifacts from the final release commit.
- Package publication gate integration is foundation-complete, but live publication still needs retained release evidence and a protected approved publication run.

## Unit Plan

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | Stable release target lock | Complete |
| 2 | Stable commercial dossier evidence map | Complete |
| 3 | Stable commercial readiness checklist | Complete |
| 4 | Stable release decision gate | Complete |
| 5 | Stable tag gate and command plan | Complete |
| 6 | Protected stable publication authorization | Complete |
| 7 | Stable publish execution plan | Complete |
| 8 | Final commercial report writer | Complete |
| 9 | Stable release audit trail | Pending |
| 10 | Final status, documentation, README alignment, validation, commit, and push | Pending |

## Current Capability

`SigtranStableReleaseTarget` locks the stable package version, source commit, stable tag, retained artifact root, requester identity, and UTC target creation time. It only enters stable gate evaluation when the version is stable SemVer, the tag matches `v{version}`, the source commit is retained, and the timestamp is UTC.

`SigtranStableCommercialDossierEvidenceMap` binds retained evidence items to that stable target. It requires maintained external peer evidence, native SCTP hardening evidence, protocol interop evidence, performance benchmark evidence, final SBOM, signing verification, provenance attestation, public API diff, release workflow artifacts, publication notes, and final commercial readiness report. Checklist evaluation is blocked when required evidence is missing, retained paths are duplicated, SHA-256 digests are invalid, or retained paths escape the target artifact root.

`SigtranStableCommercialReadinessChecklist` turns the evidence map into reviewed readiness. It requires approved checklist areas for target lock, dossier evidence, maintained external peer interoperability, native SCTP hardening, protocol interoperability, benchmark evidence, supply-chain release evidence, public API baseline, operations/compliance, and publication dossier. Stable decisioning is blocked when the target or evidence map is not ready, required readiness areas are missing, areas are duplicated, or any approval is missing.

`SigtranStableCommercialReleaseDecision` records whether the reviewed checklist is approved or blocked. It preserves the decision maker, UTC decision time, and reasons. The decision only becomes ready for stable tag gate evaluation when the checklist is ready and the decision kind is approved.

`SigtranStableTagGateResult` evaluates whether the stable tag workflow can move to protected publication authorization. The generated command plan validates the approved decision, verifies the source commit, creates the annotated `v{version}` tag on the pinned commit, verifies the tag commit, and pushes only the target tag. The tag gate blocks when the command plan is not ready, protected stable tag policy is not confirmed, or an existing tag conflict is present.

`SigtranStablePublicationAuthorization` evaluates protected stable publication approval. It requires a ready tag gate, protected stable release environment profile, explicit stable publish intent, release/security/operations approvals, required publication secret names, and UTC authorization timing. It reports blockers for missing tag readiness, missing protected environment, missing publish intent, missing approvals, and missing secret names without retaining secret values.

`SigtranStablePublishExecutionPlan` turns protected authorization into ordered stable publication commands. The plan validates authorization, dispatches the release workflow with `channel=stable` and `publish=true`, watches the workflow, downloads artifacts, verifies the package, publishes through a guarded `NUGET_API_KEY` environment reference, and retains final publication evidence. The plan is not a live publication record; it is ready only when protected authorization is ready.

`SigtranStableCommercialReportWriters` writes a retained Markdown stable commercial release report and computes a real SHA-256 digest for the report file. The report separates auditable report readiness from actual stable commercial release completion. It only marks the stable commercial release complete when the publish plan is ready, stable tag evidence exists, stable package publication evidence exists, and final publication evidence is retained.

## Commercial Gate Position

Phase 43 is in progress. Units 1 through 8 add the stable release target boundary, retained commercial dossier evidence map, approved readiness checklist, stable release decision gate, stable tag gate, protected stable publication authorization, stable publish execution plan, and final commercial report writer. Stable publication remains blocked until the audit trail and retained release evidence all pass.
