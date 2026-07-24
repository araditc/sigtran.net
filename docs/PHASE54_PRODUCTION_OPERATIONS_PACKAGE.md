# Phase 54 - Production Operations Package

Phase 54 converts earlier operations catalogs into runtime capabilities that an
operator-facing service can consume.

## Delivered Units

1. Executable aggregate health service with cancellation and failure isolation.
2. SCTP association health probe.
3. M3UA runtime and bounded-queue health probe.
4. BCL `ActivitySource` tracing contract.
5. BCL `Meter` instruments compatible with OpenTelemetry providers.
6. Structured event contract and thread-safe JSON Lines sink.
7. M3UA runtime observer for logs, metrics, and association lifecycle.
8. Environment-style topology configuration validation.
9. Native SCTP/M3UA operations host with live, ready, and metrics endpoints.
10. Docker, Compose, Kubernetes, recovery, pressure, upgrade, and rollback
    deployment material.

## Verification

The SDK test runner verifies:

- aggregate health severity and SCTP association health;
- activity tags and metric emission through BCL listeners;
- parseable JSON Lines structured events;
- valid and invalid node configuration paths.

`scripts/run-operations-host-smoke.sh` compiles the independent C/lksctp peer,
starts the native Linux operations host, and verifies healthy readiness plus an
active M3UA metric. Passing WSL2 run `operations-host-20260724T095723Z`
established native SCTP, activated the M3UA ASP, returned healthy live/ready
responses, and reported zero faults or reconnects. Its result is retained at
`docs/evidence/PHASE54_OPERATIONS_HOST_20260724T095723Z.json`.

The solution build includes the operations host. Release packing continues to
package only `Sigtran.NET`; deployment material remains source-controlled
operator guidance rather than hidden package content.

## Evidence Boundary

This phase proves the implementation and deployment composition locally. It
does not replace:

- a representative Kubernetes SCTP network test;
- an operator/vendor peer acceptance run;
- independent external M2PA evidence;
- the multi-host 20K TPS qualification;
- organization-specific alert thresholds and on-call review.

Those remain release decision inputs, not inferred outcomes of the operations
API.
