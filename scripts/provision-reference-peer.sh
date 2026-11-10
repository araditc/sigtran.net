#!/usr/bin/env bash
# Install a prebuilt native SCTP peer without installing or upgrading host packages.
set -euo pipefail
umask 077
if [[ "$EUID" -ne 0 || "$#" -ne 7 ]]; then
  echo 'Usage: sudo bash provision-reference-peer.sh <lab-user> <binary> <sha256> <source-sha> <bind-ip> <sdk-ip> <port>' >&2
  exit 2
fi
lab_user="$1" input_binary="$2" expected_digest="$3" source_sha="$4"
bind_ip="$5" sdk_ip="$6" port="$7"
[[ "$lab_user" =~ ^[a-z_][a-z0-9_-]*$ ]] && [[ "$(id -u "$lab_user")" -gt 0 ]]
[[ "$expected_digest" =~ ^[a-f0-9]{64}$ && "$source_sha" =~ ^[a-f0-9]{40}$ ]]
[[ "$port" =~ ^[0-9]{1,5}$ && "${port:0:1}" != 0 ]] && (( port >= 1024 && port <= 65535 ))
test -f "$input_binary" && test ! -L "$input_binary"
python3 - "$bind_ip" "$sdk_ip" <<'PY'
import ipaddress, json, subprocess, sys
bind, sdk = (ipaddress.IPv4Address(value) for value in sys.argv[1:])
if bind == sdk or any(a.is_loopback or a.is_unspecified or a.is_multicast for a in (bind, sdk)):
    raise SystemExit('Expected distinct unicast lab IPv4 addresses')
local = json.loads(subprocess.check_output(['ip', '-j', '-4', 'addr', 'show']))
if not any(info.get('local') == str(bind) for link in local for info in link.get('addr_info', [])):
    raise SystemExit('The peer host does not own the requested bind address')
PY

install_root=/opt/sigtran-lab-peer
unit=sigtran-reference-peer.service
unit_path="/etc/systemd/system/$unit"
sudoers_path=/etc/sudoers.d/sigtran-reference-peer
marker='# Managed by Sigtran.NET reference peer provisioning'
for path in "$unit_path" "$sudoers_path"; do
  if [[ -e "$path" || -L "$path" ]]; then
    [[ -f "$path" && ! -L "$path" ]] && grep -Fxq -- "$marker" "$path" || {
      echo "Refusing to replace an unmanaged file: $path" >&2; exit 2;
    }
  fi
done
if [[ -e "$install_root" || -L "$install_root" ]]; then
  [[ -d "$install_root" && ! -L "$install_root" && -f "$install_root/.sigtran-managed" ]] || {
    echo 'Refusing to reuse an unmanaged installation directory' >&2; exit 2;
  }
fi

stage="$(mktemp -d /tmp/sigtran-peer-install.XXXXXXXX)"
trap 'rm -rf -- "$stage"' EXIT
install -m 0755 "$input_binary" "$stage/reference-peer"
actual_digest="$(sha256sum "$stage/reference-peer" | cut -d' ' -f1)"
[[ "$actual_digest" == "$expected_digest" ]] || { echo 'Peer binary digest mismatch' >&2; exit 2; }
printf '%s\n' "$marker" > "$stage/.sigtran-managed"
printf '{"schemaVersion":1,"implementation":"repository-c-lksctp-static","version":"%s","buildDigest":"sha256:%s"}\n' \
  "$source_sha" "$actual_digest" > "$stage/build.json"

cat > "$stage/$unit" <<EOF
$marker
[Unit]
Description=Sigtran.NET isolated SCTP lab peer
After=network.target
StartLimitIntervalSec=30
StartLimitBurst=10

[Service]
ExecStart=$install_root/reference-peer $bind_ip $port 0 quiet
DynamicUser=yes
NoNewPrivileges=yes
CapabilityBoundingSet=
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
RestrictAddressFamilies=AF_INET AF_UNIX
IPAddressDeny=any
IPAddressAllow=$sdk_ip/32
UMask=0077
CPUQuota=50%
CPUWeight=10
MemoryMax=96M
TasksMax=12
Nice=10
Restart=always
RestartSec=1s
TimeoutStopSec=3s
EOF

# A dedicated unit may be restarted; no generic shell, firewall or service permission is granted.
printf '%s\n%s ALL=(root) NOPASSWD: /usr/bin/true "", /usr/bin/systemctl start %s, /usr/bin/systemctl stop %s, /usr/bin/systemctl restart %s, /usr/bin/systemctl is-active %s, /usr/bin/systemctl status %s --no-pager\n' \
  "$marker" "$lab_user" "$unit" "$unit" "$unit" "$unit" "$unit" > "$stage/sudoers"
visudo -cf "$stage/sudoers"
install -d -o root -g root -m 0755 "$install_root"
install -o root -g root -m 0644 "$stage/.sigtran-managed" "$install_root/.sigtran-managed"
install -o root -g root -m 0755 "$stage/reference-peer" "$install_root/reference-peer"
install -o root -g root -m 0644 "$stage/build.json" "$install_root/build.json"
systemd-analyze verify "$stage/$unit"
install -o root -g root -m 0644 "$stage/$unit" "$unit_path"
install -o root -g root -m 0440 "$stage/sudoers" "$sudoers_path"
systemctl daemon-reload
systemctl restart "$unit"
systemctl is-active "$unit"
