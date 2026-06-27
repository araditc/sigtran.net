# Changelog

All notable changes to SIGTRAN.NET will be documented in this file.

This project follows Semantic Versioning once the public API reaches a stable release. During the alpha phase, breaking API changes may still occur as protocol behavior, transport abstractions, and interoperability evidence are finalized.

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
