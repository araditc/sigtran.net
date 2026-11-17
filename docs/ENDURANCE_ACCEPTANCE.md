# Scoped Endurance Evidence Acceptance

The product owner explicitly accepted the risk of the seven missing managed
readings on 2026-11-17 and authorized six implementation units without a new
72-hour run. **Independent review remains outstanding.** Owner risk acceptance
is not a final acceptance exception, an original verifier pass or stable GO.

## Bound Dossier

`eng/qualification/deviations/endurance-managed-memory.json` pins the rejected
run, measured source, unchanged SDK implementation tree, original raw inventory,
seven exact signed values/timestamps and three supporting report hashes.
`scripts/evidence-deviation.py` audits those public records. It explicitly does
not claim to have replayed private artifacts merely by reading public metadata.
The original signed observations, strict rejection and failed exit remain intact.
An unchanged source tree is not an identical final package attestation.

## Delivery Units

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Immutable evidence dossier and source-tree comparison | Implemented; focused tests pass |
| 2 | Exact-scope owner risk acceptance | Pending |
| 3 | Independent review contract and request | Pending; actual human approval remains open |
| 4 | Separate fail-closed administrative assessment | Pending |
| 5 | Tamper rejection and CI boundaries | Pending |
| 6 | Final report, docs and handoff | Pending |

The strict endurance/release policies are not changed by this work. No VM load,
Kafka control, package publication or stable promotion is authorized here.
