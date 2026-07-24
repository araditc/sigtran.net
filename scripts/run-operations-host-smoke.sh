#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REMOTE_SCTP_PORT="${REMOTE_SCTP_PORT:-29154}"
MANAGEMENT_PORT="${MANAGEMENT_PORT:-18154}"
WORK_ROOT="$(mktemp -d)"
PEER_PID=""
HOST_PID=""

cleanup() {
    local exit_code=$?
    if [[ "${exit_code}" -ne 0 ]]; then
        echo "Operations host smoke failed; peer log follows." >&2
        cat "${WORK_ROOT}/peer.log" >&2 2>/dev/null || true
        echo "Operations host log follows." >&2
        cat "${WORK_ROOT}/host.log" >&2 2>/dev/null || true
    fi
    if [[ -n "${HOST_PID}" ]] && kill -0 "${HOST_PID}" 2>/dev/null; then
        kill "${HOST_PID}" 2>/dev/null || true
    fi
    if [[ -n "${PEER_PID}" ]] && kill -0 "${PEER_PID}" 2>/dev/null; then
        kill "${PEER_PID}" 2>/dev/null || true
    fi
    if [[ -n "${HOST_PID}" ]]; then
        wait "${HOST_PID}" 2>/dev/null || true
    fi
    if [[ -n "${PEER_PID}" ]]; then
        wait "${PEER_PID}" 2>/dev/null || true
    fi
    rm -rf "${WORK_ROOT}"
    return "${exit_code}"
}
trap cleanup EXIT INT TERM

for command_name in curl dotnet gcc grep; do
    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "Required command is missing: ${command_name}" >&2
        exit 2
    fi
done

gcc \
    -std=c11 \
    -O2 \
    -Wall \
    -Wextra \
    -Werror \
    "${REPOSITORY_ROOT}/tools/interop-peer/sigtran_reference_peer.c" \
    -lsctp \
    -o "${WORK_ROOT}/sigtran-reference-peer"

"${WORK_ROOT}/sigtran-reference-peer" \
    127.0.0.1 \
    "${REMOTE_SCTP_PORT}" \
    0 \
    quiet \
    >"${WORK_ROOT}/peer.log" 2>&1 &
PEER_PID=$!
sleep 1

export ASPNETCORE_URLS="http://127.0.0.1:${MANAGEMENT_PORT}"
export SIGTRAN_REMOTE_IP=127.0.0.1
export SIGTRAN_REMOTE_PORT="${REMOTE_SCTP_PORT}"
export SIGTRAN_ASP_IDENTIFIER=42
export SIGTRAN_LOCAL_POINT_CODE=1
export SIGTRAN_REMOTE_POINT_CODE=2
export SIGTRAN_ROUTING_CONTEXT=100
export SIGTRAN_NETWORK_INDICATOR=2
export SIGTRAN_SERVICE_INDICATOR=3
export SIGTRAN_QUEUE_CAPACITY=128

dotnet build \
    "${REPOSITORY_ROOT}/src/Sigtran.NET.OperationsHost/Sigtran.NET.OperationsHost.csproj" \
    -c Release \
    -m:1 \
    >"${WORK_ROOT}/host-build.log" 2>&1
dotnet \
    "${REPOSITORY_ROOT}/src/Sigtran.NET.OperationsHost/bin/Release/net10.0/Sigtran.NET.OperationsHost.dll" \
    >"${WORK_ROOT}/host.log" 2>&1 &
HOST_PID=$!

for _ in {1..60}; do
    if curl \
        --fail \
        --silent \
        --show-error \
        "http://127.0.0.1:${MANAGEMENT_PORT}/health/ready" \
        >"${WORK_ROOT}/ready.json" 2>/dev/null; then
        break
    fi
    sleep 0.25
done

curl \
    --fail \
    --silent \
    --show-error \
    "http://127.0.0.1:${MANAGEMENT_PORT}/health/live" \
    >"${WORK_ROOT}/live.json"
curl \
    --fail \
    --silent \
    --show-error \
    "http://127.0.0.1:${MANAGEMENT_PORT}/metrics" \
    >"${WORK_ROOT}/metrics.txt"

grep -q '"status":"Healthy"' "${WORK_ROOT}/ready.json"
grep -q '^sigtran_m3ua_active 1$' "${WORK_ROOT}/metrics.txt"

cat "${WORK_ROOT}/ready.json"
printf '\n'
grep '^sigtran_m3ua_active ' "${WORK_ROOT}/metrics.txt"
grep '^sigtran_m3ua_reconnect_attempts_total ' "${WORK_ROOT}/metrics.txt"
grep '^sigtran_m3ua_faults_total ' "${WORK_ROOT}/metrics.txt"
