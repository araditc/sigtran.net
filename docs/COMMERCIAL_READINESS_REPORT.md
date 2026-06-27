# SIGTRAN.NET Commercial Readiness Report

Status: not commercially releasable yet.

## Passed Evidence

- SDK codebase builds, tests, and packs successfully.
- Linux SCTP smoke capture exists from a real Ubuntu 22.04 VM and records association setup, M3UA ASPUP/ASPACTIVE/DATA/HEARTBEAT traffic, DATA exchange, and clean shutdown.
- Native SCTP VM run `commercial-native-sctp-20260627T073300Z` passed on host `sigtrannet`, Ubuntu 22.04.1 LTS, kernel `5.15.0-181-generic`: trace events `18`, M3UA events `14`, Payload DATA trace events `2`, PCAP bytes `2106`.
- Native SCTP retained evidence digests for run `commercial-native-sctp-20260627T073300Z`: PCAP `7215966b1f46578ecfbb090285269ff64e20249a5a72dcb3ff135db0c24a2248`, SDK trace `a16b1f337ebe56091ece7497597163c111543daa485a2e9f4e2fc73472ae17f8`, comparison TSV `abe082aea2423d53f6e78c814a5c408e0f77b4c06604316e3879bd977f369deb`, run report `0933d0ce1d95dd494f1a89c0f2bd84c81bc89a96a36eeb62e118c6302cf9f1b4`.
- SBOM generation is executable and produces SPDX JSON from package artifacts.
- Package signing execution exists and produces a signed NuGet package.
- Provenance generation is executable and records source, package, and SBOM digests.
- Smoke benchmark report generation is executable.
- Public API baseline generation is executable.
- Stable commercial release gate foundation is complete: target lock, dossier map, reviewed checklist, stable decision, tag gate, protected authorization, guarded publish plan, final report writer, audit trail, and final status reporting are available.
- Commercial release-day readiness runner is available and produces timestamped local evidence reports for build, tests, package creation, SBOM, public API baseline, smoke benchmark, provenance, signing verification, VM SSH probing, and GitHub release-dispatch probing.

## Remaining Commercial Blockers

- External peer interoperability still needs a passing maintained peer run with retained PCAP, peer logs, SDK traces, configuration, and comparison report. The retained legacy OpenSS7/IPSS7 attempt is blocked on Linux 5.15 `open_softirq` compatibility. The native SCTP VM loopback run proves SDK SCTP traffic on Linux, but it is not a maintained external peer interoperability run.
- External interoperability evidence still needs PCAP, peer logs, SDK traces, and comparison report from a passing run.
- Package signing verification currently fails commercial requirements because the signing certificate is not trusted by the verifier and the signature is not timestamped.
- Performance evidence is smoke-only; commercial release needs sustained peer/load benchmark evidence with latency and resource metrics.
- Release workflow must regenerate package, SBOM, signing, provenance, benchmark, API baseline, and evidence artifacts after the final release commit.
- Package publication and stable commercial release gates are foundation-complete, but live stable publication still requires retained release evidence, a completed protected publication run, and verified NuGet publication evidence.
- The current local runner reports `LocalEvidenceReady=true`, and the Linux VM SSH probe now passes. `CommercialReady=false` until maintained external peer evidence, trusted timestamped signing, production peer benchmark evidence, and protected release-dispatch access are all complete.

## Commercial Decision

Do not publish this SDK as commercially production-ready yet.

The next release-candidate milestone should complete a maintained external SIGTRAN peer lab run, replace test signing with trusted timestamped production signing, run representative peer/load benchmarks, and attach the generated evidence artifacts to the release workflow. The stable release gate is ready to evaluate that evidence, but it must not be used to claim a stable commercial release until the protected publication run and NuGet publication evidence are retained and verified. The legacy OpenSS7/IPSS7 path can remain as historical evidence, but it is no longer the SDK's permanent commercial gate.
