# Durable Endurance Execution

The frozen host-local controller and plan live under a new root-owned
`/opt/sigtran-endurance/runs/<run-id>` on each authorized VM. A run never
restarts its SDK or overwrites an earlier attempt. Builds happen before
maintenance and measured traffic, outside the execution service.

`start-endurance-lab.py` requires a clean committed tree, strict known-host
verification and explicit maintenance approval. It runs Linux safety tests
and strict native compilation before launch. `inspect-endurance-lab.py`
reads service state, retained resource/ledger progress and the verified summary
without changing the workload. It never infers qualification from liveness.
The bootstrap rejects an already active owned SDK job before building; an
exclusive host-local lock also prevents concurrent controllers from starting
maintenance. Authenticated machine/address ownership is checked before stop.
Even a child-stop exception requests host restoration, and an unused peer
control daemon expires without operating on Kafka.

## Ownership And Control

The protected controller is launched as a systemd service. Its SDK, native C
peer and capture children run as the unprivileged lab user. A private,
run-scoped SSH identity uses strict pinned-host verification and a restricted
forced JSON bridge to the peer's local Unix socket. No password, private key,
generic sudo grant or SSH shell belongs in raw evidence or Git.

The root peer agent accepts only declared incident identities/nonces and
bounded artifact exports. Stop/restart is idempotent across lost SSH replies;
remote callers cannot supply process IDs, paths, shell commands or units.
Only the owned peer child receives signals and CPU affinity. Process exits,
generations and forced-kill publication uncertainty are retained explicitly.

## Kafka Maintenance

The owner explicitly approved temporary shutdown for the entire 72-hour test
on the peer. Before stopping anything, the agent saves original unit states,
enablement and unit-file hashes and verifies an independent root restoration
timer. Stop order is health timer, health service, broker and controller.
Restore order reverses that list but starts only originally active units.
Kafka data, configuration, firewall service and enablement are untouched.

The peer requires a controller heartbeat at least every 180 seconds and has
an absolute expiring lease. Normal/failure exit and systemd ExecStopPost request
restoration; the independent deadline guard retries uncertain restoration.
A verified restore precedes guard cancellation. No successful qualification
is published if cleanup or collection is uncertain.

## Artifacts And Exit

Baseline capture starts before association setup. Each fault uses an eight-file
1 MiB ring on the owned flow, held until all five fresh probes complete.
Short diagnostics use the same ten-second fault holds as the full run, so
they exercise the same heartbeat/reconnect supervision before qualification.
After probe acknowledgement, capture remains active for three seconds with
ordinary admission still paused. Stopping immediately can discard the final
kernel capture buffer without incrementing reported drop counters; the final
verifier still requires the actual five probe pairs, regardless of this delay.
The network helper records actual owned-rule drop counters before removal;
Kafka TCP traffic is not a fault target. Source/policy/binary identities are
pinned, and each private attempt retains both failed and successful artifacts.

After the controller exits, its separate supervisor writes its actual exit
receipt, verifies scoped cleanup, hashes original artifacts and invokes the
independent verifier. A sanitized summary is created exclusively only after
verification. An execution or verifier error leaves originals intact. A short
diagnostic cannot close Phase 62 or stable release gates.
