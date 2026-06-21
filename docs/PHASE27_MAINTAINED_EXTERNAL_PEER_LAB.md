# Phase 27 Maintained External Peer Lab

Phase 27 turns the external peer decision into a package-neutral lab contract. The SDK must describe what it needs from a maintained SIGTRAN peer without naming one implementation in public type names or release gates.

## Unit 1 - Canonical SDK Name

`Sigtran.NET` is now the canonical SDK identity across source paths, namespaces, package metadata, workflows, scripts, docs, and NuGet artifacts.

See [Phase 27 Sigtran.NET Branding](PHASE27_SIGTRAN_NET_BRANDING.md) for the focused rename record.

## Unit 2 - Maintained Peer Lab Binding Catalog

`SigtranMaintainedPeerLabBindings` now provides a package-neutral binding catalog for the maintained external peer lab. The default binding records:

- The selected external SIGTRAN peer profile.
- The package id and version placeholders that a real lab can replace.
- The artifact root used by retained evidence.
- The required environment variables consumed by lab scripts.
- The maintained peer selection criteria satisfied by the binding.

The public API keeps selected package details in values and configuration, not in class names. This preserves the ability to bind a modern maintained peer package later without coupling the SDK contract to a specific project.

## Unit 3 - Host Prerequisite Model

`SigtranMaintainedPeerLabPrerequisites` now defines the package-neutral host checks that must be satisfied before a maintained peer lab run can claim execution readiness:

- Linux host or VM with kernel SCTP support.
- Native SCTP tools and kernel module availability.
- Packet capture tooling.
- .NET 10 SDK or runtime for the SDK lab runner.
- Maintained external peer package installed outside the SDK.
- Writable retained artifact storage.

The prerequisite report separates foundation code readiness from actual lab readiness. A commercial claim still requires a real host report with every prerequisite satisfied and retained alongside the run evidence.

## Environment Contract

The default binding exposes these variables:

```text
SIGTRAN_EXTERNAL_PEER_ID
SIGTRAN_EXTERNAL_PEER_PACKAGE
SIGTRAN_EXTERNAL_PEER_PACKAGE_VERSION
SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT
```

Lab scripts should read these values and write retained artifacts under the configured artifact root. Real package names, versions, and installation paths belong in lab configuration and retained evidence, not in SDK type names.

## Validation

This unit is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

The tests verify that the default binding satisfies the maintained peer selection policy, public binding summaries remain package-neutral, and host prerequisite reports identify missing lab capabilities.
