# Phase 42 - Production Package Publication Gate Integration

Phase 42 connects an approved commercial evidence publication handoff to the package publication gate. It does not publish packages by itself. The phase prepares request, package artifact, credential, evidence, release guard, channel policy, gate execution, dry-run, command, and status contracts so a release workflow can decide whether publication is allowed from retained evidence.

The first public RC package was later published by protected workflow run `28290586511`. This keeps the Phase 42 publication contracts useful as stable-release governance while the stable channel remains gated.

## Unit Plan

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | Package publication request derived from approved handoff gate | Complete |
| 2 | Package artifact binding with package and symbols digests | Complete |
| 3 | Credential readiness bridge for required publication secrets | Complete |
| 4 | Publication evidence assembly from approved run and artifacts | Complete |
| 5 | Release publish guard bridge | Complete |
| 6 | Publication channel policy evaluation bridge | Complete |
| 7 | Production package publication gate execution | Complete |
| 8 | Dry-run publication rehearsal artifact | Complete |
| 9 | Guarded publish command materialization | Complete |
| 10 | Status reporting, final documentation, README alignment, validation, commit, and push | Complete |

## Current Capability

`SigtranPackagePublicationRequest` turns a `SigtranReleaseEvidencePublicationHandoffGateResult` into a package publication request. The request preserves the package version, publication channel, requester identity, commercial evidence run id, approved promotion package id, and UTC request time.

The request can only move to package artifact binding when the upstream handoff gate allows evaluation and the request time is normalized to UTC. This keeps package publication blocked when approval handoff evidence is incomplete.

`SigtranPackagePublicationArtifactSet` binds the request to the retained NuGet package and symbols package. It requires unique artifact kinds, package and symbols coverage, non-empty retained sizes, SHA-256 hex digests, and paths that match the requested package version. The artifact set can also project into the existing `SigtranPackageIntegrityManifest` contract used by the package publication gate.

`SigtranPackagePublicationCredentialReadiness` evaluates required publication credentials by secret name only. It uses the default NuGet publication credential policy, reports missing secret names, tracks NuGet API key and signing secret availability, and requires a UTC evaluation time before moving into evidence assembly.

`SigtranPackagePublicationEvidenceAssembly` creates the publication gate evidence manifest from credential readiness, package integrity, supply-chain promotion readiness, and approved commercial evidence readiness. It reuses the existing `SigtranPublicationEvidenceManifest` contract so the final package publication gate receives the same evidence shape as earlier release foundations.

`SigtranPackagePublicationPublishGuardEvaluation` connects assembled publication evidence to the release publish guard. It retains the evaluated release context, blocks non-manual or untagged publication attempts, and only moves forward when publish intent and NuGet API key availability are present.

`SigtranPackagePublicationChannelPolicyEvaluation` connects guarded package publication evidence to channel policy. It preserves the requested channel and package version, allows prerelease channels to proceed without stable commercial approval, and blocks stable publication until stable channel commercial readiness is approved.

`SigtranPackagePublicationGateExecution` executes the final package publication gate by reusing `SigtranPublicationGate`. It aggregates publish guard, channel policy, credential policy, available secret names, publication evidence, NuGet metadata readiness, and package layout readiness into one blocker-aware result.

`SigtranPackagePublicationDryRunRehearsal` writes a retained Markdown dry-run report. It records the package version, channel, run id, gate decision, UTC rehearsal time, and dry-run commands from `SigtranNuGetPublishPlans.CreateDryRun()`. The rehearsal remains safe because the plan cannot contain a NuGet push command or require an API key.

`SigtranPackagePublicationCommandPlan` and `SigtranPackagePublicationCommandMaterialization` render the guarded publication script. The script requires `SIGTRAN_PUBLICATION_GATE_ALLOWED=true`, retains the dry-run report check, repacks and verifies the package, and uses `${NUGET_API_KEY:?missing NuGet API key}` for upload without storing secret values.

`SigtranPackagePublicationIntegrationStatus` reports the ten completed integration capabilities and keeps publication readiness separate from foundation readiness. The default stable status still blocks stable publication until retained stable release evidence exists and an approved protected stable publication run is executed.

## Production Gate Position

Phase 42 is foundation-complete. It establishes the handoff-to-publication request boundary, digest-covered package artifact binding, secret-name based credential readiness, publication evidence assembly, release publish guard evaluation, channel policy evaluation, final package publication gate execution, retained dry-run rehearsal, guarded publish command materialization, and status reporting. RC package publication is complete for `1.0.0-rc.1`; stable package publication remains blocked until retained stable release evidence exists and a protected approved stable publication run is executed.
