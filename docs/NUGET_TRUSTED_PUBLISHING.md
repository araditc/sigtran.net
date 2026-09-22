# NuGet Trusted Publishing for SIGTRAN.NET

SIGTRAN.NET uses NuGet.org Trusted Publishing instead of a long-lived API key for the RC.2 publication path.

## Current status

Configured and verified.

- First successful Trusted Publishing run: `35690123501`.
- OIDC login: PASS.
- NuGet push for `Sigtran.NET 1.0.0-rc.2`: PASS.
- Public package visibility: PASS.
- Fresh .NET 10 restore: PASS.
- GitHub prerelease evidence attachment: PASS.
- A second idempotency run detected the public package, skipped publishing, and re-verified public restore successfully.

## NuGet.org policy

Sign in to NuGet.org and create a Trusted Publishing policy with:

- Repository Owner: `araditc`
- Repository: `sigtran.net`
- Workflow File: `publish-rc2-oidc.yml`
- Environment: `nuget-prerelease`
- Scope: allow publishing new versions of `Sigtran.NET`

Choose the policy owner that owns the `Sigtran.NET` package on NuGet.org. The policy owner may be an individual NuGet.org account or a NuGet.org organization.

## GitHub secret

Set:

- `NUGET_USER` = the NuGet.org profile name used by the policy.

This must be the NuGet.org username/profile name, not an email address.

The workflow does not require `NUGET_API_KEY`. NuGet/login exchanges the GitHub OIDC token for a short-lived NuGet API key immediately before publishing.

## Workflow

Run:

`.github/workflows/publish-rc2-oidc.yml`

The workflow:

1. Verifies `v1.0.0-rc.2` points to the approved source commit.
2. Restores, builds, tests, and packs `1.0.0-rc.2`.
3. Checks whether the version is already public.
4. Uses NuGet Trusted Publishing/OIDC if a push is required.
5. Verifies public package visibility.
6. Performs a clean .NET 10 restore.
7. Uploads retained publication evidence to the GitHub prerelease.

## Canonical identity

- GitHub repository: `araditc/sigtran.net`
- GitHub tag: `v1.0.0-rc.2`
- Approved source commit: `e2c663460823cd29f073467a79c4f761fb7c1002`
- GitHub environment: `nuget-prerelease`
- NuGet package: `Sigtran.NET`

See the official NuGet Trusted Publishing documentation for policy semantics and OIDC flow.
