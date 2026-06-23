# Phase 42 - Commercial Package Publication Gate Integration

Phase 42 connects an approved commercial evidence publication handoff to the package publication gate. It does not publish packages by itself. The phase prepares request, package artifact, credential, evidence, release guard, channel policy, gate execution, dry-run, command, and status contracts so a release workflow can decide whether publication is allowed from retained evidence.

## Unit Plan

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | Package publication request derived from approved handoff gate | Complete |
| 2 | Package artifact binding with package and symbols digests | Complete |
| 3 | Credential readiness bridge for required publication secrets | Complete |
| 4 | Publication evidence assembly from approved run and artifacts | Pending |
| 5 | Release publish guard bridge | Pending |
| 6 | Publication channel policy evaluation bridge | Pending |
| 7 | Commercial package publication gate execution | Pending |
| 8 | Dry-run publication rehearsal artifact | Pending |
| 9 | Guarded publish command materialization and status | Pending |
| 10 | Final documentation, README alignment, validation, commit, and push | Pending |

## Current Capability

`SigtranPackagePublicationRequest` turns a `SigtranCommercialEvidencePublicationHandoffGateResult` into a package publication request. The request preserves the package version, publication channel, requester identity, commercial evidence run id, approved promotion package id, and UTC request time.

The request can only move to package artifact binding when the upstream handoff gate allows evaluation and the request time is normalized to UTC. This keeps package publication blocked when approval handoff evidence is incomplete.

`SigtranPackagePublicationArtifactSet` binds the request to the retained NuGet package and symbols package. It requires unique artifact kinds, package and symbols coverage, non-empty retained sizes, SHA-256 hex digests, and paths that match the requested package version. The artifact set can also project into the existing `SigtranPackageIntegrityManifest` contract used by the package publication gate.

`SigtranPackagePublicationCredentialReadiness` evaluates required publication credentials by secret name only. It uses the default NuGet publication credential policy, reports missing secret names, tracks NuGet API key and signing secret availability, and requires a UTC evaluation time before moving into evidence assembly.

## Commercial Gate Position

Phase 42 is still in progress. Units 1 through 3 establish the handoff-to-publication request boundary, digest-covered package artifact binding, and secret-name based credential readiness. Real package publication remains blocked until retained evidence, release guard, channel policy, and the final publication gate all pass.
