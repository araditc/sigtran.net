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

## Current Decision

The current decision is `NO-GO`. Passing repository evidence covers native
Linux SCTP, independent M3UA, repository-profile full-stack MAP SMS traffic,
runtime operations, and the public API baseline.

Open required gates are:

- independent external M2PA interoperability;
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
`1.0.0-rc.1` remains the latest public package. Source packaging defaults to
the next RC candidate so an ordinary `dotnet pack` cannot accidentally produce
a stable package.
