# Long-Run MAP Resilience Qualification

Phase 62 is **in progress, not qualified**. Policy: `completed-map-endurance-v1`.
The [Phase 61 capacity pass](CAPACITY_QUALIFICATION_EXIT.md) is preserved and
does not substitute for three real days of continuous execution.

## Fixed Exit Criteria

One SDK process runs the five-operation ITU MAP SMS mix over native SCTP/M3UA/
SCCP/TCAP for at least 259,200 measured seconds on two identified Linux hosts.
Short diagnostics, stitched attempts, process restarts, missing final receipts
or documentation alone cannot satisfy this duration. Source/package-bound
stable release qualification remains separate and cannot be promoted here.

The versioned [policy](../eng/qualification/endurance-policy.json) fixes a
60-second durable checkpoint cadence, memory and artifact bounds, steady-load
target, nine incidents spanning three fault families, a 90-second recovery
budget, and zero unexpected failures or unresolved requests. A known injected
failure may produce explicitly counted terminal timeout/transport outcomes;
these are never silently discarded, retried or described as successful MAP work.
Unexpected errors/rejects/malformed results remain failures even during a fault.

The planned incidents are peer crash/restart, a held private peer outage, and
an SCTP-only partition restricted to the owned lab flow. Kafka TCP ports,
configuration and topic data are outside fault-injection scope. A 72-hour
Kafka maintenance window requires separate explicit owner approval; the prior
30-minute permission is not assumed to authorize three days of service outage.
The owner has now explicitly approved temporary Kafka/health-timer shutdown
on the peer for this entire 72-hour test, with verified restoration on exit.

## Ten Units

| Unit | Deliverable | Status |
| --- | --- | --- |
| 1 | Versioned duration/resource/fault contract and validated options | Complete; build/test/API/pack passed |
| 2 | 64-bit, bounded, exact admission-to-terminal ledger | Complete; concurrent/fault tests and build/test/API/pack passed |
| 3 | Continuous runner, durable checkpoints and deadline drain | Complete; journal/deadline/cancellation tests and build/test/API/pack passed |
| 4 | Independent long-running peer and crash-surviving counter evidence | Complete; strict Linux C build/fixtures, mmap values above 2^32 and build/test/API/pack passed |
| 5 | Scoped incident control, recovery probes and rollback | Complete; control/rollback safety tests and final build/test/API/pack passed |
| 6 | Memory/heap/queue progression and bounded capture/log storage | Complete; resource/rollover/binary-format tests and final build/test/API/pack passed |
| 7 | Independent verifier, tamper rejection and release-boundary tests | Complete; corruption/release-boundary tests and final build/test/API/pack passed |
| 8 | Durable execution, watchdogs and authenticated host control | Complete; safety tests and final build/test/API/pack passed; real diagnostic is unit 9 |
| 9 | Real two-host short diagnostic of all incident families | Complete; all three faults, five fresh PCAP pairs each, exact accounting/resources and automatic restoration independently verified; failed attempts retained |
| 10 | Actual 72-hour run, restoration, verification and exit report | Pending real execution; cannot be claimed early |

Each unit includes code, behavioral tests, standard code documentation,
docs/README updates, build/test/API/pack and its own commit/push. Runtime
names describe the domain, never a phase number. Raw evidence is private;
only validated sanitized summaries and hashes are published.

## Terminal Accounting

Each fixed worker slot owns a generation-fenced admission. Counters and
generations are 64-bit; no per-transaction dictionary/list grows across the
run. A consistent checkpoint includes all five operation totals, eight terminal
outcomes, pending work, accounting violations and explicitly classified fault
outcomes. A duplicate or stale completion invalidates accounting.

The first timeout/transport/dialogue-close outcome inside a declared fault
pauses new admissions, bounding fault failures to one in-flight cohort.
Errors, rejects, malformed results and cancellation remain unexpected even
inside a fault. After drain, five fresh successful probes are required before
recovery is acknowledged; no prior request is replayed. Capture-stop
acknowledgement precedes ordinary admission resumption.

## Continuous Execution And Checkpoints

The endurance runner owns one fixed worker set and one real monotonic origin.
Deadline expiry stops admission, while in-flight requests get a bounded drain.
Late successes remain terminal but do not inflate measured TPS. Cancellation
is counted explicitly. A checkpoint/control failure aborts the run, rather
than leaving an apparently successful partial measurement.

Checkpoints are append-only, exclusively created, individually disk-flushed
frames. SHA256 binds the previous raw digest and exact UTF-8 payload bytes;
sequence, frame-size, frame-count and total-file bounds are enforced. The
resource observation includes OS peak RSS, sampled managed/GC memory, process
CPU and ThreadPool counters. No forced collection is used to make memory look
smaller. An interrupted prefix is diagnostic evidence, never a final pass.

## Independent Peer Counters

The C peer retains strict protocol/stream/PPID checks without linking the SDK.
Per-connection and summary counts are now 64-bit. Its explicit `endurance` mode
maps a supervisor-created private 64-byte file containing accepted invokes,
accepted response sends, five operation counts and validation failures.
Lock-free aligned 64-bit atomic counters survive peer process termination;
this does not assert power-loss durability or authorize stitching SDK restarts.

The supervisor reads/flushes counters after a child stops. A forced kill can
interrupt publication of one request: the corresponding bounded uncertainty
must remain explicit. Accepted sends are not assumed to be SDK completions.
Every SDK admission still requires its own terminal outcome, and any peer
validation failure prevents qualification. Counter-file type/size/identity,
response bounds and per-operation prefix reconciliation are checked.

## Fault Control And Rollback

The `endurance` application uses sequenced private control commands with
unique per-incident nonces. It durably records the scheduled fault window
before acknowledging injection. A source-side fault counter change outside a
declared window aborts qualification; transfer-event subscriptions are avoided
to prevent allocating event records for every DATA message.

Release pauses admission, drains every outstanding invoke, waits for transport
activation and requires five fresh MAP successes. The same measurement path
counts probes and ordinary work. A capture-stop acknowledgement is required
before resuming traffic. Reconnect is enabled only in the endurance session;
the Phase 61 capacity default still forbids reconnect/replay. Endurance uses
explicit two-second heartbeat/acknowledgement supervision, so a ten-second
partition exercises transport recovery rather than relying on a favourable
alignment with the thirty-minute profile's longer heartbeat interval.
Probe windows also record their actual UTC bounds and active local SCTP port
for independent capture correlation across association generations.

The privileged network helper accepts only a root-owned immutable per-run
plan. Its two exact tagged rules match SCTP, one peer address and one owned
port; Kafka TCP traffic is not matched. It verifies an independent rollback
timer before insertion, cleans partial insertion, and disarms only after both
owned rules are verified absent. Lock/permission failures never mean absence.
The guard retries uncertain cleanup independently of the SDK/controller.

## Resource And Progress Bounds

An observation is taken about once per second. Fixed-width binary records
preserve those observations while hash-chain checkpoints are written each
minute. The `SGENOBS1` header precedes 208-byte little-endian records
(`double`, six signed 64-bit counters, `double`, eighteen signed 64-bit counters).
The doubles are actual elapsed seconds and process CPU seconds. Remaining
fields are RSS/OS peak RSS/managed bytes/GC heap/fragmentation/allocation,
GC/ThreadPool counters, ledger admissions/successes/pending, active/pending
dialogues, queues and unexpected-protocol counters, in that order.
The stream has a separate 64 MiB byte bound and fixed maximum record count,
and flushes to disk each minute and on disposal. Constant-size aggregates retain actual observation gaps,
OS high-water RSS, sampled managed/GC heap, bounded queues/contexts and success
progress. Managed-memory sampling is not misrepresented as an exact transient
peak. The baseline is the first observation at or after 600 seconds (one-sixth
of a short diagnostic); the maximum subsequent RSS growth is bounded too.
No forced GC or per-transaction resource log is used.

A resource/queue/protocol breach or more than 120 seconds without a successful
result records the violated bound and aborts execution. Checkpoints retain
the evidence; no unlimited sample list grows across three days. Captures and
checkpoint storage have separate hard byte budgets. Mean TPS uses successful
in-window results, including fresh probes and the measured fault downtime;
drain remains excluded. This is not a relaxation of Phase 61's independent
per-second capacity criterion.

The default 24-bit TCAP transaction-ID range and configurable full 32-bit
range both have rollover tests; the manager's existing allocation fence skips
live identifiers. Multi-billion-run counters and dialogue handles remain 64-bit.
