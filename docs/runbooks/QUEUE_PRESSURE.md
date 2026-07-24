# Queue Pressure Runbook

Use this runbook when M3UA readiness is degraded because inbound or outbound
queue depth exceeds the configured health threshold.

## Detect

1. Read `/metrics` and `/health/ready`.
2. Determine whether pressure is inbound, outbound, or both.
3. Compare transfer rate, operation latency, CPU, memory, reconnects, and
   faults over the same interval.
4. Check the upper-layer consumer and remote peer before increasing capacity.

## Recover

1. Restore a stalled upper-layer consumer or remote signaling peer.
2. Reduce offered load according to the application admission-control policy.
3. Scale independent associations only when routing and peer traffic mode
   support it.
4. Change queue capacity only through a reviewed deployment. A larger queue
   increases memory and latency and does not repair a blocked consumer.
5. Confirm queue depth decreases continuously and P95/P99 latency returns to
   its service objective.

## Escalate

Escalate if depth remains flat at capacity, memory rises without release, the
peer reports congestion, or shedding traffic violates application guarantees.

Retain metric windows, structured events, deployment configuration, traffic
rate, and the recovery action.
