# SIGTRAN.NET Commercial Readiness Report

Status: RC evidence nearly closed; stable commercial publication remains gated.

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
- External peer SCTP/M3UA run `commercial-external-peer-20260627T111932Z` passed on host `sigtrannet` against an independent C SCTP peer. Retained artifacts include PCAP, external peer log, SDK trace, TShark decode, M3UA field comparison, configuration, run report, summary JSON, and digest manifest.
- External peer evidence digests: PCAP `2d4313440e665ddd9686bc3be3937810921d11c6d4ed2e6b4746a71d31f79416`, SDK trace `11a6da80dffc786ba66667ca0c85cc7b68fa5eb0f24316c5d47ea5ea2bb842fb`, peer log `50509d0ca7013c4496516daaec327eb269393c8fccc2036869db1bca2794c897`, TShark decode `a728d708de5f55022b776405bb7b2ffd6f12cc5239c4cdd54c9f0b36fad4712e`.
- Peer traffic benchmark `commercial-peer-benchmark-20260627T112215Z` passed with warmup, sustained, and peak stages against the independent C SCTP peer. Sustained stage: `20` runs, `0` failures, average `647.2 ms`, P95 `660 ms`, P99 `665 ms`, max RSS `50732 KB`. Peak stage: `5` concurrent runs, `0` failures, P95/P99 `769 ms`.

## Remaining Commercial Blockers

- Package signing verification currently fails commercial requirements because the signing certificate is not trusted by the verifier and the signature is not timestamped.
- The current benchmark is real Linux SCTP peer traffic evidence, but it is single-host loopback. Do not use it for broad operator capacity claims until an operator-sized deployment benchmark is retained.
- Release workflow must regenerate package, SBOM, signing, provenance, benchmark, API baseline, and evidence artifacts after the final release commit.
- Package publication and stable commercial release gates are foundation-complete, but live stable publication still requires retained release evidence, a completed protected publication run, and verified NuGet publication evidence.
- The current local runner reports `LocalEvidenceReady=true`. `CommercialReady=false` until trusted timestamped signing and protected release-dispatch/publication evidence are complete.

## Commercial Decision

Do not publish this SDK as stable commercially production-ready yet.

The next release-candidate milestone should replace test signing with trusted timestamped production signing, run the protected release workflow for the final release commit, and attach the generated evidence artifacts to the release workflow. The stable release gate is ready to evaluate that evidence, but it must not be used to claim a stable commercial release until the protected publication run and NuGet publication evidence are retained and verified. The legacy OpenSS7/IPSS7 path can remain as historical evidence, but it is no longer the SDK's permanent commercial gate.
