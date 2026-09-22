# NuGet RC.2 Publication Blocker Evidence

- Package: `Sigtran.NET 1.0.0-rc.2`
- GitHub tag: `v1.0.0-rc.2`
- Approved source commit: `e2c663460823cd29f073467a79c4f761fb7c1002`
- Publication workflow run: `35686964875`
- Build: PASS
- Tests: PASS
- Pack: PASS
- NuGet push: FAIL
- NuGet HTTP status: `403 Forbidden`

NuGet.org returned:

```text
The specified API key is invalid, has expired, or does not have permission to access the specified package.
```

No package-content, build, test, or versioning failure was observed. The blocker is the credential/scope used by the GitHub Actions `NUGET_API_KEY` secret.

The retry workflow is:

`.github/workflows/publish-rc2.yml`

It is idempotent: if `1.0.0-rc.2` becomes public before a retry, it skips the push and continues with public visibility/restore verification.

The GitHub prerelease itself is complete and currently contains:

- `Sigtran.NET.1.0.0-rc.2.nupkg`
- `Sigtran.NET.1.0.0-rc.2.snupkg`
- `Sigtran.NET.1.0.0-rc.2.spdx.json`
- `Sigtran.NET.1.0.0-rc.2.sha256`

## Resolution

RESOLVED on workflow run `35690123501`.

The project migrated RC.2 publication to NuGet Trusted Publishing / GitHub OIDC. OIDC login, NuGet push, public visibility, and fresh .NET 10 restore all passed.

Successful evidence: `docs/evidence/NUGET_RC2_TRUSTED_PUBLISHING_35690123501.md`.
