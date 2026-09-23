# Changelog

All notable changes to SIGTRAN.NET will be documented in this file.

This project follows Semantic Versioning once the public API reaches a stable release. Before the first stable release, breaking API changes may still occur when they are required to align protocol behavior, naming, packaging, or interoperability with the intended stable SDK surface.

## [Unreleased]

Current source development package identity: `1.0.0-rc.3-dev`. This is not a published release candidate and must not be represented as one.

### Added

- Initial fail-closed network/operator profile foundation for the implemented ITU 14-bit, connectionless SCCP, and MAP SMS surface.
- Deterministic structured validation for network indicator, point-code format, compatibility mode, SCCP global-title translation rules, and MAP SMS operation allowlists.
- Synthetic operator-profile documentation and dedicated CI regression tests.
- Durable roadmap-execution checkpoint with a frozen stable 1.0 protocol scope.

### Changed

- Development package/version identity moved past the immutable public `1.0.0-rc.2` artifact so later source changes cannot collide with the published RC.
- Phase 53/55/56 readiness text now reflects that independent M2PA and the numeric 20K TPS capacity gate are closed; four stable gates remain open.

### Release status

No package from this section has been published. Stable `1.0.0` remains blocked by operator/vendor profile acceptance, representative multi-host soak/failover, representative Kubernetes SCTP qualification, and an organization-trusted signing identity.

## [1.0.0-rc.2] - 2026-09-22

### Added

- Formal layer contracts and stateful SCTP, M3UA, M2PA, SCCP, TCAP, and MAP SMS
  runtime APIs.
- Runtime health, BCL/OpenTelemetry diagnostics, structured events,
  configuration validation, and an operations host.
- Reflection-based public API baseline and protected stable release decision.

### Changed

- Normal package builds for the RC.2 source line produced `1.0.0-rc.2` rather than accidental stable `1.0.0` packages.
- Repository release/evidence governance types are internal and no longer part
  of the consumer API.

### Release status

Published as both a GitHub prerelease and NuGet prerelease. NuGet publication was verified through Trusted Publishing/OIDC and a clean .NET 10 restore. Stable `1.0.0` remains blocked by the retained `NO-GO` decision.

## [0.1.0-alpha] - 2026-06-27

### Release type

First public alpha / preview release preparation.

### Summary

SIGTRAN.NET is introduced as the first open-source .NET 10 SDK dedicated to SIGTRAN and SS7-over-IP protocol engineering.

This alpha milestone is intended for early contributors, protocol review, lab validation, and community feedback. It is not yet a production-ready telecom signaling stack.

### Current focus

- M3UA as the first production-oriented protocol milestone.
- Transport abstraction for SIGTRAN workloads.
- SCTP direction with Linux native SCTP as the intended production path.
- SCCP, TCAP, and MAP foundations for future standards-oriented layers.
- Byte-level protocol testing and protocol validation.
- Wireshark-friendly diagnostics and trace-oriented tooling.
- Release governance, package metadata, and documentation readiness.

### Contributor areas

Community contributions are especially welcome in the following areas:

- M3UA protocol review and validation.
- SCTP transport testing on Linux.
- SCCP, TCAP, and MAP standards alignment.
- ASN.1 BER validation and telecom protocol conformance.
- Wireshark trace comparison and interoperability testing.
- Documentation, examples, and developer experience.
- High-performance C# review and memory-allocation improvements.

### Production-readiness notice

This is an alpha release track. Production usage should wait for retained interoperability evidence, Linux SCTP verification, external peer validation, and stable release governance.

[0.1.0-alpha]: https://github.com/araditc/sigtran.net/releases/tag/v0.1.0-alpha
