# Observability

Production SIGTRAN deployments need repeatable metrics, traces, structured
events, and health signals.

`SigtranTelemetry` is the executable instrumentation surface. It publishes BCL
`ActivitySource` and `Meter` instruments under the `Sigtran.NET`
instrumentation scope, so OpenTelemetry providers can subscribe without the SDK
forcing an exporter dependency.

`SigtranObservability.CreateDefaultProfile()` remains the static signal catalog
used by existing tooling.

## Metrics

- `sigtran.m3ua.messages.sent`
- `sigtran.m3ua.messages.received`
- `sigtran.sctp.association.state`
- `sigtran.interop.vector.failures`

## Trace Categories

- `sigtran.trace.packet`
- `sigtran.trace.asp-state`
- `sigtran.trace.routing`
- `sigtran.trace.interop`

## Health Signals

- `transport-associated`
- `asp-active`
- `routes-installed`
- `interop-evidence-present`

The profile is transport-neutral. Applications can map these names to OpenTelemetry, Prometheus, logs, or proprietary monitoring systems.

The executable instruments, cardinality rules, health probes, structured JSONL
events, and host endpoints are documented in
[Runtime Operations](OPERATIONS_RUNTIME.md).
