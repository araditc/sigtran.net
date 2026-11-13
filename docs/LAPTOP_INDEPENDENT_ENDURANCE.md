# Laptop-Independent Endurance Execution

The current measured source stays frozen. No SDK process, peer or workload
has been restarted to add the read-only reconnection tooling described here.

## Server Ownership

Both load/control jobs are Linux **system** services launched through
`systemd-run`, not desktop processes, SSH-session children, user login services
or Codex background tasks. The SDK host owns its compiled files, peer-control
SSH identity, pinned known host, checkpoints, monotonic deadline and collection
logic. The peer owns its independent process/counters, maintenance audit,
heartbeat lease and restoration watchdog. No live credential exchange with the
laptop is needed after launch.

The observed SDK-to-peer route is the servers' `ens34` LAN path, with the SDK
server as source. Data and control traffic do not traverse the laptop's VPN.
The desktop VPN is needed only for management/status/retrieval. Closing Codex,
losing SSH, disconnecting this VPN or shutting down the laptop does not stop
these server-local jobs.

**Both VMs and their hypervisor must remain powered on independently of the
laptop.** If a VM actually runs in a hypervisor on that same laptop, shutting
the laptop down would stop it; no application can remove that dependency.
Do not suspend/reboot the lab VMs or their host during the measured interval.

## Durable Progress And Completion

Server-local artifacts survive desktop disconnects: disk-flushed hash-chain
checkpoints, bounded resource observations, last control acknowledgement,
actual child exit receipts and original capture/peer evidence. At normal or
failed completion, the existing server code restores the approved Kafka state,
removes scoped network/policy changes, collects receipts, hashes originals and
runs independent verification. A verified summary is retained on the SDK host.
Expired guards cannot leave permanent restart exclusions behind.

Codex itself does not keep thinking or making Git commits after the desktop app
exits. The already deployed Linux programs continue their scheduled work and
retain the result. Documentation/final review/commit can be completed when
Codex reconnects; no fabricated completion or fresh measurement is needed.

## Reconnect And Inspect

From the repository, with the existing management VPN connected:

```powershell
python scripts/inspect-endurance-lab.py --latest
```

This command sends only read-only inspection code. It discovers the single live
full-duration run, ignoring short diagnostics and older failed attempts. When
none is active it selects the latest retained full attempt. Its start frame
binds source, original SDK PID, duration and earliest finish. It returns fresh
resource/ledger observations, last incident acknowledgement, unit/collection
state and any saved independent verdict. It never starts/stops/resumes load.
Multiple active full runs cause an explicit error rather than a guessed choice.

An explicit identity still works:

```powershell
python scripts/inspect-endurance-lab.py --run-id map-endurance-full-20261113-a
```

`SIGTRAN_LAB_SDK_HOST` and `SIGTRAN_LAB_SDK_KEY` override local connection
defaults; `--known-hosts` selects the pinned-host file. Do not put private keys
or passwords into a repository descriptor. [Agent instructions](../AGENTS.md)
make this check the first lab action in a later Codex session.

Unreachable management returns `management=unreachable`,
`executionState=unknown`, `workloadRestarted=false` and exit code 2. It is not
evidence that traffic failed. Reconnect and inspect the same server-side run.
An active service is not qualification; final exact-duration/accounting/resource/
wire/restoration evidence remains mandatory. If a retained summary exists but
systemd's final supervision state is no longer available, the reader reports
that uncertainty instead of silently promoting a pass.
