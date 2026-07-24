# End-To-End SS7 Traffic Lab

- Run id: `end-to-end-20260724T085858Z`
- Completed UTC: `2026-07-24T08:59:08Z`
- Host: `AmmarPC`
- Kernel: `6.18.33.2-microsoft-standard-WSL2`
- Peer: `independent-c-reference-peer`
- SCTP endpoint: `127.0.0.1:2906`
- Result: `true`

## Stack

`MAP SMS -> TCAP -> SCCP -> M3UA -> native Linux SCTP`

## Validated Operations

- `sendRoutingInfoForSM`
- `mo-ForwardSM`
- `mt-ForwardSM`
- `reportSM-DeliveryStatus`
- `alertServiceCentre`

## Counts

| Signal | Observed | Expected |
| --- | ---: | ---: |
| SDK MAP outcomes | 5 | 5 |
| Peer MAP invokes | 5 | 5 |
| Peer MAP results | 5 | 5 |
| M3UA DATA messages in PCAP | 10 | 10 |
| SCTP packets in PCAP | 23 | greater than 10 |

## Evidence Scope

The peer is an independent C implementation that links only to Linux lksctp.
It parses and validates SCTP metadata, M3UA Protocol Data, SCCP UDT, TCAP
transactions/components, MAP operation codes, and operation parameter tags.
This run is cross-implementation evidence for the repository profile. Vendor or
operator peer validation remains a separate promotion requirement.
