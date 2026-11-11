# Qualification Tooling

Run the .NET 10 tool using `dotnet run --project eng/Sigtran.NET.Qualification --`
followed by a command below. Exit codes: 0 success, 1 rejected/NO-GO, 2 invalid
input/tool failure. Repository qualification types are not SDK public APIs.

| Command | Arguments |
| --- | --- |
| baseline | repository-root [protected-inventory.json ...] |
| scope | scope.json |
| defects | defects.json repository-root |
| topology | protected-topology.json |
| inventory | topology.json sdk-inventory.json peer-inventory.json |
| plan | topology.json sdk-inventory.json peer-inventory.json run-id tier fault |
| bundle | bundle.json evidence-root |
| evaluate | policy.json evidence-root [source-commit candidate.nupkg scope.json] |

Inventory requires native SCTP and distinct machine identities for both roles.
The SDK role also requires the SDK build and capture toolchain. A prebuilt peer
only needs endpoint inventory tooling; the protected runner separately checks
the live endpoint, service control and build metadata readable by its SSH user.
See [Linux lab setup](LINUX_LAB_SETUP.md) for the restricted peer service.

The no-package evaluation deliberately returns NO-GO. The stable evaluator
requires the candidate validator as an additional mandatory gate. Existing
historical evidence remains visible; toggling legacy `passed` fields cannot
promote a stable release. Use `eng/test-qualification-gate.ps1` to reproduce
that negative integration test.

Evidence envelopes use schema 2 with scopeId, gateId, runId, sourceCommit,
packageVersion, packageSha256, scopeSha256, completedAtUtc, result and artifacts.
Result contains outcome, positive observations, zero failures and finite
numeric metrics. Each artifact contains kind, relative path and expected SHA-256.
All fifteen mandatory gates must be present. Empty/missing envelopes fail.
The owner-approved capacity revision is `completed-map-10k-v1`: 10K completed
TPS, a 10K minimum one-second window, 1,800 seconds, at least 18M completed
observations, two dedicated hosts and zero unresolved work. Capacity envelopes
also require targetTps=10000, PCAP and comparison artifacts; historical 20K
flags or a shared-host result cannot satisfy the new exact-candidate gate.

Scope SHA-256 covers strict deserialized scope JSON serialized by the tool,
so checkout newline conventions do not change semantic scope identity.

For final release, supply a reviewed immutable evidence index from protected
storage using `-QualificationPolicyPath` and `-QualificationEvidenceRoot` on
the stable evaluator. The index pins the external envelopes; it is not committed
back into the tested source candidate. This avoids a circular dependency where
adding evidence digests changes the very source commit they must qualify.
The repository default index deliberately contains no passing envelopes.
Package SHA-256 covers the actual tested nupkg; the ID/version are read from
its nuspec. Source SHA must be the exact tested candidate. Evidence is valid
for 30 days, with at most five minutes of future clock skew.

The trusted producer identity is fixed to
`araditc/sigtran.net/.github/workflows/qualification-evidence.yml` on main.
Actual producer admission, protected external execution and final candidate
attestation remain release-qualification prerequisites. No passing envelopes
or attestations are fabricated to bootstrap that process.

Private raw evidence remains outside the repository. Do not publish actual
topology, machine IDs, subscriber traffic or credentials. Public reports retain
sanitized counts/status only. Inventory hashes are kept private as well.
