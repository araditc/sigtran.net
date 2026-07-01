# SIGTRAN.NET Production Readiness Report

Status: public RC prerelease publication is closed; stable commercial publication remains gated.

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
- Production release-day readiness runner is available and produces timestamped local evidence reports for build, tests, package creation, SBOM, public API baseline, smoke benchmark, provenance, signing verification, VM SSH probing, and GitHub release-dispatch probing.
- External peer SCTP/M3UA run `commercial-external-peer-20260627T111932Z` passed on host `sigtrannet` against an independent C SCTP peer. Retained artifacts include PCAP, external peer log, SDK trace, TShark decode, M3UA field comparison, configuration, run report, summary JSON, and digest manifest.
- External peer evidence digests: PCAP `2d4313440e665ddd9686bc3be3937810921d11c6d4ed2e6b4746a71d31f79416`, SDK trace `11a6da80dffc786ba66667ca0c85cc7b68fa5eb0f24316c5d47ea5ea2bb842fb`, peer log `50509d0ca7013c4496516daaec327eb269393c8fccc2036869db1bca2794c897`, TShark decode `a728d708de5f55022b776405bb7b2ffd6f12cc5239c4cdd54c9f0b36fad4712e`.
- Peer traffic benchmark `commercial-peer-benchmark-20260627T112215Z` passed with warmup, sustained, and peak stages against the independent C SCTP peer. Sustained stage: `20` runs, `0` failures, average `647.2 ms`, P95 `660 ms`, P99 `665 ms`, max RSS `50732 KB`. Peak stage: `5` concurrent runs, `0` failures, P95/P99 `769 ms`.
- Internal RC signing run `internal-signing-20260627T122124Z` passed with `SignExitCode=0` and `VerifyExitCode=0`. The retained detailed verification log records the NuGet author signature, the certificate fingerprint `678103179f7dd54c1427bed0074e3809a3398c569799bc556f26677be89354a3`, the timestamp time, and the Sectigo timestamping certificate chain.
- Protected release workflow dry-run `28289987418` passed on commit `fd1224143361307673e4ec7b14e732098aa78a5e` with `publish=false`. It uploaded `sigtran-package`, `sigtran-symbols`, `sigtran-supply-chain`, and `sigtran-release-dry-run` artifacts and retained package, SBOM, signing verification, timestamp, digest, API diff, dry-run, and local provenance marker evidence.
- Protected prerelease publication workflow `28290586511` passed on commit `914fc333fc3b99184af9781d25585928583a3239` with `publish=true`. It pushed `Sigtran.NET.1.0.0-rc.1.nupkg` and `Sigtran.NET.1.0.0-rc.1.snupkg` to NuGet.org, uploaded package, symbols, supply-chain, and dry-run artifacts, and retained publication evidence at `docs/evidence/NUGET_PRERELEASE_PUBLISH_28290586511.json`.
- NuGet.org visibility and restore evidence passed for `Sigtran.NET` version `1.0.0-rc.1`: the package page returned HTTP 200, the flat-container index includes `1.0.0-rc.1`, the flat-container package returned HTTP 200, and a clean `dotnet add package Sigtran.NET --version 1.0.0-rc.1` restore succeeded.
- Final local readiness run `20260627T130623Z` evaluated the retained evidence manifest and reported `LocalEvidenceReady=true`, `ProductionReady=true`, and no commercial blockers for the internal RC gate.

## Remaining Production Blockers

- The current benchmark is real Linux SCTP peer traffic evidence, but it is single-host loopback. Do not use it for broad operator capacity claims until an operator-sized deployment benchmark is retained.
- Package publication evidence is closed for the public RC prerelease. Stable commercial release gates are foundation-complete, but live stable publication still requires retained stable release evidence, a completed protected stable publication run, and verified stable NuGet publication evidence.
- Public/stable signing must use the organization's approved trusted certificate in the protected release environment; the current signing evidence is internal self-signed RC evidence.
- Hosted GitHub provenance/SBOM attestations were skipped for dry-run and prerelease because private repository or organization attestation persistence can require a supported plan or public repository. These runs retained local provenance markers; stable runs keep hosted attestation reserved for the protected stable gate.

## Production Decision

Do not publish this SDK as stable commercially production-ready yet.

The SDK has enough retained evidence for public RC consumption and controlled integration use. The stable release gate is ready to evaluate future stable publication evidence, but it must not be used to claim a stable public commercial release until the protected stable publication run, trusted public signing evidence, and stable NuGet publication evidence are retained and verified. The legacy OpenSS7/IPSS7 path can remain as historical evidence, but it is no longer the SDK's permanent commercial gate.
