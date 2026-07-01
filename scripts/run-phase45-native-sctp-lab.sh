#!/usr/bin/env bash
set -euo pipefail

RUN_ID="${SIGTRAN_RUN_ID:-phase45-native-sctp-$(date -u +%Y%m%dT%H%M%SZ)}"
ARTIFACT_ROOT="${SIGTRAN_ARTIFACT_ROOT:-$(pwd)/artifacts/${RUN_ID}}"
LOCAL_IP="${SIGTRAN_LOCAL_IP:-127.0.0.1}"
LOCAL_PORT="${SIGTRAN_LOCAL_PORT:-2905}"
REMOTE_IP="${SIGTRAN_REMOTE_IP:-127.0.0.1}"
REMOTE_PORT="${SIGTRAN_REMOTE_PORT:-2906}"
STREAM_ID="${SIGTRAN_STREAM_ID:-1}"
PAYLOAD_PROTOCOL_IDENTIFIER="${SIGTRAN_PPID:-3}"
SERVER_START_DELAY_MS="${SIGTRAN_SERVER_START_DELAY_MS:-900}"

PCAP_DIR="${ARTIFACT_ROOT}/pcap"
LOG_DIR="${ARTIFACT_ROOT}/logs"
TRACE_DIR="${ARTIFACT_ROOT}/trace"
COMPARISON_DIR="${ARTIFACT_ROOT}/comparison"
REPORT_DIR="${ARTIFACT_ROOT}/reports"
DIGEST_DIR="${ARTIFACT_ROOT}/digests"
PUBLISH_DIR="${SIGTRAN_PUBLISH_DIR:-${ARTIFACT_ROOT}/publish}"
LAB_BINARY="${SIGTRAN_LAB_BINARY:-}"

mkdir -p "$PCAP_DIR" "$LOG_DIR" "$TRACE_DIR" "$COMPARISON_DIR" "$REPORT_DIR" "$DIGEST_DIR" "$PUBLISH_DIR"

PCAP_FILE="${PCAP_DIR}/${RUN_ID}.pcap"
CAPTURE_LOG="${LOG_DIR}/${RUN_ID}-tcpdump.log"
LAB_LOG="${LOG_DIR}/${RUN_ID}-lab.log"
TRACE_FILE="${TRACE_DIR}/${RUN_ID}.jsonl"
TSHARK_DECODE="${COMPARISON_DIR}/${RUN_ID}-tshark.txt"
TSHARK_FIELDS="${COMPARISON_DIR}/${RUN_ID}-sctp-fields.tsv"
REPORT_FILE="${REPORT_DIR}/${RUN_ID}-report.md"
DIGEST_FILE="${DIGEST_DIR}/${RUN_ID}-sha256.txt"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 2
  fi
}

require_command tcpdump
require_command tshark
require_command python3

if ! ldconfig -p 2>/dev/null | grep -q 'libsctp\.so\.1'; then
  echo "Missing libsctp.so.1. Install lksctp-tools or libsctp1 before running this lab." >&2
  exit 2
fi

sudo modprobe sctp 2>/dev/null || true

if [[ -z "$LAB_BINARY" ]]; then
  LAB_BINARY="${PUBLISH_DIR}/Sigtran.NET.NativeSctpLab"
fi

if [[ -x "$LAB_BINARY" ]]; then
  echo "Using existing lab binary: $LAB_BINARY" > "${LOG_DIR}/${RUN_ID}-publish.log"
else
  require_command dotnet
  dotnet publish src/Sigtran.NET.NativeSctpLab/Sigtran.NET.NativeSctpLab.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o "$PUBLISH_DIR" \
    > "${LOG_DIR}/${RUN_ID}-publish.log"
fi

cleanup() {
  if [[ -n "${CAPTURE_PID:-}" ]] && kill -0 "$CAPTURE_PID" >/dev/null 2>&1; then
    sudo kill -INT "$CAPTURE_PID" >/dev/null 2>&1 || true
    wait "$CAPTURE_PID" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

sudo tcpdump -i lo -w "$PCAP_FILE" "sctp or port ${LOCAL_PORT} or port ${REMOTE_PORT}" >"$CAPTURE_LOG" 2>&1 &
CAPTURE_PID=$!
sleep 1

"$LAB_BINARY" \
  --mode loopback \
  --local-ip "$LOCAL_IP" \
  --local-port "$LOCAL_PORT" \
  --remote-ip "$REMOTE_IP" \
  --remote-port "$REMOTE_PORT" \
  --trace "$TRACE_FILE" \
  --run-id "$RUN_ID" \
  --timeout-seconds 45 \
  --stream-id "$STREAM_ID" \
  --ppid "$PAYLOAD_PROTOCOL_IDENTIFIER" \
  --validate-reconnect true \
  --server-start-delay-ms "$SERVER_START_DELAY_MS" \
  2>&1 | tee "$LAB_LOG"

sleep 1
cleanup
trap - EXIT

tshark -r "$PCAP_FILE" -Y sctp > "$TSHARK_DECODE"

if tshark -G fields 2>/dev/null | grep -q $'\tsctp.stream_id\t'; then
  tshark -r "$PCAP_FILE" -Y sctp -T fields \
    -e frame.number \
    -e sctp.srcport \
    -e sctp.dstport \
    -e sctp.stream_id \
    -e sctp.data_payload_proto_id \
    > "$TSHARK_FIELDS" 2>/dev/null || true
fi

python3 - "$RUN_ID" "$TRACE_FILE" "$PCAP_FILE" "$REPORT_FILE" "$STREAM_ID" "$PAYLOAD_PROTOCOL_IDENTIFIER" "$ARTIFACT_ROOT" <<'PY'
import json
import pathlib
import sys

run_id, trace_path, pcap_path, report_path, stream_id, ppid, root = sys.argv[1:]
trace = pathlib.Path(trace_path)
records = []
with trace.open("r", encoding="utf-8") as handle:
    for line in handle:
        if line.strip():
            records.append(json.loads(line))

messages = [r for r in records if r.get("kind") == "m3ua-message"]
status = [r for r in records if r.get("kind") == "status"]
metrics = [r for r in records if r.get("kind") == "sctp-metrics"]
metadata_ok = all(m.get("metadata", {}).get("valid") is True for m in messages)
stream_ok = all(str(m.get("metadata", {}).get("streamId")) == str(stream_id) for m in messages)
ppid_ok = all(str(m.get("metadata", {}).get("ppid")) == str(ppid) for m in messages)
attempts = [s for s in status if s.get("status") == "client-connect-attempt"]
failed_attempts = [a for a in attempts if "successful=False" in a.get("detail", "")]
successful_attempts = [a for a in attempts if "successful=True" in a.get("detail", "")]
shutdowns = [s for s in status if s.get("status") in ("client-shutdown", "server-shutdown")]

ready = bool(messages) and metadata_ok and stream_ok and ppid_ok and failed_attempts and successful_attempts and len(shutdowns) >= 2

report = [
    f"# Native SCTP Production Transport Evidence - {run_id}",
    "",
    f"- artifact root: `{root}`",
    f"- pcap: `{pcap_path}`",
    f"- trace: `{trace_path}`",
    f"- expected stream id: `{stream_id}`",
    f"- expected PPID: `{ppid}`",
    f"- messages: `{len(messages)}`",
    f"- metadata validation: `{metadata_ok}`",
    f"- stream validation: `{stream_ok}`",
    f"- PPID validation: `{ppid_ok}`",
    f"- reconnect failed attempts observed: `{len(failed_attempts)}`",
    f"- reconnect successful attempts observed: `{len(successful_attempts)}`",
    f"- shutdown events observed: `{len(shutdowns)}`",
    f"- metrics snapshots: `{len(metrics)}`",
    f"- production transport sample result: `{'PASS' if ready else 'FAIL'}`",
    "",
]
pathlib.Path(report_path).write_text("\n".join(report), encoding="utf-8")
if not ready:
    sys.exit(3)
PY

find "$ARTIFACT_ROOT" -type f \
  ! -path "${DIGEST_FILE}" \
  -print0 | sort -z | xargs -0 sha256sum > "$DIGEST_FILE"

echo "Phase 45 native SCTP lab complete"
echo "Report: $REPORT_FILE"
echo "Digests: $DIGEST_FILE"
