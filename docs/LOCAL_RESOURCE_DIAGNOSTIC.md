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

GC context and raw managed readings cover the whole workload, but EventPipe
traces cover **bounded windows**, not the whole hour. The existing private
`dotnet-trace` collector attaches to the owned SDK PID as the same user. Its
version and executable hash are retained. Three 20-second windows are requested
approximately 2, 1800 and 3540 seconds after SDK spawn; a ten-second smoke uses
one five-second window. These offsets are not the SDK measurement clock.
The provider is `Microsoft-Windows-DotNETRuntime:0x1:4` with a 32 MiB buffer.
At most two additional post-negative-reading windows may be requested if enough
time remains; these cannot recover events preceding attachment. Window times,
PIDs, actual collector exits and observed negative readings are retained.
No global environment, forced GC or native/kernel tracing privilege is used.
See [Microsoft's collector documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace).
Each trace has a 32 MiB file budget and total artifacts a 512 MiB budget; low
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

Preparation attempt `local-resource-smoke-20261117-a` failed during exclusive
SFTP upload before any system unit, peer, capture or workload started. Read-only
inspection found only a zero-byte first DLL and no unit/trace. The launcher
now requests Paramiko write-plus-exclusive mode (`wx`), with a regression test;
file-I/O errors are no longer misreported as management-network outages. The
failed directory is retained; a new identity is required for another attempt.

Smoke `local-resource-smoke-20261117-b` reached the local peer/capture, but its
ready check incorrectly expected JSON instead of the peer's text log. It
stopped its own peer after ten seconds without launching the SDK; zero packets
were recorded. The actual failed controller/peer receipts and original files
are retained. Cleanup removed its exact restart exclusion. Ready parsing now
has a regression fixture from the actual text. Read-only status also tolerates
a root-private cleanup receipt and reports independently observable guard-file
absence rather than failing or interpreting permission denial as removal.

Smoke `local-resource-smoke-20261117-c`, source `fa4d410`, completed ten measured
seconds with 70,785 in-window successes and 256 separately reported drain
successes. All 71,041 requests completed; no unexpected failures or pending
requests remained. SDK, independent peer and capture exited zero, and the
owned restart exclusion was removed. Fifteen original artifact hashes were
verified on retrieval. Its startup EventPipe trace decoded 32 GC starts/stops,
one SDK PID and zero lost events. This validates the actual pipeline, not
capacity, historical causation or the subsequent windowed collector. A fresh
smoke must validate windowed collection before the hour is launched.

Windowed smoke `local-resource-smoke-20261117-d`, frozen source
`fb4c7159e74b31f35db6004596b469ba593fe442`, passed ten measured seconds:
86,294 in-window successes plus 256 drain successes, no failures/pending work,
and exact independent peer totals of 86,550. All four children exited zero,
18 original hashes matched, and the owned restart exclusion was absent.
The five-second trace independently decoded 18 GC starts/stops on SDK PID
2712819 with zero lost events. Binary/resource replay passed all 11 rows;
there were no negative readings or retries.

## Approved Hour

`local-resource-hour-20261117-a` was launched from that same frozen source,
using SDK PID 2715299, .NET 10.0.12/server GC and four maximum workers.
Actual measurement started `2026-11-17T10:23:54.6275857+00:00` (13:53:54 Tehran).
The earliest measured finish is 11:23:54 UTC (14:53:54 Tehran), followed by
drain and collection. This record describes a **running diagnostic**, not a
successful hour. Initial trace collection exited zero. Its own system unit,
deadline, exact-service protection and cleanup are server-local. No Kafka
host is used. Later desktop documentation/verifier edits do not change the
frozen files already executing on the SDK host.

`scripts/verify-local-resource-evidence.py` separately replays the completed
hash inventory, journal, exact admission/terminal/peer totals, binary resource
rows, every JSON raw attempt and GC bracket, and scheduled collector identities
and exits. It deliberately reports trace contents as not independently decoded:
run `TraceAnalysis --gc` on analysis copies for that separate check. PCAP payload
decoding, frozen input hashes/source archive and host cleanup also require
separate verification. A local execution pass never closes the endurance gate.

```text
python scripts/verify-local-resource-evidence.py PRIVATE_ORIGINAL_ROOT --source-commit FULL_SHA --seconds 3600
```

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
