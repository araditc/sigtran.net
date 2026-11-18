#!/usr/bin/env bash
# Build the independent C/lksctp peer from the exact Git blob; do not start it.
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo 'Usage: build-kubernetes-lab-peer.sh <full-source-commit> <new-output-directory>' >&2
  exit 2
fi
source_sha="$1"
output="$2"
[[ "$(uname -s)" == "Linux" ]]
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]]
test "$(git rev-parse HEAD)" = "$source_sha"
test -n "$output"

# A fresh directory preserves earlier/failed builds instead of overwriting them.
umask 077
mkdir -- "$output"
git show "$source_sha:tools/interop-peer/sigtran_reference_peer.c" > "$output/peer-source.c"
gcc -std=c11 -O2 -Wall -Wextra -Werror "$output/peer-source.c" -lsctp -o "$output/reference-peer"
"$output/reference-peer" --self-test > "$output/self-test.log" 2>&1
{
  printf 'sourceCommit=%s\n' "$source_sha"
  printf 'architecture=%s\n' "$(uname -m)"
  printf 'compiler=%s\n' "$(gcc -dumpfullversion)"
  printf 'selfTest=passed\npeerStarted=false\n'
} > "$output/build.txt"
(
  cd -- "$output"
  sha256sum -- peer-source.c reference-peer self-test.log build.txt > sha256.txt
)
