# Quality And Contribution Rules

This project is intended to become a commercial-grade open-source SDK. Every change should improve protocol correctness, API clarity, or operational reliability.

## Required Checks

Run these before committing:

```powershell
dotnet build src\sigtran.net.sln
dotnet run --project src\sigtran.net.Tests\sigtran.net.Tests.csproj
dotnet pack src\sigtran.net\sigtran.net.csproj -c Release
```

Alpha package publishing should also follow [Alpha release checklist](ALPHA_RELEASE.md).

## Public API Documentation

All public types and members must have XML documentation comments. The library treats `CS1591` as an error, so undocumented public API changes fail the build.

Good comments should explain protocol meaning, units, optionality, and important constraints. Avoid comments that only repeat the member name.

## Protocol Change Checklist

- Add positive byte-level tests for every new builder.
- Add typed parser tests for every new message model.
- Add at least one malformed input or missing-parameter test for every required parameter rule.
- Verify big-endian encoding and TLV padded length behavior.
- Document whether the feature is production-ready or experimental.
- Add diagnostics coverage when a feature changes packet visibility or operational troubleshooting.
- Add counter or health-signal coverage when a feature changes send/receive behavior.

## API Stability

M3UA APIs are the stabilization focus. SCCP, TCAP, and MAP APIs are experimental and may change until standards-based encoders and decoders are implemented.

## Commit Hygiene

Keep commits scoped to one protocol capability or documentation concern. Include generated packages only if release automation explicitly requires them.
