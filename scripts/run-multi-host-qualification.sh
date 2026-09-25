#!/usr/bin/env bash
set -euo pipefail
umask 077

PROFILE="${QUALIFICATION_PROFILE:-smoke}"
FAULT_SCENARIO="${FAULT_SCENARIO:-peer-restart}"
FAULT_DURATION_SECONDS="${FAULT_DURATION_SECONDS:-10}"
PLAN_ONLY="${PLAN_ONLY:-false}"

case "$PROFILE" in
  smoke)   SOAK_SECONDS=900 ;;
  stress)  SOAK_SECONDS=3600 ;;
  soak)    SOAK_SECONDS=21600 ;;
  release) SOAK_SECONDS=86400 ;;
  *) echo "Unsupported qualification profile: $PROFILE" >&2; exit 2 ;;
esac
case "$FAULT_SCENARIO" in
  peer-restart|peer-outage|sctp-partition) ;;
  *) echo "Unsupported fault scenario: $FAULT_SCENARIO" >&2; exit 2 ;;
esac

validated_settings="$(python3 - "$FAULT_DURATION_SECONDS" "${REMOTE_IP:-127.0.0.1}" "${REMOTE_SCTP_PORT:-2906}" <<'PY'
import ipaddress, sys
duration=int(sys.argv[1])
if duration < 1 or duration > 60:
    raise SystemExit("FAULT_DURATION_SECONDS must be between 1 and 60")
ipaddress.ip_address(sys.argv[2])
port=int(sys.argv[3])
if port < 1 or port > 65535:
    raise SystemExit("REMOTE_SCTP_PORT must be between 1 and 65535")

failover_timeout=duration + 45
retry_target=failover_timeout + 5
attempts=0
delay=0.1
delay_budget=0.0
while delay_budget < retry_target:
    attempts += 1
    if attempts > 180:
        raise SystemExit("Reconnect retry budget exceeds bounded maximum")
    delay_budget += delay
    delay=min(delay * 2, 1.0)
attempts=max(30, attempts)
print(duration, failover_timeout, attempts)
PY
)"
read -r FAULT_DURATION_SECONDS failover_timeout_seconds reconnect_max_attempts <<<"$validated_settings"

if [[ "$PLAN_ONLY" == "true" ]]; then
  python3 - "$PROFILE" "$FAULT_SCENARIO" "$SOAK_SECONDS" "$FAULT_DURATION_SECONDS" \
    "$failover_timeout_seconds" "$reconnect_max_attempts" <<'PY'
import json, sys
print(json.dumps({
    "schemaVersion": 1,
    "profile": sys.argv[1],
    "faultScenario": sys.argv[2],
    "soakDurationSeconds": int(sys.argv[3]),
    "faultDurationSeconds": int(sys.argv[4]),
    "failoverTimeoutSeconds": int(sys.argv[5]),
    "reconnectMaxAttempts": int(sys.argv[6]),
    "requiresDistinctHosts": True,
    "rawEvidenceStorage": "protected",
    "stableGatePromotion": False,
}, indent=2))
PY
  exit 0
fi

required_vars=(
  RUN_ID SOURCE_SHA REMOTE_IP REMOTE_SCTP_PORT OPC DPC NETWORK_INDICATOR
  PEER_NAME SDK_HOST_ID PEER_HOST_ID CAPTURE_INTERFACE RAW_EVIDENCE_ROOT
  PEER_SSH_HOST PEER_SSH_USER PEER_SSH_IDENTITY_FILE PEER_SSH_KNOWN_HOSTS_FILE PEER_SERVICE
  PEER_BUILD_METADATA_FILE
)
for name in "${required_vars[@]}"; do
  if [[ -z "${!name:-}" ]]; then
    echo "Required environment variable is missing: $name" >&2
    exit 2
  fi
done

[[ "$RUN_ID" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]{0,120}$ ]] || { echo "Invalid attempt RUN_ID" >&2; exit 2; }
for host_label in "$SDK_HOST_ID" "$PEER_HOST_ID"; do
  [[ "$host_label" =~ ^[A-Za-z0-9][A-Za-z0-9_.:-]{0,120}$ ]] || {
    echo "SDK_HOST_ID and PEER_HOST_ID must be bounded safe labels." >&2
    exit 2
  }
done
[[ "$PEER_SERVICE" =~ ^[A-Za-z0-9][A-Za-z0-9_.@:-]*\.service$ ]] || { echo "PEER_SERVICE must be a service unit name" >&2; exit 2; }
[[ "$PEER_BUILD_METADATA_FILE" =~ ^/[A-Za-z0-9._/-]+$ ]] || { echo "PEER_BUILD_METADATA_FILE must be an absolute safe path" >&2; exit 2; }
case "/${PEER_BUILD_METADATA_FILE#/}/" in *"/../"*|*"/./"*|*"//"*) echo "PEER_BUILD_METADATA_FILE contains an unsafe path segment" >&2; exit 2 ;; esac
if [[ "$SDK_HOST_ID" == "$PEER_HOST_ID" ]]; then
  echo "SDK_HOST_ID and PEER_HOST_ID must identify distinct hosts." >&2
  exit 2
fi
for cmd in dotnet tcpdump tshark sha256sum python3 ssh timeout ip mktemp; do
  command -v "$cmd" >/dev/null || { echo "Required command is missing: $cmd" >&2; exit 2; }
done
sudo -n true >/dev/null

python3 - "$REMOTE_IP" "$RAW_EVIDENCE_ROOT" "$RUN_ID" <<'PY'
import ipaddress, os, stat, sys
from pathlib import Path
address = ipaddress.ip_address(sys.argv[1])
if address.version != 4:
    raise SystemExit("Representative multi-host qualification currently requires an IPv4 data endpoint.")
if address.is_loopback or address.is_unspecified or address.is_multicast or address.is_link_local:
    raise SystemExit("REMOTE_IP must be a non-local representative IPv4 data endpoint.")
root = Path(sys.argv[2])
if not root.is_absolute():
    raise SystemExit("RAW_EVIDENCE_ROOT must be an absolute protected path")
root.mkdir(mode=0o700, parents=True, exist_ok=True)
info = root.lstat()
if not stat.S_ISDIR(info.st_mode) or info.st_uid != os.geteuid() or info.st_mode & 0o077:
    raise SystemExit("RAW_EVIDENCE_ROOT must be an owned private directory (0700)")
target = root / sys.argv[3]
if target.exists() or target.is_symlink():
    raise SystemExit("Attempt evidence already exists; use a distinct run attempt")
PY

remote_route="$(ip route get "$REMOTE_IP" 2>/dev/null | head -n 1)"
if [[ -z "$remote_route" || "$remote_route" == local\ * || "$remote_route" == *" dev lo "* ]]; then
  echo "REMOTE_IP resolves to a local/loopback data path; representative multi-host qualification is not allowed." >&2
  exit 2
fi
if [[ "$FAULT_SCENARIO" == "sctp-partition" ]]; then
  for cmd in iptables systemd-run systemctl; do
    command -v "$cmd" >/dev/null || { echo "sctp-partition requires $cmd on the SDK lab host." >&2; exit 2; }
  done
  sudo -n iptables -m comment -h >/dev/null 2>&1 || { echo "sctp-partition requires the iptables comment match extension." >&2; exit 2; }
fi

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Raw evidence never starts in the persistent/multi-user repository workspace.
# mktemp creates a private directory atomically; all descendants inherit umask 077.
root="$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/sigtran-multihost.XXXXXXXXXX")"
raw="$root/raw"
safe="$root/safe"
persistent="$RAW_EVIDENCE_ROOT/$RUN_ID"
persistent_safe="$persistent/safe"
mkdir -m 700 "$raw" "$safe"
metrics="$raw/metrics.json"
report="$raw/report.md"
trace="$raw/sdk-trace.jsonl"
failover_ready="$raw/failover-ready"
failover_complete="$raw/failover-complete"
recovery_complete="$raw/recovery-complete"
capture_stopped="$raw/capture-stopped"
capture_prefix="$raw/failover.pcap"
capture_limit_mb=64
capture_file_count=4
fault_log="$raw/fault-events.log"
sdk_host="$raw/sdk-host.txt"
peer_host="$raw/peer-host.txt"
peer_addresses="$raw/peer-addresses.txt"
network_path="$raw/network-path.txt"
peer_build="$raw/peer-build.json"
started_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
sdk_pid=""
tcpdump_pid=""
partition_active=false
peer_rollback_armed=false
rollback_unit="sigtran-sctp-rollback-$RUN_ID"
peer_rollback_unit="sigtran-peer-rollback-$RUN_ID"
partition_rule_tag="sigtran-multihost-$RUN_ID"

ssh_peer() {
  ssh -o BatchMode=yes -o IdentitiesOnly=yes -o ConnectTimeout=10 \
    -o ServerAliveInterval=5 -o ServerAliveCountMax=2 \
    -i "$PEER_SSH_IDENTITY_FILE" \
    -o UserKnownHostsFile="$PEER_SSH_KNOWN_HOSTS_FILE" \
    "$PEER_SSH_USER@$PEER_SSH_HOST" "$@"
}

start_capture() {
  [[ "$tcpdump_pid" == "" ]] || { echo "Packet capture is already active." >&2; return 1; }
  sudo -n tcpdump -i "$CAPTURE_INTERFACE" --immediate-mode -U \
    -C "$capture_limit_mb" -W "$capture_file_count" -w "$capture_prefix" \
    "sctp and host $REMOTE_IP and port $REMOTE_SCTP_PORT" >"$raw/tcpdump.log" 2>&1 &
  tcpdump_pid=$!
  sleep 1
  sudo -n kill -0 "$tcpdump_pid"
}

stop_capture() {
  if [[ -n "$tcpdump_pid" ]] && sudo -n kill -0 "$tcpdump_pid" 2>/dev/null; then
    sudo -n kill -INT "$tcpdump_pid" 2>/dev/null || true
    wait "$tcpdump_pid" 2>/dev/null || true
  fi
  tcpdump_pid=""

  local -a captures=()
  while IFS= read -r -d '' file; do
    captures+=("$file")
  done < <(find "$raw" -maxdepth 1 -type f -name 'failover.pcap*' -print0)

  if (( ${#captures[@]} == 0 )); then
    echo "Packet capture produced no PCAP evidence." >&2
    return 1
  fi

  sudo -n chown "$(id -u):$(id -g)" "${captures[@]}" 2>/dev/null || return 1
  chmod 600 "${captures[@]}"
}

persist_protected_evidence() {
  [[ -d "$root" ]] || return 0
  python3 "$script_dir/persist-qualification-evidence.py" "$root" "$persistent"
}

recover_peer() {
  # Only an acknowledged, owned outage rollback may be disarmed here. A lost
  # SSH acknowledgement leaves the peer-side timer alive for independent recovery.
  ssh_peer "sudo -n /bin/bash -s -- recover '$peer_rollback_unit' '$PEER_SERVICE'" \
    <"$script_dir/peer-outage-recovery.sh" || return 1
  peer_rollback_armed=false
}

verify_partition_rule_absent() {
  local chain="$1"
  shift
  local rc
  if sudo -n iptables -C "$chain" "$@" >/dev/null 2>&1; then
    return 1
  else
    rc=$?
  fi
  # 1 means absent. Lock/other errors are not evidence of cleanup.
  [[ "$rc" -eq 1 ]] && return 0
  return "$rc"
}

remove_partition() {
  [[ "$partition_active" == "true" ]] || return 0
  local output_rule=(-p sctp -d "$REMOTE_IP" --dport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP)
  local input_rule=(-p sctp -s "$REMOTE_IP" --sport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP)
  sudo -n iptables -D OUTPUT "${output_rule[@]}" >/dev/null 2>&1 || true
  sudo -n iptables -D INPUT "${input_rule[@]}" >/dev/null 2>&1 || true
  local output_absent=true input_absent=true
  verify_partition_rule_absent OUTPUT "${output_rule[@]}" || output_absent=false
  verify_partition_rule_absent INPUT "${input_rule[@]}" || input_absent=false
  if [[ "$output_absent" != "true" || "$input_absent" != "true" ]]; then
    echo "SCTP partition rollback could not verify both DROP rules absent; independent rollback remains armed." >&2
    return 1
  fi
  partition_active=false
  sudo -n systemctl stop "$rollback_unit.timer" "$rollback_unit.service" >/dev/null 2>&1 || true
  printf '%s scenario=sctp-partition event=removed\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >>"$fault_log"
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  remove_partition || exit_code=1
  if [[ -n "$sdk_pid" ]] && kill -0 "$sdk_pid" 2>/dev/null; then
    kill -TERM "$sdk_pid" 2>/dev/null || true
    wait "$sdk_pid" 2>/dev/null || true
  fi
  if [[ -n "$tcpdump_pid" ]]; then
    stop_capture || exit_code=1
  fi
  if [[ "$peer_rollback_armed" == "true" ]]; then
    recover_peer >/dev/null 2>&1 || exit_code=1
  else
    # Do not cancel a timer whose acknowledgement was lost (or a prior attempt).
    ssh_peer "sudo -n systemctl start '$PEER_SERVICE'" >/dev/null 2>&1 || true
  fi
  if [[ "$exit_code" -ne 0 && -d "$raw" ]]; then
    printf '%s exitCode=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$exit_code" >"$raw/qualification-failure.txt"
  fi
  if ! persist_protected_evidence; then
    echo "Private scratch retained for failed persistence at: $root" >&2
    exit_code=1
  fi
  exit "$exit_code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

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
ssh_peer "sudo -n systemctl start '$PEER_SERVICE'"
ssh_peer "sudo -n systemctl is-active '$PEER_SERVICE'"
ssh_peer "sudo -n cat -- '$PEER_BUILD_METADATA_FILE'" | \
  python3 "$script_dir/normalize-peer-build-metadata.py" >"$peer_build"
ssh_peer "command -v ip >/dev/null && ip -o -4 addr show" >"$peer_addresses"
python3 "$script_dir/verify-peer-endpoint.py" "$REMOTE_IP" "$peer_addresses"
{
  echo "label=$PEER_HOST_ID"
  echo "verifiedRemoteIp=$REMOTE_IP"
  ssh_peer "hostname; uname -r; nproc; free -h | sed -n '2p'; sudo -n systemctl status '$PEER_SERVICE' --no-pager"
} >"$peer_host"
{
  echo "remoteIp=$REMOTE_IP"
  echo "remotePort=$REMOTE_SCTP_PORT"
  echo "localPointCode=$OPC"
  echo "remotePointCode=$DPC"
  echo "networkIndicator=$NETWORK_INDICATOR"
  echo "peerName=$PEER_NAME"
  printf '%s\n' "$remote_route"
  sysctl net.sctp 2>/dev/null || true
} >"$network_path"
sdk_hostname="$(hostname)"
peer_hostname="$(ssh_peer hostname | tr -d '\r')"
if [[ "$sdk_hostname" == "$peer_hostname" ]]; then
  echo "Runtime hostnames are identical; same-host qualification is not allowed." >&2
  exit 2
fi

timeout_seconds=$((SOAK_SECONDS + 1800))
timeout "$((timeout_seconds + 300))s" dotnet run \
  --project src/Sigtran.NET.PerformanceLab/Sigtran.NET.PerformanceLab.csproj \
  -c Release --no-build -- --run-id "$RUN_ID" --artifact-root "$raw" \
  --remote-ip "$REMOTE_IP" --remote-port "$REMOTE_SCTP_PORT" \
  --local-point-code "$OPC" --remote-point-code "$DPC" --network-indicator "$NETWORK_INDICATOR" \
  --peer-name "$PEER_NAME" --warmup-operations 5000 --sustained-operations 100000 \
  --peak-operations 100000 --recovery-operations 10000 --soak-duration-seconds "$SOAK_SECONDS" \
  --latency-sample-capacity 200000 --warmup-concurrency 32 --sustained-concurrency 192 \
  --peak-concurrency 384 --recovery-concurrency 64 --soak-concurrency 192 \
  --timeout-seconds "$timeout_seconds" --failover-timeout-seconds "$failover_timeout_seconds" \
  --reconnect-max-attempts "$reconnect_max_attempts" \
  --metrics "$metrics" --report "$report" --trace "$trace" \
  --failover-ready "$failover_ready" --failover-complete "$failover_complete" \
  --recovery-complete "$recovery_complete" --capture-stopped "$capture_stopped" >"$raw/sdk.log" 2>&1 &
sdk_pid=$!
for _ in $(seq 1 7200); do
  [[ -s "$failover_ready" ]] && break
  kill -0 "$sdk_pid" 2>/dev/null || break
  sleep 0.25
done
test -s "$failover_ready"
start_capture
fault_started_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
printf '%s scenario=%s event=starting durationSeconds=%s\n' \
  "$fault_started_utc" "$FAULT_SCENARIO" "$FAULT_DURATION_SECONDS" >>"$fault_log"
case "$FAULT_SCENARIO" in
  peer-restart)
    ssh_peer "sudo -n systemctl restart '$PEER_SERVICE'"
    ssh_peer "sudo -n systemctl is-active '$PEER_SERVICE'"
    ;;
  peer-outage)
    rollback_delay=$((FAULT_DURATION_SECONDS + 60))
    # Arm AND stop on the peer in one command. If arming fails, no stop occurs.
    ssh_peer "sudo -n /bin/bash -s -- outage '$peer_rollback_unit' '$PEER_SERVICE' '$rollback_delay'" \
      <"$script_dir/peer-outage-recovery.sh"
    peer_rollback_armed=true
    sleep "$FAULT_DURATION_SECONDS"
    recover_peer
    ;;
  sctp-partition)
    rollback_delay=$((FAULT_DURATION_SECONDS + 60))
    iptables_path="$(command -v iptables)"
    rollback_command="$(cat <<EOF
while true; do
  "$iptables_path" -D OUTPUT -p sctp -d "$REMOTE_IP" --dport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP >/dev/null 2>&1 || true
  "$iptables_path" -D INPUT -p sctp -s "$REMOTE_IP" --sport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP >/dev/null 2>&1 || true
  output_rc=0
  "$iptables_path" -C OUTPUT -p sctp -d "$REMOTE_IP" --dport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP >/dev/null 2>&1 || output_rc=\$?
  input_rc=0
  "$iptables_path" -C INPUT -p sctp -s "$REMOTE_IP" --sport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP >/dev/null 2>&1 || input_rc=\$?
  if [ "\$output_rc" -eq 1 ] && [ "\$input_rc" -eq 1 ]; then
    exit 0
  fi
  sleep 1
done
EOF
)"
    sudo -n systemd-run --quiet --unit "$rollback_unit" --on-active="${rollback_delay}s" /bin/bash -c "$rollback_command"
    partition_active=true
    sudo -n iptables -I OUTPUT 1 -p sctp -d "$REMOTE_IP" --dport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP
    sudo -n iptables -I INPUT 1 -p sctp -s "$REMOTE_IP" --sport "$REMOTE_SCTP_PORT" -m comment --comment "$partition_rule_tag" -j DROP
    sleep "$FAULT_DURATION_SECONDS"
    remove_partition
    ;;
esac
printf '%s scenario=%s event=released\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$FAULT_SCENARIO" >>"$fault_log"
touch "$failover_complete"

# PerformanceLab writes recovery-complete only after the bounded recovery stage has
# finished, then waits for capture-stopped before it is allowed to enter the
# long timed soak. This handshake prevents soak traffic from rotating the fault
# and reconnect packets out of the bounded capture ring.
recovery_marker_timeout_seconds=$((failover_timeout_seconds + 300))
recovery_marker_polls=$((recovery_marker_timeout_seconds * 4))
for _ in $(seq 1 "$recovery_marker_polls"); do
  [[ -s "$recovery_complete" ]] && break
  kill -0 "$sdk_pid" 2>/dev/null || break
  sleep 0.25
done
if [[ ! -s "$recovery_complete" ]]; then
  echo "PerformanceLab did not signal recovery completion within the bounded capture window." >&2
  exit 1
fi
stop_capture
touch "$capture_stopped"

wait "$sdk_pid"
sdk_pid=""
completed_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

python3 - "$metrics" "$safe/summary.json" "$safe/report.md" \
  "$SOAK_SECONDS" "$SDK_HOST_ID" "$PEER_HOST_ID" "$PROFILE" \
  "$FAULT_SCENARIO" "$FAULT_DURATION_SECONDS" "$failover_timeout_seconds" "$reconnect_max_attempts" \
  "$started_utc" "$completed_utc" "$SOURCE_SHA" <<'PY'
import json, sys
from pathlib import Path
metrics_path, summary_path, report_path = map(Path, sys.argv[1:4])
minimum_soak=float(sys.argv[4])
sdk_host=sys.argv[5]
peer_host=sys.argv[6]
profile=sys.argv[7]
fault=sys.argv[8]
fault_duration=int(sys.argv[9])
failover_timeout=int(sys.argv[10])
reconnect_max_attempts=int(sys.argv[11])
started=sys.argv[12]
completed=sys.argv[13]
source_sha=sys.argv[14]
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
    "failoverTimeoutSeconds":failover_timeout,
    "reconnectMaxAttempts":reconnect_max_attempts,
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
    f"- Fault duration seconds: {fault_duration}\n"
    f"- Failover timeout seconds: {failover_timeout}\n"
    f"- Reconnect max attempts: {reconnect_max_attempts}\n"
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

# Raw evidence and the copy-verification manifest never enter the public branch.
(
  cd "$raw"
  find . -type f -print0 | sort -z | xargs -0 sha256sum
) >"$safe/raw-evidence.sha256"
(
  cd "$safe"
  find . -type f ! -name sha256.txt -print0 | sort -z | xargs -0 sha256sum >sha256.txt
)
persist_protected_evidence
trap - EXIT INT TERM
echo "runId=$RUN_ID"
echo "profile=$PROFILE"
echo "faultScenario=$FAULT_SCENARIO"
echo "safeEvidence=$persistent_safe"
