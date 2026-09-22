# Phase 56 Trusted Signing Preflight Blocker

- Workflow: `.github/workflows/phase56-trusted-signing-preflight.yml`
- Run: `35710407279`
- Stable candidate build/test/pack: PASS
- Signing certificate secret: PRESENT
- Signing certificate password secret: PRESENT
- Certificate discovery step: FAIL
- Failure classification: SELF-ISSUED CERTIFICATE
- Stable signing gate: OPEN

The preflight intentionally rejects a self-issued certificate before fingerprint
promotion or package signing. This is a policy gate, not a workflow defect.

GitHub Actions reported:

```text
Stable signing certificate must not be self-issued.
```

Required remediation:

1. Obtain an organization-controlled code-signing certificate whose leaf
   certificate is issued by a trusted CA and is suitable for NuGet author
   signing.
2. Replace the protected `SIGNING_CERTIFICATE` and
   `SIGNING_CERTIFICATE_PASSWORD` values in the `nuget-stable` environment.
3. Run the trusted-signing preflight again.
4. After public chain and expiry checks pass, set
   `TRUSTED_SIGNING_CERTIFICATE_SHA256` to the independently reviewed leaf
   certificate SHA-256 fingerprint.
5. Re-run the preflight and retain timestamped package verification evidence.

Do not promote `trusted-signing` in `eng/release/stable-release.json` until
all of the above pass.
