#!/usr/bin/env bash
# Sent over the existing authorized SSH channel; runs on the PEER, not the SDK.
set -euo pipefail
mode="${1:-}"
unit="${2:-}"
service="${3:-}"
delay="${4:-}"
[[ "$unit" =~ ^sigtran-peer-rollback-[A-Za-z0-9_.-]+$ && ${#unit} -le 180 ]] || exit 2
[[ "$service" =~ ^[A-Za-z0-9][A-Za-z0-9_.@:-]*\.service$ ]] || exit 2
systemctl_path="$(command -v systemctl)"
case "$mode" in
  outage)
    [[ "$delay" =~ ^[0-9]{1,3}$ ]] || exit 2
    (( 10#$delay >= 1 && 10#$delay <= 120 )) || exit 2
    # No stop is attempted unless the peer's service manager accepted its own
    # attempt-specific rollback. Losing SSH/SDK afterward cannot remove it.
    systemd-run --quiet --unit "$unit" --on-active="${delay}s" \
      --timer-property=AccuracySec=1s --property=Restart=on-failure \
      --property=RestartSec=1s /bin/bash -c '
        until "$1" start "$2" && "$1" is-active --quiet "$2"; do
          sleep 1
        done
      ' sigtran-peer-recovery "$systemctl_path" "$service"
    "$systemctl_path" stop "$service"
    ;;
  recover)
    # If start or its verification fails, errexit leaves the peer rollback alive.
    "$systemctl_path" start "$service"
    "$systemctl_path" is-active --quiet "$service"
    "$systemctl_path" stop "$unit.timer" "$unit.service"
    ;;
  *) exit 2 ;;
esac
