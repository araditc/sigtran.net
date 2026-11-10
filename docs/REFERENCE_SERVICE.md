# ITU MAP SMS Reference Service

The deployable reference service extends `Sigtran.NET.OperationsHost`.
It does not change the SDK's package boundary or enable a raw signalling
endpoint. Existing operations-only deployments remain the default.

## Implementation And Evidence

| Unit | Output | State |
| --- | --- | --- |
| 1 | Opt-in ITU service configuration and validation | Implemented |
| 2 | Single SCTP/M3UA/SCCP/TCAP/MAP composition | Implemented |
| 3 | Bounded typed MAP operation execution | Implemented |
| 4 | Loopback-only authenticated service entry point | Implemented |
| 5 | Readiness, metrics and shutdown drain | Implemented |
| 6 | Explicit M3UA application-server routing context | Implemented |
| 7 | Independent peer positive and negative scenario matrix | Implemented; C wire fixtures verified |
| 8 | Real host traffic and independent PCAP/trace comparison | Verified on Linux |
| 9 | Separately maintained peer path | Verified on Linux |
| 10 | Reproducible evidence, limitations and phase decision | Three source-bound lab profiles verified |

## Service Configuration

Set `SIGTRAN_REFERENCE_MODE=map-sms` to compose the ITU reference
profile and enable its local entry point.

| Environment key | Reference mode |
| --- | --- |
| `SIGTRAN_MAP_SMS_OPERATIONS` | Required comma-separated allowlist: `sri-sm`, `mo-forward-sm`, `mt-forward-sm`, `report-sm-delivery-status`, `alert-service-centre` |
| `SIGTRAN_MAP_SMS_MAX_IN_FLIGHT` | Default 32 or the configured queue limit, whichever is smaller; maximum 256 and never greater than the queue limit |
| `SIGTRAN_MAP_SMS_TIMEOUT_MS` | Default 5000; 250-60000 |
| `SIGTRAN_MAP_SMS_TOKEN` | Required 32-128 character base64url-form secret; use a randomly generated token. Diagnostics never include its value. |

MAP SMS requires 14-bit ITU point codes and SCCP service indicator 3.
Set a nonzero `SIGTRAN_ROUTING_CONTEXT` matching the peer's configured AS.
For a peer with a fixed ASP endpoint, set both `SIGTRAN_LOCAL_SCTP_IP`
(unicast IPv4) and `SIGTRAN_LOCAL_SCTP_PORT`; otherwise the kernel selects
the local endpoint.
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

## Operation Inputs

| Operation | Required inputs | Optional inputs |
| --- | --- | --- |
| `sri-sm` | `msisdn`, `serviceCentre` | `gprsSupport` |
| `mo-forward-sm` | `msisdn`, `serviceCentre`, `tpduHex` | None |
| `mt-forward-sm` | `imsi`, `serviceCentre`, `tpduHex` | None |
| `report-sm-delivery-status` | `msisdn`, `serviceCentre`, `deliveryStatus` | None |
| `alert-service-centre` | `msisdn`, `serviceCentre` | None |

Digit strings are ASCII and at most 15 digits. TPDU inputs are 1-200
hex-encoded bytes. Delivery status is `delivered`, `absent-subscriber`
or `memory-capacity-exceeded`. The executor rejects irrelevant fields
and does not queue requests beyond the configured in-flight limit.
Results distinguish success, peer error, reject, malformed result,
dialogue closure and timeout. Only the decoded routing fields of a
successful SRI-SM result are returned; raw TCAP and MAP bytes are not.

## Local HTTP Boundary

Bind the lab host with `ASPNETCORE_URLS=http://127.0.0.1:8080`.
Send JSON to `POST /v1/map-sms/{operation}` with the configured Bearer
token. The route is not registered in operations-only mode. The real
socket peer must be loopback; forwarded address headers are ignored.
Do not expose or proxy this endpoint. The body is limited to 4 KiB
even without a Content-Length, and unknown or duplicate properties
are rejected. Responses use `Cache-Control: no-store`.

The local API returns 200 for Result, 422 for MAP Error, 502 for
Reject/malformed result/early dialogue closure, and 504 for timeout.
Invalid requests return 400, disabled operations 403, busy 429,
and a non-ready stack 503. The API does not retry an operation.

For a manual isolated run, load the [synthetic host settings](../eng/interop/reference-host.example.env)
and supply the token separately. With the authorized lab peer listening:

```bash
set -a
source eng/interop/reference-host.example.env
set +a
export SIGTRAN_MAP_SMS_TOKEN="$(python3 -c 'import secrets; print(secrets.token_urlsafe(32))')"
dotnet run --project src/Sigtran.NET.OperationsHost --configuration Release
```

The existing OperationsHost image can use the same profile. Do not
expose the operation endpoint via ingress or an HTTP proxy. Cluster
SCTP, network policy and failover qualification are separate work.

## Health And Shutdown

`/health/live` only describes the HTTP process. Reference-mode
`/health/ready` requires active M3UA and live SCCP/TCAP readers and
an open admission gate. `/metrics` adds in-flight, accepted,
terminal, busy, unsolicited-invoke and SCCP return counts. Counters
do not include subscriber data; terminal does not mean successful.

On process shutdown the gate closes immediately. Admitted work can
complete before the upper layers stop. The drain waits up to ten
seconds within the host shutdown budget, cancels remaining work
and allows up to two more seconds for cleanup. The host does not
retry work that times out or gets canceled.

## M3UA Route Binding

The reference association sends the configured Routing Context in ASP
Active, ASP Inactive and DATA. A missing or different ASP Active Ack
context is a startup failure. The endpoint admits only SCCP DATA for
its configured point codes, NI and context. The SDK's
`SendOnActiveAssociationAsync` waits for the selected generation's
local transport write. A queued transfer for a lost generation is
not dispatched on its replacement. An uncertain write is a transport
failure, not a reason to retry. See [RFC 4666 section 4.3.4.3](https://www.rfc-editor.org/rfc/rfc4666.html#section-4.3.4.3).

## Independent Peer Matrix

The repository-owned C/lksctp peer does not link to the SDK. Its
reference profile decodes five successful MAP SMS operations, then
returns a MAP user error, a TCAP Reject and an invalid SRI-SM result,
and withholds a response to one invoke. A final successful operation
is preceded by a foreign-context ReturnError that must not reach the
application. A separate activation run sends the wrong ASP Active Ack
context and must not admit any DATA. The C `--self-test` command checks
the negative BER fixtures and M3UA Routing Context encoding offline.

## Rootless Host Lab

```bash
bash scripts/run-reference-service-lab.sh reference
bash scripts/run-reference-service-lab.sh wrong-context
```

The Linux runner builds the real OperationsHost and the independent C
peer, creates a private loopback network namespace and generates an
ephemeral local API token. It does not install services or change host
interfaces. Raw captures, host/peer logs, HTTP outcome summaries and
subscriber-free metric snapshots stay under the unique private run
directory. The verifier checks explicit activation, PPID 3,
nonzero DATA streams, point codes, TCAP transaction/invoke correlation,
dialogue acceptance and the declared negative outcomes. A missing or
contradictory observation prevents `report.json` from being published.

The first strict peer run exposed DATA on SCTP stream zero. The SDK
now sends DATA on ordered stream one and control on stream zero,
with a regression test. Configure at least two SCTP streams.

## Maintained STP Path

Use an extracted Ubuntu/Debian amd64 OsmoSTP bundle with its runtime
libraries. The runner does not install packages or change a system
service. It runs the supplied STP for traffic only inside the private
loopback namespace, using [the fixed lab configuration](../eng/interop/reference-stp.cfg).

```bash
SIGTRAN_MAINTAINED_STP_PREFIX=/absolute/path/to/extracted/root \
  bash scripts/run-reference-service-lab.sh maintained
```

The SDK ASP and the independent C MAP endpoint each connect to the STP.
They use Routing Context 100 and 200 respectively. The verifier compares
the exact Protocol Data bytes across both SCTP legs, validates all
positive and negative application outcomes, and requires a real
M3UA Invalid Routing Context error for the attempted context 201.
The C endpoint waits for that response before closing its association.
The run captures the STP version, binary hashes, configuration and logs.
See the [upstream STP manual](https://downloads.osmocom.org/docs/osmo-stp/1.2.1/osmostp-usermanual.pdf).

The STP is separately maintained; the MAP endpoint is a repository-owned
independent implementation. This is neither a maintained MAP server
qualification nor operator acceptance.

## Qualification Result

The [retained summary](evidence/REFERENCE_SERVICE_20261110.json) records
three passing runs from the same runtime source commit. The direct and
maintained-STP profiles each completed ten admitted HTTP operations:
five distinct positive MAP SMS operations, four negative MAP/TCAP
outcomes, and a successful operation after a foreign-context probe.
The separate wrong-activation profile stayed non-ready and sent no DATA.
The maintained STP forwarded ten invokes and nine responses across two
SCTP legs and returned Invalid Routing Context for the extra probe.

Re-decode the private PCAPs and verify their saved hashes together:

```bash
python3 scripts/collect-reference-service-evidence.py \
  --source-commit <full-tested-runtime-commit> \
  --reference /path/to/direct-run --wrong-context /path/to/rejected-run \
  --maintained /path/to/stp-run
```

Phase 60 completes the outbound ITU MAP SMS reference profile over
M3UA. It does not qualify a MAP responder product, M2PA changeover with
an external stack, cross-host capacity/endurance, production deployment,
or stable release of the exact candidate package.
