# Native SCTP Multihoming

Phase 58 of the [qualification roadmap](RELEASE_QUALIFICATION_ROADMAP.md)
covers a single Linux kernel association with multiple paths. Reconnecting
to another address is not proof of failover within the same association.

| Unit | Scope | State |
| --- | --- | --- |
| 1 | Resolved IPv4 endpoint contract | Implemented |
| 2 | Linux address layout and multi-address bind | Implemented |
| 3 | Nonblocking multi-address connect | Implemented |
| 4 | SDK connector and listener integration | Implemented |
| 5 | Kernel notification decoding | Implemented |
| 6 | Lifecycle and path notification exposure | Implemented |
| 7 | Receive record boundaries and cancellation | Implemented |
| 8 | Bounded multistream traffic workload | Verified on Linux |
| 9 | Isolated dual-path Linux failure lab | Verified on Linux |
| 10 | Evidence checks and qualification decision | Verified for the isolated Linux profile |

## Address Contract

The native IPv4 address set keeps caller order and checks the port and resolved
addresses before a socket is configured. All addresses on each side share one
port, and DNS aliases resolving to the same address are rejected. Explicit
sets support up to 16 unicast IPv4 addresses per side. An omitted local set
permits automatic binding; a wildcard is not a path in an explicit set.
See [RFC 6458](https://www.rfc-editor.org/rfc/rfc6458.html) and
[RFC 9260](https://www.rfc-editor.org/rfc/rfc9260.html).

Linux multi-bind passes an ordered array of 16-byte `sockaddr_in` values
to `sctp_bindx` with `SCTP_BINDX_ADD_ADDR`. The family is native endian;
the port and address are network endian. A failed bind is surfaced and
must not be converted to a wildcard or single-path bind.

`sctp_connectx` submits the ordered peer list to one nonblocking socket.
After a pending connect, readiness is followed by a `SO_ERROR` check.
Cancellation is observed before the syscall and while polling for completion;
the caller owns and closes the socket if the attempt fails or is cancelled.

`NativeSctpConnector.ConnectMultiHomedAsync` accepts
`SctpMultiHomingEndpointSet`; `NativeSctpListener.StartMultiHomedAsync`
accepts an ordered local list. Match the first endpoint to the corresponding
primary in the existing options. The first local address uses managed bind
so accepted sockets retain normal .NET behavior; the rest use `sctp_bindx`.
Both APIs require Linux kernel metadata and reject invalid lists before
opening a socket. Existing single-address calls are unchanged.

## Kernel Notifications

The [Linux SCTP UAPI](https://github.com/torvalds/linux/blob/master/include/uapi/linux/sctp.h)
notification decoder checks the common length, the type-specific structure and
IPv4 peer address fields. Association-change, peer-address-change, shutdown
and partial-delivery notifications have distinct layouts. Native integer
fields are host endian; peer port and IPv4 bytes remain network order.

On a native adapter, `SnapshotKernelNotifications()` returns the latest
64 decoded records in receive order. Notifications are read while the
application receives and never returned as user payload. Linux path states
0/1/4/5/6 indicate available/unreachable/made-primary/confirmed/potentially
failed, respectively. A path change does not invalidate the association.
Communication loss and shutdown are reflected in the lifecycle journal.

## Message Boundaries

The native adapter serializes readers and returns one record only after
`MSG_EOR`. An oversized record is drained under the receive deadline and
reported as `InvalidDataException`, without delivering a truncated message.
If cancellation or an error interrupts an unfinished record, the adapter
fails and closes its socket so a later receive cannot return the tail as
a new message. A cancellation before reading any fragment leaves the
association available.

## Traffic Workload

The native lab's `multihoming-client` and `multihoming-server` modes use
one association, four streams, three synthetic PPIDs and 48 ordered
request/reply checks with mixed record sizes up to 8 KiB. The client
cancels one idle receive and the server rejects a 4 KiB probe using an
eight-byte buffer, then receives the next complete record. The optional
fault handshake holds exactly one response after the request reaches the
server. No reconnect is enabled in this workload.

## Isolated Path-Loss Run

On a Linux host with kernel SCTP, lksctp, .NET 10, iproute2, util-linux,
dumpcap, TShark and Python 3.10 or later, run:

```bash
SIGTRAN_ARTIFACT_ROOT="$HOME/sigtran-lab/artifacts" bash scripts/run-native-sctp-multihoming-lab.sh
```

Run as an unprivileged lab user with user and network namespaces enabled.
The runner creates two private network namespaces connected by two veth
pairs, with no default route. It sets shorter SCTP timers inside the
endpoint namespaces only, then takes both ends of the primary link down
while the client is waiting for sequence 16's echo. The alternate link
stays up. Neither endpoint reconnects. The runner captures SCTP only on
the private client namespace; it does not use host interfaces, routes,
firewall rules or other services.

The unique run directory holds a PCAP, SDK traces, kernel and runtime
versions, topology, the UTC cut timestamp, a TShark SCTP field table, and
`report.json`. Success requires one INIT/COOKIE exchange, an unchanged
verification tag in each direction, primary-path DATA before the cut and
alternate-path DATA after it. All four streams and all three PPIDs must
appear on the alternate in both directions. Each SDK trace must contain
48 validated records, one COMM_UP, the original path's unreachable
notification, and no reconnect. The request for sequence 16 must reach
the server before the cut and its response reach the client after it.

Recheck retained evidence directly from the PCAP and the SDK traces:

```bash
python3 scripts/verify-sctp-multihoming-evidence.py /path/to/sctp-multihoming.RUN
```

## Qualification Result

The [retained report](evidence/NATIVE_SCTP_MULTIHOMING_20261110.json)
records a passing live run with 48 round trips, four streams, three PPIDs,
zero capture drops, a single handshake and zero reconnects. Both kernel
endpoints reported their original peer address unreachable. The raw PCAP,
traces, capture log and topology are private; their SHA-256 digests are in
the report.

Phase 58 implementation and the declared isolated Linux test are complete.
The test covers one primary-path cut, not failback, sustained load, IPv6,
independent SCTP implementations or failure across physical hosts. It is
not a release-candidate evidence bundle. The stable evaluator and defect
ledger retain their independent evidence requirements.
