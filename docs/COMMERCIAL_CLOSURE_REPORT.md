# Commercial Closure Report

Status: internal RC dry-run evidence is closed; stable commercial publication is still gated.

## Evidence Closed Today

- External peer SCTP/M3UA evidence passed against an independent C SCTP peer, not the SDK loopback server.
- Native SCTP traffic was captured on Ubuntu 22.04.1 LTS with kernel `5.15.0-181-generic`.
- Retained external peer artifacts include PCAP, peer log, SDK trace, TShark decode, field comparison, config, run report, run summary, and digest manifest.
- Peer traffic benchmark evidence passed with warmup, sustained, and peak stages against the same independent peer.
- Local release evidence passed for build, tests, package creation, SBOM, public API baseline, smoke benchmark, and provenance.
- Internal RC package-signing evidence passed with a timestamped NuGet author signature and detailed verification log.
- Protected GitHub release workflow dry-run `28289987418` passed with `publish=false` and uploaded package, symbols, supply-chain, and dry-run artifacts.

## Evidence Manifest

The audit manifest is retained at:

`docs/evidence/COMMERCIAL_EVIDENCE_20260627.json`

Key run ids:

- External peer: `commercial-external-peer-20260627T111932Z`
- Performance: `commercial-peer-benchmark-20260627T112215Z`
- Local release readiness: `20260627T122913Z`
- Final local readiness with release-dispatch evidence: `20260627T130623Z`
- Internal signing: `internal-signing-20260627T122124Z`
- Protected release workflow dry-run: `28289987418`

## Signing Evidence

Internal RC signing used a self-signed code-signing certificate trusted in the current user's root store and timestamped through Sectigo:

- Signed package: `artifacts/internal-signing/20260627T122124Z/Sigtran.NET.1.0.0-rc.1.nupkg`
- Package SHA-256: `9f97a2b03ae120ef633baf4fb7522924d644ad73dc1282e15c3f02f82475dfab`
- Certificate SHA-256 fingerprint: `678103179f7dd54c1427bed0074e3809a3398c569799bc556f26677be89354a3`
- Verification log SHA-256: `751c53a77b2b5c05aa8fbbd841491e0b03a74472582a7dd5b0c33e55032f9691`
- Timestamp authority: `http://timestamp.sectigo.com`

This closes internal RC signing evidence. It does not claim public CA-backed stable signing.

## Remaining Gates

- Public/stable signing must use the organization's approved trusted certificate and protected release environment.
- NuGet publication is intentionally paused until explicitly requested.
- Operator-sized deployment benchmarks are still required before broad capacity claims beyond the current single-host peer benchmark.

## Decision

The SDK is ready for internal signed RC consumption and controlled SIGTRAN/M3UA integration trials. It is not yet ready to claim stable public commercial publication until NuGet publication, stable signing policy, and any operator-specific capacity evidence are approved.
