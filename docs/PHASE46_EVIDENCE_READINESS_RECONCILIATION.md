# Phase 46 Evidence And Readiness Reconciliation

## Objective

Use one structured technical evidence catalog for readiness decisions so retained
verification results are neither hidden by stale hard-coded values nor promoted
beyond their actual scope.

## Retained Evidence Catalog

`SigtranVerificationCatalog` stores immutable verification facts by technical area,
run identifier, artifact reference, UTC observation time, and outcome.

`SigtranVerificationCatalogs.CreateCurrent()` registers the evidence currently
retained by the repository:

- Native Linux SCTP verification from
  `docs/evidence/PHASE45_NATIVE_SCTP_20260701T103951Z.json`.
- External SCTP peer and M3UA decode evidence from
  `docs/evidence/COMMERCIAL_EVIDENCE_20260627.json`.
- Release SBOM and provenance evidence from the protected dry-run workflow.
- Public API baseline and NuGet prerelease publication evidence.

The current catalog deliberately contains no passing record for SCCP, TCAP, MAP,
operator-sized performance, trusted package signing, or stable publication.

## Readiness Integration

The following reports now consume the same catalog:

- `NativeSctpReadiness`
- `SctpProductionHardeningReadiness`
- `SigtranInteroperabilityReadiness`
- `SigtranInteropLabReadiness`
- `SigtranProductionReadiness`

Native Linux SCTP, external peer SCTP, and M3UA interoperability now report their
retained PASS evidence. `SigtranProductionReadiness` remains blocked and exposes
the product-level work still required for the M3UA runtime, M2PA, stateful SCCP,
TCAP dialogue management, MAP SMS, operator-sized performance, trusted signing,
and stable publication.

## Support Matrix

The Linux entry in `SigtranNativeSctpSupport` is now
`ProductionVerified`. Windows and macOS remain contract-only. Consumers can query
one platform with `IsProductionVerified(SigtranOperatingSystemFamily)`.

The parameterless query retains its original meaning: every listed platform must
be verified, so it remains false while Windows and macOS are contract-only.

## Completion Criteria

Phase 46 is complete when:

- Every retained PASS claim is represented by a structured record.
- Native SCTP and M3UA readiness consume the retained records.
- Missing higher-layer, performance, signing, and stable-release evidence remains
  explicitly blocked.
- Public API additions include XML documentation.
- Build, test, and package validation pass.

