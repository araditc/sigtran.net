# Local Real-Stack Resource Diagnostic

The owner approved one targeted diagnostic lasting at most 3,600 measured
seconds. It uses the actual SDK SCTP/M3UA/SCCP/TCAP/five-operation MAP pipeline
and independent C peer on **the SDK host's IPv4 loopback only**. The Kafka
machine is not a load endpoint and no Kafka service/data/configuration command
is part of this runner. This is not a fresh 72-hour attempt or capacity result.

The [prior isolated allocation diagnostics](MANAGED_MEMORY_SAMPLING.md) did not
reproduce the seven invalid managed readings. This session exercises the actual
runtime pipeline instead of only allocating arrays. A successful short session
still cannot prove the historical cause or reconstruct missing values.

## Measurement

`Sigtran.NET.PerformanceLab resource-session` reuses `CapacitySession`, the
continuous `EnduranceRunner`, 64-bit ledger and audited managed sampler.
Its options accept only `127.0.0.1`, 10..3600 seconds, at most 1024 workers
and a new output root. Diagnostic/qualification flags cannot be overridden.
No incidents are injected, no outstanding request is replayed and reconnect
is disabled. The planned hour uses concurrency 256, queue 8192, four maximum
workers, server GC and the explicit `lan-bounded-v1` socket profile.

The GC probe additionally records `GCMemoryInfo.Index` and generation-zero
collection counts before/after managed-memory sampling. These separate reads
bracket the observation; they do not claim an atomic runtime snapshot.
Every second's JSON line retains the full sample and protocol/queue snapshot;
all raw attempts remain in `SGENOBS2`. JSON observations are bounded to 16 MiB.

The SDK process alone enables startup EventPipe through
`DOTNET_EnableEventPipe=1`, provider `Microsoft-Windows-DotNETRuntime:0x1:4`,
`DOTNET_EventPipeCircularMB=20` (hexadecimal 32 MiB), output streaming and a
private trace path. No global environment, forced GC or native/kernel tracing
privilege is used. See [Microsoft's EventPipe configuration](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe).
The trace is capped at 256 MiB and total retained artifacts at 512 MiB; low
free space or a breached budget stops only owned diagnostic processes.

`Sigtran.NET.TraceAnalysis --gc ANALYSIS_COPY.nettrace` independently decodes
GC starts/stops, indices, generations, reasons, process identities and lost
events. Conversion creates a derived `.etlx`; use a separate analysis copy,
not the protected original root. Trace capture overhead makes this a diagnostic,
not a throughput qualification. GC events alone do not establish native-memory
causation or exclude every unsafe-memory defect.

## Durable Ownership

`scripts/run-local-resource-diagnostic.py` runs unprivileged inside a dedicated
Linux system service with `Restart=no`, `KillMode=control-group`, a hard time
limit and private umask. It uses only local child processes, not SSH, service
control, firewall changes or Kafka. An exclusive lock prevents two such jobs.
A bounded initial loopback capture corroborates the actual wire path.

The supervisor compiles the source-bound independent peer, checks input hashes,
owns SDK/peer/capture PIDs and retains their actual exits. Cleanup can signal
only those `Popen` children. A peer's orderly EOF after the SDK report is not
mistaken for a mid-run crash while the CLR finishes its trace file.
Failed attempts and partial evidence are retained, never restarted or stitched.
An exact owned-service, expiring restart exclusion may protect this short job;
its cleanup is separate from the original endurance run's removed exclusions.

All work remains server-local if the laptop, VPN, SSH or Codex disconnects.
The Linux VM/hypervisor must remain powered on. User-service linger is not
assumed; use a system service executing as `ammar`. Never use the endurance
bootstrap here: that bootstrap's separate peer-maintenance contract is outside
this local diagnostic authorization.

## Execution Record

Implementation and safety tests precede the frozen-source launch. A separate
10-second smoke must validate real traffic, capture and GC trace decoding before
the approved one-hour session. Actual identities, timestamps, exits, accounting,
raw negative attempts and decoded trace results will be appended after execution.
No result is implied by this preparation document.

The original full run remains rejected, Phase 62 remains open and no exception,
stable tag, package publication or production-readiness claim is authorized.

## Start And Reconnect

The launcher requires a clean committed source, pinned SDK-host SSH identity
and an ephemeral `SIGTRAN_LAB_SUDO_PASSWORD` for creating only the owned system
unit/restart guard. It never contacts the peer/Kafka host. It publishes the
SDK from that commit and retains the source archive and input hashes.
No cgroup memory limit is added that would silently change the GC heap budget.

```text
python scripts/start-local-resource-diagnostic.py --run-id OWNED_ID --seconds 3600 --port 33463
python scripts/start-local-resource-diagnostic.py --run-id OWNED_ID --status
```

The second command is read-only and needs no sudo password. A management
timeout means execution unknown; inspect the same identity after reconnecting,
never relaunch it. The protected original 72-hour status remains available
through the existing `inspect-endurance-lab.py --latest` command.
