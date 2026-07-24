#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_ID="${SIGTRAN_RUN_ID:-performance-$(date -u +%Y%m%dT%H%M%SZ)}"
ARTIFACT_BASE="${SIGTRAN_ARTIFACT_ROOT:-${HOME}/sigtran-lab/artifacts}"
ARTIFACT_ROOT="${ARTIFACT_BASE}/performance/${RUN_ID}"
REMOTE_IP="${REMOTE_IP:-127.0.0.1}"
REMOTE_SCTP_PORT="${REMOTE_SCTP_PORT:-2906}"
LOCAL_POINT_CODE="${OPC:-1}"
REMOTE_POINT_CODE="${DPC:-2}"
NETWORK_INDICATOR="${NETWORK_INDICATOR:-2}"
PEER_NAME="${PEER_NAME:-independent-c-reference-peer}"
WARMUP_OPERATIONS="${SIGTRAN_WARMUP_OPERATIONS:-500}"
SUSTAINED_OPERATIONS="${SIGTRAN_SUSTAINED_OPERATIONS:-5000}"
PEAK_OPERATIONS="${SIGTRAN_PEAK_OPERATIONS:-5000}"
RECOVERY_OPERATIONS="${SIGTRAN_RECOVERY_OPERATIONS:-500}"
SOAK_OPERATIONS="${SIGTRAN_SOAK_OPERATIONS:-5000}"
WARMUP_CONCURRENCY="${SIGTRAN_WARMUP_CONCURRENCY:-16}"
SUSTAINED_CONCURRENCY="${SIGTRAN_SUSTAINED_CONCURRENCY:-64}"
PEAK_CONCURRENCY="${SIGTRAN_PEAK_CONCURRENCY:-128}"
RECOVERY_CONCURRENCY="${SIGTRAN_RECOVERY_CONCURRENCY:-32}"
SOAK_CONCURRENCY="${SIGTRAN_SOAK_CONCURRENCY:-64}"

CONFIG_DIR="${ARTIFACT_ROOT}/config"
PCAP_DIR="${ARTIFACT_ROOT}/pcap"
LOG_DIR="${ARTIFACT_ROOT}/logs"
TRACE_DIR="${ARTIFACT_ROOT}/trace"
METRICS_DIR="${ARTIFACT_ROOT}/metrics"
COMPARISON_DIR="${ARTIFACT_ROOT}/comparison"
REPORT_DIR="${ARTIFACT_ROOT}/reports"
BUILD_DIR="${ARTIFACT_ROOT}/build"
CONFIG_FILE="${CONFIG_DIR}/${RUN_ID}.env"
PCAP_FILE="${PCAP_DIR}/${RUN_ID}.pcap"
CAPTURE_LOG="${LOG_DIR}/${RUN_ID}-tcpdump.log"
PEER_ONE_LOG="${LOG_DIR}/${RUN_ID}-peer-before-failover.log"
PEER_TWO_LOG="${LOG_DIR}/${RUN_ID}-peer-after-failover.log"
SDK_LOG="${LOG_DIR}/${RUN_ID}-sdk.log"
SDK_BUILD_LOG="${LOG_DIR}/${RUN_ID}-sdk-build.log"
PEER_BUILD_LOG="${LOG_DIR}/${RUN_ID}-peer-build.log"
HOST_LOG="${LOG_DIR}/${RUN_ID}-host.txt"
TRACE_FILE="${TRACE_DIR}/${RUN_ID}-sdk.jsonl"
METRICS_FILE="${METRICS_DIR}/${RUN_ID}-metrics.json"
TSHARK_FILE="${COMPARISON_DIR}/${RUN_ID}-tshark.tsv"
COMPARISON_FILE="${COMPARISON_DIR}/${RUN_ID}-comparison.txt"
REPORT_FILE="${REPORT_DIR}/${RUN_ID}-report.md"
DIGEST_FILE="${REPORT_DIR}/${RUN_ID}-sha256.txt"
FAILOVER_READY="${BUILD_DIR}/failover-ready"
FAILOVER_COMPLETE="${BUILD_DIR}/failover-complete"
PEER_BINARY="${BUILD_DIR}/sigtran-reference-peer"
CAPTURE_PID_FILE="${BUILD_DIR}/tcpdump.pid"

mkdir -p \
    "${CONFIG_DIR}" \
    "${PCAP_DIR}" \
    "${LOG_DIR}" \
    "${TRACE_DIR}" \
    "${METRICS_DIR}" \
    "${COMPARISON_DIR}" \
    "${REPORT_DIR}" \
    "${BUILD_DIR}"

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command is missing: $1" >&2
        exit 2
    fi
}

for command_name in dotnet gcc gzip sed tcpdump tshark sha256sum timeout; do
    require_command "${command_name}"
done

if sudo -n true >/dev/null 2>&1; then
    SUDO=(sudo -n)
elif [[ -n "${SIGTRAN_SUDO_PASSWORD:-}" ]]; then
    printf '%s\n' "${SIGTRAN_SUDO_PASSWORD}" | sudo -S -v >/dev/null
    SUDO=(sudo)
else
    echo "Packet capture requires passwordless sudo or SIGTRAN_SUDO_PASSWORD." >&2
    exit 2
fi

cat >"${CONFIG_FILE}" <<EOF
RUN_ID=${RUN_ID}
PEER_NAME=${PEER_NAME}
REMOTE_IP=${REMOTE_IP}
REMOTE_SCTP_PORT=${REMOTE_SCTP_PORT}
OPC=${LOCAL_POINT_CODE}
DPC=${REMOTE_POINT_CODE}
NETWORK_INDICATOR=${NETWORK_INDICATOR}
WARMUP_OPERATIONS=${WARMUP_OPERATIONS}
SUSTAINED_OPERATIONS=${SUSTAINED_OPERATIONS}
PEAK_OPERATIONS=${PEAK_OPERATIONS}
RECOVERY_OPERATIONS=${RECOVERY_OPERATIONS}
SOAK_OPERATIONS=${SOAK_OPERATIONS}
WARMUP_CONCURRENCY=${WARMUP_CONCURRENCY}
SUSTAINED_CONCURRENCY=${SUSTAINED_CONCURRENCY}
PEAK_CONCURRENCY=${PEAK_CONCURRENCY}
RECOVERY_CONCURRENCY=${RECOVERY_CONCURRENCY}
SOAK_CONCURRENCY=${SOAK_CONCURRENCY}
EOF

{
    echo "hostname=$(hostname)"
    echo "kernel=$(uname -r)"
    echo "distribution=$(grep '^PRETTY_NAME=' /etc/os-release | cut -d= -f2-)"
    echo "processors=$(nproc)"
    echo "memory=$(free -h | sed -n '2p')"
    dotnet --version
} >"${HOST_LOG}"

gcc \
    -std=c11 \
    -O2 \
    -Wall \
    -Wextra \
    -Werror \
    "${REPOSITORY_ROOT}/tools/interop-peer/sigtran_reference_peer.c" \
    -lsctp \
    -o "${PEER_BINARY}" \
    2>&1 | tee "${PEER_BUILD_LOG}"

dotnet build \
    "${REPOSITORY_ROOT}/src/Sigtran.NET.PerformanceLab/Sigtran.NET.PerformanceLab.csproj" \
    -c Release \
    -m:1 \
    2>&1 | tee "${SDK_BUILD_LOG}"

CAPTURE_LAUNCH_PID=""
CAPTURE_PID=""
PEER_ONE_PID=""
PEER_TWO_PID=""
SDK_PID=""

cleanup() {
    local exit_code=$?
    for process_id in "${SDK_PID}" "${PEER_ONE_PID}" "${PEER_TWO_PID}"; do
        if [[ -n "${process_id}" ]] && kill -0 "${process_id}" 2>/dev/null; then
            kill -TERM "${process_id}" 2>/dev/null || true
            wait "${process_id}" 2>/dev/null || true
        fi
    done
    if [[ -n "${CAPTURE_PID}" ]] && "${SUDO[@]}" kill -0 "${CAPTURE_PID}" 2>/dev/null; then
        "${SUDO[@]}" kill -INT "${CAPTURE_PID}" 2>/dev/null || true
    fi
    if [[ -n "${CAPTURE_LAUNCH_PID}" ]]; then
        wait "${CAPTURE_LAUNCH_PID}" 2>/dev/null || true
    fi
    return "${exit_code}"
}
trap cleanup EXIT INT TERM

"${SUDO[@]}" sh -c \
    'echo "$$" > "$1"; shift; exec tcpdump "$@"' \
    sh \
    "${CAPTURE_PID_FILE}" \
    -i lo \
    --immediate-mode \
    -U \
    -w "${PCAP_FILE}" \
    "sctp and port ${REMOTE_SCTP_PORT}" \
    >"${CAPTURE_LOG}" 2>&1 &
CAPTURE_LAUNCH_PID=$!
for _ in {1..50}; do
    if [[ -s "${CAPTURE_PID_FILE}" ]]; then
        CAPTURE_PID="$(cat "${CAPTURE_PID_FILE}")"
        break
    fi
    sleep 0.1
done
if [[ -z "${CAPTURE_PID}" ]] || ! "${SUDO[@]}" kill -0 "${CAPTURE_PID}" 2>/dev/null; then
    echo "tcpdump did not start successfully." >&2
    exit 2
fi

"${PEER_BINARY}" "${REMOTE_IP}" "${REMOTE_SCTP_PORT}" 0 quiet \
    >"${PEER_ONE_LOG}" 2>&1 &
PEER_ONE_PID=$!
sleep 1

set +e
timeout 180s dotnet run \
    --project "${REPOSITORY_ROOT}/src/Sigtran.NET.PerformanceLab/Sigtran.NET.PerformanceLab.csproj" \
    -c Release \
    --no-build \
    -- \
    --run-id "${RUN_ID}" \
    --artifact-root "${ARTIFACT_ROOT}" \
    --remote-ip "${REMOTE_IP}" \
    --remote-port "${REMOTE_SCTP_PORT}" \
    --local-point-code "${LOCAL_POINT_CODE}" \
    --remote-point-code "${REMOTE_POINT_CODE}" \
    --network-indicator "${NETWORK_INDICATOR}" \
    --peer-name "${PEER_NAME}" \
    --warmup-operations "${WARMUP_OPERATIONS}" \
    --sustained-operations "${SUSTAINED_OPERATIONS}" \
    --peak-operations "${PEAK_OPERATIONS}" \
    --recovery-operations "${RECOVERY_OPERATIONS}" \
    --soak-operations "${SOAK_OPERATIONS}" \
    --warmup-concurrency "${WARMUP_CONCURRENCY}" \
    --sustained-concurrency "${SUSTAINED_CONCURRENCY}" \
    --peak-concurrency "${PEAK_CONCURRENCY}" \
    --recovery-concurrency "${RECOVERY_CONCURRENCY}" \
    --soak-concurrency "${SOAK_CONCURRENCY}" \
    --metrics "${METRICS_FILE}" \
    --report "${REPORT_FILE}" \
    --trace "${TRACE_FILE}" \
    --failover-ready "${FAILOVER_READY}" \
    --failover-complete "${FAILOVER_COMPLETE}" \
    >"${SDK_LOG}" 2>&1 &
SDK_PID=$!
set -e

for _ in {1..2400}; do
    if [[ -s "${FAILOVER_READY}" ]]; then
        break
    fi
    if ! kill -0 "${SDK_PID}" 2>/dev/null; then
        break
    fi
    sleep 0.05
done
if [[ ! -s "${FAILOVER_READY}" ]]; then
    wait "${SDK_PID}" || true
    echo "SDK did not request failover." >&2
    exit 1
fi

kill -TERM "${PEER_ONE_PID}" 2>/dev/null || true
wait "${PEER_ONE_PID}" || true
PEER_ONE_PID=""
sleep 0.25

"${PEER_BINARY}" "${REMOTE_IP}" "${REMOTE_SCTP_PORT}" 0 quiet \
    >"${PEER_TWO_LOG}" 2>&1 &
PEER_TWO_PID=$!
for _ in {1..100}; do
    if grep -q 'event=listening' "${PEER_TWO_LOG}" 2>/dev/null; then
        break
    fi
    if ! kill -0 "${PEER_TWO_PID}" 2>/dev/null; then
        cat "${PEER_TWO_LOG}" >&2
        exit 1
    fi
    sleep 0.05
done
touch "${FAILOVER_COMPLETE}"

set +e
wait "${SDK_PID}"
SDK_EXIT=$?
SDK_PID=""
wait "${PEER_TWO_PID}"
PEER_TWO_EXIT=$?
PEER_TWO_PID=""
set -e

sleep 1
"${SUDO[@]}" kill -INT "${CAPTURE_PID}" 2>/dev/null || true
wait "${CAPTURE_LAUNCH_PID}" 2>/dev/null || true
CAPTURE_LAUNCH_PID=""
CAPTURE_PID=""
"${SUDO[@]}" chown "$(id -u):$(id -g)" "${PCAP_FILE}" "${CAPTURE_LOG}" \
    2>/dev/null || true

tshark \
    -r "${PCAP_FILE}" \
    -d "sctp.port==${REMOTE_SCTP_PORT},m3ua" \
    -T fields \
    -E header=y \
    -E separator=$'\t' \
    -e frame.number \
    -e frame.time_epoch \
    -e sctp.srcport \
    -e sctp.dstport \
    -e m3ua.message_class \
    -e m3ua.message_type \
    2>"${LOG_DIR}/${RUN_ID}-tshark.log" \
    | sed 's/[[:space:]]*$//' \
    >"${TSHARK_FILE}"

PCAP_SCTP_PACKETS="$(
    tshark -r "${PCAP_FILE}" -Y sctp -T fields -e frame.number \
        2>/dev/null | wc -l
)"
PCAP_M3UA_DATA="$(
    tshark \
        -r "${PCAP_FILE}" \
        -d "sctp.port==${REMOTE_SCTP_PORT},m3ua" \
        -Y 'm3ua.message_class == 1 && m3ua.message_type == 1' \
        -T fields \
        -e frame.number \
        2>/dev/null | wc -l
)"
EXECUTION_PASSED="$(
    grep -m1 '"ExecutionPassed":' "${METRICS_FILE}" \
        | grep -oE 'true|false' || echo false
)"
CAPACITY_QUALIFIED="$(
    grep -m1 '"CapacityQualified":' "${METRICS_FILE}" \
        | grep -oE 'true|false' || echo false
)"
PEER_ONE_OPERATIONS="$(
    grep 'event=complete' "${PEER_ONE_LOG}" \
        | sed -n 's/.*operations=\([0-9][0-9]*\).*/\1/p' \
        | tail -1
)"
PEER_TWO_OPERATIONS="$(
    grep 'event=complete' "${PEER_TWO_LOG}" \
        | sed -n 's/.*operations=\([0-9][0-9]*\).*/\1/p' \
        | tail -1
)"
PEER_ONE_OPERATIONS="${PEER_ONE_OPERATIONS:-0}"
PEER_TWO_OPERATIONS="${PEER_TWO_OPERATIONS:-0}"

cat >"${COMPARISON_FILE}" <<EOF
runId=${RUN_ID}
executionPassed=${EXECUTION_PASSED}
capacityQualified=${CAPACITY_QUALIFIED}
sdkExitCode=${SDK_EXIT}
peerAfterFailoverExitCode=${PEER_TWO_EXIT}
peerBeforeFailoverOperations=${PEER_ONE_OPERATIONS}
peerAfterFailoverOperations=${PEER_TWO_OPERATIONS}
pcapSctpPacketCount=${PCAP_SCTP_PACKETS}
pcapM3uaDataCount=${PCAP_M3UA_DATA}
EOF

gzip -9 -c "${PCAP_FILE}" >"${PCAP_FILE}.gz"
gzip -9 -c "${TSHARK_FILE}" >"${TSHARK_FILE}.gz"

sha256sum \
    "${CONFIG_FILE}" \
    "${PCAP_FILE}.gz" \
    "${PEER_ONE_LOG}" \
    "${PEER_TWO_LOG}" \
    "${SDK_LOG}" \
    "${HOST_LOG}" \
    "${TRACE_FILE}" \
    "${METRICS_FILE}" \
    "${TSHARK_FILE}.gz" \
    "${COMPARISON_FILE}" \
    "${REPORT_FILE}" \
    >"${DIGEST_FILE}"

echo "runId=${RUN_ID}"
echo "artifactRoot=${ARTIFACT_ROOT}"
echo "executionPassed=${EXECUTION_PASSED}"
echo "capacityQualified=${CAPACITY_QUALIFIED}"

if [[ "${SDK_EXIT}" -ne 0 \
    || "${PEER_TWO_EXIT}" -ne 0 \
    || "${EXECUTION_PASSED}" != true \
    || "${PCAP_M3UA_DATA}" -le 0 ]]; then
    exit 1
fi
