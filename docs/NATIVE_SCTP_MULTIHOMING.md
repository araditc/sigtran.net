# Native SCTP Multihoming

Phase 58 of the [qualification roadmap](RELEASE_QUALIFICATION_ROADMAP.md)
covers a single Linux kernel association with multiple paths. Reconnecting
to another address is not proof of failover within the same association.

| Unit | Scope | State |
| --- | --- | --- |
| 1 | Resolved IPv4 endpoint contract | Implemented |
| 2 | Linux address layout and multi-address bind | Pending |
| 3 | Nonblocking multi-address connect | Pending |
| 4 | SDK connector and listener integration | Pending |
| 5 | Kernel notification decoding | Pending |
| 6 | Lifecycle and path notification exposure | Pending |
| 7 | Receive record boundaries and cancellation | Pending |
| 8 | Bounded multistream traffic workload | Pending |
| 9 | Isolated dual-path Linux failure lab | Pending |
| 10 | Evidence checks and qualification decision | Pending |

## Address Contract

The native IPv4 address set keeps caller order and checks the port and resolved
addresses before a socket is configured. All addresses on each side share one
port, and DNS aliases resolving to the same address are rejected. Explicit
sets support up to 16 unicast IPv4 addresses per side. An omitted local set
permits automatic binding; a wildcard is not a path in an explicit set.
See [RFC 6458](https://www.rfc-editor.org/rfc/rfc6458.html) and
[RFC 9260](https://www.rfc-editor.org/rfc/rfc9260.html).
