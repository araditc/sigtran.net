#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_PATH="${PACKAGE_PATH:-$REPO_ROOT/src/sigtran.net/bin/Release/Sigtran.Net.1.0.0.nupkg}"
OUTPUT_DIR="${OUTPUT_DIR:-$REPO_ROOT/artifacts/signing}"
SECRETS_FILE="${SECRETS_FILE:-$HOME/sigtran-lab/secrets/phase25-secrets.local}"
SIGN_LOG="$OUTPUT_DIR/sign-package.log"
SIGNED_PACKAGE="$OUTPUT_DIR/$(basename "$PACKAGE_PATH")"
SIGNED_DIGEST="$SIGNED_PACKAGE.sha256"

mkdir -p "$OUTPUT_DIR"

if [[ ! -f "$PACKAGE_PATH" ]]; then
    echo "Package file not found: $PACKAGE_PATH" >&2
    exit 2
fi

if [[ ! -f "$SECRETS_FILE" ]]; then
    echo "Signing secrets file not found: $SECRETS_FILE" >&2
    exit 3
fi

set +x
source "$SECRETS_FILE"

if [[ -z "${SIGNING_CERTIFICATE:-}" || -z "${SIGNING_CERTIFICATE_PASSWORD:-}" ]]; then
    echo "SIGNING_CERTIFICATE and SIGNING_CERTIFICATE_PASSWORD must be set." >&2
    exit 4
fi

CERT_FILE="$(mktemp)"
cleanup() {
    rm -f "$CERT_FILE"
}
trap cleanup EXIT

if ! printf "%s" "$SIGNING_CERTIFICATE" | tr -d '\r\n ' | base64 -d > "$CERT_FILE"; then
    echo "SIGNING_CERTIFICATE must be a base64-encoded PFX certificate." >&2
    exit 5
fi

dotnet nuget sign "$PACKAGE_PATH" \
    --certificate-path "$CERT_FILE" \
    --certificate-password "$SIGNING_CERTIFICATE_PASSWORD" \
    --hash-algorithm SHA256 \
    --output "$OUTPUT_DIR" \
    --overwrite \
    -v minimal > "$SIGN_LOG" 2>&1

sha256sum "$SIGNED_PACKAGE" | awk '{print $1}' > "$SIGNED_DIGEST"

echo "SIGNED_PACKAGE=$SIGNED_PACKAGE"
echo "SIGNED_PACKAGE_SHA256=$(cat "$SIGNED_DIGEST")"
echo "SIGN_LOG=$SIGN_LOG"
