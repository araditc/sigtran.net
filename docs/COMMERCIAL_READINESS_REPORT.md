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

## Remaining Commercial Blockers

- OpenSS7/IPSS7 interoperability is blocked on Linux 5.15 `open_softirq` compatibility and still needs a passing external peer run.
- External interoperability evidence still needs PCAP, peer logs, SDK traces, and comparison report from a passing run.
- Package signing verification currently fails commercial requirements because the signing certificate is not trusted by the verifier and the signature is not timestamped.
- Performance evidence is smoke-only; commercial release needs sustained peer/load benchmark evidence with latency and resource metrics.
- Release workflow must regenerate package, SBOM, signing, provenance, benchmark, API baseline, and evidence artifacts after the final release commit.

## Commercial Decision

Do not publish this SDK as commercially production-ready yet.

The next release-candidate milestone should close the OpenSS7/IPSS7 lab blocker on a Linux 4.x-era OpenSS7-compatible environment or a maintained SIGTRAN peer, replace test signing with trusted timestamped production signing, and attach the generated evidence artifacts to the release workflow.
