# ITU MAP SMS Reference Service

The deployable reference service extends `Sigtran.NET.OperationsHost`.
It does not change the SDK's package boundary or enable a raw signalling
endpoint. Existing operations-only deployments remain the default.

## Implementation And Evidence

| Unit | Output | State |
| --- | --- | --- |
| 1 | Opt-in ITU service configuration and validation | Implemented |
| 2 | Single SCTP/M3UA/SCCP/TCAP/MAP composition | Implemented |
| 3 | Bounded typed MAP operation execution | Pending |
| 4 | Loopback-only authenticated service entry point | Pending |
| 5 | Readiness, metrics and shutdown drain | Pending |
| 6 | Explicit M3UA application-server routing context | Pending |
| 7 | Independent peer positive and negative scenario matrix | Pending |
| 8 | Real host traffic and independent PCAP/trace comparison | Pending |
| 9 | Separately maintained peer path | Pending |
| 10 | Reproducible evidence, limitations and phase decision | Pending |

## Service Configuration

Set `SIGTRAN_REFERENCE_MODE=map-sms` to compose the ITU reference
profile. Operation execution and the local HTTP entry point are
delivered separately.

| Environment key | Reference mode |
| --- | --- |
| `SIGTRAN_MAP_SMS_OPERATIONS` | Required comma-separated allowlist: `sri-sm`, `mo-forward-sm`, `mt-forward-sm`, `report-sm-delivery-status`, `alert-service-centre` |
| `SIGTRAN_MAP_SMS_MAX_IN_FLIGHT` | Default 32 or the configured queue limit, whichever is smaller; maximum 256 and never greater than the queue limit |
| `SIGTRAN_MAP_SMS_TIMEOUT_MS` | Default 5000; 250-60000 |
| `SIGTRAN_MAP_SMS_TOKEN` | Required 32-128 character base64url-form secret; use a randomly generated token. Diagnostics never include its value. |

MAP SMS requires 14-bit ITU point codes and SCCP service indicator 3.
Keep this host on authorized lab or operator endpoints. A repository-owned
peer and an independently maintained signalling stack are distinct
evidence classes. Neither qualifies production or stable release alone.

## Runtime Ownership

The hosted runtime starts SCTP/M3UA and waits for ASP activation, then
starts TCAP, which owns the SCCP receive loop. MAP uses those same
contracts. The reference host initiates MAP dialogues; it is not an
HLR or SMSC and refuses unsolicited inbound invokes. It drains TCAP
events and SCCP returns without logging subscriber data. Shutdown stops
the upper readers before stopping the M3UA transport.
