#!/usr/bin/env bash
set -euo pipefail
umask 077

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_ID="${SIGTRAN_RUN_ID:-end-to-end-$(date -u +%Y%m%dT%H%M%SZ)}"
ARTIFACT_BASE="${SIGTRAN_ARTIFACT_ROOT:-${HOME}/sigtran-lab/artifacts}"
ARTIFACT_ROOT="${ARTIFACT_BASE}/end-to-end/${RUN_ID}"
LOCAL_IP="${LOCAL_IP:-127.0.0.1}"
LOCAL_SCTP_PORT="${LOCAL_SCTP_PORT:-2905}"
REMOTE_IP="${REMOTE_IP:-127.0.0.1}"
REMOTE_SCTP_PORT="${REMOTE_SCTP_PORT:-2906}"
LOCAL_POINT_CODE="${OPC:-1}"
REMOTE_POINT_CODE="${DPC:-2}"
NETWORK_INDICATOR="${NETWORK_INDICATOR:-2}"
PEER_NAME="${PEER_NAME:-independent-c-reference-peer}"
CAPTURE_BACKEND="${SIGTRAN_CAPTURE_BACKEND:-tcpdump}"
VERIFY_WIRE_PROFILE="${SIGTRAN_VERIFY_WIRE_PROFILE:-false}"

[[ "${RUN_ID}" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$ ]] || { echo "Invalid lab run ID." >&2; exit 2; }
[[ "${REMOTE_SCTP_PORT}" =~ ^[1-9][0-9]{0,4}$ ]] && (( REMOTE_SCTP_PORT <= 65535 )) || {
    echo "Invalid SCTP port." >&2; exit 2;
}

CONFIG_DIR="${ARTIFACT_ROOT}/config"
PCAP_DIR="${ARTIFACT_ROOT}/pcap"
LOG_DIR="${ARTIFACT_ROOT}/logs"
TRACE_DIR="${ARTIFACT_ROOT}/trace"
COMPARISON_DIR="${ARTIFACT_ROOT}/comparison"
REPORT_DIR="${ARTIFACT_ROOT}/reports"
BUILD_DIR="${ARTIFACT_ROOT}/build"
SUMMARY_FILE="${REPORT_DIR}/${RUN_ID}-summary.json"
TRACE_FILE="${TRACE_DIR}/${RUN_ID}-sdk.jsonl"
PEER_LOG="${LOG_DIR}/${RUN_ID}-peer.log"
SDK_LOG="${LOG_DIR}/${RUN_ID}-sdk.log"
CAPTURE_LOG="${LOG_DIR}/${RUN_ID}-${CAPTURE_BACKEND}.log"
PCAP_FILE="${PCAP_DIR}/${RUN_ID}.pcap"
TSHARK_FILE="${COMPARISON_DIR}/${RUN_ID}-tshark.tsv"
COMPARISON_FILE="${COMPARISON_DIR}/${RUN_ID}-comparison.txt"
REPORT_FILE="${REPORT_DIR}/${RUN_ID}-report.md"
DIGEST_FILE="${REPORT_DIR}/${RUN_ID}-sha256.txt"
WIRE_PROFILE_FILE="${REPORT_DIR}/${RUN_ID}-wire-profile.json"
CONFIG_FILE="${CONFIG_DIR}/${RUN_ID}.env"
PEER_BINARY="${BUILD_DIR}/sigtran-reference-peer"

mkdir -p \
    "${CONFIG_DIR}" \
    "${PCAP_DIR}" \
    "${LOG_DIR}" \
    "${TRACE_DIR}" \
    "${COMPARISON_DIR}" \
    "${REPORT_DIR}" \
    "${BUILD_DIR}"

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command is missing: $1" >&2
        exit 2
    fi
}

for command_name in dotnet gcc sed tshark sha256sum timeout; do
    require_command "${command_name}"
done
if [[ "${VERIFY_WIRE_PROFILE}" == true ]]; then
    require_command python3
fi

case "${CAPTURE_BACKEND}" in
    dumpcap)
        require_command dumpcap
        dumpcap -i lo -p -L >/dev/null
        ;;
    tcpdump)
        require_command tcpdump
        if sudo -n true >/dev/null 2>&1; then
            SUDO=(sudo -n)
        elif [[ -n "${SIGTRAN_SUDO_PASSWORD:-}" ]]; then
            printf '%s\n' "${SIGTRAN_SUDO_PASSWORD}" | sudo -S -v >/dev/null
            SUDO=(sudo)
        else
            echo "Configure dumpcap access or authorize tcpdump capture." >&2
            exit 2
        fi
        ;;
    *) echo "Unknown capture backend." >&2; exit 2 ;;
esac

cat > "${CONFIG_FILE}" <<EOF
RUN_ID=${RUN_ID}
PEER_NAME=${PEER_NAME}
LOCAL_IP=${LOCAL_IP}
LOCAL_SCTP_PORT=${LOCAL_SCTP_PORT}
REMOTE_IP=${REMOTE_IP}
REMOTE_SCTP_PORT=${REMOTE_SCTP_PORT}
SIGTRAN_ADAPTATION=M3UA
NETWORK_INDICATOR=${NETWORK_INDICATOR}
SERVICE_INDICATOR=3
OPC=${LOCAL_POINT_CODE}
DPC=${REMOTE_POINT_CODE}
TRAFFIC_MODE=loadshare
ARTIFACT_ROOT=${ARTIFACT_ROOT}
EOF

gcc \
    -std=c11 \
    -O2 \
    -Wall \
    -Wextra \
    -Werror \
    "${REPOSITORY_ROOT}/tools/interop-peer/sigtran_reference_peer.c" \
    -lsctp \
    -o "${PEER_BINARY}" \
    2>&1 | tee "${LOG_DIR}/${RUN_ID}-peer-build.log"

dotnet build \
    "${REPOSITORY_ROOT}/src/Sigtran.NET.EndToEndLab/Sigtran.NET.EndToEndLab.csproj" \
    -c Release \
    -m:1 \
    2>&1 | tee "${LOG_DIR}/${RUN_ID}-sdk-build.log"

CAPTURE_LAUNCH_PID=""
CAPTURE_PID=""
CAPTURE_PID_FILE="${BUILD_DIR}/tcpdump.pid"
PEER_PID=""

stop_capture() {
    if [[ -n "${CAPTURE_PID}" ]]; then
        if [[ "${CAPTURE_BACKEND}" == dumpcap ]]; then
            kill -INT "${CAPTURE_PID}" 2>/dev/null || true
        else
            "${SUDO[@]}" kill -INT "${CAPTURE_PID}" 2>/dev/null || true
        fi
    fi
    if [[ -n "${CAPTURE_LAUNCH_PID}" ]]; then
        wait "${CAPTURE_LAUNCH_PID}" 2>/dev/null || true
    fi
    CAPTURE_LAUNCH_PID=""
    CAPTURE_PID=""
}

cleanup() {
    local exit_code=$?
    if [[ -n "${PEER_PID}" ]] && kill -0 "${PEER_PID}" 2>/dev/null; then
        kill "${PEER_PID}" 2>/dev/null || true
        wait "${PEER_PID}" 2>/dev/null || true
    fi
    stop_capture
    return "${exit_code}"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

capture_filter="sctp and port ${REMOTE_SCTP_PORT}"
if [[ "${CAPTURE_BACKEND}" == dumpcap ]]; then
    dumpcap -q -p -P -i lo -a duration:120 -a filesize:8192 -w "${PCAP_FILE}" \
        -f "${capture_filter}" >"${CAPTURE_LOG}" 2>&1 &
    CAPTURE_LAUNCH_PID=$!
    CAPTURE_PID="${CAPTURE_LAUNCH_PID}"
else
    "${SUDO[@]}" sh -c 'echo "$$" > "$1"; shift; exec tcpdump "$@"' \
        sh "${CAPTURE_PID_FILE}" -i lo --immediate-mode -U -w "${PCAP_FILE}" \
        "${capture_filter}" >"${CAPTURE_LOG}" 2>&1 &
    CAPTURE_LAUNCH_PID=$!
fi
for _ in {1..50}; do
    if [[ "${CAPTURE_BACKEND}" == tcpdump && -s "${CAPTURE_PID_FILE}" ]]; then
        CAPTURE_PID="$(cat "${CAPTURE_PID_FILE}")"
    fi
    if [[ -n "${CAPTURE_PID}" && -s "${PCAP_FILE}" ]]; then
        break
    fi
    kill -0 "${CAPTURE_LAUNCH_PID}" 2>/dev/null || break
    sleep 0.1
done
if [[ -z "${CAPTURE_PID}" || ! -s "${PCAP_FILE}" ]] || ! kill -0 "${CAPTURE_LAUNCH_PID}" 2>/dev/null; then
    echo "Packet capture did not start successfully." >&2
    exit 2
fi

timeout 60s "${PEER_BINARY}" "${REMOTE_IP}" "${REMOTE_SCTP_PORT}" \
    >"${PEER_LOG}" 2>&1 &
PEER_PID=$!
sleep 1
if ! kill -0 "${PEER_PID}" 2>/dev/null; then
    echo "The lab peer failed to start; see ${PEER_LOG}." >&2
    exit 2
fi

set +e
timeout 60s dotnet run \
    --project "${REPOSITORY_ROOT}/src/Sigtran.NET.EndToEndLab/Sigtran.NET.EndToEndLab.csproj" \
    -c Release \
    --no-build \
    -- \
    --remote-ip "${REMOTE_IP}" \
    --remote-port "${REMOTE_SCTP_PORT}" \
    --local-point-code "${LOCAL_POINT_CODE}" \
    --remote-point-code "${REMOTE_POINT_CODE}" \
    --network-indicator "${NETWORK_INDICATOR}" \
    --peer-name "${PEER_NAME}" \
    --trace "${TRACE_FILE}" \
    --summary "${SUMMARY_FILE}" \
    --run-id "${RUN_ID}" \
    --timeout-seconds 45 \
    >"${SDK_LOG}" 2>&1
SDK_EXIT=$?
wait "${PEER_PID}"
PEER_EXIT=$?
PEER_PID=""
set -e

# Allow the capture process to drain packets already delivered by the kernel.
sleep 1
stop_capture
if [[ "${CAPTURE_BACKEND}" == tcpdump ]]; then
    "${SUDO[@]}" chown "$(id -u):$(id -g)" "${PCAP_FILE}" "${CAPTURE_LOG}" 2>/dev/null || true
fi

tshark \
    -r "${PCAP_FILE}" \
    -d "sctp.port==${REMOTE_SCTP_PORT},m3ua" \
    -T fields \
    -E header=y \
    -E separator=$'\t' \
    -e frame.number \
    -e frame.time_epoch \
    -e ip.src \
    -e ip.dst \
    -e sctp.srcport \
    -e sctp.dstport \
    -e m3ua.message_class \
    -e m3ua.message_type \
    -e m3ua.protocol_data_opc \
    -e m3ua.protocol_data_dpc \
    -e m3ua.protocol_data_si \
    2>"${LOG_DIR}/${RUN_ID}-tshark.log" \
    | sed 's/[[:space:]]*$//' \
    >"${TSHARK_FILE}"

SDK_OPERATION_COUNT="$(grep -c '"Layer":"map"' "${TRACE_FILE}" || true)"
PEER_INVOKE_COUNT="$(grep -c 'event=map-invoke' "${PEER_LOG}" || true)"
PEER_RESULT_COUNT="$(grep -c 'event=map-result' "${PEER_LOG}" || true)"
PCAP_PACKET_COUNT="$(tshark -r "${PCAP_FILE}" -Y sctp -T fields -e frame.number 2>/dev/null | wc -l)"
M3UA_DATA_COUNT="$(
    tshark \
        -r "${PCAP_FILE}" \
        -d "sctp.port==${REMOTE_SCTP_PORT},m3ua" \
        -Y 'm3ua.message_class == 1 && m3ua.message_type == 1' \
        -T fields \
        -e frame.number \
        2>/dev/null | wc -l
)"

PASSED=false
if [[ "${SDK_EXIT}" -eq 0 \
    && "${PEER_EXIT}" -eq 0 \
    && "${SDK_OPERATION_COUNT}" -eq 5 \
    && "${PEER_INVOKE_COUNT}" -eq 5 \
    && "${PEER_RESULT_COUNT}" -eq 5 \
    && "${M3UA_DATA_COUNT}" -ge 10 ]]; then
    PASSED=true
fi
if [[ "${PASSED}" == true && "${VERIFY_WIRE_PROFILE}" == true ]]; then
    python3 "${REPOSITORY_ROOT}/scripts/verify-wire-profile.py" "${PCAP_FILE}" "${REMOTE_SCTP_PORT}" \
        >"${WIRE_PROFILE_FILE}" || PASSED=false
fi

cat > "${COMPARISON_FILE}" <<EOF
runId=${RUN_ID}
passed=${PASSED}
sdkExitCode=${SDK_EXIT}
peerExitCode=${PEER_EXIT}
sdkOperationCount=${SDK_OPERATION_COUNT}
peerInvokeCount=${PEER_INVOKE_COUNT}
peerResultCount=${PEER_RESULT_COUNT}
pcapSctpPacketCount=${PCAP_PACKET_COUNT}
pcapM3uaDataCount=${M3UA_DATA_COUNT}
expectedOperations=5
expectedM3uaDataMessages=10
EOF

cat > "${REPORT_FILE}" <<EOF
# End-To-End SS7 Traffic Lab

- Run id: \`${RUN_ID}\`
- Completed UTC: \`$(date -u +%Y-%m-%dT%H:%M:%SZ)\`
- Host: \`$(hostname)\`
- Kernel: \`$(uname -r)\`
- Peer: \`${PEER_NAME}\`
- SCTP endpoint: \`${REMOTE_IP}:${REMOTE_SCTP_PORT}\`
- Result: \`${PASSED}\`

## Stack

\`MAP SMS -> TCAP -> SCCP -> M3UA -> native Linux SCTP\`

## Validated Operations

- \`sendRoutingInfoForSM\`
- \`mo-ForwardSM\`
- \`mt-ForwardSM\`
- \`reportSM-DeliveryStatus\`
- \`alertServiceCentre\`

## Counts

| Signal | Observed | Expected |
| --- | ---: | ---: |
| SDK MAP outcomes | ${SDK_OPERATION_COUNT} | 5 |
| Peer MAP invokes | ${PEER_INVOKE_COUNT} | 5 |
| Peer MAP results | ${PEER_RESULT_COUNT} | 5 |
| M3UA DATA messages in PCAP | ${M3UA_DATA_COUNT} | 10 |
| SCTP packets in PCAP | ${PCAP_PACKET_COUNT} | greater than 10 |

## Evidence Scope

The peer is an independent C implementation that links only to Linux lksctp.
It parses and validates SCTP metadata, M3UA Protocol Data, SCCP UDT, TCAP
transactions/components, MAP operation codes, and operation parameter tags.
This run is cross-implementation evidence for the repository profile. Vendor or
operator peer validation remains a separate promotion requirement.
EOF

digest_inputs=(
    "${CONFIG_FILE}" \
    "${PCAP_FILE}" \
    "${PEER_LOG}" \
    "${SDK_LOG}" \
    "${TRACE_FILE}" \
    "${SUMMARY_FILE}" \
    "${TSHARK_FILE}" \
    "${COMPARISON_FILE}" \
    "${REPORT_FILE}"
)
if [[ -s "${WIRE_PROFILE_FILE}" ]]; then
    digest_inputs+=("${WIRE_PROFILE_FILE}")
fi
sha256sum "${digest_inputs[@]}" >"${DIGEST_FILE}"

echo "runId=${RUN_ID}"
echo "artifactRoot=${ARTIFACT_ROOT}"
echo "passed=${PASSED}"

if [[ "${PASSED}" != true ]]; then
    exit 1
fi
