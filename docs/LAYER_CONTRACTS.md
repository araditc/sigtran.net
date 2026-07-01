# Layer Contracts

SIGTRAN.NET now exposes official contracts between the protocol layers so applications can substitute implementations without rewriting the layers above them.

## Dependency Direction

The direction is intentionally downward:

| Layer | Public Contract | Depends On |
| --- | --- | --- |
| SCTP association | `ISctpAssociation` | Platform association state |
| SCTP transport | `ISctpTransport` | `ISctpAssociation` |
| MTP2 link | `IMtp2Link` | SCTP, M2PA, or another link provider |
| MTP3 network | `IMtp3Network` | `IMtp2Link` or an M3UA network adapter |
| SCCP service | `ISccpService` | `IMtp3Network` |
| TCAP dialogues | `ITcapDialogues` | `ISccpService` |
| MAP SMS service | `IMapSmsService` | `ITcapDialogues` |

Upper layers should depend on the public contract of the layer below them, not on concrete classes such as a specific socket adapter, session implementation, or lab runner.

## SCTP

`ISctpTransport` is the transport contract for complete SCTP user messages. It carries `SctpOutboundMessage` and `SctpReceiveResult`, preserving stream id, PPID, unordered delivery intent, and association visibility through `ISctpAssociation`.

`SctpSocketTransportAdapter` adapts the older `ISctpSocket` shape to `ISctpTransport`. `TcpSctpAdapter` and `NativeSctpSocketAdapter` implement the transport contract directly, so existing users can migrate incrementally.

## MTP2 And MTP3

`IMtp2Link` is the MTP2-compatible link boundary intended for M2PA or physical-link style providers.

`IMtp3Network` is the MTP3 transfer boundary consumed by SCCP. `M3uaMtp3Network` adapts `M3uaTransportSession` to this contract, giving upper layers an MTP3 view over M3UA Payload Data messages.

## SCCP, TCAP, And MAP

`ISccpService` exposes SCCP Unitdata primitives over `IMtp3Network`. `SccpConnectionlessService` provides a connectionless UDT implementation.

`ITcapDialogues` exposes Begin, Continue, End, and Receive dialogue primitives over `ISccpService`. `TcapDialogueService` provides a stateful dialogue service over SCCP Unitdata.

`IMapSmsService` exposes SMS-oriented MAP operations over `ITcapDialogues`. `MapSmsService` composes the existing MAP SMS TCAP builder with the dialogue contract.

## Consumer Rule

Applications should wire dependencies like this:

```csharp
ISctpTransport sctp = CreateProductionSctpTransport();
using M3uaTransportSession m3uaSession = new(sctp);
IMtp3Network mtp3 = new M3uaMtp3Network(m3uaSession);
ISccpService sccp = new SccpConnectionlessService(mtp3, routingLabel);
ITcapDialogues tcap = new TcapDialogueService(sccp);
IMapSmsService map = new MapSmsService(tcap, calledParty, callingParty);
```

This keeps replacement points explicit. A production Linux SCTP transport, an M2PA-backed MTP2 link, or a different TCAP dialogue manager can be swapped in without changing the layers above the relevant interface.
