#!/usr/bin/env bash
set -euo pipefail
umask 077
export LC_ALL=C

die() { printf '%s\n' "$*" >&2; exit 1; }

wait_for_marker() {
    local marker=$1 pid=$2 ticks=$3
    while [[ ! -f "$marker" ]] && (( ticks > 0 )); do
        kill -0 "$pid" 2>/dev/null || die "Process exited before producing $marker"
        sleep 0.05
        ticks=$((ticks - 1))
    done
    [[ -f "$marker" ]] || die "Timed out waiting for $marker"
}

in_net() {
    local pid=$1
    shift
    nsenter --target "$pid" --net -- "$@"
}

configure_path() {
    local client=$1 server=$2 client_if=$3 server_if=$4 client_address=$5 server_address=$6
    ip link add "$client_if" type veth peer name "$server_if"
    ip link set "$client_if" netns "$client"
    ip link set "$server_if" netns "$server"
    in_net "$client" ip address add "$client_address/30" dev "$client_if"
    in_net "$server" ip address add "$server_address/30" dev "$server_if"
    in_net "$client" ip link set "$client_if" up
    in_net "$server" ip link set "$server_if" up
}

run_isolated() {
    local root=$1 dll=$2 host_ns=$3
    capture= client= server= client_ns= server_ns=

    cleanup() {
        # Only signal child processes that belong to this run.
        local pid
        for pid in "$capture" "$client" "$server"; do
            [[ -n "$pid" ]] || continue
            kill -TERM "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        done
        for pid in "$client_ns" "$server_ns"; do
            [[ -n "$pid" ]] || continue
            kill -KILL "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        done
    }

    [[ $(readlink /proc/self/ns/net) != "$host_ns" ]] || die "Refusing to run in the host network namespace"
    [[ $(ip -o link show | wc -l) -eq 1 && $(ip -o link show lo) ]] || die "Expected an empty network namespace"
    trap cleanup EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    mkdir "$root/coord"

    unshare --net bash -c 'ip link set lo up; touch "$1"; exec sleep 90' bash "$root/client-ns-ready" &
    client_ns=$!
    unshare --net bash -c 'ip link set lo up; touch "$1"; exec sleep 90' bash "$root/server-ns-ready" &
    server_ns=$!
    wait_for_marker "$root/client-ns-ready" "$client_ns" 100
    wait_for_marker "$root/server-ns-ready" "$server_ns" 100
    local client_id server_id parent_id
    client_id=$(readlink "/proc/$client_ns/ns/net")
    server_id=$(readlink "/proc/$server_ns/ns/net")
    parent_id=$(readlink /proc/self/ns/net)
    [[ "$client_id" != "$server_id" && "$client_id" != "$host_ns" && "$server_id" != "$host_ns"
        && "$client_id" != "$parent_id" && "$server_id" != "$parent_id" ]] || die "Endpoint isolation failed"

    # Documentation-only addresses stay on these two veth links, with no default route.
    configure_path "$client_ns" "$server_ns" client0 server0 192.0.2.1 192.0.2.2
    configure_path "$client_ns" "$server_ns" client1 server1 198.51.100.1 198.51.100.2
    local endpoint
    for endpoint in "$client_ns" "$server_ns"; do
        # These timers belong to the endpoint network namespace, not the host.
        in_net "$endpoint" sysctl -w net.sctp.rto_min=200 net.sctp.rto_initial=400 \
            net.sctp.rto_max=800 net.sctp.hb_interval=400 net.sctp.path_max_retrans=1
        in_net "$endpoint" ip -o -4 address show
        in_net "$endpoint" ip -4 route show
    done >"$root/topology.log"

    nsenter --target "$client_ns" --net -- dumpcap -p -P -i any -a packets:4000 \
        -f 'sctp and port 32906' -w "$root/traffic.pcap" >"$root/capture.log" 2>&1 &
    capture=$!
    wait_for_marker "$root/traffic.pcap" "$capture" 100
    kill -0 "$capture" 2>/dev/null || die "Capture did not start"

    local -a common=(--coordination-dir "$root/coord" --timeout-seconds 45 --exercise-path-loss true)
    nsenter --target "$server_ns" --net -- dotnet "$dll" --mode multihoming-server "${common[@]}" \
        --local-ip 192.0.2.2 --local-alternate-ip 198.51.100.2 --local-port 32906 \
        --trace "$root/server.jsonl" >"$root/server.log" 2>&1 &
    server=$!
    wait_for_marker "$root/coord/listener-ready" "$server" 200
    nsenter --target "$client_ns" --net -- dotnet "$dll" --mode multihoming-client "${common[@]}" \
        --local-ip 192.0.2.1 --local-alternate-ip 198.51.100.1 --local-port 32905 \
        --remote-ip 192.0.2.2 --remote-alternate-ip 198.51.100.2 --remote-port 32906 \
        --trace "$root/client.jsonl" >"$root/client.log" 2>&1 &
    client=$!
    wait_for_marker "$root/coord/record-in-flight" "$server" 400

    # The application has an outstanding reply. Keep the primary down until both endpoints finish.
    in_net "$client_ns" ip link set client0 down
    in_net "$server_ns" ip link set server0 down
    date +%s.%N >"$root/cut-epoch.txt"
    touch "$root/coord/path-cut"

    wait "$client" || die "Client failed: $root/client.log"
    client=
    wait "$server" || die "Server failed: $root/server.log"
    server=
    kill -TERM "$capture" || die "Capture stopped before the workload finished"
    wait "$capture" || die "Capture failed: $root/capture.log"
    capture=
    [[ -s "$root/traffic.pcap" ]] || die "Capture was empty"
}

if [[ ${1:-} == --isolated ]]; then
    [[ $# -eq 4 ]] || die "Invalid isolated invocation"
    run_isolated "$2" "$3" "$4"
    exit
fi

[[ $# -eq 0 ]] || die "Usage: bash scripts/run-native-sctp-multihoming-lab.sh"
[[ $(uname -s) == Linux ]] || die "This lab requires Linux with kernel SCTP and lksctp"
for tool in dotnet ip nsenter sysctl dumpcap tshark python3 timeout unshare; do
    command -v "$tool" >/dev/null || die "Missing required tool: $tool"
done

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
base=${SIGTRAN_ARTIFACT_ROOT:-"$HOME/sigtran-lab/artifacts"}
mkdir -p "$base"
base=$(cd -- "$base" && pwd)
root=$(mktemp -d "$base/sctp-multihoming.XXXXXXXX")
printf 'Artifacts: %s\n' "$root"
uname -sr >"$root/kernel.txt"
dotnet --version >"$root/dotnet.txt"
if [[ -n ${SIGTRAN_SOURCE_COMMIT:-} ]]; then
    [[ $SIGTRAN_SOURCE_COMMIT =~ ^[a-f0-9]{40}$ ]] || die "Expected a full source commit SHA"
    printf '%s\n' "$SIGTRAN_SOURCE_COMMIT" >"$root/source-commit.txt"
else
    git -C "$repo" rev-parse HEAD >"$root/source-commit.txt"
fi

project="$repo/src/Sigtran.NET.NativeSctpLab/Sigtran.NET.NativeSctpLab.csproj"
dotnet build "$project" --configuration Release --disable-build-servers --output "$root/app" >"$root/build.log" 2>&1 \
    || die "Build failed: $root/build.log"

timeout --kill-after=5s 75s unshare --user --map-root-user --net --mount \
    bash "$repo/scripts/run-native-sctp-multihoming-lab.sh" --isolated \
    "$root" "$root/app/Sigtran.NET.NativeSctpLab.dll" "$(readlink /proc/self/ns/net)"

# Publish a report file only after both the raw capture and the SDK traces pass.
python3 "$repo/scripts/verify-sctp-multihoming-evidence.py" "$root" >"$root/report.pending.json"
mv "$root/report.pending.json" "$root/report.json"
printf 'Verified SCTP path loss without reconnect: %s/report.json\n' "$root"
