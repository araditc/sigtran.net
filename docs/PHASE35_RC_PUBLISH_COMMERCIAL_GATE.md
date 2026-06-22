# Phase 35 RC Publish And Commercial Gate

Phase 35 adds the release-candidate publication and final commercial gate foundation for Sigtran.NET. It covers dry-run release rehearsal, gated NuGet prerelease publication, final commercial readiness reporting, release notes, migration notes, and the RC-versus-stable decision.

The public APIs use domain names such as `SigtranReleaseDryRuns`; phase numbers are intentionally kept out of source type names.

## Unit 1 - Dry-Run Release

`SigtranReleaseDryRuns` defines the dry-run release rehearsal plan:

- Pack the SDK with the release candidate version.
- Verify the generated NuGet package.
- Evaluate the dry-run release gate.
- Retain dry-run evidence under a versioned artifact root.
- Avoid every NuGet upload path.

The dry-run plan is release-rehearsal ready only when it includes package creation, package verification, retained evidence, and no `nuget push` command.

## Unit 2 - Gated NuGet Prerelease

`SigtranPrereleasePublicationGate` defines the NuGet prerelease publication gate:

- The package version must be a prerelease version such as `1.0.0-rc.1`.
- Publication must be explicitly requested.
- `NUGET_API_KEY` must be available.
- The dry-run release must have passed.
- The supply-chain release execution foundation must be ready.

Stable versions are intentionally rejected by this gate. Stable publication remains controlled by the commercial release gate.

## Unit 3 - Release Notes Artifact

`SigtranReleaseNotesArtifacts` defines retained RC release notes:

- Versioned Markdown artifact path.
- SHA-256 digest.
- Publishable release notes content.
- Migration notes link.
- Rendered Markdown sections for changes and breaking changes.

The artifact is review-ready only when release notes are publishable, the retained file is Markdown, the path contains the release version, a digest is present, and migration notes are linked.

## Unit 4 - Migration Notes Artifact

`SigtranMigrationNotesArtifacts` defines retained RC migration notes:

- Source and target release versions.
- Versioned Markdown artifact path.
- SHA-256 digest.
- Migration guide entries.
- Code sample requirement.
- Experimental boundary statement for SCCP, TCAP, and MAP.

The artifact is review-ready only when the notes are versioned, digest-covered, Markdown-based, code-sample aware, and explicit about experimental protocol surfaces.

## Unit 5 - Final Commercial Readiness Report

`SigtranFinalCommercialReadinessReports` aggregates the RC and stable gates into one retained commercial readiness report:

- Dry-run release rehearsal readiness.
- Gated NuGet prerelease publication readiness.
- Release notes and migration notes artifact readiness.
- Supply-chain release execution foundation readiness.
- Stable commercial release readiness.
- Retained commercial blockers such as external peer interop, package signing verification, and production performance evidence.

The report intentionally separates `ReleaseCandidateReady` from `StableReleaseReady`. A release candidate can pass the prerelease gate while stable publication remains blocked until commercial evidence is complete.

## Unit 6 - RC Versus Stable Decision

`SigtranReleaseDecisions` evaluates the final readiness report and returns one of three outcomes:

- `Blocked` when the RC gate itself is incomplete.
- `ReleaseCandidate` when prerelease publication is allowed but stable commercial evidence is still incomplete.
- `Stable` when both RC and stable commercial gates are ready.

The default current decision is `ReleaseCandidate` when the NuGet prerelease secret is available, because the dry-run, prerelease, release notes, migration notes, and supply-chain foundations are present while commercial blockers remain retained. This prevents accidental stable publication.

## Unit 7 - RC Publication Evidence Manifest

`SigtranReleaseCandidatePublicationEvidence` defines the retained evidence manifest for RC upload review:

- Package and symbol package artifacts.
- Dry-run release evidence.
- Release notes and migration notes artifacts.
- Final commercial readiness report artifact.
- Release decision record.
- Digest manifest.

The manifest allows RC publication only when the release decision is `ReleaseCandidate`, every required artifact kind is present, and every required artifact has SHA-256 digest coverage. It does not allow stable publication unless the decision is `Stable`.

## Unit 8 - Release Workflow Dry-Run And Prerelease Wiring

`.github/workflows/release.yml` now exposes an explicit release `channel`:

- `dry-run` builds, tests, packs, verifies, and uploads retained dry-run evidence without allowing NuGet publication.
- `prerelease` requires a prerelease version, explicit `publish=true`, a NuGet API key, and retained dry-run evidence before upload.
- `stable` keeps the commercial evidence gate in the publication path.

The workflow uploads package, symbols, supply-chain evidence, and dry-run evidence artifacts with audit-friendly retention. The validator requires the dry-run evidence step, RC publication gate, dry-run evidence upload, and dry-run publish block.

## Unit 9 - RC Publication Status Summary

`SigtranReleaseCandidatePublicationStatus` summarizes the RC publication gate:

- Completed RC gate capabilities.
- Default blockers that remain for real publication and stable promotion.
- RC gate foundation readiness.
- Real publication readiness.
- Stable commercial publication readiness.

The current status marks the RC gate foundation as ready, but keeps real publication blocked until a real release workflow run produces retained artifacts and the NuGet prerelease secret is available at publish time. Stable publication remains blocked by commercial evidence requirements.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
