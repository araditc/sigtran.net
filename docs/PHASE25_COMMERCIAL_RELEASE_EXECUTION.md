# Phase 25 Commercial Release Execution And Evidence

Phase 25 converts the commercial readiness foundations into retained execution evidence.

The phase is intentionally evidence-driven. Source APIs keep domain names and do not use roadmap phase labels.

## Unit 1 - Execution Evidence Manifest

`SigtranCommercialReleaseEvidenceManifest` records retained artifacts by area, kind, status, path, digest, and review note.

Current retained evidence includes:

- Passed Linux SCTP loopback PCAP from WSL2.
- Blocked OpenSS7/IPSS7 configure log showing the WSL2 kernel-major compatibility blocker.

The manifest does not support commercial promotion while any blocker is present.

## Unit 2 - Linux SCTP Smoke Evidence

`SigtranLinuxSctpEvidence` records the retained WSL2 Linux SCTP smoke capture summary.

The current capture has:

- `PACKET_COUNT=14`
- `SCTP_COUNT=14`
- `PCAP_SIZE=1556`
- SCTP association handshake, DATA exchange, and clean shutdown.

This is valid Linux SCTP smoke evidence, but it is not yet full external peer interoperability evidence.

## Unit 3 - OpenSS7/IPSS7 Blocker Evidence

`SigtranOpenSs7InteropBlockerEvidence` records the retained OpenSS7/IPSS7 execution blocker as structured evidence.

Current blocker:

- Environment: WSL2 Ubuntu 24.04.
- Log: `/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log`.
- Failure: OpenSS7 configure rejects Linux kernel major version 6 before peer runtime can start.
- Required action: retest on a compatible Linux kernel or patch the OpenSS7 kernel-version check with retained build evidence.

The blocker intentionally prevents interoperability promotion until a passing OpenSS7/IPSS7 run produces PCAP, peer logs, SDK traces, and comparison evidence.
