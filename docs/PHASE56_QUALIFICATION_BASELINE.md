# Phase 56 - Qualification Baseline

Status: all ten implementation units delivered; two distinct Linux guests
have passed baseline inventory and a bounded native SCTP smoke test. This
does not establish external acceptance. Stable decision remains NO-GO.

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Locked roadmap and executable product scope | Complete |
| 2 | Owned defect ledger and acceptance mapping | Complete |
| 3 | Multi-host topology contract | Complete |
| 4 | Read-only Linux host inventory and prerequisites | Complete |
| 5 | Remote peer traffic runner with selectable capture | Complete |
| 6 | Versioned evidence envelopes and artifact integrity | Complete |
| 7 | Fail-closed release gate evaluation | Complete |
| 8 | Provenance, freshness and source/package binding | Complete |
| 9 | CI and stable workflow integration | Complete |
| 10 | Rehearsal, retained inventory and final report | Baseline and two-VM smoke verified; full qualification open |

Repository tooling lives in `eng/Sigtran.NET.Qualification`, outside the SDK
assembly. Run `dotnet run --project eng/Sigtran.NET.Qualification -- scope
eng/qualification/scope.json` to validate the approved scope.

Every unit is verified with `pwsh -File eng/verify-qualification-unit.ps1 -Unit N`.
The tool is not a statement that protocol or deployment qualification has passed.

Unit 2 records seven source-anchored findings in `eng/qualification/defects.json`.
Validate with `qualification defects <ledger> <repository-root>`. A ledger
entry cannot close a defect by changing its state; closure requires evaluated
evidence. Existing HA, M2PA and numeric capacity work is preserved.

Unit 3 validates a protected two-node topology with exactly one SDK and peer.
Use `qualification topology <path>`. The documentation-address example is not
an executed lab. Inventory must additionally prove distinct machine identities;
different labels/IPs alone do not prove separate hosts. Real topology stays in
protected evidence storage, never in the public repository.

Unit 4 adds `scripts/collect-host-inventory.py` (read-only, no installation).
Run it on each host, retain JSON privately, then run `qualification inventory
<topology.json> <sdk-inventory.json> <peer-inventory.json>`. The validator
requires fresh inventories, distinct machine hashes and Linux kernel SCTP.
The SDK role additionally requires lksctp, .NET 10, capture privileges and
the SDK tools. A prebuilt reference peer needs endpoint inventory tooling
without the SDK toolchain. Private inventory is not automatically an
authenticated external qualification result.

Unit 5 reuses `run-multi-host-qualification.sh`; no competing traffic runner
is introduced. `qualification plan <topology> <sdk-inventory> <peer-inventory>
<run-id> <tier> <fault>` emits protected JSON inputs. The Python plan launcher
passes only whitelisted environment inputs without shell interpolation.
Existing protected SSH/source/raw-evidence requirements still apply. The new
`endurance` tier adds the approved 72-hour target; historical `release` remains
24 hours. Plan-only output never counts as executed traffic evidence.

Unit 6 introduces schema-2 evidence envelopes outside the SDK API. Every
artifact has an expected SHA-256, kind and root-relative path; traversal,
absolute paths, links, empty inventories and changed bytes fail validation.
`qualification bundle <bundle.json> <evidence-root>` checks integrity only.
It does not establish independent provenance or declare a release GO.

Unit 7 evaluates all 15 mandatory product gates with pinned envelopes,
non-empty observations, zero failures, required artifact kinds and fixed
numeric acceptance criteria. Missing/duplicate gates, a successful-looking
empty run, short single-host capacity and non-finite metrics fail closed.
The candidate policy intentionally starts NO-GO with no converted historical
passes. Existing schema-1 evidence stays retained with its original scope;
it is not relabeled as exact-candidate qualification.

Unit 8 reads candidate identity/version from the actual nupkg, binds source
commit, package SHA-256 and semantic scope digest, and rejects evidence older
than 30 days or future timestamps. A structurally successful envelope additionally
requires `gh attestation verify` against the fixed repository, candidate source
SHA, main ref and protected `qualification-evidence.yml` producer identity.
No retained `verified=true` flag is trusted. Missing verifier/network/provenance
fails closed. Producer admission and genuine external results are later-phase
requirements; the synthetic test verifier exists only in the test assembly.

Unit 9 makes candidate qualification mandatory in the stable evaluator and CI.
Four historical blockers remain; the aggregate candidate gate is a fifth
release blocker. A negative integration test sets all legacy flags to passed
and proves `-RequireGo` still rejects publication. See
[Qualification Tooling](QUALIFICATION_TOOLING.md) for commands and schema.

## Observed Environment And Exit Decision

The read-only collector ran successfully on the previously authorized Linux VM
on 2026-11-04. Kernel SCTP, lksctp, .NET 10 and the required binaries were
observed. Non-root dumpcap capture is now configured and verified with generated
SCTP traffic. Passwordless sudo remains unavailable and is not required for
capture; fault-control permissions are handled separately. The historical
audit retained only one independent Linux machine inventory.

On 2026-11-10 the fresh SDK and peer inventories passed topology validation.
The baseline audit reports `toolingValid=true`, `labReady=true` and
`stableDecision=NO-GO`. A separate [native SCTP smoke test](evidence/SEPARATE_HOST_LAB_SMOKE_20261110.json)
passed on both sides of a scoped peer-service restart. No in-flight fault,
endurance, external operator or exact-candidate qualification was exercised.

The final rehearsal uses synthetic evidence in temporary directories, with a
test-only verifier. It proves all-gate evaluation, provenance rejection and
tamper rejection; it never creates retained passing production envelopes.
Legacy M2PA/capacity evidence remains historical. The four legacy blockers and
new aggregate candidate qualification gate remain open.
