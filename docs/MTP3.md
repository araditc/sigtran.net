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
| 5 | Network management framing and route state | Pending |
| 6 | 24-bit link retrieval and peer confirmation | Pending |
| 7 | Extended changeover and emergency handling | Pending |
| 8 | Acknowledged changeback and ordered traffic | Pending |
| 9 | SCCP traffic against an independent wire peer | Pending |
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
