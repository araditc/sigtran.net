# Phase 52 - End-To-End SS7 Traffic Lab

Phase 52 adds an executable cross-implementation lab for carrying MAP SMS
operations through every implemented layer:

`MAP SMS -> TCAP -> SCCP -> M3UA -> native Linux SCTP`

The SDK endpoint is .NET code. The reference endpoint is an independent C
program that links only to Linux lksctp and constructs its own response bytes.

## Completed Units

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | Executable .NET full-stack lab endpoint | Complete |
| 2 | Independent C SCTP/M3UA reference peer | Complete |
| 3 | SCTP stream and PPID receive validation | Complete |
| 4 | M3UA ASPUP and ASPACTIVE handshake | Complete |
| 5 | SCCP UDT parsing and response construction | Complete |
| 6 | TCAP transaction and invoke correlation | Complete |
| 7 | Five MAP SMS request/result exchanges | Complete |
| 8 | PCAP, logs, trace, TShark, and comparison capture | Complete |
| 9 | SHA-256 evidence manifest and retained bundle | Complete |
| 10 | Runtime shutdown regression, docs, build, test, and pack | Complete |

## Run The Lab

The runner requires Linux kernel SCTP, lksctp headers, GCC, .NET 10,
`tcpdump`, and `tshark`.

```bash
SIGTRAN_ARTIFACT_ROOT="$HOME/sigtran-lab/artifacts" \
bash scripts/run-end-to-end-ss7-lab.sh
```

When passwordless sudo is unavailable, provide the password only in the process
environment:

```bash
SIGTRAN_SUDO_PASSWORD="<runtime-secret>" \
SIGTRAN_ARTIFACT_ROOT="$HOME/sigtran-lab/artifacts" \
bash scripts/run-end-to-end-ss7-lab.sh
```

The script never writes the sudo password to configuration or evidence files.

## Validation Contract

The run passes only when all of the following are true:

- SDK and peer exit successfully.
- The SDK records five successful MAP outcomes.
- The peer parses five MAP invokes and emits five MAP results.
- PCAP contains at least ten M3UA DATA messages.
- All retained artifacts receive SHA-256 digest coverage.

The validated operations are:

- `sendRoutingInfoForSM`
- `mo-ForwardSM`
- `mt-ForwardSM`
- `reportSM-DeliveryStatus`
- `alertServiceCentre`

## Passing Evidence

Run `end-to-end-20260724T085858Z` passed on WSL2 kernel
`6.18.33.2-microsoft-standard-WSL2`.

| Signal | Observed |
| --- | ---: |
| SCTP packets | 23 |
| M3UA DATA messages | 10 |
| SDK MAP outcomes | 5 |
| Peer MAP invokes | 5 |
| Peer MAP results | 5 |

The retained bundle is under
`docs/evidence/PHASE52_END_TO_END_20260724T085858Z/` and includes the PCAP,
peer and capture logs, SDK JSONL trace, generated configuration, TShark field
export, comparison report, summary, and digest manifest.

## Evidence Boundary

This run is independent cross-implementation evidence for the protocol profile
currently implemented by this repository. The C peer does not share SDK codec
code and validates every envelope before constructing its response.

It is not evidence of interoperability with an operator STP, SMSC, HLR, vendor
stack, or a separately maintained standards implementation. Those environments
can impose profile, addressing, ASN.1, network-management, and operational
requirements that this local peer does not cover. Stable operator-grade claims
therefore remain gated on an external operator or vendor profile run.
