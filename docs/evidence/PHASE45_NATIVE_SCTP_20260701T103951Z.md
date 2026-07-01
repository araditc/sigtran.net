# Phase 45 Native SCTP Evidence - 20260701T103951Z

This evidence record captures a real Linux SCTP loopback run for the native SCTP production transport path.

## Environment

- Host: `sigtrannet`
- VM address used by the lab: `192.168.100.28`
- Kernel observed during preflight: `5.15.0-181-generic`
- SCTP libraries/tools observed during preflight: `libsctp.so.1`, `tcpdump`, `tshark`, `python3`
- Run id: `phase45-native-sctp-20260701T103951Z`
- Artifact root: `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z`

## Result

- Production transport sample result: `PASS`
- Expected stream id: `1`
- Expected PPID: `3`
- M3UA message events: `14`
- Metadata validation: `True`
- Stream validation: `True`
- PPID validation: `True`
- Reconnect failed attempts observed: `3`
- Reconnect successful attempts observed: `1`
- Shutdown events observed: `2`
- Metrics snapshots: `4`

## Retained Artifacts

| Artifact | Retained path | SHA-256 |
| --- | --- | --- |
| PCAP | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/pcap/phase45-native-sctp-20260701T103951Z.pcap` | `33c7708b66fba17b7f5e72ed30e06be3e1f8e01d27e60c25d8c7165fe5663f35` |
| SDK trace | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/trace/phase45-native-sctp-20260701T103951Z.jsonl` | `abaa2a9153b10db6abd73050c85d26c820d16d892594e781e4ade9559e863c90` |
| TShark decode | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/comparison/phase45-native-sctp-20260701T103951Z-tshark.txt` | `2352dfce5f929f4678c9194d7e9819d97086344eb7443db5793b4f5a1f053a95` |
| Run report | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/reports/phase45-native-sctp-20260701T103951Z-report.md` | `e9bda8768130a2823195be5cd4e499e70045f5e8e55e3435f798e0e96d706858` |
| TCP dump log | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/logs/phase45-native-sctp-20260701T103951Z-tcpdump.log` | `63900285e2c7224d0d120d3642337715793ed8a012ed8b574dea2bd9e765e761` |
| Publish log | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/logs/phase45-native-sctp-20260701T103951Z-publish.log` | `4146f251e8a70c35f48de67b0e1592b0ce465f2d26f1b4d5fba716e9a925d921` |
| Lab log | `/home/ammar/sigtran-phase45-run/artifacts/phase45-native-sctp-20260701T103951Z/logs/phase45-native-sctp-20260701T103951Z-lab.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |

## Packet Summary

The retained TShark decode includes:

- SCTP reconnect attempts with `INIT` followed by `ABORT`.
- A successful association with `INIT`, `INIT_ACK`, `COOKIE_ECHO`, and `COOKIE_ACK`.
- M3UA `ASPUP`, `ASPUP_ACK`, `ASPAC`, `ASPAC_ACK`, `BEAT`, and `BEAT_ACK` over SCTP.
- Graceful shutdown with `SHUTDOWN`, `SHUTDOWN_ACK`, and `SHUTDOWN_COMPLETE`.

## Scope

This closes the Phase 45 native Linux SCTP loopback evidence gate for stream id, PPID, receive metadata, reconnect observation, metrics, graceful shutdown, and retained PCAP/log/trace/report/digest artifacts.

It does not replace the independent external peer interoperability evidence gate.
