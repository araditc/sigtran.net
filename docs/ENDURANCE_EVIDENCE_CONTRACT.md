# Endurance Evidence Contract

This contract applies to `completed-map-endurance-v1`, not historical soak
summaries. Qualification requires original bytes, one continuous SDK process,
all declared incidents and verified host/network restoration. A saved flag,
short diagnostic or stitched attempts cannot qualify or promote stable.

## Private Layout

The protected raw root contains `source.tar`, executed `policy.json`,
`inventory.json`, actual `sdk.exit` and `controller.exit` receipts,
`sdk/checkpoints.jsonl`, `sdk/sdk.json`, `sdk/resource-observations.bin`,
`peer-result.json`, `peer-counters.bin`, `host-maintenance.json`,
`controller-events.jsonl`, `network-events.jsonl`, `network-cleanup.json`,
`captures.json`, bounded capture/log files, `peer`, `libsctp.so.1`,
`sdk-executable.dll`, `sdk-library.dll` and a final `sha256.json` inventory.
Private SSH keys and credentials are outside this evidence root and must not
enter hashes, reports, Git or workflow artifacts.

The final collector runs only after the host-local service/children stop. It
records actual exit receipts, execution binary hashes, before/after machine
and boot identities, observed SDK PID/start ticks, scoped-rule cleanup and
collection errors. It hashes every retained artifact without following links
or escaping the root. The source archive's commit comment and policy bytes
must match the candidate/executed policy. No raw artifact is overwritten by
publication. Aborted prefixes remain diagnostic, not passing evidence.

## Independent Recalculation

The verifier replays exact journal digests and one-second binary resource
records, checks every admission/terminal vector, separates drain from measured
TPS and verifies memory/queue/progress aggregates. Recovery must follow the
fixed schedule with unique nonces, a live in-flight cohort, drained pending
work, five fresh successes, capture-stop acknowledgement and bounded recovery.
All SDK runtime faults must be classified inside declared windows.

Controller and independent peer receipts must match each SDK intent. The
crash case records actual SIGKILL exit; a held outage records stopped peer
state and subsequent restart. A network case must prove tagged SCTP-rule
packet drops and verified removal, not merely successful rule installation.
Kafka unit states, enablement and unit-file hashes must be restored after an
explicitly approved maintenance window; an independent rollback guard is
armed before stopping services or inserting network rules.

The peer's mmap publication tail is bounded by actual forced terminations;
accepted sends are not confused with SDK completions. Every SDK admission
still has an exact terminal result. A peer validation failure is never excused
as an expected injected failure.

## Packet Windows

The baseline capture opens before association startup. Each incident uses a
bounded ring capture, retained through the recorded five-probe window. Normal
admission stays paused until capture stop is acknowledged. Absolute wire TSNs
and payload-preserving extraction are used, with exact payload/stream/PPID
duplicate comparison. A recovery window is selected using the actual SDK
local SCTP port and SDK UTC probe boundaries; captured activation for that
association must precede DATA. Exactly five fresh correlated pairs spanning
all five operations must match, not orphan replies from a mid-traffic prefix.
PCAP samples corroborate protocol recovery, never extrapolate full-run TPS.

## Release Boundary

Even a passing 72-hour source/profile result is not a deployed packaged-candidate
attestation, an operator SLA or stable `GO`. Final package/source/scope-bound
authentication, cluster/operator acceptance and release trust remain required.
