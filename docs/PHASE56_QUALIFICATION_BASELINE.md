# Phase 56 - Qualification Baseline

Status: in progress. Stable decision remains NO-GO.

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Locked roadmap and executable product scope | Complete |
| 2 | Owned defect ledger and acceptance mapping | Complete |
| 3 | Multi-host topology contract | Pending |
| 4 | Read-only Linux host inventory and prerequisites | Pending |
| 5 | Remote peer traffic runner with selectable capture | Pending |
| 6 | Versioned evidence envelopes and artifact integrity | Pending |
| 7 | Fail-closed release gate evaluation | Pending |
| 8 | Provenance, freshness and source/package binding | Pending |
| 9 | CI and stable workflow integration | Pending |
| 10 | Rehearsal, retained inventory and final report | Pending |

Repository tooling lives in `eng/Sigtran.NET.Qualification`, outside the SDK
assembly. Run `dotnet run --project eng/Sigtran.NET.Qualification -- scope
eng/qualification/scope.json` to validate the approved scope.

Every unit is verified with `pwsh -File eng/verify-qualification-unit.ps1 -Unit N`.
The tool is not a statement that protocol or deployment qualification has passed.

Unit 2 records seven source-anchored findings in `eng/qualification/defects.json`.
Validate with `qualification defects <ledger> <repository-root>`. A ledger
entry cannot close a defect by changing its state; closure requires evaluated
evidence. Existing HA, M2PA and numeric capacity work is preserved.
