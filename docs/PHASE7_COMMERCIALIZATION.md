# Phase 7 Commercialization

Phase 7 is the final roadmap phase for turning the SDK foundation into a commercially usable release.

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
