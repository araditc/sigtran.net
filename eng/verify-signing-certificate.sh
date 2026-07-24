#!/usr/bin/env bash
set -euo pipefail

if [[ "$#" -ne 2 ]]; then
    echo "Usage: verify-signing-certificate.sh <pfx-path> <expected-sha256>" >&2
    exit 2
fi

PFX_PATH="$1"
PASSWORD="${SIGNING_CERTIFICATE_PASSWORD:-}"
EXPECTED_SHA256="${2//:/}"
EXPECTED_SHA256="${EXPECTED_SHA256,,}"
CERTIFICATE_PATH="$(mktemp)"
CHAIN_PATH="$(mktemp)"

cleanup() {
    rm -f "${CERTIFICATE_PATH}" "${CHAIN_PATH}"
}
trap cleanup EXIT INT TERM

test -f "${PFX_PATH}"
test -n "${PASSWORD}"
test -n "${EXPECTED_SHA256}"

openssl pkcs12 \
    -in "${PFX_PATH}" \
    -passin "pass:${PASSWORD}" \
    -clcerts \
    -nokeys \
    -out "${CERTIFICATE_PATH}"
openssl pkcs12 \
    -in "${PFX_PATH}" \
    -passin "pass:${PASSWORD}" \
    -cacerts \
    -nokeys \
    -out "${CHAIN_PATH}"

subject="$(openssl x509 -in "${CERTIFICATE_PATH}" -noout -subject -nameopt RFC2253 | sed 's/^subject=//')"
issuer="$(openssl x509 -in "${CERTIFICATE_PATH}" -noout -issuer -nameopt RFC2253 | sed 's/^issuer=//')"
if [[ "${subject}" == "${issuer}" ]]; then
    echo "Stable signing certificate must not be self-issued." >&2
    exit 1
fi

openssl x509 -in "${CERTIFICATE_PATH}" -checkend 2592000 -noout
actual_sha256="$(
    openssl x509 \
        -in "${CERTIFICATE_PATH}" \
        -noout \
        -fingerprint \
        -sha256 |
        cut -d= -f2 |
        tr -d ':' |
        tr '[:upper:]' '[:lower:]'
)"
if [[ "${actual_sha256}" != "${EXPECTED_SHA256}" ]]; then
    echo "Stable signing certificate fingerprint does not match the protected repository variable." >&2
    exit 1
fi

if [[ -s "${CHAIN_PATH}" ]]; then
    openssl verify \
        -CApath /etc/ssl/certs \
        -untrusted "${CHAIN_PATH}" \
        "${CERTIFICATE_PATH}"
else
    openssl verify -CApath /etc/ssl/certs "${CERTIFICATE_PATH}"
fi

printf 'subject=%s\n' "${subject}"
printf 'issuer=%s\n' "${issuer}"
printf 'sha256=%s\n' "${actual_sha256}"
