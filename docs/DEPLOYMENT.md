# Deployment Profiles

SIGTRAN.NET separates local development from commercial production deployment.

## Commercial Linux

`SigtranDeploymentProfiles.CreateCommercialLinuxProfile()` describes the recommended commercial target:

- Linux platform
- Native SCTP required
- External interoperability evidence required
- Observability required
- Security policy required

## Local Development

`SigtranDeploymentProfiles.CreateLocalDevelopmentProfile()` describes a development-only profile:

- Windows workstation profile
- Native SCTP not required
- External evidence not required
- Security policy still required

The local profile is useful for SDK tests, examples, and TCP adapter workflows. It must not be used as a commercial production claim.
