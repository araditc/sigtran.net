#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_ID="${SIGTRAN_RUN_ID:-phase56-m2pa-$(date -u +%Y%m%dT%H%M%SZ)}"
ARTIFACT_BASE="${SIGTRAN_ARTIFACT_ROOT:-${REPOSITORY_ROOT}/artifacts}"
ARTIFACT_ROOT="${ARTIFACT_BASE}/m2pa/${RUN_ID}"
BIND_IP="${M2PA_BIND_IP:-127.0.0.1}"
PORT="${M2PA_PORT:-2907}"
EXPECTED_DATA="${M2PA_EXPECTED_DATA:-2}"
CAPTURE_INTERFACE="${SIGTRAN_CAPTURE_INTERFACE:-lo}"

BUILD_DIR="${ARTIFACT_ROOT}/build"
LOG_DIR="${ARTIFACT_ROOT}/logs"
TRACE_DIR="${ARTIFACT_ROOT}/trace"
PCAP_DIR="${ARTIFACT_ROOT}/pcap"
REPORT_DIR="${ARTIFACT_ROOT}/reports"
PEER_BINARY="${BUILD_DIR}/m2pa-reference-peer"
PEER_LOG="${LOG_DIR}/peer.log"
SDK_LOG="${LOG_DIR}/sdk.log"
SDK_TRACE="${TRACE_DIR}/sdk.jsonl"
SDK_RESULT="${REPORT_DIR}/sdk-result.json"
PCAP_FILE="${PCAP_DIR}/m2pa.pcap"
TSHARK_FILE="${REPORT_DIR}/sctp-fields.tsv"
SUMMARY_FILE="${REPORT_DIR}/summary.json"
REPORT_FILE="${REPORT_DIR}/report.md"
DIGEST_FILE="${REPORT_DIR}/sha256.txt"
TCPDUMP_LOG="${LOG_DIR}/tcpdump.log"

mkdir -p "${BUILD_DIR}" "${LOG_DIR}" "${TRACE_DIR}" "${PCAP_DIR}" "${REPORT_DIR}"

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Required command is missing: $1" >&2
    exit 2
  }
}

for command_name in dotnet gcc tcpdump tshark sha256sum timeout python3; do
  require_command "${command_name}"
done

gcc   -std=c11   -O2   -Wall   -Wextra   -Werror   "${REPOSITORY_ROOT}/tools/interop-peer/m2pa_reference_peer.c"   -lsctp   -o "${PEER_BINARY}"

dotnet build   "${REPOSITORY_ROOT}/src/Sigtran.NET.M2paInteropLab/Sigtran.NET.M2paInteropLab.csproj"   -c Release

CAPTURE_PID=""
PEER_PID=""

cleanup() {
  local exit_code=$?
  if [[ -n "${PEER_PID}" ]] && kill -0 "${PEER_PID}" 2>/dev/null; then
    kill -TERM "${PEER_PID}" 2>/dev/null || true
    wait "${PEER_PID}" 2>/dev/null || true
  fi
  if [[ -n "${CAPTURE_PID}" ]] && sudo kill -0 "${CAPTURE_PID}" 2>/dev/null; then
    sudo kill -INT "${CAPTURE_PID}" 2>/dev/null || true
    wait "${CAPTURE_PID}" 2>/dev/null || true
  fi
  return "${exit_code}"
}
trap cleanup EXIT INT TERM

sudo tcpdump   -i "${CAPTURE_INTERFACE}"   --immediate-mode   -U   -w "${PCAP_FILE}"   "sctp and port ${PORT}"   >"${TCPDUMP_LOG}" 2>&1 &
CAPTURE_PID=$!
sleep 0.5

"${PEER_BINARY}" "${BIND_IP}" "${PORT}" "${EXPECTED_DATA}" >"${PEER_LOG}" 2>&1 &
PEER_PID=$!

for _ in {1..100}; do
  if grep -q 'event=listening' "${PEER_LOG}" 2>/dev/null; then
    break
  fi
  if ! kill -0 "${PEER_PID}" 2>/dev/null; then
    cat "${PEER_LOG}" >&2
    exit 1
  fi
  sleep 0.05
done

set +e
timeout 45s dotnet run   --project "${REPOSITORY_ROOT}/src/Sigtran.NET.M2paInteropLab/Sigtran.NET.M2paInteropLab.csproj"   -c Release   --no-build   --   --remote-ip "${BIND_IP}"   --remote-port "${PORT}"   --run-id "${RUN_ID}"   --trace "${SDK_TRACE}"   --result "${SDK_RESULT}"   --timeout-seconds 30   >"${SDK_LOG}" 2>&1
SDK_EXIT=$?
set -e

set +e
wait "${PEER_PID}"
PEER_EXIT=$?
PEER_PID=""
set -e

sleep 0.5
sudo kill -INT "${CAPTURE_PID}" 2>/dev/null || true
wait "${CAPTURE_PID}" 2>/dev/null || true
CAPTURE_PID=""
sudo chown "$(id -u):$(id -g)" "${PCAP_FILE}" "${TCPDUMP_LOG}" 2>/dev/null || true

tshark   -r "${PCAP_FILE}"   -Y sctp   -T fields   -E header=y   -E separator=$'\t'   -e frame.number   -e frame.time_epoch   -e ip.src   -e ip.dst   -e sctp.srcport   -e sctp.dstport   >"${TSHARK_FILE}" 2>"${LOG_DIR}/tshark.log"

SCTP_PACKETS="$(tshark -r "${PCAP_FILE}" -Y sctp -T fields -e frame.number 2>/dev/null | wc -l)"
PEER_PASSED=false
SDK_PASSED=false

if grep -q 'event=complete .*passed=true' "${PEER_LOG}"; then
  PEER_PASSED=true
fi

if python3 - "${SDK_RESULT}" <<'PY'
import json
import sys
with open(sys.argv[1], encoding="utf-8") as stream:
    value = json.load(stream)
raise SystemExit(0 if value.get("passed") is True else 1)
PY
then
  SDK_PASSED=true
fi

RUN_PASSED=false
if [[ "${SDK_EXIT}" -eq 0    && "${PEER_EXIT}" -eq 0    && "${SDK_PASSED}" == true    && "${PEER_PASSED}" == true    && "${SCTP_PACKETS}" -gt 0 ]]; then
  RUN_PASSED=true
fi

python3 -   "${SUMMARY_FILE}"   "${RUN_ID}"   "${SDK_EXIT}"   "${PEER_EXIT}"   "${SDK_PASSED}"   "${PEER_PASSED}"   "${SCTP_PACKETS}"   "${RUN_PASSED}" <<'PY'
import json
import sys
from datetime import datetime, timezone

path, run_id, sdk_exit, peer_exit, sdk_passed, peer_passed, packets, run_passed = sys.argv[1:]
value = {
    "schemaVersion": 1,
    "runId": run_id,
    "observedAtUtc": datetime.now(timezone.utc).isoformat(),
    "protocol": "RFC 4165 M2PA over native Linux SCTP",
    "peerImplementation": "independent C/lksctp reference peer",
    "sdkExitCode": int(sdk_exit),
    "peerExitCode": int(peer_exit),
    "sdkPassed": sdk_passed == "true",
    "peerPassed": peer_passed == "true",
    "sctpPacketCount": int(packets),
    "passed": run_passed == "true",
}
with open(path, "w", encoding="utf-8") as stream:
    json.dump(value, stream, indent=2)
    stream.write("\n")
PY

cp "${PEER_LOG}" "${REPORT_DIR}/peer-events.txt"
cp "${SDK_LOG}" "${REPORT_DIR}/sdk-output.txt"

python3 - \
  "${REPORT_FILE}" \
  "${RUN_ID}" \
  "${SDK_EXIT}" \
  "${PEER_EXIT}" \
  "${SDK_PASSED}" \
  "${PEER_PASSED}" \
  "${SCTP_PACKETS}" \
  "${RUN_PASSED}" <<'PY'
import sys

path, run_id, sdk_exit, peer_exit, sdk_passed, peer_passed, packets, run_passed = sys.argv[1:]
text = f"""# Phase 56 Independent M2PA Interoperability Run

- Run ID: `{run_id}`
- Protocol: RFC 4165 M2PA over native Linux SCTP
- SDK implementation: Sigtran.NET `M2paLink`
- Peer implementation: independent C/lksctp reference peer
- SCTP PPID: 5
- Link-status stream: 0
- User-data stream: 1
- SDK exit: `{sdk_exit}`
- Peer exit: `{peer_exit}`
- SDK validation: `{sdk_passed}`
- Peer validation: `{peer_passed}`
- SCTP packets captured: `{packets}`
- Result: **{run_passed}**

## Exercised behavior

- Out-of-Service / Alignment / Proving / Ready handshake
- ordered SCTP metadata with PPID 5
- 24-bit M2PA BSN/FSN sequencing
- acknowledgement-only User Data
- two bidirectional User Data round trips
- Busy / Busy Ended signaling
- Processor Outage / Processor Recovered / Ready recovery handshake
- retrieval depth returning to zero
- PCAP, independent peer log, SDK trace, result summary, and SHA-256 retention
"""
with open(path, "w", encoding="utf-8") as stream:
    stream.write(text)
PY

(
  cd "${ARTIFACT_ROOT}"
  find pcap trace reports \
    -type f \
    ! -path "reports/sha256.txt" \
    -print0 \
    | sort -z \
    | xargs -0 sha256sum
) >"${DIGEST_FILE}"

echo "M2PA evidence root: ${ARTIFACT_ROOT}"
echo "M2PA evidence result: ${RUN_PASSED}"

if [[ "${RUN_PASSED}" != true ]]; then
  echo "--- SDK LOG ---" >&2
  cat "${SDK_LOG}" >&2 || true
  echo "--- PEER LOG ---" >&2
  cat "${PEER_LOG}" >&2 || true
  exit 1
fi
