# External Peer Interop Migration

SIGTRAN.NET now uses package-neutral external peer interoperability contracts.

## Naming Rule

Public SDK source names must describe the telecom domain and release gate, not a specific peer package.

Use names such as:

- External peer.
- Reference peer.
- Interop evidence.
- Production readiness.

Do not introduce public SDK names that include a peer package or implementation name.

## Legacy Evidence

The retained OpenSS7/IPSS7 VM attempt remains useful historical evidence because it documents a real Linux kernel compatibility blocker. It does not define the permanent commercial interoperability gate.

Future commercial interop evidence should be captured against a reference external SIGTRAN peer that satisfies the selection policy in `SigtranReferencePeerSelectionPolicy`.

## Lab Package Selection

The selected peer package belongs in lab configuration, artifact notes, and retained evidence. It does not belong in public type names, enum values, release gate names, or status capability names.

Required external peer evidence remains:

- PCAP.
- SDK trace.
- Peer configuration.
- Peer log.
- Comparison report.
- SHA-256 digest coverage.
