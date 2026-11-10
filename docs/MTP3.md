# MTP3 Routing

Phase 3 starts by making the MTP3 boundary explicit. SCCP is carried as an MTP3 user part, so the SDK now exposes service information octet and routing label primitives instead of treating OPC, DPC, NI, MP, and SLS as unrelated integers.

## Service Information Octet

`Mtp3ServiceInformationOctet` stores the service indicator, network indicator, and message priority.

```csharp
Mtp3ServiceInformationOctet sio = new(
    Mtp3ServiceIndicator.Sccp,
    networkIndicator: 2,
    messagePriority: 1);

byte encoded = sio.Encode();
```

For SCCP traffic the service indicator is `Mtp3ServiceIndicator.Sccp`.

## Routing Label

`Mtp3RoutingLabel` models an ITU-style routing label with 14-bit DPC, 14-bit OPC, and 4-bit SLS.

```csharp
Mtp3RoutingLabel label = new(
    destinationPointCode: 0x1234,
    originatingPointCode: 0x2345,
    signallingLinkSelection: 0x0A);

Span<byte> buffer = stackalloc byte[4];
label.Encode(buffer);
```

The label validates point-code and SLS ranges before encoding, which keeps SCCP and M3UA call sites from silently producing malformed protocol data.

## Network Contract

`IMtp3Network` is the public MTP3 transfer contract consumed by SCCP and other MTP3 users. It exchanges `Mtp3TransferMessage` values that pair a service information octet, routing label, user payload, and optional adaptation-layer values such as Network Appearance, Routing Context, and Correlation Id.

`M3uaMtp3Network` adapts `M3uaTransportSession` to `IMtp3Network`, so SCCP and higher layers can work against the MTP3 contract without depending on the concrete M3UA session implementation.

## MTP2 Service Data

The ITU MSU at the MTP3/MTP2 service boundary is SIO, a four-byte
routing label, and nonempty user-part or network-management data.
The codec rejects truncated MSUs and does not silently drop M3UA-only
adaptation parameters. The M2PA-specific spare octet is not part of the
MTP3 codec. See [ITU-T Q.704](https://www.itu.int/rec/T-REC-Q.704/en)
and [RFC 4165 section 2.3.1](https://www.rfc-editor.org/rfc/rfc4165.html#section-2.3.1).

| Unit | Runtime acceptance | Status |
| --- | --- | --- |
| 1 | ITU MSU byte layout and input validation | Implemented |
| 2 | M2PA framing at the MTP2 service boundary | Implemented |
| 3 | Linkset, routeset and stable SLS selection | Implemented |
| 4 | MTP3 transfer network and bounded receive ownership | Implemented |
| 5 | Network management framing and route state | Implemented |
| 6 | 24-bit link retrieval and peer confirmation | Implemented |
| 7 | Extended changeover and emergency handling | Implemented |
| 8 | Acknowledged changeback and ordered traffic | Implemented |
| 9 | SCCP traffic against an independent wire peer | Verified on Linux |
| 10 | Live fault evidence and qualification decision | Pending |

Pass `M2paLink.CreateMtp2ServiceLink()` as the MTP2 service endpoint.
It adds the RFC 4165 spare octet on send and removes it on receive. The
application still owns the underlying M2PA link and its alignment lifecycle.

## Linksets And Routesets

Bind one MTP2 service endpoint per `Mtp3LinkBinding`. Links to the
same adjacent point code form a linkset, with unique four-bit link
codes. Each `Mtp3Route` connects a destination to a configured linkset;
lower priority values are preferred and ties use ascending adjacent
point code. Within a linkset, SLS starts at a bucket in sorted link-code
order and walks forward only if that link is unavailable. A single
unavailable link does not redistribute healthy buckets.

`Mtp3LinkNetwork` implements `IMtp3Network` for one ITU point code
and network indicator. It owns one reader per configured link and
backpressures a bounded receive queue. It delivers only local user-part
traffic; it is not an STP and does not relay incoming traffic to other
destinations. Dispose the network before disposing its links. A send
that fails after dispatch is not automatically retried on another link.

## Signalling Network Management

The endpoint consumes Q.704 TFP and TFA and, for national NI values,
TFR. It applies them only to the announced destination via the
configured neighbour that supplied the message. Prohibited routes
are excluded; restricted routes rank below available unrestricted
routes. Wrong NI, DPC or neighbour, unknown route, unsupported heading,
and malformed lengths or point-code spare bits cannot change routing.
Management frames never reach SCCP. This is an endpoint route policy,
not a complete STP routing or MTP restart implementation.

`IMtp2LinkRecovery` optionally adds a frozen receive boundary and
peer-confirmed retrieval to the ordinary MTP2 contract. Retrieval
accepts only the last acknowledged boundary or a sequence in the
frozen window, including wrap from `0xFFFFFF` to zero. It returns only
subsequent MSUs in send order. Unknown sequences never trigger replay.

## Link Changeover

The network observes an established link failing or an order from its
adjacent peer. It holds outbound user traffic during changeover, drains
locally accepted ingress to the upper queue, and exchanges Q.2210
XCO/XCA on a surviving link in the same linkset. The label identifies
the affected link code, not the carrier. Retrieved local user MSUs
are replayed before new traffic, only with a reconciled peer boundary
and while their route remains permitted. See [RFC 4165 section 4.2.3](https://www.rfc-editor.org/rfc/rfc4165.html#section-4.2.3)
and [ITU-T Q.2210](https://www.itu.int/rec/T-REC-Q.2210/en).

Emergency or timed-out changeover permits new traffic to take an
available route without replaying uncertain transmissions. A failed
replay is not blindly retried. `IsLinkDiverted` reports that diversion
finished; it does not certify that all pre-fault transfers were delivered.
The application must wait for diversion before replacing a failed
association. Transient Busy is backpressure, not a changeover trigger.

## Acknowledged Changeback

After diversion and realignment, call `ChangebackAsync` for the recovered
link. The network pauses new sends while it sends a Q.704 CBD on each
operational alternate in the same linkset. A reader acknowledges a
peer's CBD after it has queued the preceding traffic from that link.
Each barrier uses a distinct eight-bit code and must receive its own
CBA. On a timeout the network repeats unconfirmed barriers once and
then keeps the link diverted if acknowledgements are still missing.
It does not implement time-only promotion or a cross-adjacency
ordering guarantee. An individual user send has a five-second
lower-layer dispatch budget.

## Independent SCCP Traffic Lab

On Linux with kernel SCTP, lksctp development headers, .NET 10, gcc,
iproute2, util-linux, dumpcap and TShark, run:

```bash
bash scripts/run-mtp3-m2pa-lab.sh
```

The runner starts a C/lksctp peer and the SDK in a private loopback-only
network namespace without sudo. The peer independently validates the
M2PA spare octet, ITU routing label and SCCP class-0 UDT bytes with PC/SSN
addresses. It constructs a reversed routing label and a distinct reply,
not a raw echo. The SDK validates both responses, and the capture check
requires two UDTs in each direction with PPID 5, stream 1 and SLS 10.
The run is bounded to the declared synthetic fixture and does not
represent operator STP interoperation.
