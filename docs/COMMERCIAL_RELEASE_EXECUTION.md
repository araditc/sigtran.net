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

`CommercialReady` is stricter. It must stay false until all commercial blockers are cleared with retained evidence:

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

## Signing And Publication

Commercial package signing requires a trusted code-signing certificate and timestamp authority. A local unsigned package can pass build and pack checks, but `dotnet nuget verify --all` must fail until the package is signed. That failure is a valid commercial blocker, not a runner defect.

Publication requires the protected release workflow and live secrets:

- `NUGET_API_KEY`
- `SIGNING_CERTIFICATE`
- `SIGNING_CERTIFICATE_PASSWORD`

These values belong in GitHub protected environments or equivalent secret stores. They must never be committed to the repository.
