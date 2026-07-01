# API Naming Policy

SIGTRAN.NET public API names must describe telecom protocol concepts, transport behavior, diagnostics, or release mechanics without leaking roadmap or phase wording into the SDK surface.

Use stable domain terms such as:

- `Production` for runtime deployment readiness.
- `ReferencePeer` for package-neutral interoperability peers.
- `Prerelease` for NuGet RC or preview publication flows.
- `StableRelease` for final release gates.
- `ReadinessSnapshot` for point-in-time capability state.

Avoid public type, member, and parameter names containing:

- `Commercial`
- `StableCommercial`
- `Maintained`
- `ReleaseCandidate`
- `ReadinessReport`
- Phase-numbered names such as `Phase17`

Historical planning documents can still describe business goals in prose, but SDK identifiers must remain neutral, protocol-oriented, and reusable outside this repository's release plan.

The test suite includes a public API naming guard that scans exported types, public members, and public parameters for these banned planning terms.
