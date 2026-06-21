# Phase 20 Commercial Evidence Dossier

Phase 20 adds the commercial evidence dossier foundation for SIGTRAN.NET.

Earlier phases created separate foundations for native SCTP verification, external peer execution, protocol interop vectors, release provenance, package governance, SBOM policy, and signing policy. This phase introduces a single evidence bundle contract that can collect those artifacts into one release dossier.

The public APIs use domain names such as `SigtranCommercialEvidenceStatus`; phase numbers are intentionally kept out of source type names.

## Requirements

`SigtranCommercialEvidenceRequirements.GetRequirements()` defines five production claim areas:

- Native SCTP Linux kernel peer-traffic evidence.
- External peer M3UA ASP-to-SG evidence.
- SCCP, TCAP, and MAP SMS protocol vector evidence.
- Release provenance and package manifest evidence.
- Package, symbol package, SBOM, and signature evidence.

These requirements make the remaining commercial blockers explicit and reviewable.

## Artifacts

`SigtranCommercialEvidenceArtifact` records an evidence area, artifact kind, path, and optional SHA-256 digest.

The artifact kinds cover packet captures, SDK traces, peer configuration, peer logs, reference vectors, SDK vectors, comparison reports, provenance, package manifests, packages, SBOMs, and signatures.

## Manifest

`SigtranCommercialEvidenceManifest` stores retained artifacts and can evaluate whether all required artifacts are present for one requirement or for the complete requirement set.

The manifest also checks digest coverage. A bundle without digests cannot support a commercial evidence claim.

## Bundle

`SigtranCommercialEvidenceBundle` combines a release version, requirements, and a manifest.

A bundle is complete only when all requirements are satisfied and every retained artifact has a digest.

## Gate

`SigtranCommercialEvidenceGate.Evaluate()` checks both the retained evidence bundle and the underlying verification claims:

- Complete commercial evidence artifacts.
- Complete commercial evidence digests.
- Native SCTP verification.
- External peer interoperability verification.
- Protocol vector verification.
- Release governance readiness.

This keeps a complete-looking artifact folder from being treated as production evidence unless the actual verification gates are also true.

## Readiness

`SigtranCommercialEvidenceReadiness.GetReport()` separates foundation readiness from current commercial evidence readiness.

The foundation is ready. Current commercial evidence readiness remains false until real evidence from the required labs and release process is retained.

## CI Profile

`SigtranCommercialEvidenceCi.CreateDefault()` defines an opt-in commercial evidence profile.

The profile is enabled by `SIGTRAN_COMMERCIAL_EVIDENCE` and uses `SIGTRAN_COMMERCIAL_EVIDENCE_ROOT` for retained dossier artifacts.

## Naming Cleanup

Phase 20 also normalizes older status capability strings from `phase-documentation` to `documentation`.

This keeps source metadata aligned with the project rule that public SDK naming and source-level status metadata should describe domains, not roadmap phase numbers.

## Status

`SigtranCommercialEvidenceStatus.Describe()` summarizes the dossier foundation and keeps commercial evidence readiness false until retained artifacts and verification gates are complete.
