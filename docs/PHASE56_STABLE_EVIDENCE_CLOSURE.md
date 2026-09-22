# Phase 56 - Stable Evidence Closure

Phase 56 closes the required evidence gates that still block the stable
`Sigtran.NET 1.0.0` release. It does not lower or bypass the Phase 55 release
policy. A gate is promoted only when retained evidence exists and the
source-controlled stable manifest declares the gate passed.

## Entry State

The Phase 55 machine decision entered Phase 56 as `NO-GO` with six required
open gates:

1. independent external M2PA interoperability;
2. operator/vendor profile acceptance;
3. 20K TPS capacity target;
4. representative multi-host soak/failover;
5. representative Kubernetes SCTP deployment;
6. organization-trusted stable package signing identity.

## Continuous Stable Gate Assessment

`.github/workflows/stable-gate-assessment.yml` evaluates
`eng/release/stable-release.json` after stable-manifest or retained-evidence
changes. The workflow publishes the generated JSON and Markdown decision as a
retained GitHub Actions artifact and job summary.

The evaluator remains authoritative:

`eng/evaluate-stable-release.ps1`

Stable publication is still prohibited while the decision is `NO-GO`.

## Independent M2PA Gate

Status: **CLOSED**

### Execution

- Workflow: `.github/workflows/phase56-m2pa-interop.yml`
- Passing run: `35695182775`
- SDK side: `Sigtran.NET.M2paInteropLab` using public `M2paLink`
- Peer side: independently compiled C/lksctp reference peer
- Peer source: `tools/interop-peer/m2pa_reference_peer.c`
- Runner: GitHub-hosted Ubuntu with native Linux SCTP
- Standard: RFC 4165
- SCTP PPID: `5`
- Link-status stream: `0`
- User-data stream: `1`

### Retained evidence

`docs/evidence/PHASE56_M2PA_35695182775/`

The retained bundle includes:

- native SCTP PCAP;
- independent peer event log;
- SDK trace;
- SDK result/metrics;
- TShark SCTP field extract;
- human-readable report;
- run summary;
- relative SHA-256 manifest.

### Passing observations

- SDK exit code: `0`
- Peer exit code: `0`
- SDK validation: `true`
- Peer validation: `true`
- SCTP packets captured: `39`
- final M2PA state: `InService`
- sent User Data: `2`
- received User Data: `2`
- acknowledged User Data: `2`
- out-of-order discards: `0`
- retrieval depth: `0`

The traffic exercises:

- Out-of-Service / Alignment / Proving / Ready;
- ordered SCTP delivery with PPID 5;
- 24-bit BSN/FSN sequencing;
- acknowledgement-only User Data;
- bidirectional User Data;
- Busy / BusyEnded;
- ProcessorOutage / ProcessorRecovered / Ready recovery.

The independent peer is implemented in C/lksctp and does not link to or reuse
Sigtran.NET protocol code. The `independent-m2pa` gate is therefore promoted
to passed in `eng/release/stable-release.json`.

## Capacity Target Gate

Status: **CLOSED**

### Execution

- Workflow: `.github/workflows/phase56-loadshare-capacity.yml`
- Passing run: `35696403076`
- Topology: controlled single-host two-association native-SCTP/M3UA load-share
- Aggregate sustained throughput: approximately `35.9K TPS`
- Aggregate peak throughput: approximately `34.5K TPS`
- Aggregate soak throughput: approximately `45.8K TPS`
- Failed operations: `0`
- Aggregate latency/resource checks: PASS

### Retained evidence

`docs/evidence/PHASE56_CAPACITY_35696403076/`

This closes only the numeric `capacity-target` gate. It does **not** close the
separate representative multi-host long-duration soak/failover gate.

## Remaining Required Gates

Four required stable gates remain open:

1. **operator-profile** — retained operator or vendor SCCP/TCAP/MAP SMS
   acceptance evidence.
2. **multi-host-soak** — representative multi-host long-duration soak/failover.
3. **kubernetes-sctp** — representative Kubernetes SCTP/CNI deployment,
   termination, readiness, and rollback evidence.
4. **trusted-signing** — organization-approved stable signing identity and
   trusted timestamped package verification.

## Exit Criteria

Phase 56 exits only when every required gate in
`eng/release/stable-release.json` is passing with retained evidence and the
machine evaluator returns `GO`. At that point Phase 55 protected stable
publication may be executed; before that point, no stable `v1.0.0` tag or
stable NuGet publication is authorized.
