# Phase 21 Supply Chain Automation

Phase 21 adds the supply-chain automation foundation for SIGTRAN.NET.

The earlier release work defined SBOM and package signing as commercial release requirements. This phase turns those requirements into a release-security execution contract with ordered commands, required artifacts, promotion gates, readiness, and CI metadata.

The public APIs use domain names such as `SigtranSupplyChainStatus`; phase numbers are intentionally kept out of source type names.

## Automation Plan

`SigtranSupplyChainAutomation.CreateDefaultPlan()` defines the default supply-chain plan.

The plan wires together:

- Tool restore.
- SBOM generation.
- Package signing.
- Signature verification.
- Provenance attestation.
- Production evidence verification.

The plan uses `SigtranSbom.CreateDefaultPlan()` and `SigtranPackageSigning.CreateDefaultPlan()` so release-security execution stays aligned with the existing SBOM and signing policies.

## Artifacts

`SigtranSupplyChainArtifactManifest` requires:

- SBOM.
- Package signature.
- Timestamp receipt.
- Provenance attestation.
- Verification report.

All retained artifacts must have digests before a supply-chain promotion can be accepted.

## Gate

`SigtranSupplyChainGate.Evaluate()` checks:

- Executable supply-chain plan.
- Complete supply-chain artifacts.
- Complete artifact digests.
- Release provenance references.
- Production evidence readiness.

This keeps supply-chain automation separate from commercial evidence. Both must be ready before a commercial release can be promoted.

## Readiness

`SigtranSupplyChainReadiness.GetReport()` separates automation foundation readiness from current promotion readiness.

The foundation is ready. Promotion remains false until real signed packages, SBOMs, provenance attestations, verification reports, and commercial evidence are retained.

## CI Profile

`SigtranSupplyChainCi.CreateDefault()` defines the opt-in supply-chain CI profile.

The profile is enabled by `SIGTRAN_SUPPLY_CHAIN`, writes artifacts under `SIGTRAN_SUPPLY_CHAIN_ARTIFACT_ROOT`, and requires signing secret names for commercial release runs.

## Status

`SigtranSupplyChainStatus.Describe()` summarizes the supply-chain automation foundation and keeps promotion readiness false until the real artifact and evidence gates pass.
