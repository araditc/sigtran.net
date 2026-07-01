# Architecture

SIGTRAN.NET is organized as layered protocol components with explicit boundaries between transport, adaptation, network signalling, transaction handling, and application profiles.

## Layer Model

| Layer | Responsibility | Current maturity |
| --- | --- | --- |
| SCTP transport | Association lifecycle, streams, PPID, reconnect, async I/O | Contract only; TCP adapter is development-only |
| M3UA | MTP3 user adaptation, ASP state, routing context, SSNM, management, RKM | Active production path |
| SCCP | Global title and subsystem routing, UDT/XUDT/LUDT | Experimental proof of concept |
| TCAP | Dialogues, components, transaction IDs, BER encoding | Foundation complete; interop vectors pending |
| MAP | SMS operation profiles over TCAP | Foundation complete; interop vectors pending |
| Tooling | Trace formatting, vectors, simulators, samples, CI profile, readiness reports | Foundation complete; external lab evidence pending |

## Design Principles

- Protocol encoders write into caller-provided buffers where practical.
- Decoders validate lengths, padding, and required parameters before exposing typed messages.
- Public APIs must carry XML documentation because the package is intended for external SDK users.
- Experimental layers stay clearly labelled until they match the relevant standards.
- Transport and protocol-layer concerns are kept behind interfaces so M3UA, SCCP, TCAP, and MAP can swap lower-layer implementations without changing upper-layer APIs.

## Official Layer Contracts

| Layer | Contract | Lower Contract |
| --- | --- | --- |
| SCTP association | `ISctpAssociation` | Platform association state |
| SCTP transport | `ISctpTransport` | `ISctpAssociation` |
| MTP2 link | `IMtp2Link` | SCTP, M2PA, or physical link provider |
| MTP3 network | `IMtp3Network` | `IMtp2Link` or M3UA adapter |
| SCCP service | `ISccpService` | `IMtp3Network` |
| TCAP dialogues | `ITcapDialogues` | `ISccpService` |
| MAP SMS service | `IMapSmsService` | `ITcapDialogues` |

See [Layer Contracts](LAYER_CONTRACTS.md) for the product-facing dependency direction and composition guidance.

## M3UA Flow

1. A transport implementation receives an M3UA byte stream from an SCTP association.
2. `M3uaMessage` validates the common header and exposes the parameter block.
3. `M3uaParameterReader` walks RFC-style TLV parameters and skips padding.
4. `M3uaTypedMessageParser` converts generic messages into typed DATA, ASPSM, ASPTM, management, SSNM, and RKM objects.
5. `M3uaInboundProcessor` can orchestrate decode, typed dispatch, ASP acknowledgement state updates, and DATA route resolution.
6. `M3uaOutboundProcessor` can apply association defaults and optional ASP active-state policy while building outbound messages.
7. `M3uaTransportSession` connects processors to an `ISctpTransport` for async send/receive flows.
8. `M3uaAspClient` can run common ASP startup, heartbeat, and shutdown handshakes over the transport session.
9. `M3uaRkmClient` can run dynamic Routing Key registration and deregistration handshakes.
10. `M3uaPayloadRouteTable` can resolve typed DATA messages to application routes.
11. `M3uaAspSession` applies acknowledgement messages to the local ASP state machine.
12. Higher layers consume Protocol Data only after M3UA state and routing context checks are satisfied.
13. Tooling helpers can render traces, compare vectors, build deterministic scripts, and report release readiness without binding the protocol stack to a specific logger or test runner.

## Production Boundaries

The current SDK should be consumed as an M3UA-focused alpha with official layer contracts available for application composition. SCCP, TCAP, MAP, and interoperability tooling foundations are present, but production integrations must still isolate experimental APIs and collect external interoperability evidence before production claims.

## Planned Stabilization Order

1. M3UA binary correctness and public API polish.
2. Production SCTP transport and association lifecycle events.
3. Standards-based SCCP encode/decode.
4. TCAP external interoperability vectors and MAP profile validation.
5. MAP SMS external interoperability vectors and operator-profile validation.
6. External interoperability lab runs, native SCTP verification, and release automation hardening.
