# Phase 55 - Stable Release Execution

Phase 55 turns the stable release foundation into executable controls and
records the real release decision.

## Delivered Units

1. Package defaults changed from accidental stable `1.0.0` to source version
   `1.0.0-rc.2`.
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

## Phase 56 M2PA Gate Closure

Phase 56 run `35695182775` passed RFC 4165 interoperability between
Sigtran.NET `M2paLink` and an independently compiled C/lksctp peer. The
retained evidence contains a 39-packet native SCTP PCAP, independent peer
events, SDK state/metric results, SDK trace, report, and relative SHA-256
manifest. The run validates PPID 5, streams 0/1, alignment/proving/Ready,
24-bit BSN/FSN acknowledgements, bidirectional User Data, Busy/BusyEnded, and
processor outage/recovery. The `independent-m2pa` gate is now closed.

## Current Decision

The current decision is `NO-GO`. Passing repository evidence covers native
Linux SCTP, independent M3UA, independent M2PA, repository-profile full-stack MAP SMS traffic,
runtime operations, and the public API baseline.

Open required gates are:

- operator/vendor profile acceptance;
- 20K TPS target;
- representative multi-host soak/failover;
- representative Kubernetes SCTP deployment;
- organization-trusted stable signing identity.

The protected publication gate is configured and retained at
`docs/evidence/PHASE55_GITHUB_PROTECTION_20260724T102354Z.json`. Main requires
strict `build-test-pack`, one PR approval, stale-review dismissal, conversation
resolution, linear history, and blocks force-push/deletion. `nuget-stable`
requires a reviewer and protected branch. Administrators can bypass repository
rules, so the machine evidence manifest and exact workflow confirmation remain
mandatory independent controls. Protection does not make protocol and capacity
gates pass.

## Publication Result

No stable tag or NuGet package is created while the decision is `NO-GO`.
`1.0.0-rc.2` is the latest public package and is verified through NuGet Trusted
Publishing/OIDC and a clean .NET 10 restore. Source packaging remains on the RC
line so an ordinary `dotnet pack` cannot accidentally produce a stable package.
