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
| 2 | Stable commercial dossier evidence map | Pending |
| 3 | Stable commercial readiness checklist | Pending |
| 4 | Stable release decision gate | Pending |
| 5 | Stable tag gate and command plan | Pending |
| 6 | Protected stable publication authorization | Pending |
| 7 | Stable publish execution plan | Pending |
| 8 | Final commercial report writer | Pending |
| 9 | Stable release audit trail | Pending |
| 10 | Final status, documentation, README alignment, validation, commit, and push | Pending |

## Current Capability

`SigtranStableReleaseTarget` locks the stable package version, source commit, stable tag, retained artifact root, requester identity, and UTC target creation time. It only enters stable gate evaluation when the version is stable SemVer, the tag matches `v{version}`, the source commit is retained, and the timestamp is UTC.

## Commercial Gate Position

Phase 43 is in progress. Unit 1 adds the stable release target boundary. Stable publication remains blocked until a complete commercial dossier, stable decision, protected tag/publish authorization, final commercial report, and retained release evidence all pass.
