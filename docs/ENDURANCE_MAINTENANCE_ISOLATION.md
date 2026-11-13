# Endurance Maintenance Isolation

The first full attempt is **interrupted and rejected**, not a 72-hour pass.
Its [sanitized failed-attempt report](evidence/ENDURANCE_INTERRUPTED_20261113.json)
binds source `5f5ce0a0119df978f78f3724284b5effde3baaff` and hashes the
separately retained original receipts and peer recovery supplement.

## Observed Interruption

The SDK ran from 2026-11-12 23:39:28 UTC until 2026-11-13 06:12:24 UTC:
23,576.11 actual seconds and 324,327,749 successful completed transactions.
The three first-cycle incidents recovered. Peak SDK RSS was 109.03 MiB;
the resource bounds passed, all 324,328,742 admissions reached terminal states
and none remained unresolved. These observations do not satisfy 72 hours.

At 06:11:52 UTC `apt-daily-upgrade.service` started. The unattended-upgrade
log records `libc6` and related library updates; its dpkg log explicitly lists
the owned peer service in a `systemctl restart` command. The peer service was
stopped at 06:12:24 UTC despite `Restart=no` and a three-day runtime limit.
The SDK correctly rejected this transport fault outside a declared incident:
one transport failure and 255 cancellation terminals were counted, in addition
to 737 already classified injected-fault terminals. SDK/controller exits remain 1.

The peer's normal stop path restored original Kafka states/enablement/unit-file
hashes and cancelled its fallback timer after verification. The original SDK
peer-export failure remains in place. A separate read-only recovery archive
retains the peer counters/log/result and restoration audit; it never overwrites
the original failed inventory or concatenates SDK runs.

## Scoped Correction

Ubuntu documents automatic restarts of affected services by `needrestart` on
24.04 and supports additive per-service restart-selection overrides in
`/etc/needrestart/conf.d/`: [Ubuntu automatic updates](https://ubuntu.com/server/docs/how-to/software/automatic-updates/),
[upstream configuration](https://github.com/liske/needrestart/blob/master/ex/needrestart.conf).

The helper creates a new root-owned file for exactly the owned SDK or peer
service and current run identity. It adds one `override_rc` entry, without
replacing the existing selection map or changing global restart mode. The
entry is conditional on the approved lease expiry, so it becomes inert even
if cleanup cannot run. Installed Perl syntax and effective selection are
checked before any Kafka stop or load. Automatic reboot must be disabled.

Security updates, SSH/network services, Kafka restart selection and all
unrelated policies remain unchanged. Normal/failed exit removes only the
exact owned file after verifying its digest. Tampering is never resolved by
deleting somebody else's configuration. SDK/peer policy-install/removal
receipts are required by the final independent verifier.

This is a temporary experiment boundary, not a production update policy.
Production security maintenance must use the deployment/failover procedures
qualified separately. A new attempt starts from zero for the full 259,200
seconds; the interrupted six hours are not reused.

The [actual guarded diagnostic](evidence/ENDURANCE_POLICY_DIAGNOSTIC_20261113.json)
passed independent verification on source `1579bad8b2c731819bfeb063260f23d10a0e427f`:
360 seconds, 4,532,534 measured successes, three recovered incidents, no
unexpected/unresolved work, matching fresh PCAP probes and verified restoration
of Kafka plus removal of both exact-service policy files. A new full run on
that frozen source is recorded in [execution status](ENDURANCE_EXECUTION_STATUS.md).
