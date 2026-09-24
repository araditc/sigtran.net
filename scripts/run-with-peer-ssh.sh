#!/usr/bin/env bash
# Only used after the build, within the protected qualification step.
set -euo pipefail
umask 077
[[ $# -gt 0 ]] || exit 2
: "${RUNNER_TEMP:?}"
: "${PEER_SSH_PRIVATE_KEY:?}"
: "${PEER_SSH_KNOWN_HOSTS:?}"
private_ssh_dir="$(mktemp -d "$RUNNER_TEMP/sigtran-peer.XXXXXXXXXX")"
cleanup_ssh_material() { rm -rf -- "$private_ssh_dir"; }
trap cleanup_ssh_material EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
# Directory is private from creation; umask protects each file from creation too.
export PEER_SSH_IDENTITY_FILE="$private_ssh_dir/identity"
export PEER_SSH_KNOWN_HOSTS_FILE="$private_ssh_dir/known_hosts"
printf '%s\n' "$PEER_SSH_PRIVATE_KEY" >"$PEER_SSH_IDENTITY_FILE"
printf '%s\n' "$PEER_SSH_KNOWN_HOSTS" >"$PEER_SSH_KNOWN_HOSTS_FILE"
unset PEER_SSH_PRIVATE_KEY PEER_SSH_KNOWN_HOSTS
"$@"
