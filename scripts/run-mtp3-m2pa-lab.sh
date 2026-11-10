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
    [[ $# -eq 3 && $(readlink /proc/self/ns/net) != "$3" ]] || die "Refusing to use the host network namespace"
    [[ $(ip -o link show | wc -l) -eq 1 && $(ip -o link show lo) ]] || die "Expected a private loopback-only namespace"
    root=$2
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
    dumpcap -p -P -i lo -a packets:1000 -f 'sctp and port 32907' \
        -w "$root/traffic.pcap" >"$root/capture.log" 2>&1 &
    capture=$!
    wait_for "$capture" "$root/traffic.pcap" ''

    "$root/reference-peer" 127.0.0.1 32907 2 itu-sccp >"$root/peer.log" 2>&1 &
    peer=$!
    wait_for "$peer" "$root/peer.log" 'event=listening'
    dotnet "$root/app/Sigtran.NET.M2paInteropLab.dll" --itu-sccp true --remote-port 32907 \
        --trace "$root/sdk.jsonl" --result "$root/sdk-result.json" --timeout-seconds 25 >"$root/sdk.log" 2>&1
    wait "$peer" || die "Independent peer failed: $root/peer.log"
    peer=
    kill -TERM "$capture" || die "Capture exited before the lab completed"
    wait "$capture" || die "Capture failed: $root/capture.log"
    capture=
    exit
fi

[[ $# -eq 0 && $(uname -s) == Linux ]] || die "Run with no arguments on Linux with kernel SCTP"
for tool in dotnet gcc ip dumpcap tshark timeout unshare; do
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

gcc -std=c11 -O2 -Wall -Wextra -Werror "$repo/tools/interop-peer/m2pa_reference_peer.c" \
    -lsctp -o "$root/reference-peer"
dotnet build "$repo/src/Sigtran.NET.M2paInteropLab/Sigtran.NET.M2paInteropLab.csproj" \
    --configuration Release --disable-build-servers --output "$root/app" >"$root/build.log" 2>&1
timeout --kill-after=5s 45s unshare --user --map-root-user --net \
    bash "$repo/scripts/run-mtp3-m2pa-lab.sh" --isolated "$root" "$(readlink /proc/self/ns/net)"

grep -Eq 'received/dropped.*: [1-9][0-9]*/0 ' "$root/capture.log" || die "Capture had drops or no traffic"
[[ $(grep -c 'event=itu-sccp-roundtrip' "$root/peer.log") -eq 2 ]] || die "Peer did not validate two SCCP requests"
tshark -n -r "$root/traffic.pcap" -Y 'sccp.message_type == 9 && sctp.data_sid == 1 && sctp.data_payload_proto_id == 5 && mtp3.sls == 10' \
    -T fields -E header=y -e mtp3.opc -e mtp3.dpc >"$root/wire-sccp.tsv" 2>"$root/tshark.log"
awk -F '\t' 'NR > 1 { if ($1 == 1 && $2 == 2) req++; else if ($1 == 2 && $2 == 1) rsp++; else exit 1 }
    END { if (req != 2 || rsp != 2) exit 1 }' "$root/wire-sccp.tsv" || die "PCAP did not show two request/reply pairs"
printf 'Verified two ITU SCCP round trips with an independent M2PA peer.\n'
