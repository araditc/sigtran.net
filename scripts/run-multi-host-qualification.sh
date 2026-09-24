#!/usr/bin/env bash
set -euo pipefail

PROFILE="${QUALIFICATION_PROFILE:-smoke}"
FAULT_SCENARIO="${FAULT_SCENARIO:-peer-restart}"
FAULT_DURATION_SECONDS="${FAULT_DURATION_SECONDS:-10}"
PLAN_ONLY="${PLAN_ONLY:-false}"

case "$PROFILE" in
  smoke)   SOAK_SECONDS=900 ;;
  stress)  SOAK_SECONDS=3600 ;;
  soak)    SOAK_SECONDS=21600 ;;
  release) SOAK_SECONDS=86400 ;;
  *)
    echo "Unsupported qualification profile: $PROFILE" >&2
    exit 2
    ;;
esac

case "$FAULT_SCENARIO" in
  peer-restart|peer-outage|sctp-partition) ;;
  *)
    echo "Unsupported fault scenario: $FAULT_SCENARIO" >&2
    exit 2
    ;;
esac

python3 - "$FAULT_DURATION_SECONDS" "${REMOTE_IP:-127.0.0.1}" "${REMOTE_SCTP_PORT:-2906}" <<'PY'
import ipaddress, sys
duration=int(sys.argv[1])
if duration < 1 or duration > 60:
    raise SystemExit("FAULT_DURATION_SECONDS must be between 1 and 60")
ipaddress.ip_address(sys.argv[2])
port=int(sys.argv[3])
if port < 1 or port > 65535:
    raise SystemExit("REMOTE_SCTP_PORT must be between 1 and 65535")
PY

if [[ "$PLAN_ONLY" == "true" ]]; then
  python3 - "$PROFILE" "$FAULT_SCENARIO" "$SOAK_SECONDS" "$FAULT_DURATION_SECONDS" <<'PY'
import json, sys
print(json.dumps({
    "schemaVersion": 1,
    "profile": sys.argv[1],
    "faultScenario": sys.argv[2],
    "soakDurationSeconds": int(sys.argv[3]),
    "faultDurationSeconds": int(sys.argv[4]),
    "requiresDistinctHosts": True,
    "rawEvidenceStorage": "protected",
    "stableGatePromotion": False,
}, indent=2))
PY
  exit 0
fi

required_vars=(
  RUN_ID
  SOURCE_SHA
  REMOTE_IP
  REMOTE_SCTP_PORT
  OPC
  DPC
  NETWORK_INDICATOR
  PEER_NAME
  SDK_HOST_ID
  PEER_HOST_ID
  CAPTURE_INTERFACE
  RAW_EVIDENCE_ROOT
  PEER_SSH_HOST
  PEER_SSH_USER
  PEER_SERVICE
)
for name in "${required_vars[@]}"; do
  if [[ -z "${!name:-}" ]]; then
    echo "Required environment variable is missing: $name" >&2
    exit 2
  fi
done

if [[ "$SDK_HOST_ID" == "$PEER_HOST_ID" ]]; then
  echo "SDK_HOST_ID and PEER_HOST_ID must identify distinct hosts." >&2
  exit 2
fi

for cmd in dotnet tcpdump tshark sha256sum python3 ssh timeout; do
  command -v "$cmd" >/dev/null || {
    echo "Required command is missing: $cmd" >&2
    exit 2
  }
done
sudo -n true >/dev/null

if [[ "$FAULT_SCENARIO" == "sctp-partition" ]]; then
  for cmd in iptables systemd-run systemctl; do
    command -v "$cmd" >/dev/null || {
      echo "sctp-partition requires $cmd on the SDK lab host." >&2
      exit 2
    }
  done
fi

workspace="${GITHUB_WORKSPACE:-$(pwd)}"
root="$workspace/artifacts/multihost/$RUN_ID"
raw="$root/raw"
safe="$root/safe"
persistent="$RAW_EVIDENCE_ROOT/$RUN_ID"
persistent_raw="$persistent/raw"
persistent_safe="$persistent/safe"
mkdir -p "$raw" "$safe" "$persistent"
chmod 700 "$persistent"

metrics="$raw/metrics.json"
report="$raw/report.md"
trace="$raw/sdk-trace.jsonl"
failover_ready="$raw/failover-ready"
failover_complete="$raw/failover-complete"
pcap="$raw/traffic.pcap"
fault_log="$raw/fault-events.log"
sdk_host="$raw/sdk-host.txt"
peer_host="$raw/peer-host.txt"
network_path="$raw/network-path.txt"
started_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

sdk_pid=""
tcpdump_pid=""
partition_active=false
rollback_unit="sigtran-sctp-rollback-${RUN_ID//[^a-zA-Z0-9_.-]/-}"

ssh_peer() {
  ssh -o BatchMode=yes "$PEER_SSH_USER@$PEER_SSH_HOST" "$@"
}

remove_partition() {
  if [[ "$partition_active" != "true" ]]; then
    return
  fi

  sudo -n iptables -D OUTPUT -p sctp -d "$REMOTE_IP"     --dport "$REMOTE_SCTP_PORT" -j DROP 2>/dev/null || true
  sudo -n iptables -D INPUT -p sctp -s "$REMOTE_IP"     --sport "$REMOTE_SCTP_PORT" -j DROP 2>/dev/null || true
  partition_active=false
  sudo -n systemctl stop "$rollback_unit.timer" "$rollback_unit.service" >/dev/null 2>&1 || true
  printf '%s scenario=sctp-partition event=removed\n'     "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >>"$fault_log"
}

cleanup() {
  local exit_code=$?
  remove_partition || true
  if [[ -n "$sdk_pid" ]] && kill -0 "$sdk_pid" 2>/dev/null; then
    kill -TERM "$sdk_pid" 2>/dev/null || true
    wait "$sdk_pid" 2>/dev/null || true
  fi
  if [[ -n "$tcpdump_pid" ]] && sudo -n kill -0 "$tcpdump_pid" 2>/dev/null; then
    sudo -n kill -INT "$tcpdump_pid" 2>/dev/null || true
    wait "$tcpdump_pid" 2>/dev/null || true
  fi
  ssh_peer "sudo systemctl start '$PEER_SERVICE'" >/dev/null 2>&1 || true
  exit "$exit_code"
}
trap cleanup EXIT INT TERM

{
  echo "label=$SDK_HOST_ID"
  echo "sourceSha=$SOURCE_SHA"
  echo "hostname=$(hostname)"
  echo "kernel=$(uname -r)"
  echo "distribution=$(grep '^PRETTY_NAME=' /etc/os-release | cut -d= -f2-)"
  echo "processors=$(nproc)"
  echo "memory=$(free -h | sed -n '2p')"
  echo "dotnet=$(dotnet --version)"
} >"$sdk_host"

ssh_peer "hostname; uname -r; nproc; free -h | sed -n '2p'; sudo systemctl status '$PEER_SERVICE' --no-pager"   >"$peer_host"

{
  echo "remoteIp=$REMOTE_IP"
  echo "remotePort=$REMOTE_SCTP_PORT"
  ip route get "$REMOTE_IP" || true
  sysctl net.sctp 2>/dev/null || true
} >"$network_path"

sdk_hostname="$(hostname)"
peer_hostname="$(ssh_peer hostname | tr -d '\r')"
if [[ "$sdk_hostname" == "$peer_hostname" ]]; then
  echo "Runtime hostnames are identical; same-host qualification is not allowed." >&2
  exit 2
fi

ssh_peer "sudo systemctl start '$PEER_SERVICE'"
ssh_peer "sudo systemctl is-active '$PEER_SERVICE'"

sudo -n tcpdump -i "$CAPTURE_INTERFACE" --immediate-mode -U   -w "$pcap" "sctp and host $REMOTE_IP and port $REMOTE_SCTP_PORT"   >"$raw/tcpdump.log" 2>&1 &
tcpdump_pid=$!
sleep 1
sudo -n kill -0 "$tcpdump_pid"

timeout_seconds=$((SOAK_SECONDS + 1800))
timeout "$((timeout_seconds + 300))s" dotnet run   --project src/Sigtran.NET.PerformanceLab/Sigtran.NET.PerformanceLab.csproj   -c Release --no-build --   --run-id "$RUN_ID"   --artifact-root "$raw"   --remote-ip "$REMOTE_IP"   --remote-port "$REMOTE_SCTP_PORT"   --local-point-code "$OPC"   --remote-point-code "$DPC"   --network-indicator "$NETWORK_INDICATOR"   --peer-name "$PEER_NAME"   --warmup-operations 5000   --sustained-operations 100000   --peak-operations 100000   --recovery-operations 10000   --soak-duration-seconds "$SOAK_SECONDS"   --latency-sample-capacity 200000   --warmup-concurrency 32   --sustained-concurrency 192   --peak-concurrency 384   --recovery-concurrency 64   --soak-concurrency 192   --timeout-seconds "$timeout_seconds"   --failover-timeout-seconds 45   --metrics "$metrics"   --report "$report"   --trace "$trace"   --failover-ready "$failover_ready"   --failover-complete "$failover_complete"   >"$raw/sdk.log" 2>&1 &
sdk_pid=$!

for _ in $(seq 1 7200); do
  if [[ -s "$failover_ready" ]]; then
    break
  fi
  if ! kill -0 "$sdk_pid" 2>/dev/null; then
    break
  fi
  sleep 0.25
done
test -s "$failover_ready"

fault_started_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
printf '%s scenario=%s event=starting durationSeconds=%s\n'   "$fault_started_utc" "$FAULT_SCENARIO" "$FAULT_DURATION_SECONDS" >>"$fault_log"

case "$FAULT_SCENARIO" in
  peer-restart)
    ssh_peer "sudo systemctl restart '$PEER_SERVICE'"
    ssh_peer "sudo systemctl is-active '$PEER_SERVICE'"
    ;;
  peer-outage)
    ssh_peer "sudo systemctl stop '$PEER_SERVICE'"
    sleep "$FAULT_DURATION_SECONDS"
    ssh_peer "sudo systemctl start '$PEER_SERVICE'"
    ssh_peer "sudo systemctl is-active '$PEER_SERVICE'"
    ;;
  sctp-partition)
    sudo -n iptables -I OUTPUT 1 -p sctp -d "$REMOTE_IP"       --dport "$REMOTE_SCTP_PORT" -j DROP
    partition_active=true
    sudo -n iptables -I INPUT 1 -p sctp -s "$REMOTE_IP"       --sport "$REMOTE_SCTP_PORT" -j DROP

    rollback_delay=$((FAULT_DURATION_SECONDS + 60))
    sudo -n systemd-run --quiet --unit "$rollback_unit" --on-active="${rollback_delay}s" /bin/sh -c "iptables -D OUTPUT -p sctp -d '$REMOTE_IP' --dport '$REMOTE_SCTP_PORT' -j DROP 2>/dev/null || true; iptables -D INPUT -p sctp -s '$REMOTE_IP' --sport '$REMOTE_SCTP_PORT' -j DROP 2>/dev/null || true"

    sleep "$FAULT_DURATION_SECONDS"
    remove_partition
    ;;
esac

printf '%s scenario=%s event=released\n'   "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$FAULT_SCENARIO" >>"$fault_log"
touch "$failover_complete"

wait "$sdk_pid"
sdk_pid=""

sudo -n kill -INT "$tcpdump_pid" 2>/dev/null || true
wait "$tcpdump_pid" 2>/dev/null || true
tcpdump_pid=""
sudo -n chown "$(id -u):$(id -g)" "$pcap" "$raw/tcpdump.log" 2>/dev/null || true

completed_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

python3 - "$metrics" "$safe/summary.json" "$safe/report.md"   "$SOAK_SECONDS" "$SDK_HOST_ID" "$PEER_HOST_ID" "$PROFILE"   "$FAULT_SCENARIO" "$FAULT_DURATION_SECONDS" "$started_utc" "$completed_utc" "$SOURCE_SHA" <<'PY'
import json, sys
from pathlib import Path

metrics_path, summary_path, report_path = map(Path, sys.argv[1:4])
minimum_soak=float(sys.argv[4])
sdk_host=sys.argv[5]
peer_host=sys.argv[6]
profile=sys.argv[7]
fault=sys.argv[8]
fault_duration=int(sys.argv[9])
started=sys.argv[10]
completed=sys.argv[11]
source_sha=sys.argv[12]
value=json.loads(metrics_path.read_text())
stages={s["Name"]:s for s in value.get("Stages",[])}

def seconds(value):
    if not isinstance(value,str):
        return 0.0
    days=0
    if "." in value and value.split(".",1)[0].isdigit():
        d, value=value.split(".",1)
        days=int(d)
    parts=value.split(":")
    if len(parts)!=3:
        return 0.0
    h,m,s=parts
    return days*86400 + int(h)*3600 + int(m)*60 + float(s)

soak=stages.get("soak",{})
soak_seconds=seconds(soak.get("Duration"))
resilience=value.get("Resilience",{})
passed=(
    value.get("ExecutionPassed") is True
    and soak.get("FailedOperations")==0
    and soak_seconds >= minimum_soak
    and resilience.get("LostOperations")==0
    and resilience.get("ReconnectAttempts",0) > 0
    and sdk_host != peer_host
)
result={
    "schemaVersion":2,
    "runId":value.get("RunId"),
    "sourceSha":source_sha,
    "qualificationProfile":profile,
    "faultScenario":fault,
    "faultDurationSeconds":fault_duration,
    "topology":"representative multi-host",
    "distinctHostsVerified":sdk_host != peer_host,
    "startedUtc":started,
    "completedUtc":completed,
    "executionPassed":value.get("ExecutionPassed"),
    "capacityQualified":value.get("CapacityQualified"),
    "soakDurationSeconds":soak_seconds,
    "minimumSoakSeconds":minimum_soak,
    "soakSuccessfulOperations":soak.get("SuccessfulOperations"),
    "soakThroughputPerSecond":soak.get("ThroughputPerSecond"),
    "soakP95Milliseconds":soak.get("P95Milliseconds"),
    "soakP99Milliseconds":soak.get("P99Milliseconds"),
    "soakMaximumMilliseconds":soak.get("MaximumMilliseconds"),
    "soakFailedOperations":soak.get("FailedOperations"),
    "lostRecoveryOperations":resilience.get("LostOperations"),
    "reconnectAttempts":resilience.get("ReconnectAttempts"),
    "associationRecovery":resilience.get("AssociationRecovery"),
    "trafficRestoration":resilience.get("TrafficRestoration"),
    "passed":passed,
}
summary_path.write_text(json.dumps(result,indent=2)+"\n")
report_path.write_text(
    "# Representative Multi-Host Qualification\n\n"
    f"- Source SHA: {source_sha}\n"
    f"- Profile: {profile}\n"
    f"- Fault scenario: {fault}\n"
    f"- Distinct hosts verified: {sdk_host != peer_host}\n"
    f"- Soak duration seconds: {soak_seconds:.1f}\n"
    f"- Successful soak operations: {result['soakSuccessfulOperations']}\n"
    f"- Soak throughput TPS: {result['soakThroughputPerSecond']}\n"
    f"- Failed soak operations: {result['soakFailedOperations']}\n"
    f"- Lost recovery operations: {result['lostRecoveryOperations']}\n"
    f"- Reconnect attempts: {result['reconnectAttempts']}\n"
    f"- Result: {passed}\n"
)
if not passed:
    raise SystemExit("multi-host qualification failed")
PY

# Raw evidence stays on protected lab storage. The public branch receives only
# sanitized qualification summaries and digest references.
(
  cd "$raw"
  sha256sum traffic.pcap metrics.json report.md sdk-trace.jsonl sdk-host.txt peer-host.txt network-path.txt fault-events.log
) >"$safe/raw-evidence.sha256"
(
  cd "$safe"
  find . -type f ! -name sha256.txt -print0 | sort -z | xargs -0 sha256sum >sha256.txt
)

rm -rf "$persistent_raw" "$persistent_safe"
mkdir -p "$persistent_raw" "$persistent_safe"
chmod 700 "$persistent_raw" "$persistent_safe"
cp -a "$raw/." "$persistent_raw/"
cp -a "$safe/." "$persistent_safe/"
chmod -R go-rwx "$persistent"

trap - EXIT INT TERM
echo "runId=$RUN_ID"
echo "profile=$PROFILE"
echo "faultScenario=$FAULT_SCENARIO"
echo "safeEvidence=$persistent_safe"
