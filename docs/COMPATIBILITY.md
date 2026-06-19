# Compatibility Policy

SIGTRAN.NET currently targets `net10.0`.

## Versioning

The SDK follows Semantic Versioning.

Before the first stable release, breaking API changes are allowed when they are required to align protocol behavior, naming, or packaging with the commercial SDK surface.

After a stable release, breaking public API changes must move through a major version.

## API Documentation

Public APIs must include XML documentation. Missing public XML comments are treated as build errors.

`SigtranCompatibility.CreateCurrentPolicy()` exposes this compatibility posture for release tooling and downstream governance checks.
