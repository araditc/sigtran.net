# Phase 22 Release Workflow Orchestration

Phase 22 turns release workflow work into smaller, commit-sized parts.

Part 1 adds the release workflow contract foundation. It defines what the commercial release workflow must contain before a concrete workflow file is generated or promoted.

The public APIs use domain names such as `SigtranReleaseWorkflowStatus`; phase numbers are intentionally kept out of source type names.

## Part 1 - Workflow Contract

`SigtranReleaseWorkflows.CreateCommercialReleasePlan()` defines the commercial release workflow contract.

The contract includes:

- Manual dispatch and version tag triggers.
- Checkout and .NET setup stages.
- Restore, build, test, and pack stages.
- Supply-chain automation stage.
- Commercial evidence verification stage.
- Package publish stage.
- Required secret names for signing and publishing.

## Readiness Boundary

`SigtranReleaseWorkflowReadiness.GetReport()` separates the workflow contract from the concrete workflow file.

The contract is ready after Part 1. Full orchestration remains false until a real workflow file is added and validated in a later part.

## Status

`SigtranReleaseWorkflowStatus.Describe()` summarizes the contract foundation.

`ContractReady` is expected to be true. `OrchestrationReady` remains false until the workflow file exists and is wired to the contract.

## Next Parts

Later Phase 22 parts should:

- Add the concrete GitHub Actions release workflow.
- Align the workflow file with the SDK contract.
- Add release promotion gates for publish/tag/provenance behavior.
- Keep each part separately documented, tested, packed, committed, and pushed.

## Stage 23 Unit 1 - Concrete Workflow File

Stage 23 Unit 1 adds `.github/workflows/release.yml` and `SigtranReleaseWorkflowFiles.CreateDefault()`.

The workflow file follows the contract from Phase 22 and contains restore, build, test, pack, supply-chain, commercial-evidence, and publish stages. The publish stage is gated behind manual dispatch with `publish=true`.

## Stage 23 Unit 2 - Workflow YAML Validation

Stage 23 Unit 2 adds `SigtranReleaseWorkflowValidation.ValidateYaml()`.

The validator checks that the concrete workflow YAML contains the release name, manual and tag triggers, .NET 10 setup, supply-chain and commercial evidence environment variables, signing secrets, NuGet secret, and publish gate.

## Stage 23 Unit 3 - Workflow Readiness Alignment

Stage 23 Unit 3 connects the concrete workflow file contract to `SigtranReleaseWorkflowReadiness`.

With the workflow file contract and YAML validation in place, release workflow orchestration foundation readiness can now be reported as ready. This does not mean the release can be commercially promoted; commercial evidence and supply-chain promotion gates still control that claim.

## Stage 23 Unit 4 - Publish Guard

Stage 23 Unit 4 adds `SigtranReleasePublishGuard`.

The guard blocks accidental publication unless publishing is explicitly requested through manual dispatch, a version tag is present, and the NuGet API key is available.

## Stage 23 Unit 5 - Artifact Retention

Stage 23 Unit 5 adds `SigtranReleaseWorkflowArtifacts`.

The release workflow now has a contract for retaining NuGet packages, symbol packages, supply-chain artifacts, and commercial evidence artifacts with audit-friendly retention periods.

## Stage 23 Unit 6 - Workflow Permissions

Stage 23 Unit 6 adds `SigtranReleaseWorkflowPermissions`.

The default permission set keeps repository contents read-only, allows OIDC token issuance for provenance workflows, and keeps package permissions disabled because NuGet publishing uses the explicit NuGet API key secret.
