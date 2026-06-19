# Phase 15 API Stability Deprecation And Migration Readiness

Phase 15 adds API lifecycle readiness for SIGTRAN.NET.

This phase makes the public SDK surface easier to consume in large projects by documenting stability levels, version-line behavior, deprecation rules, migration expectations, breaking-change review, and public API baseline governance.

## API Surface Catalog

`SigtranApiSurfaceCatalog.GetSurfaces()` exposes the public API surfaces that adopters should track:

- M3UA
- SCTP
- SCCP
- TCAP
- MAP
- CoreUtilities

Each surface includes a category and namespace prefix.

## Stability Contracts

`SigtranApiStability.GetContracts()` assigns a stability level to each public surface.

M3UA and SCTP are currently preview surfaces. SCCP, TCAP, and MAP remain experimental until their external interoperability evidence and profile validation are complete.

## Version Matrix

`SigtranApiVersionMatrix.GetEntries()` defines release-line behavior:

- `0.x` is pre-stable and can accept breaking changes.
- `1.x` is planned stable and should reject breaking changes unless a major version is used.

## Deprecation Policy

`SigtranDeprecationPolicies.CreateStableDefault()` defines stable API deprecation expectations:

- At least 180 days of notice.
- `ObsoleteAttribute` on deprecated APIs.
- Migration guide coverage.
- Release-note coverage.

## Migration Guides

`SigtranMigrationGuides.GetEntries()` identifies planned migration guides for the move from pre-stable APIs toward 1.0.

Migration guides must include code samples so users can update applications without reverse-engineering API changes.

## Breaking-Change Review

`SigtranBreakingChangeReview.CreateDefault()` requires API baseline diffs, migration guides, maintainer approval, and major-version handling after stable release.

## Public API Baseline

`SigtranPublicApiBaseline.CreateCurrent()` defines the current pre-stable public API baseline manifest.

The baseline covers known public surfaces and requires diff review before API-shaping changes are accepted.

## API Lifecycle Readiness

`SigtranApiLifecycleReadiness.GetReport()` separates API lifecycle foundation readiness from stable API lifecycle claims.

The API lifecycle foundation is ready when surface catalog, stability contracts, version matrix, deprecation policy, migration guide catalog, breaking-change review, and public API baseline are all present. Stable lifecycle claims still require wider commercial readiness.

## API Lifecycle CI

`SigtranApiLifecycleCi.CreateDefault()` reuses the official build, test, and pack commands while requiring API lifecycle readiness and public API diff review.

## Phase Status

`SigtranApiLifecycleStatus.Describe()` summarizes the completed Phase 15 units and separates API lifecycle foundation readiness from stable API lifecycle claims.
