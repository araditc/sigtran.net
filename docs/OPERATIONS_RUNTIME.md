# Runtime Operations

Sigtran.NET exposes operational primitives without requiring a specific
logging, metrics, dependency-injection, or hosting package. Applications can
use the primitives directly or bridge them into their existing platform.

## Health

`SigtranHealthService` evaluates an ordered set of `ISigtranHealthProbe`
instances and returns a `SigtranHealthReport`.

Built-in probes:

- `SctpAssociationHealthProbe` reports an established association as healthy,
  transitional states as degraded, and closed or failed states as unhealthy.
- `M3uaRuntimeHealthProbe` reports ASP lifecycle, association identity, queue
  pressure, faults, and reconnect attempts.
- `DelegateSigtranHealthProbe` adapts application-specific dependencies.

Probe exceptions become unhealthy component results. Caller cancellation is
preserved and is never converted to a health failure.

Liveness should indicate that the process can execute. Readiness should include
the active SIGTRAN runtime and any route or application dependencies required
to accept traffic.

## OpenTelemetry

`SigtranTelemetry` uses only BCL diagnostics:

- instrumentation scope: `Sigtran.NET`
- traces: `System.Diagnostics.ActivitySource`
- metrics: `System.Diagnostics.Metrics.Meter`

An OpenTelemetry application should subscribe to the
`SigtranTelemetry.InstrumentationName` source and meter. The SDK itself does not
force a collector or exporter dependency.

Published instruments:

| Instrument | Type | Unit |
| --- | --- | --- |
| `sigtran.transfer.count` | Counter | transfers |
| `sigtran.fault.count` | Counter | faults |
| `sigtran.reconnect.count` | Counter | attempts |
| `sigtran.operation.duration` | Histogram | milliseconds |
| `sigtran.queue.depth` | Histogram | messages |
| `sigtran.association.active` | Up/down counter | associations |

Tags use bounded values such as protocol, direction, operation, queue, fault
type, and association name. Subscriber identities, IMSIs, MSISDNs, message
payloads, and transaction identifiers must not be metric labels.

## Structured Events

`SigtranEventRecord` provides stable event name, UTC timestamp, severity,
protocol, association, message, and string attributes.
`JsonLineSigtranEventSink` emits one JSON object per line and serializes writes
across runtime callbacks. `M3uaRuntimeObserver` attaches to `M3uaRuntime` and
projects lifecycle, transfer, reconnect, fault, queue, and shutdown events into
the event sink and diagnostics instruments.

Event files can contain network topology and fault details. Apply the evidence
redaction and retention policy before sharing them outside the operating team.

## Configuration

`SigtranNodeConfigurationParser` validates the environment contract and returns
all discovered issues in one result.

Required keys:

```text
SIGTRAN_REMOTE_IP
SIGTRAN_REMOTE_PORT
SIGTRAN_ASP_IDENTIFIER
SIGTRAN_LOCAL_POINT_CODE
SIGTRAN_REMOTE_POINT_CODE
SIGTRAN_ROUTING_CONTEXT
SIGTRAN_NETWORK_INDICATOR
SIGTRAN_SERVICE_INDICATOR
SIGTRAN_QUEUE_CAPACITY
```

The parser validates IP format, port range, 24-bit point-code range, NI/SI
ranges, queue capacity, and distinct local/remote point codes. It does not
accept defaults for topology-sensitive values.

## Operations Host

`src/Sigtran.NET.OperationsHost` is an executable composition sample. It:

- validates configuration before opening a socket;
- opens native Linux SCTP with M3UA PPID metadata;
- starts a long-running reconnecting M3UA ASP;
- emits structured events to standard output;
- serves `/health/live`, `/health/ready`, and `/metrics`;
- performs graceful M3UA and SCTP shutdown.

The host is intentionally an M3UA/MTP3 boundary. Product-specific SCCP, TCAP,
and MAP request handling belongs in the adopter service and can use the same
health, event, and telemetry contracts.

Run the Linux host smoke test with:

```bash
bash scripts/run-operations-host-smoke.sh
```

## Container Deployment

The `deploy` directory contains:

- `Dockerfile` for the operations host;
- `compose.yaml` for a single-node Linux test deployment;
- `kubernetes/deployment.yaml` with startup, liveness, readiness, resource, and
  security settings;
- `kubernetes/configmap.yaml` as a topology template.

The container must run on a Linux host whose kernel supports SCTP. Kubernetes
network policy, CNI, load balancer, and firewall configuration must permit SCTP;
TCP or UDP service exposure does not imply SCTP forwarding.

Replace the example peer address, image version, point codes, routing context,
resource limits, and topology values before deployment.
