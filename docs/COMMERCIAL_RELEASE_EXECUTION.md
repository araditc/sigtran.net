# Commercial Release Execution

This document is the release-day runbook for closing SIGTRAN.NET commercial readiness evidence. It separates local evidence that can be generated from the repository from evidence that requires external systems, protected secrets, or a real maintained SIGTRAN peer.

## Runner

Use the commercial release readiness runner from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-commercial-release-readiness.ps1
```

The runner writes a timestamped report under `artifacts/commercial-release/<run-id>/`. The artifact directory is intentionally ignored by git because it can contain run-specific logs, package hashes, VM observations, and retained release evidence.

## What The Runner Executes

The runner performs these local release-day checks:

- Release build.
- Release test workload.
- NuGet package creation.
- SPDX SBOM generation.
- Public API baseline generation.
- Smoke benchmark evidence generation.
- Provenance file generation for the current source commit.
- Package signature verification.
- Optional Linux VM SSH readiness probe.
- GitHub CLI release-dispatch readiness probe.

## Evidence Boundary

`LocalEvidenceReady` means build, tests, package creation, SBOM, public API baseline, smoke benchmark, and provenance generation passed for the current repository state.

`CommercialReady` is stricter. It must stay false until all commercial blockers are cleared with retained evidence. Use `-EvidenceManifestPath` to bind reviewed external evidence into the runner:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-commercial-release-readiness.ps1 `
  -Version 1.0.0-rc.1 `
  -EvidenceManifestPath docs\evidence\COMMERCIAL_EVIDENCE_20260627.json
```

The manifest can close evidence-only blockers such as external peer, production benchmark, and internal RC signing evidence when retained artifact paths and digests have been reviewed. It cannot replace protected release dispatch or NuGet publication evidence.

Commercial readiness requires:

- Trusted timestamped package signing is verified.
- A maintained external SIGTRAN peer run produces retained PCAP, peer logs, SDK traces, configuration, comparison report, run report, and digests.
- Native SCTP behavior is verified with retained Linux peer traffic evidence.
- Production peer benchmark evidence is retained with latency, resource, and failover observations.
- Release dispatch and artifact upload are executed from the protected release workflow.
- Stable publication evidence is retained after the protected publication run.

## Linux VM Access

The default VM probe expects key-based SSH access:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-commercial-release-readiness.ps1 `
  -VmHost 192.168.100.28 `
  -VmUser ammar `
  -SshKeyPath "$HOME\.ssh\sigtran_vm_release_ed25519"
```

The VM should retain lab evidence under a run-specific artifact root. The repository must not store VM passwords, signing certificate private keys, or NuGet API keys.

## Native SCTP Lab Tool

`src/Sigtran.NET.NativeSctpLab` publishes a self-contained Linux executable for native SCTP evidence runs:

```powershell
dotnet publish .\src\Sigtran.NET.NativeSctpLab\Sigtran.NET.NativeSctpLab.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o .\artifacts\native-sctp-lab\linux-x64
```

The executable can run in `loopback`, `server`, or `client` mode. The commercial VM loopback run sends M3UA ASPUP, ASPACTIVE, Payload DATA, HEARTBEAT, and HEARTBEAT ACK over native SCTP, then writes SDK trace JSONL records with message labels, lengths, SHA-256 values, and hex payloads.

Retained VM evidence run:

| Field | Value |
| --- | --- |
| Run id | `commercial-native-sctp-20260627T073300Z` |
| Host | `sigtrannet` |
| OS | Ubuntu 22.04.1 LTS |
| Kernel | `5.15.0-181-generic` |
| Trace events | `18` |
| M3UA trace events | `14` |
| Payload DATA trace events | `2` |
| PCAP bytes | `2106` |
| PCAP SHA-256 | `7215966b1f46578ecfbb090285269ff64e20249a5a72dcb3ff135db0c24a2248` |
| SDK trace SHA-256 | `a16b1f337ebe56091ece7497597163c111543daa485a2e9f4e2fc73472ae17f8` |
| TShark comparison SHA-256 | `abe082aea2423d53f6e78c814a5c408e0f77b4c06604316e3879bd977f369deb` |
| Run report SHA-256 | `0933d0ce1d95dd494f1a89c0f2bd84c81bc89a96a36eeb62e118c6302cf9f1b4` |

This closes the SDK native SCTP loopback evidence gap. It does not close the maintained external peer interoperability gap because both endpoints are the SDK lab executable on the same Linux host.

## External Peer Evidence

The current external peer evidence run is `commercial-external-peer-20260627T111932Z` on host `sigtrannet`. The SDK ran as an SCTP/M3UA client against an independent C SCTP peer that emitted ASPUP ACK, ASPACTIVE ACK, and HEARTBEAT ACK over Linux kernel SCTP.

Retained external peer evidence:

| Field | Value |
| --- | --- |
| Artifact root | `/home/ammar/sigtran-lab/artifacts/commercial-external-peer/commercial-external-peer-20260627T111932Z` |
| PCAP bytes | `2030` |
| SDK trace events | `7` |
| Peer events | `7` |
| TShark M3UA decode hits | `7` |
| PCAP SHA-256 | `2d4313440e665ddd9686bc3be3937810921d11c6d4ed2e6b4746a71d31f79416` |
| SDK trace SHA-256 | `11a6da80dffc786ba66667ca0c85cc7b68fa5eb0f24316c5d47ea5ea2bb842fb` |
| Peer log SHA-256 | `50509d0ca7013c4496516daaec327eb269393c8fccc2036869db1bca2794c897` |
| TShark decode SHA-256 | `a728d708de5f55022b776405bb7b2ffd6f12cc5239c4cdd54c9f0b36fad4712e` |

## Peer Benchmark Evidence

The current peer traffic benchmark is `commercial-peer-benchmark-20260627T112215Z` on host `sigtrannet`. It runs the SDK client against the independent C SCTP peer with warmup, sustained, and peak stages.

| Stage | Count | Failures | Avg ms | P95 ms | P99 ms | Max RSS KB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Warmup | `3` | `0` | `642.0` | `661` | `661` | `50340` |
| Sustained | `20` | `0` | `647.2` | `660` | `665` | `50732` |
| Peak | `5` | `0` | `675.8` | `769` | `769` | `50440` |

This benchmark closes the smoke-only evidence gap for the RC gate. It is still single-host loopback evidence and must not be used as an operator-wide capacity claim.

## Signing And Publication

Commercial package signing requires a code-signing certificate, timestamp authority, and retained verification evidence. The internal RC signing helper creates self-signed RC evidence for dry-runs:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\new-internal-signing-evidence.ps1 `
  -Version 1.0.0-rc.1 `
  -TrustCurrentUserRoot
```

The retained internal signing run is `internal-signing-20260627T122124Z`. Its detailed verification log records the NuGet author signature, timestamp, and Sectigo timestamping chain. This is enough for internal RC dry-run evidence after reviewer approval. Stable public release signing still requires the organization's approved trusted signing certificate in the protected release environment.

For the protected GitHub dry-run workflow, create internal signing secrets with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\set-github-internal-signing-secrets.ps1
```

The script uploads `SIGNING_CERTIFICATE` and `SIGNING_CERTIFICATE_PASSWORD` to GitHub Secrets, sets `TIMESTAMP_AUTHORITY`, and deletes the local PFX/private key. During `channel=dry-run`, the workflow extracts the public certificate from the PFX and trusts it only inside the temporary GitHub runner so `dotnet nuget verify --all --verbosity detailed` can retain timestamped signature evidence. Do not use this internal certificate path for `prerelease` or `stable` publication.

Publication requires the protected release workflow and live secrets:

- `NUGET_API_KEY`
- `SIGNING_CERTIFICATE`
- `SIGNING_CERTIFICATE_PASSWORD`

These values belong in GitHub protected environments or equivalent secret stores. They must never be committed to the repository.
