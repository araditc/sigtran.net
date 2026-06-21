# Phase 7 Commercialization

Phase 7 is the final roadmap phase for turning the SDK foundation into a commercially usable release.

See [Phase 7 Summary](PHASE7_SUMMARY.md) for the completed unit inventory and remaining external production gates.

## Commercial Readiness Gate

`SigtranCommercialReadiness.GetReport()` separates internal release readiness from commercial production readiness.

```csharp
SigtranCommercialReadinessReport report = SigtranCommercialReadiness.GetReport();
bool canPublishCandidate = report.InternalReleaseReady;
bool canClaimCommercialProduction = report.CommercialReady;
```

Internal release readiness is currently available because SDK foundations, interoperability tooling, and CI verification are present.

Commercial readiness remains blocked until native SCTP verification, external interoperability evidence, and release governance are complete.

## Native SCTP Support Matrix

`SigtranNativeSctpSupport.GetSupportMatrix()` exposes the current native SCTP support plan:

| Platform | Status |
| --- | --- |
| Linux | Verification required |
| Windows | Contract only |
| macOS | Contract only |

Linux is the target for native SCTP lab verification. Windows and macOS remain supported through transport contracts and development adapters until a production provider is selected and verified.

Phase 8 adds the native SCTP implementation foundation and exposes it through `SigtranNativeSctpSupport.IsImplementationFoundationReady()`. Commercial readiness still requires Linux verification through `NativeSctpReadinessReport.IsProductionReady`.

## External Interoperability Evidence

`SigtranInteropEvidenceRegistry` records evidence from real peer stacks and captured traces.

```csharp
SigtranInteropEvidenceRegistry registry = new();
registry.Add(new SigtranInteropEvidenceItem(
    "lab/linux/m3ua-asp",
    "external-sigtran-peer",
    "M3UA ASP to SG",
    "traces/m3ua-asp.pcapng",
    SigtranInteropEvidenceResult.Passed));
```

The current registry is intentionally empty until real lab artifacts are added. Commercial readiness requires at least one passing external evidence item, and later release policies should require multiple peer stacks and scenarios.

## Release Candidate Manifest

`SigtranReleaseCandidate.Create(version, commitSha)` creates an auditable release snapshot.

```csharp
SigtranReleaseCandidateManifest manifest =
    SigtranReleaseCandidate.Create("1.0.0-alpha.1", "abcdef0");
```

Release candidates can be published after internal gates pass. Promotion to commercial production remains blocked until the commercial readiness report is fully green.

## Package Governance

`SigtranPackageGovernance.CreateCurrentPolicy()` describes the package metadata already present: license, README, repository metadata, and symbols.

`SigtranPackageGovernance.CreateCommercialTargetPolicy()` adds the commercial governance target: package signing and SBOM publication.

Commercial release governance remains incomplete until signing and SBOM automation are added to the release pipeline.

## Security Policy

`SECURITY.md` defines the public disclosure process and response targets. `SigtranSecurityPolicy.CreateCurrentPolicy()` exposes the same values to SDK governance tooling.

Critical vulnerabilities target a 2-day response. High severity vulnerabilities target a 7-day response. Other severities target a 14-day response.

## Compatibility Policy

`docs/COMPATIBILITY.md` defines the public versioning posture. The SDK targets `net10.0`, follows Semantic Versioning, allows necessary breaking changes before stable release, and requires a major version for breaking changes after stable release.

## Observability Profile

`docs/OBSERVABILITY.md` defines the metric names, trace categories, and health signals expected by commercial deployments. `SigtranObservability.CreateDefaultProfile()` exposes the same profile to applications and release tooling.

## Deployment Profiles

`docs/DEPLOYMENT.md` defines commercial Linux and local development deployment profiles. The commercial profile requires native SCTP, external evidence, observability, and security policy support. The local profile remains development-only.
