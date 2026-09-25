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
1,000,000. Run-wide non-transfer M3UA runtime-event retention is independently
bounded to the latest 4,096 records; full trace output remains protected raw
evidence rather than an unbounded in-memory collection.

Ordinary PerformanceLab execution keeps the historical reconnect default of 30
attempts. Representative qualification explicitly passes a bounded
`--reconnect-max-attempts` value to both native SCTP and M3UA runtime policies.
The runner derives that value from the selected fault duration and failover
window using the actual 100/200/400/800 ms then 1-second-capped backoff model,
requires at least a five-second delay-budget margin beyond the failover timeout,
and rejects any derived budget above 180 attempts.

The self-hosted workflow separates qualification from evidence publication.
Each execution attempt is owned by `github.run_id + github.run_attempt`:
protected storage, rollback units/rule tags and sanitized evidence destinations
are attempt-unique. Reruns do not overwrite a prior attempt or disarm its rollback.
The SSH wrapper additionally uses a random private directory under runner temp,
created atomically by `mktemp -d`, and sets `umask 077` **before** writing any
material. The directory is 0700 and key/known-hosts files are 0600 from creation,
not after a chmod window. Creation occurs after the build; original secret-value
environment variables are removed before invoking qualification, and the wrapper's
EXIT trap removes only its own directory. A SIGKILL or host failure cannot run
shell traps: any residue remains private and requires controlled runner cleanup.
No key or raw host inventory is printed to the Actions log.

This matters for a release-grade run because GitHub documents a maximum
24-hour lifetime for `GITHUB_TOKEN`, while a self-hosted job may execute longer.
The qualification job writes to protected lab storage; a second job receives a
fresh token and publishes only sanitized evidence.

Primary references:

- <https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idtimeout-minutes>
- <https://docs.github.com/en/actions/reference/limits>
- <https://www.gnu.org/s/coreutils/manual/html_node/mktemp-invocation.html>
- <https://www.freedesktop.org/software/systemd/man/latest/systemd-run.html>

## Required topology

A qualifying run must satisfy all of the following:

- SDK workload and peer execute on distinct physical hosts or VMs;
- runtime hostnames and configured host labels are different;
- native Linux SCTP is used on the measured data path;
- the current runner accepts only a non-loopback, non-local IPv4 data endpoint
  and rejects a `REMOTE_IP` route resolving locally or through `lo`; IPv6
  partition behavior is not qualified by this implementation;
- exact source SHA is retained in the sanitized summary;
- local/remote point codes, network indicator and peer identity are recorded;
- SDK/peer kernel, CPU, memory and route/SCTP settings are retained in protected
  raw evidence, not copied to the public branch;
- execution uses the protected `sigtran-performance` environment;
- `PEER_SERVICE` is a validated service-unit name ending in `.service`;
- `PERF_PEER_BUILD_METADATA_FILE` points to an authorized peer-local JSON
  marker. After the peer service is active, the runner reads it over the same
  protected SSH path and requires `schemaVersion: 1`, non-empty
  `implementation` and `version`, plus an optional `buildDigest` in
  `sha256:<64-hex>` form;
- `PERF_RAW_EVIDENCE_ROOT` is an absolute, runner-owned private directory (0700).

Loopback, a locally routed endpoint, same-host containers and network namespaces
do not close `multi-host-soak`. Labels alone are insufficient: runtime hostnames
and data-route locality are also checked. All real fault controls require the
existing authorized lab; code or mocked tests do not grant network authority.

## Fault scenarios in the first executable matrix

### `peer-restart`

Restarts the authorized peer service through the protected management path.
This validates process/association restart, not host loss.

### `peer-outage`

The peer-side `peer-outage-recovery.sh` first arms an attempt-specific transient
systemd timer **on the peer**, then stops only the validated service. Failure to
arm aborts before the stop. Both actions are delivered in one SSH invocation.

Normal recovery starts the service and verifies it is active before disarming
that owned rollback. A failed start/verification leaves rollback armed. If the
SDK dies or SSH is lost, the peer timer remains independent: after the configured
hold plus a 60-second recovery margin, its service retries start/verification
until active. A lost SSH acknowledgement does not authorize local cleanup to
cancel a timer it did not acknowledge. The peer requires already-authorized
privileged `systemd-run`/`systemctl` execution; this change provisions no access.

The fault hold is controlled, but the margin/repair loop is not a guarantee that
a broken peer OS/service can recover within a fixed deadline. The SDK reconnect
budget is sized before execution so its bounded retry-delay horizon extends past
the complete failover window for every admitted 1-60 second outage. A failure of
the peer host itself remains the separate host-loss matrix row. Recovery evidence
must contain a real reconnect with zero lost recovery operations.

### `sctp-partition`

Temporary SDK-host firewall rules are scoped to SCTP, peer data address and port.
The runner:

- tags each DROP rule with the unique run-attempt ownership marker;
- arms an independent SDK-side systemd rollback before any firewall mutation;
- removes only owned rules and verifies both absences before disarming rollback;
- leaves rollback armed on failed or unverifiable normal cleanup;
- retries and verifies independent rollback deletion after transient xtables
  errors, rather than converting a failed deletion into success;
- does not modify SSH/GitHub TCP management traffic.

A run is invalid if cleanup cannot be proven. Signal exits retain nonzero status;
cleanup failure or persistence failure cannot be reported as qualification PASS.

## Missing representative matrix rows

Host loss, delay/loss impairment and route withdrawal/recovery are not renamed
service restart or SCTP partition tests. They need representative out-of-band
management and reviewed controls that cannot strand the management connection.
Until those controls execute and evidence is retained, the matrix is incomplete
and `multi-host-soak` remains open.

## Evidence model

Raw PCAP/SDK trace/host inventory is created in a **random mode-0700 scratch
directory under runner temp**, not in `GITHUB_WORKSPACE`. The script sets umask
077 before directory/file creation. Even on failure the working copy is private.
Tcpdump is stopped and its ownership normalized before persistence.

`persist-qualification-evidence.py` copies stopped-run `raw/` and `safe/` trees to
a private staging directory under `PERF_RAW_EVIDENCE_ROOT`, restricts all copied
modes, compares every file's SHA-256 and the complete tree (including empty dirs),
and verifies the original did not change during copying. Symlinks/special files,
nonprivate roots, overlapping paths and existing attempt destinations are rejected.
Only a verified copy is renamed to the final attempt path; the final tree is
checked before scratch removal. Copy/verification failure returns failure and
retains private scratch for recovery, rather than suppressing errors or deleting
the sole copy. Interrupted staging/temporary directories require controlled lab
cleanup, never blind public artifact upload.

The validated marker is retained only as protected `raw/peer-build.json`.
Unknown marker fields are rejected rather than copied, so qualification records
the exact peer implementation/version without publishing peer configuration or
turning repository simulation into vendor/operator acceptance.

Persistent raw evidence is under `PERF_RAW_EVIDENCE_ROOT/<run-id>/raw`. The private
`protected-evidence.sha256.json` at the attempt root covers raw and sanitized files;
it is **not** copied to the public branch. The public branch still contains only:

- `summary.json`;
- `report.md`;
- `raw-evidence.sha256` (digest references, not payloads);
- `sha256.txt` covering sanitized files.

Summaries record source SHA, profile/fault/duration, failover timeout, reconnect
attempt budget, distinct-host result without host labels/names, times,
throughput/latency, operation counts, recovery outcomes and pass/fail. An evidence
PR is opened only after successful execution. Merging it never automatically sets
the stable manifest to passed.

## Offline safety regression coverage

Normal PR CI executes all 12 profile/fault plans, shell syntax checks and
`python3 scripts/tests/test_qualification_safety.py` without external traffic.
The 18 isolated tests cover peer build-marker validation, decimal fault-duration
normalization and maximum-outage reconnect-budget coverage plus peer
arm-before-stop ordering, failed arming,
independent retry after the initiating process exits, verify-before-disarm,
failed recovery preserving rollback, invalid service rejection, full copy/digest
verification, corrupted/failed copies preserving scratch, immutable attempt
retention, symlink/private-root/overlap rejection, and SSH permissions/cleanup
under a deliberately permissive inherited umask.

Systemd commands are stubs and file contents are synthetic. This is executable
safety-unit evidence, **not** a live remote rollback or multi-host qualification.
Real peer-side timer behavior, permissions and cleanup must still be validated
in the protected representative lab before a qualification gate is closed.

## Acceptance boundary

One successful 15-minute smoke or peer restart does not close the stable gate.
Gate closure needs digest-covered representative evidence for the approved
matrix and required long-duration tier. Behavioral source changes require
freshness review before older runs can be used for release promotion.

The gate is independent of the already-closed numeric `capacity-target` and the
still-open `operator-profile`, `kubernetes-sctp`, and `trusted-signing` gates.
