#!/usr/bin/env bash
set -euo pipefail
umask 077
export LC_ALL=C

die() { printf '%s\n' "$*" >&2; exit 1; }
wait_for() {
    local pid=$1 path=$2 pattern=$3
    for _ in {1..160}; do
        kill -0 "$pid" 2>/dev/null || die "Lab process stopped before $path was ready"
        if [[ -s "$path" ]] && { [[ -z "$pattern" ]] || grep -q "$pattern" "$path"; }; then return; fi
        sleep 0.05
    done
    die "Timed out waiting for $path"
}

if [[ ${1:-} == --isolated ]]; then
    [[ $# -eq 5 && $(readlink /proc/self/ns/net) != "$4" ]] || die "Refusing to use the host network namespace"
    [[ $(ip -o link show | wc -l) -eq 1 && $(ip -o link show lo) ]] || die "Expected an isolated loopback-only lab"
    root=$2 repo=$3 profile=$5
    host= peer= capture= stp=
    cleanup() {
        local pid
        for pid in "$host" "$peer" "$stp" "$capture"; do
            [[ -n "$pid" ]] || continue
            kill -TERM "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        done
    }
    trap cleanup EXIT
    trap 'exit 143' TERM
    ip link set lo up
    dumpcap -p -P -i lo -a packets:2000 -f 'sctp and port 33060' \
        -w "$root/traffic.pcap" >"$root/capture.log" 2>&1 &
    capture=$!
    wait_for "$capture" "$root/traffic.pcap" ''
    driver=$profile
    if [[ "$profile" == maintained ]]; then
        LD_LIBRARY_PATH="$SIGTRAN_MAINTAINED_STP_PREFIX/usr/lib/x86_64-linux-gnu" \
            "$SIGTRAN_MAINTAINED_STP_PREFIX/usr/bin/osmo-stp" -c "$root/stp.cfg" >"$root/stp.log" 2>&1 &
        stp=$!
        listening=false
        for _ in {1..100}; do
            kill -0 "$stp" 2>/dev/null || die "Maintained STP stopped: $root/stp.log"
            if ss -H -l -S | grep -Eq ':33060([[:space:]]|$)'; then listening=true; break; fi
            sleep 0.05
        done
        [[ $listening == true ]] || die "Maintained STP did not listen"
        "$root/reference-peer" 127.0.0.1 33060 10 reference-asp >"$root/peer.log" 2>&1 &
        peer=$!
        wait_for "$peer" "$root/peer.log" 'event=asp-client-active'
        export SIGTRAN_LOCAL_SCTP_IP=127.0.0.1 SIGTRAN_LOCAL_SCTP_PORT=33061
        driver=reference
    else
        "$root/reference-peer" 127.0.0.1 33060 10 "$profile" >"$root/peer.log" 2>&1 &
        peer=$!
        wait_for "$peer" "$root/peer.log" 'event=listening'
        unset SIGTRAN_LOCAL_SCTP_IP SIGTRAN_LOCAL_SCTP_PORT
    fi

    export SIGTRAN_REMOTE_IP=127.0.0.1 SIGTRAN_REMOTE_PORT=33060 SIGTRAN_ASP_IDENTIFIER=1
    export SIGTRAN_LOCAL_POINT_CODE=1 SIGTRAN_REMOTE_POINT_CODE=2 SIGTRAN_ROUTING_CONTEXT=100
    export SIGTRAN_NETWORK_INDICATOR=2 SIGTRAN_SERVICE_INDICATOR=3 SIGTRAN_QUEUE_CAPACITY=32
    export SIGTRAN_REFERENCE_MODE=map-sms SIGTRAN_MAP_SMS_MAX_IN_FLIGHT=4 SIGTRAN_MAP_SMS_TIMEOUT_MS=650
    export SIGTRAN_MAP_SMS_OPERATIONS=sri-sm,mo-forward-sm,mt-forward-sm,report-sm-delivery-status,alert-service-centre
    SIGTRAN_MAP_SMS_TOKEN=$(python3 -c 'import secrets; print(secrets.token_urlsafe(32))')
    export SIGTRAN_MAP_SMS_TOKEN ASPNETCORE_URLS=http://127.0.0.1:33080
    dotnet "$root/app/Sigtran.NET.OperationsHost.dll" >"$root/host.log" 2>&1 &
    host=$!
    if [[ "$profile" == wrong-context ]]; then
        wait_for "$host" "$root/peer.log" 'event=wrong-activation-context'
    fi
    python3 "$repo/scripts/reference-service-traffic.py" "$driver" http://127.0.0.1:33080 "$root/http-result.json"
    if [[ "$profile" != wrong-context ]]; then
        wait "$peer" || die "Independent peer failed: $root/peer.log"
        peer=
    fi
    kill -TERM "$host"
    wait "$host" || die "Reference host shutdown failed: $root/host.log"
    host=
    if [[ -n "$stp" ]]; then
        kill -TERM "$stp"
        wait "$stp" || die "Maintained STP shutdown failed: $root/stp.log"
        stp=
    fi
    sleep 1
    kill -TERM "$capture"
    wait "$capture" || die "Capture failed: $root/capture.log"
    capture=
    exit
fi

profile=${1:-reference}
[[ $# -le 1 && ( "$profile" == reference || "$profile" == wrong-context || "$profile" == maintained ) && $(uname -s) == Linux ]] \
    || die "Usage on Linux: bash scripts/run-reference-service-lab.sh [reference|wrong-context|maintained]"
for tool in dotnet gcc ip unshare timeout dumpcap python3 tshark; do
    command -v "$tool" >/dev/null || die "Missing lab prerequisite: $tool"
done
repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
base=${SIGTRAN_ARTIFACT_ROOT:-"$HOME/sigtran-lab/artifacts"}
mkdir -p "$base"
root=$(mktemp -d "$(cd -- "$base" && pwd)/reference-service.XXXXXXXX")
printf 'Artifacts: %s\n' "$root"
uname -sr >"$root/kernel.txt"
dotnet --version >"$root/dotnet.txt"
sha=${SIGTRAN_SOURCE_COMMIT:-$(git -C "$repo" rev-parse HEAD)}
[[ $sha =~ ^[a-f0-9]{40}$ ]] || die "Expected the full SDK source commit"
printf '%s\n' "$sha" >"$root/source-commit.txt"
if [[ "$profile" == maintained ]]; then
    prefix=${SIGTRAN_MAINTAINED_STP_PREFIX:-}
    [[ "$prefix" == /* && -x "$prefix/usr/bin/osmo-stp" && -r "$prefix/usr/lib/x86_64-linux-gnu/libosmo-sigtran.so.5" ]] \
        || die "Set SIGTRAN_MAINTAINED_STP_PREFIX to a private extracted STP bundle"
    command -v ss >/dev/null || die "Missing lab prerequisite: ss"
    export SIGTRAN_MAINTAINED_STP_PREFIX="$prefix"
    LD_LIBRARY_PATH="$prefix/usr/lib/x86_64-linux-gnu" "$prefix/usr/bin/osmo-stp" -V >"$root/stp-version.txt"
    sha256sum "$prefix/usr/bin/osmo-stp" "$prefix/usr/lib/x86_64-linux-gnu/libosmo-sigtran.so.5" >"$root/stp-binaries.sha256"
    cp "$repo/eng/interop/reference-stp.cfg" "$root/stp.cfg"
fi
gcc -std=c11 -O2 -Wall -Wextra -Werror "$repo/tools/interop-peer/sigtran_reference_peer.c" -lsctp -o "$root/reference-peer"
"$root/reference-peer" --self-test
dotnet build "$repo/src/Sigtran.NET.OperationsHost/Sigtran.NET.OperationsHost.csproj" --configuration Release \
    --disable-build-servers --output "$root/app" >"$root/build.log" 2>&1
timeout --kill-after=5s 50s unshare --user --map-root-user --net \
    bash "$repo/scripts/run-reference-service-lab.sh" --isolated "$root" "$repo" "$(readlink /proc/self/ns/net)" "$profile"
python3 "$repo/scripts/verify-reference-service-evidence.py" "$profile" "$root" >"$root/report.pending.json"
mv "$root/report.pending.json" "$root/report.json"
printf 'Verified reference service %s: %s/report.json\n' "$profile" "$root"
