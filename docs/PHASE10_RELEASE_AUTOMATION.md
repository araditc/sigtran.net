# Phase 10 Release Automation

Phase 10 turns the SDK release path into a deterministic, reviewable supply-chain process.

## Release Automation Plan

`SigtranReleaseAutomation.CreateDefaultPlan()` defines the default release flow:

1. Restore
2. Build
3. Test
4. Pack
5. Validate artifacts
6. Publish

The plan targets `.NET 10` through the `10.0.x` SDK band and keeps publish as an explicit final step with a caller-provided package feed.

## Artifact Manifest

`SigtranReleaseArtifactManifest` records release artifacts for one package version.

Required package artifacts:

- NuGet package (`.nupkg`)
- Symbol package (`.snupkg`)

All release artifacts should carry SHA-256 digests before a governed publish.

## SBOM Plan

`SigtranSbom.CreateDefaultPlan()` defines the commercial SBOM requirement.

The default plan targets SPDX JSON output at `artifacts/sbom/sigtran.net.spdx.json` and records `Microsoft.Sbom.Tool` as the intended generation tool.

## Package Signing Plan

`SigtranPackageSigning.CreateDefaultPlan()` defines commercial package signing requirements.

The default plan requires author signing and a timestamp authority. Signing material is referenced by certificate subject; private keys and secrets must stay outside the repository.

## Provenance

`SigtranReleaseProvenanceFactory.Create()` records the source repository, commit SHA, release workflow, and artifact manifest path for a release.

Every governed release should preserve this provenance beside package artifacts and checksums.

## Release Notes

`SigtranReleaseNotesFactory.CreateAlpha()` creates structured alpha release notes with SemVer validation and required change entries.

Release notes are publishable only when the version is SemVer-compatible and at least one notable change is recorded.

## Publish Channels

`SigtranPublishChannels.GetChannels()` defines internal, alpha, beta, and stable publication rules.

Prerelease versions are allowed on internal, alpha, and beta channels. Stable publication rejects prerelease versions and requires commercial readiness.

## Release Gate

`SigtranReleaseGate.Evaluate()` checks channel version rules, package artifacts, artifact digests, release notes, provenance, and commercial readiness.

Alpha and beta releases can publish before commercial readiness. Stable releases require commercial readiness and a stable version.

## Release CI Profile

`SigtranReleaseCiProfiles.CreateDefault()` declares a release workflow profile with manual dispatch and version-tag triggers.

Required secret names are recorded as metadata only: `NUGET_API_KEY` and `SIGNING_CERTIFICATE`. Secret values must never be committed.

## Phase Status

`SigtranPhase10Status.Describe()` summarizes the completed Phase 10 units.

The phase foundation can be ready while stable commercial publication remains blocked by wider commercial gates such as real signing, SBOM generation, native SCTP lab evidence, and external interoperability evidence.
