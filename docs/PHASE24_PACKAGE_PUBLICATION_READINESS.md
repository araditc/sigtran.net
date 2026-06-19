# Phase 24 Package Publication Readiness

Phase 24 turns the release workflow foundation into a stricter package publication foundation.

The phase is split into ten small units. Source type names stay domain-based; the phase number is used only in documentation.

## Unit 1 - Version Policy

`SigtranReleaseVersionPolicy` defines the publication version contract:

- Source tags must use the `v` prefix.
- Package versions must use a three-part SemVer core.
- Pre-release and stable publication lanes are both represented.

The policy is intentionally local and deterministic so CI, release workflow inputs, and package governance can reject ambiguous versions before a publish command is allowed.

## Unit 2 - NuGet Metadata Contract

`SigtranNuGetMetadataContract` defines the package metadata required for publication:

- Package identity and title.
- Apache-2.0 license expression.
- Repository and project URLs.
- README inclusion.
- XML documentation and symbol package generation.

The contract validates the project file text so packaging metadata remains testable without contacting NuGet.
