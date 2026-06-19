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
