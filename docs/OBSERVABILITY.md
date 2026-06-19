# Observability

Commercial SIGTRAN deployments need repeatable metrics, traces, and health signals.

`SigtranObservability.CreateDefaultProfile()` exposes the current SDK observability profile.

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
