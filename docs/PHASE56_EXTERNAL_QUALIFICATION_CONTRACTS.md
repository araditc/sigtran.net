# Phase 56 External Qualification Execution Contracts

The remaining stable qualification gates that require deployment-specific
infrastructure are executed through explicit evidence contracts. These
contracts do not make a gate pass by configuration alone. A gate is promoted
only after a real run succeeds and the retained evidence is reviewed.

## Operator / Vendor Profile Interoperability

Required environment: `operator-interop`

Required secrets/variables:

- `OPERATOR_REMOTE_IP`
- `OPERATOR_REMOTE_PORT`
- `OPERATOR_LOCAL_POINT_CODE`
- `OPERATOR_REMOTE_POINT_CODE`
- `OPERATOR_NETWORK_INDICATOR`
- `OPERATOR_PEER_NAME`
- `OPERATOR_SUBSCRIBER_MSISDN`
- `OPERATOR_SUBSCRIBER_IMSI`
- `OPERATOR_SERVICE_CENTRE`

The execution must retain:

- exact non-secret profile identity/version;
- SDK trace;
- peer/vendor trace or log where available;
- PCAP;
- operation outcome summary;
- field comparison;
- SHA-256 manifest;
- explicit reviewer acceptance.

Required MAP SMS operations are SRI-SM, MO-ForwardSM, MT-ForwardSM,
ReportSM-DeliveryStatus, and AlertServiceCentre unless the operator profile
explicitly excludes an operation. Any exclusion must be documented and reviewed.

## Multi-Host Capacity / Soak

Required environment: `sigtran-performance`

The SDK workload runner and peer must be on distinct hosts or VMs. Loopback or
same-host network namespaces do not satisfy this gate.

The retained topology must identify:

- host/VM identity;
- vCPU and memory limits;
- OS and kernel;
- NIC/network path;
- SCTP settings;
- peer implementation and version;
- test start/end UTC;
- sustained, peak, recovery, and soak durations.

The multi-host gate requires sustained traffic, failover/recovery, zero lost
recovery operations, and a long-duration soak. The separate 20K TPS gate may
be closed by controlled native-SCTP qualification, but this gate remains open
until representative multi-host evidence exists.

## Kubernetes SCTP/CNI

Required environment: `kubernetes-sctp`

Required evidence:

- Kubernetes version;
- CNI plugin and version;
- worker kernel and SCTP module state;
- deployment/service/network policy manifests;
- live/readiness behavior;
- SCTP association establishment;
- traffic trace;
- pod termination and graceful shutdown;
- restart/recovery;
- rollout/rollback;
- node or pod failure behavior;
- retained logs/metrics/PCAP where permitted;
- SHA-256 manifest.

A local manifest build or unit test is not sufficient. The run must use a real
Kubernetes cluster with SCTP-capable networking.

## Promotion Rule

Each successful external run is retained under `docs/evidence/`, reviewed by
a maintainer, and then referenced from `eng/release/stable-release.json`.
The continuous `stable-gate-assessment` workflow is the authoritative check
that the evidence path exists and the gate declaration is consistent.
