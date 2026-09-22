# NuGet RC.2 Trusted Publishing Evidence

- Package: `Sigtran.NET 1.0.0-rc.2`
- GitHub tag: `v1.0.0-rc.2`
- Approved source commit: `e2c663460823cd29f073467a79c4f761fb7c1002`
- Trusted Publishing workflow: `.github/workflows/publish-rc2-oidc.yml`
- Workflow run: `35690123501`
- GitHub environment: `nuget-prerelease`
- Authentication: NuGet Trusted Publishing / GitHub OIDC
- Build: PASS
- Tests: PASS
- Pack: PASS
- OIDC login: PASS
- NuGet push: PASS
- NuGet public visibility: PASS
- Fresh .NET 10 restore: PASS
- GitHub prerelease evidence upload: PASS

The previous long-lived `NUGET_API_KEY` publication path returned HTTP 403 and is superseded by Trusted Publishing/OIDC for this RC.

Canonical public release:

https://github.com/araditc/sigtran.net/releases/tag/v1.0.0-rc.2
