# Repository Work Rules

- Keep SDK/runtime names independent of roadmap phase numbers and vendor names.
- Include focused tests, standard API comments, relevant docs and README updates
  for behavioral changes. Run build/tests/API comparison/pack before commit/push.
- Preserve user changes, private keys, original lab artifacts and failed receipts.
  Never infer stable/operator readiness from a source/profile benchmark alone.

## Resume An Existing Endurance Run

Read `docs/ENDURANCE_EXECUTION_STATUS.md` and
`docs/LAPTOP_INDEPENDENT_ENDURANCE.md` before working on the lab hosts.
On reconnect or a new Codex session, first run this read-only command:

```powershell
python scripts/inspect-endurance-lab.py --latest
```

It discovers the live full run on the SDK host, or the latest retained full
attempt when no load is active. An explicit `--run-id` remains available.
Host/key overrides use `SIGTRAN_LAB_SDK_HOST`, `SIGTRAN_LAB_SDK_KEY` or CLI
options; credentials and private keys must never enter Git or evidence files.
Use pinned SSH known hosts. The existing local lab key is under the user's
`.ssh` directory, not a Codex login/session credential.

A desktop/VPN/SSH timeout means **management unreachable, execution unknown**.
Reconnect the authorized management VPN and retry status; do not run a launch
script, restart a service, kill a PID, clear evidence or stitch runs to recover
desktop visibility. Measurement/control/cleanup run on Linux without Codex.
Keep the measured source and host workload unchanged during the live interval.
The actual full duration, exact accounting, independent verification and
restoration remain mandatory before calling the endurance phase complete.

NuGet publication, tag/stable promotion and unrelated Kafka data/configuration
changes are outside this endurance task. The approved temporary Kafka shutdown
is restored by the host-local job/watchdogs, not by a desktop process.
