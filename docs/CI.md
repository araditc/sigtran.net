# Continuous Integration

Phase 6 adds the first official CI profile for the SDK.

## Verification Profile

`SigtranCiVerification.CreateDefaultProfile()` exposes the same command sequence used by local development and GitHub Actions:

1. `dotnet build src/sigtran.net.sln --configuration Release`
2. `dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release`
3. `dotnet pack src/sigtran.net/sigtran.net.csproj --configuration Release --no-build`

The profile targets `.NET 10` through the `10.0.x` SDK band.

## GitHub Actions

The workflow at `.github/workflows/dotnet.yml` runs on pushes and pull requests against `main`.

It restores the solution, builds in Release mode, runs the test harness, and packs the SDK. This keeps package validation, XML documentation enforcement, and byte-level protocol tests on the same path used before each manual commit.

Phase 7 package governance keeps package signing and SBOM generation as commercial release gates. They are not yet part of the default CI workflow.

## External Interoperability Lab

Phase 9 adds `SigtranInteropLabCiProfiles.CreateDefault()` for opt-in external lab runs.

The lab profile is disabled unless `SIGTRAN_INTEROP_LAB` is set to `1` or `true`. A real lab run also needs `SIGTRAN_INTEROP_LAB_ARTIFACT_ROOT` and `SIGTRAN_INTEROP_PEER`; native SCTP runs can use `SIGTRAN_NATIVE_SCTP_LAB`.

## Release CI

Phase 10 adds `SigtranReleaseCiProfiles.CreateDefault()` as the metadata contract for release workflows.

Release CI is intended for manual dispatch and version tags. It requires `NUGET_API_KEY` and `SIGNING_CERTIFICATE` secret names, but the repository must never contain secret values or private signing material.

## Developer Experience CI

Phase 11 adds `SigtranDeveloperExperienceCi.CreateDefault()` as the metadata contract for validating developer experience gates.

The profile reuses build, test, and pack commands and requires documentation readiness plus adoption readiness.

## Operations CI

Phase 12 adds `SigtranOperationsCi.CreateDefault()` as the metadata contract for validating operations readiness.

The profile reuses build, test, and pack commands and requires operations readiness.

## Compliance CI

Phase 13 adds `SigtranComplianceCi.CreateDefault()` as the metadata contract for validating compliance readiness.

The profile reuses build, test, and pack commands and requires compliance readiness.

## Performance CI

Phase 14 adds `SigtranPerformanceCi.CreateDefault()` as the metadata contract for validating performance readiness.

The profile reuses build, test, and pack commands, requires performance readiness, and keeps long-running benchmarks opt-in.

## API Lifecycle CI

Phase 15 adds `SigtranApiLifecycleCi.CreateDefault()` as the metadata contract for validating API lifecycle readiness.

The profile reuses build, test, and pack commands and requires public API diff review for API-shaping changes.

## Configuration CI

Phase 16 adds `SigtranConfigurationCi.CreateDefault()` as the metadata contract for validating configuration readiness.

The profile reuses build, test, and pack commands, requires configuration readiness, and rejects production plaintext secrets.

## Native SCTP Lab CI

Phase 17 adds `SigtranNativeSctpLabCi.CreateDefault()` as the metadata contract for native SCTP verification.

The profile is opt-in, Linux-only, and enabled by `SIGTRAN_NATIVE_SCTP_LAB`. Real runs must retain artifacts under `SIGTRAN_NATIVE_SCTP_ARTIFACT_ROOT`.

## OpenSS7/IPSS7 Interop CI

Phase 18 adds `SigtranOpenSs7InteropCi.CreateDefault()` as the metadata contract for OpenSS7/IPSS7 interoperability execution.

The profile is opt-in and enabled by `SIGTRAN_OPENSS7_INTEROP`. Real runs require a prepared OpenSS7/IPSS7 runner and must retain artifacts under `SIGTRAN_OPENSS7_ARTIFACT_ROOT`.

## Protocol Interop Vector CI

Phase 19 adds `SigtranProtocolInteropCi.CreateDefault()` as the metadata contract for SCCP, TCAP, and MAP SMS protocol vector execution.

The profile is opt-in and enabled by `SIGTRAN_PROTOCOL_INTEROP`. Real runs require retained external reference vectors and SDK-generated vectors under `SIGTRAN_PROTOCOL_VECTOR_ROOT`, plus comparison reports for every required vector.
