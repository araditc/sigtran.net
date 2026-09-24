# Representative Multi-Host Qualification

Status: **IMPLEMENTING** under roadmap Milestone D / issue #6.

This qualification track validates the admitted SIGTRAN/MAP stack on genuinely
separate SDK and peer hosts. It extends the existing Phase 53/56 performance
runner; it does not reinterpret single-host capacity evidence as multi-host
qualification and it does not promote the stable manifest automatically.

## Qualification profiles

The protected workflow exposes four named duration profiles:

| Profile | Minimum timed soak | Intended use |
| --- | ---: | --- |
| `smoke` | 15 minutes | topology/configuration/fault-path admission |
| `stress` | 1 hour | sustained load and recovery characterization |
| `soak` | 6 hours | multi-hour stability qualification |
| `release` | 24 hours | release-grade long-duration evidence |

`Sigtran.NET.PerformanceLab` supports the duration through
`--soak-duration-seconds`. The timed stage uses fixed-memory deterministic
reservoir sampling for percentile calculation and tracks maximum latency across
all successful operations. Count-based historical Phase 53/56 runs remain
supported. The default reservoir is 200,000 observations and is hard-bounded to
1,000,000.

The self-hosted workflow separates qualification from evidence publication.
This matters for a release-grade run because GitHub documents a maximum
24-hour lifetime for `GITHUB_TOKEN`, while a self-hosted job may execute
longer. The qualification job writes raw and sanitized results to protected
lab storage; a second job receives a fresh token and publishes only the
sanitized evidence.

Primary GitHub references:

- <https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idtimeout-minutes>
- <https://docs.github.com/en/actions/reference/limits>

## Required topology

A qualifying run must satisfy all of the following:

- SDK workload and peer execute on distinct physical hosts or VMs;
- runtime hostnames are different and configured host labels are different;
- native Linux SCTP is used on the measured data path;
- exact source SHA is retained in the sanitized summary;
- local/remote point codes, network indicator and peer identity are recorded;
- SDK and peer kernel/CPU/memory information is retained in protected raw
  evidence;
- route/path and SCTP host settings are retained in protected raw evidence;
- the run is executed through the protected `sigtran-performance`
  environment.

Loopback, same-host containers and network namespaces do not close the
`multi-host-soak` gate.

## Fault scenarios in the first executable matrix

The reusable runner currently supports:

### `peer-restart`

Restarts the authorized peer service through the protected management path.
This validates process/association restart and reconnect ownership. It is not
described as host loss.

### `peer-outage`

Stops the peer service for a bounded interval and then starts it. Recovery must
produce a real reconnect and recovery traffic must have zero lost operations.

### `sctp-partition`

Installs temporary SDK-host firewall rules scoped to SCTP, the configured peer
data address, and the configured SCTP port. The runner:

- records the fault transition;
- removes rules in the normal cleanup trap;
- installs an independent transient systemd rollback timer before waiting;
- never modifies SSH/GitHub TCP management traffic;
- requires passwordless privileged execution on the owned lab host.

A run is invalid if cleanup cannot be proven.

## Not yet represented as completed fault scenarios

The approved roadmap also requires host loss, delay/loss impairment and route
withdrawal/recovery. These are **not** renamed versions of service restart or
the scoped SCTP partition.

They require a representative lab with an out-of-band management plane and
reviewed control hooks so the fault cannot strand the runner or management
connection. Until those controls exist and execute, the evidence matrix remains
incomplete and `multi-host-soak` stays open.

## Evidence model

Raw evidence remains outside the public repository under
`PERF_RAW_EVIDENCE_ROOT/<run-id>/raw` with restrictive permissions. It
includes packet capture, SDK metrics/report/trace, host details, network path,
fault events and peer status.

The public evidence branch contains only:

- `summary.json`;
- `report.md`;
- `raw-evidence.sha256` (digest references, not raw payloads);
- `sha256.txt` covering the sanitized files.

The summary records:

- exact source SHA;
- qualification profile;
- fault scenario and duration;
- distinct host labels;
- start/end UTC;
- timed-soak duration and successful/failed operation counts;
- throughput and latency;
- reconnect/failover result;
- lost recovery operations;
- pass/fail.

The workflow opens an evidence PR after successful execution. Merging an
evidence PR **does not** set `eng/release/stable-release.json` to passed.
Promotion requires review that the retained run corresponds to the required
matrix and representative topology.

## Acceptance boundary

One successful 15-minute smoke or one peer restart does not close the stable
gate. Gate closure requires digest-covered representative evidence for the
approved qualification matrix, including the long-duration tier required for
release readiness. Any source behavior change after the qualified SHA requires
freshness review before the old run can be used for release promotion.

The multi-host gate is independent from the already-closed numeric
`capacity-target` gate and from the still-open `operator-profile`,
`kubernetes-sctp`, and `trusted-signing` gates.
