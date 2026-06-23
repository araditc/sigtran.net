# SIGTRAN.NET Commercial Readiness Report

Status: not commercially releasable yet.

## Passed Evidence

- SDK codebase builds, tests, and packs successfully.
- Linux SCTP smoke capture exists from a real Ubuntu 22.04 VM and records association setup, DATA exchange, and clean shutdown.
- SBOM generation is executable and produces SPDX JSON from package artifacts.
- Package signing execution exists and produces a signed NuGet package.
- Provenance generation is executable and records source, package, and SBOM digests.
- Smoke benchmark report generation is executable.
- Public API baseline generation is executable.
- Stable commercial release gate foundation is complete: target lock, dossier map, reviewed checklist, stable decision, tag gate, protected authorization, guarded publish plan, final report writer, audit trail, and final status reporting are available.

## Remaining Commercial Blockers

- External peer interoperability still needs a passing maintained peer run with retained PCAP, peer logs, SDK traces, configuration, and comparison report. The retained legacy OpenSS7/IPSS7 attempt is blocked on Linux 5.15 `open_softirq` compatibility.
- External interoperability evidence still needs PCAP, peer logs, SDK traces, and comparison report from a passing run.
- Package signing verification currently fails commercial requirements because the signing certificate is not trusted by the verifier and the signature is not timestamped.
- Performance evidence is smoke-only; commercial release needs sustained peer/load benchmark evidence with latency and resource metrics.
- Release workflow must regenerate package, SBOM, signing, provenance, benchmark, API baseline, and evidence artifacts after the final release commit.
- Package publication and stable commercial release gates are foundation-complete, but live stable publication still requires retained release evidence, a completed protected publication run, and verified NuGet publication evidence.

## Commercial Decision

Do not publish this SDK as commercially production-ready yet.

The next release-candidate milestone should complete a maintained external SIGTRAN peer lab run, replace test signing with trusted timestamped production signing, run representative peer/load benchmarks, and attach the generated evidence artifacts to the release workflow. The stable release gate is ready to evaluate that evidence, but it must not be used to claim a stable commercial release until the protected publication run and NuGet publication evidence are retained and verified. The legacy OpenSS7/IPSS7 path can remain as historical evidence, but it is no longer the SDK's permanent commercial gate.
