# M2PA

SIGTRAN.NET implements the MTP2 User Peer-to-Peer Adaptation Layer defined by
[RFC 4165](https://www.rfc-editor.org/rfc/rfc4165.html). The implementation is
available through `M2paLink`, which implements the package-neutral
`IMtp2Link` contract over `ISctpTransport`.

## Transport Contract

M2PA uses SCTP Payload Protocol Identifier `5` and ordered delivery.

| Traffic | SCTP stream |
| --- | --- |
| Link Status, alignment, proving, Busy, Busy Ended, Out of Service | `0` |
| User Data, acknowledgements, Processor Outage, Processor Recovered, recovery Ready | `1` |

`M2paProtocol.TryValidateSctpMetadata` rejects an incorrect PPID, unordered
delivery, or a message received on the wrong stream. Alignment Ready is accepted
on stream `0`; processor-recovery Ready is accepted on stream `1`.

## Link Lifecycle

`M2paLink.StartAsync` performs the following sequence:

1. Send Out of Service and Alignment.
2. Wait for peer Alignment or Proving.
3. Enter normal or emergency proving.
4. Send Ready and wait for peer Ready.
5. Enter `Mtp2LinkState.InService`.

The proving duration and alignment timeout are configurable through
`M2paLinkOptions`. `StopAsync` sends Out of Service before stopping the receive
loop. `RecoverAsync` accepts a replacement SCTP transport, resets association
sequence state, and performs alignment again.

## User Data And Acknowledgement

User Data uses 24-bit forward and backward sequence numbers with wraparound at
`0xFFFFFF`. Non-empty outbound messages increment FSN and remain in
`M2paRetrievalBuffer` until the peer BSN acknowledges them. Empty User Data is an
acknowledgement and does not increment FSN.

Inbound User Data must contain the next expected FSN. Out-of-sequence messages
are discarded and counted. Accepted payloads are delivered through the bounded
receive queue and acknowledged unless local Busy or Processor Outage handling
temporarily defers acknowledgement.

M2PA relies on SCTP reliability while an association remains available. The
retrieval buffer exists for MTP3 changeover retrieval after link failure; it is
not used to retransmit messages on the same link.

## Congestion And Processor Outage

`SetLocalBusyAsync` sends Busy or Busy Ended. A peer Busy status pauses
`SendAsync` through an asynchronous gate until Busy Ended is received.

`SetLocalProcessorOutageAsync` implements Processor Outage and recovery status
exchange. Recovery waits for peer Ready, replies with Ready on stream `1`, and
only then restores the link to service when the remote processor is available.

## Observability

`M2paLink.StateChanged` reports operational transitions. `GetMetrics` returns:

- sent and received User Data;
- sent and received acknowledgement-only messages;
- sent and received Link Status messages;
- acknowledged outbound messages;
- discarded out-of-order messages;
- current retrieval depth.

These counters are point-in-time process metrics. Applications should export
them through their selected metrics system and apply deployment-specific labels.

## Validation Status

The SDK test suite validates framing, 24-bit sequence wrap, retrieval
acknowledgement, SCTP PPID/stream/ordering policy, alignment, proving, Ready,
payload delivery, Busy backpressure, processor-outage recovery, metrics, and
graceful stop through an independent in-process peer implementation.

The runtime foundation is complete. Independent external M2PA peer PCAP and
long-running failure evidence remain part of the end-to-end lab gate; this
document does not claim that external evidence has already been retained.
