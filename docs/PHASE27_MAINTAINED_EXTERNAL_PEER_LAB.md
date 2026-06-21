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

## Unit 4 - Lab Configuration Contract

`SigtranMaintainedPeerLabConfiguration` now captures the ASP-to-SG lab configuration used by maintained external peer runs:

- Peer name.
- Local and remote SCTP endpoints.
- SIGTRAN adaptation.
- SS7 network indicator and service indicator.
- OPC and DPC.
- Routing context.
- Traffic mode.
- Retained artifact root.

The helper can create the default lab configuration or parse environment values from a shell-driven lab file. Validation rejects invalid IP addresses, invalid SCTP ports, unsupported adaptation values, missing routing context, and unsupported traffic modes before a lab run is promoted.

## Unit 5 - Retained Artifact Plan

`SigtranMaintainedPeerLabArtifactPlans` now defines the retained artifact layout for each maintained peer lab run. The default plan creates deterministic paths for:

- PCAP capture.
- Peer log.
- Peer configuration.
- SDK trace.
- Trace comparison report.
- Run summary report.

This unit still describes expected artifacts, not proof that they exist. Promotion evidence remains blocked until a real lab run retains those files and records digests.

## Unit 6 - Command Plan

`SigtranMaintainedPeerLabCommandPlans` now defines the ordered execution plan for a maintained peer lab run:

- Prepare artifact directories and retained configuration.
- Capture SCTP packets.
- Start or verify the maintained external peer.
- Run SDK-side traffic.
- Compare traces.
- Collect the run report.

The command plan is intentionally package-neutral. Real deployments can map `external-peer-runner`, `sigtran-trace-compare`, and `sigtran-lab-report` to local scripts or CI steps without changing SDK public type names.

## Unit 7 - Traffic Vector Catalog

`SigtranMaintainedPeerLabTrafficVectors` now defines the expected maintained peer traffic sequence for comparison:

- M3UA ASP lifecycle.
- M3UA heartbeat and acknowledgement.
- M3UA DATA payload.

The catalog flattens these vectors into an ordered expected message sequence so comparison reports can separate protocol mismatch from host or package setup issues.

## Unit 8 - Evidence Promotion Gate

`SigtranMaintainedPeerLabEvidenceReport` now separates planned execution from promotable evidence. Promotion requires:

- Host prerequisites ready.
- Lab configuration valid.
- Every required artifact retained.
- Every retained artifact digest-covered.
- Trace comparison passed.

This keeps commercial readiness honest: a command plan or artifact plan is not treated as proof until retained outputs and digests exist.

## Environment Contract

The default binding exposes these variables:

```text
SIGTRAN_EXTERNAL_PEER_ID
SIGTRAN_EXTERNAL_PEER_PACKAGE
SIGTRAN_EXTERNAL_PEER_PACKAGE_VERSION
SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT
```

Configuration env files should also provide:

```text
PEER_NAME
LOCAL_IP
LOCAL_SCTP_PORT
REMOTE_IP
REMOTE_SCTP_PORT
SIGTRAN_ADAPTATION
NETWORK_INDICATOR
SERVICE_INDICATOR
OPC
DPC
ROUTING_CONTEXT
TRAFFIC_MODE
ARTIFACT_ROOT
```

Lab scripts should read these values and write retained artifacts under the configured artifact root. Real package names, versions, and installation paths belong in lab configuration and retained evidence, not in SDK type names.

## Validation

This unit is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

The tests verify that the default binding satisfies the maintained peer selection policy, public binding summaries remain package-neutral, host prerequisite reports identify missing lab capabilities, environment-derived lab configuration is validated before use, the artifact plan covers every required retained evidence path, the command plan covers every required execution step, the traffic vector catalog yields a comparable expected message sequence, and evidence promotion is blocked without complete digest-covered artifacts.
