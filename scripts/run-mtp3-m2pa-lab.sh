#!/usr/bin/env bash
set -euo pipefail
umask 077
export LC_ALL=C

die() { printf '%s\n' "$*" >&2; exit 1; }

wait_for() {
    local pid=$1 path=$2 pattern=$3
    for _ in {1..160}; do
        kill -0 "$pid" 2>/dev/null || die "Lab process exited before $path was ready"
        if [[ -s "$path" ]] && { [[ -z "$pattern" ]] || grep -q "$pattern" "$path"; }; then
            return
        fi
        sleep 0.05
    done
    die "Timed out waiting for $path"
}

if [[ ${1:-} == --isolated ]]; then
    [[ $# -eq 4 && $(readlink /proc/self/ns/net) != "$3" ]] || die "Refusing to use the host network namespace"
    [[ $(ip -o link show | wc -l) -eq 1 && $(ip -o link show lo) ]] || die "Expected a private loopback-only namespace"
    root=$2
    profile=$4
    capture= peer=
    cleanup() {
        local pid
        for pid in "$peer" "$capture"; do
            [[ -n "$pid" ]] || continue
            kill -TERM "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        done
    }
    trap cleanup EXIT
    trap 'exit 143' TERM
    ip link set lo up
    capture_filter='sctp and port 32907'
    if [[ "$profile" == changeover ]]; then
        capture_filter='sctp and (port 32908 or port 32909 or port 32910)'
    fi
    dumpcap -p -P -i lo -a packets:1000 -f "$capture_filter" \
        -w "$root/traffic.pcap" >"$root/capture.log" 2>&1 &
    capture=$!
    wait_for "$capture" "$root/traffic.pcap" ''

    if [[ "$profile" == changeover ]]; then
        dotnet "$root/app/Sigtran.NET.M2paInteropLab.dll" --live-changeover true \
            --trace "$root/sdk.jsonl" --result "$root/sdk-result.json" --timeout-seconds 25 >"$root/sdk.log" 2>&1
    else
        "$root/reference-peer" 127.0.0.1 32907 2 itu-sccp >"$root/peer.log" 2>&1 &
        peer=$!
        wait_for "$peer" "$root/peer.log" 'event=listening'
        dotnet "$root/app/Sigtran.NET.M2paInteropLab.dll" --itu-sccp true --remote-port 32907 \
            --trace "$root/sdk.jsonl" --result "$root/sdk-result.json" --timeout-seconds 25 >"$root/sdk.log" 2>&1
        wait "$peer" || die "Independent peer failed: $root/peer.log"
        peer=
    fi
    # Let libpcap deliver the final buffered block before stopping capture.
    sleep 1
    kill -TERM "$capture" || die "Capture exited before the lab completed"
    wait "$capture" || die "Capture failed: $root/capture.log"
    capture=
    exit
fi

profile=${1:-itu-sccp}
[[ $# -le 1 && $(uname -s) == Linux && ( "$profile" == itu-sccp || "$profile" == changeover ) ]] \
    || die "Usage on Linux: bash scripts/run-mtp3-m2pa-lab.sh [itu-sccp|changeover]"
for tool in dotnet ip dumpcap tshark python3 timeout unshare; do
    command -v "$tool" >/dev/null || die "Missing required tool: $tool"
done

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
base=${SIGTRAN_ARTIFACT_ROOT:-"$HOME/sigtran-lab/artifacts"}
mkdir -p "$base"
root=$(mktemp -d "$(cd -- "$base" && pwd)/mtp3-m2pa.XXXXXXXX")
printf 'Artifacts: %s\n' "$root"
uname -sr >"$root/kernel.txt"
dotnet --version >"$root/dotnet.txt"
if [[ -n ${SIGTRAN_SOURCE_COMMIT:-} ]]; then
    [[ $SIGTRAN_SOURCE_COMMIT =~ ^[a-f0-9]{40}$ ]] || die "Expected the full SDK source commit SHA"
    printf '%s\n' "$SIGTRAN_SOURCE_COMMIT" >"$root/source-commit.txt"
else
    git -C "$repo" rev-parse HEAD >"$root/source-commit.txt"
fi

if [[ "$profile" == itu-sccp ]]; then
    command -v gcc >/dev/null || die "Missing required tool: gcc"
    gcc -std=c11 -O2 -Wall -Wextra -Werror "$repo/tools/interop-peer/m2pa_reference_peer.c" \
        -lsctp -o "$root/reference-peer"
fi
dotnet build "$repo/src/Sigtran.NET.M2paInteropLab/Sigtran.NET.M2paInteropLab.csproj" \
    --configuration Release --disable-build-servers --output "$root/app" >"$root/build.log" 2>&1
timeout --kill-after=5s 45s unshare --user --map-root-user --net \
    bash "$repo/scripts/run-mtp3-m2pa-lab.sh" --isolated "$root" "$(readlink /proc/self/ns/net)" "$profile"

if [[ "$profile" == itu-sccp ]]; then
    tshark -n -r "$root/traffic.pcap" -Y 'sccp.message_type == 9 && sctp.data_sid == 1 && sctp.data_payload_proto_id == 5 && mtp3.sls == 10' \
        -T fields -E header=y -e mtp3.opc -e mtp3.dpc >"$root/wire-sccp.tsv" 2>"$root/tshark.log"
    awk -F '\t' 'NR > 1 { if ($1 == 1 && $2 == 2) req++; else if ($1 == 2 && $2 == 1) rsp++; else exit 1 }
        END { if (req != 2 || rsp != 2) exit 1 }' "$root/wire-sccp.tsv" || die "PCAP did not show two request/reply pairs"
fi

# Never publish a successful report when capture or trace verification fails.
python3 "$repo/scripts/verify-mtp3-m2pa-evidence.py" "$profile" "$root" >"$root/report.pending.json"
mv "$root/report.pending.json" "$root/report.json"
printf 'Verified MTP3/M2PA %s: %s/report.json\n' "$profile" "$root"
