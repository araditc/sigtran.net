#!/usr/bin/env bash
# Configure the distribution's non-root capture mechanism, without granting general sudo.
set -euo pipefail

if [[ "$EUID" -ne 0 || "$#" -ne 1 ]]; then
  echo "Usage: sudo bash configure-capture-access.sh <existing-lab-user>" >&2
  exit 2
fi
lab_user="$1"
[[ "$lab_user" =~ ^[a-z_][a-z0-9_-]*$ ]] || { echo "Invalid lab user" >&2; exit 2; }
[[ "$(id -u "$lab_user")" -gt 0 ]] || { echo "An existing non-root account is required" >&2; exit 2; }
dpkg-query -W -f='${Status}' wireshark-common | grep -qx 'install ok installed'
command -v setcap >/dev/null
test -x /usr/bin/dumpcap

# Keep package upgrades aligned with the supported Debian/Ubuntu capture policy.
printf '%s\n' 'wireshark-common wireshark-common/install-setuid boolean true' | debconf-set-selections
DEBIAN_FRONTEND=noninteractive dpkg-reconfigure wireshark-common
getent group wireshark >/dev/null
chown root:wireshark /usr/bin/dumpcap
chmod 0750 /usr/bin/dumpcap
setcap cap_net_raw,cap_net_admin=eip /usr/bin/dumpcap
usermod -a -G wireshark "$lab_user"

test ! -u /usr/bin/dumpcap
getcap /usr/bin/dumpcap
stat -c 'captureBinaryMode=%a owner=%U group=%G' /usr/bin/dumpcap
echo 'Capture access configured. Verify from a new SSH session.'
