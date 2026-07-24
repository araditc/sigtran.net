# Association Recovery Runbook

Use this runbook when readiness is degraded or unhealthy because SCTP or M3UA
is connecting, reconnecting, closed, or faulted.

## Detect

1. Query `/health/ready` and record the M3UA state, association, queue depths,
   fault count, and reconnect count.
2. Inspect structured events for `m3ua.fault.observed`,
   `m3ua.reconnect.scheduled`, and `m3ua.state.changed`.
3. Confirm the remote IP and SCTP port match the approved topology.
4. Check host SCTP support, routing, firewall policy, peer process state, and
   peer ASP/AS state.
5. Capture SCTP traffic before restarting either side when incident policy
   permits it.

## Recover

1. Correct network or peer availability without changing point codes or
   routing context during the incident.
2. Allow the bounded exponential reconnect policy to recover the association.
3. If reconnect is exhausted, restart one SDK instance at a time.
4. Confirm SCTP `COMM_UP`, ASP Up Ack, ASP Active Ack, heartbeat Ack, and DATA
   exchange.
5. Verify readiness is healthy and queue depths return below the warning
   threshold.

## Escalate

Escalate when the peer rejects ASP activation, metadata reports an unexpected
PPID or stream, reconnect repeats after network recovery, DATA is not
acknowledged by the application flow, or failure affects every association.

Retain configuration, redacted PCAP, peer log, SDK JSONL events, health
snapshots, and UTC timestamps for the incident record.
