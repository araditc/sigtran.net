# Phase 55 - Stable Release Execution

Phase 55 turns the stable release foundation into executable controls and
records the real release decision.

## Delivered Units

1. Package defaults were moved away from accidental stable `1.0.0` packaging to
   the prerelease line before the stable gate was introduced.
2. Repository governance and evidence orchestration types removed from the
   exported SDK surface.
3. Reflection-based RC.1 and stable-candidate public API baselines retained.
4. Machine-enforced API comparison added to the release workflow.
5. Versioned stable evidence manifest and `GO/NO-GO` evaluator added.
6. SBOM tool and GitHub Actions pinned; package/SBOM attestations retained.
7. Stable certificate trust, fingerprint, validity, and chain validation added.
8. Stable publication bound to an existing matching tag, exact confirmation,
   protected GitHub environment, and NuGet secret.
9. Public NuGet visibility/clean restore verification and GitHub release
   creation added after a successful stable push.
10. Stable assessment execution and final release decision retained.

The public `1.0.0-rc.2` package and tag are immutable release evidence. Source
changes after that tagged commit must use a later unpublished prerelease identity
until another governed release candidate is explicitly admitted.

## Stable Gate

`eng/release/stable-release.json` is the source-controlled evidence manifest.
`eng/evaluate-stable-release.ps1` verifies:

- stable SemVer and requested version agreement;
- existence and SHA-256 digest of every declared evidence file;
- every required gate's declared outcome;
- presence and digest of the public API baseline.

The workflow can run a stable assessment with `publish=false`. A stable
publication additionally requires:

- decision `GO`;
- exact confirmation `publish Sigtran.NET 1.0.0`;
- existing tag `v1.0.0` pointing at the workflow commit;
- approval in the `nuget-stable` GitHub environment;
- trusted non-self-issued certificate matching the protected fingerprint;
- timestamped signature verification;
- `NUGET_API_KEY`.

## Executed Assessment

GitHub Actions run
[`30088170594`](https://github.com/araditc/sigtran.net/actions/runs/30088170594)
completed successfully against commit
`6490c1069df8dc5a867380ec94b3e280d6582fc8` with publication disabled. It
generated the candidate package, final SPDX 2.2 SBOM, exact API comparison,
digest manifest, SLSA provenance attestation, SBOM attestation, and all five
release artifact bundles.

Independent download verification confirmed all 17 digest-manifest entries.
Both hosted attestations verified against the repository, release workflow,
source commit, GitHub-hosted runner identity, OIDC issuer, and Rekor timestamp.
See the
[retained stable assessment summary](evidence/PHASE55_STABLE_ASSESSMENT_20260724T110519Z.json).

## Phase 56 Gate Closures

Phase 56 run `35695182775` passed RFC 4165 interoperability between
Sigtran.NET `M2paLink` and an independently compiled C/lksctp peer. The
retained evidence contains a 39-packet native SCTP PCAP, independent peer
events, SDK state/metric results, SDK trace, report, and relative SHA-256
manifest. The run validates PPID 5, streams 0/1, alignment/proving/Ready,
24-bit BSN/FSN acknowledgements, bidirectional User Data, Busy/BusyEnded, and
processor outage/recovery. The `independent-m2pa` gate is closed.

Phase 56 run `35696403076` also closed the numeric `capacity-target` gate with a
controlled single-host, two-association native-SCTP/M3UA load-share topology.
Aggregate sustained throughput was approximately 35.9K TPS, peak throughput
approximately 34.5K TPS, and soak throughput approximately 45.8K TPS with zero
failed operations and passing latency/resource criteria. This evidence does not
close the separate representative multi-host long-duration soak/failover gate.

## Current Decision

The current decision remains `NO-GO`. Passing repository evidence covers native
Linux SCTP, independent M3UA, independent M2PA, repository-profile full-stack
MAP SMS traffic, the numeric capacity target, runtime operations, and the public
API baseline.

Four required gates remain open:

- operator/vendor profile acceptance;
- representative multi-host soak/failover;
- representative Kubernetes SCTP deployment;
- organization-trusted stable signing identity.

The protected publication gate is configured and retained at
`docs/evidence/PHASE55_GITHUB_PROTECTION_20260724T102354Z.json`. Main requires
strict `build-test-pack`, one PR approval, stale-review dismissal, conversation
resolution, linear history, and blocks force-push/deletion. `nuget-stable`
requires a reviewer and protected branch. Administrators can bypass repository
rules, so the machine evidence manifest and exact workflow confirmation remain
mandatory independent controls. Protection does not make protocol, deployment,
or signing gates pass.

## Publication Result

No stable tag or NuGet package is created while the decision is `NO-GO`.
`1.0.0-rc.2` is the latest public package and is verified through NuGet Trusted
Publishing/OIDC and a clean .NET 10 restore. Ordinary development packaging must
remain on a prerelease identity that cannot collide with the immutable public
RC.2 package, while the protected stable workflow supplies the exact stable
version only after every release gate passes.
