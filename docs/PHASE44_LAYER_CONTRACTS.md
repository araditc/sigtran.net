# Phase 44 - Layer Contracts And Package Boundaries

Phase 44 defines product-facing contracts between the SDK layers and wires the current implementation through those contracts.

## Goals

- Expose official interfaces for SCTP, MTP2, MTP3, SCCP, TCAP, and MAP SMS.
- Keep namespace ownership aligned with protocol layers.
- Allow each layer to consume the interface below it instead of a concrete implementation.
- Add adapters where the current implementation already has a usable lower-layer path.
- Document dependency direction for SDK consumers and future package splits.

## Completed Units

| Unit | Capability | Status |
| --- | --- | --- |
| 1 | SCTP association and transport contracts | Complete |
| 2 | Legacy SCTP socket adapter to official transport contract | Complete |
| 3 | MTP2 link contract | Complete |
| 4 | MTP3 network contract and M3UA-backed adapter | Complete |
| 5 | SCCP service contract and connectionless implementation | Complete |
| 6 | TCAP dialogue contract and stateful service implementation | Complete |
| 7 | MAP SMS service contract and TCAP-backed implementation | Complete |
| 8 | Layer contract catalog and dependency-direction checks | Complete |
| 9 | Tests for contract shape and cross-layer composition | Complete |
| 10 | README, architecture, roadmap, and phase documentation alignment | Complete |

## Public Contracts

- `ISctpAssociation`
- `ISctpTransport`
- `IMtp2Link`
- `IMtp3Network`
- `ISccpService`
- `ITcapDialogues`
- `IMapSmsService`

## Implementation Adapters

- `SctpSocketTransportAdapter` lets existing `ISctpSocket` implementations run behind `ISctpTransport`.
- `TcpSctpAdapter` and `NativeSctpSocketAdapter` now implement `ISctpTransport` and `ISctpAssociation`.
- `M3uaTransportSession` can now be constructed with `ISctpTransport`.
- `M3uaMtp3Network` exposes M3UA Payload Data as `IMtp3Network`.
- `SccpConnectionlessService` sends and receives SCCP UDT over `IMtp3Network`.
- `TcapDialogueService` sends TCAP transaction messages over `ISccpService`.
- `MapSmsService` exposes MAP SMS operations over `ITcapDialogues`.

## Completion Criteria

Phase 44 is complete when the SDK has a tested interface chain from transport to MAP SMS and higher layers can be composed from lower-layer interfaces:

```text
IMapSmsService -> ITcapDialogues -> ISccpService -> IMtp3Network -> ISctpTransport
```

The current implementation satisfies this through unit coverage for the contract catalog, interface dependency shape, M3UA-backed MTP3 send path, and MAP SMS composition through TCAP, SCCP, and MTP3 contracts.
