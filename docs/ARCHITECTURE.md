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

## Design Principles

- Protocol encoders write into caller-provided buffers where practical.
- Decoders validate lengths, padding, and required parameters before exposing typed messages.
- Public APIs must carry XML documentation because the package is intended for external SDK users.
- Experimental layers stay clearly labelled until they match the relevant standards.
- Transport concerns are kept behind interfaces so M3UA can run over production SCTP later without changing protocol APIs.

## M3UA Flow

1. A transport implementation receives an M3UA byte stream from an SCTP association.
2. `M3uaMessage` validates the common header and exposes the parameter block.
3. `M3uaParameterReader` walks RFC-style TLV parameters and skips padding.
4. `M3uaTypedMessageParser` converts generic messages into typed DATA, ASPSM, ASPTM, management, SSNM, and RKM objects.
5. `M3uaInboundProcessor` can orchestrate decode, typed dispatch, ASP acknowledgement state updates, and DATA route resolution.
6. `M3uaOutboundProcessor` can apply association defaults and optional ASP active-state policy while building outbound messages.
7. `M3uaTransportSession` connects processors to an `ISctpSocket` for async send/receive flows.
8. `M3uaAspClient` can run common ASP startup, heartbeat, and shutdown handshakes over the transport session.
9. `M3uaRkmClient` can run dynamic Routing Key registration and deregistration handshakes.
10. `M3uaPayloadRouteTable` can resolve typed DATA messages to application routes.
11. `M3uaAspSession` applies acknowledgement messages to the local ASP state machine.
12. Higher layers consume Protocol Data only after M3UA state and routing context checks are satisfied.

## Production Boundaries

The current SDK should be consumed as an M3UA-focused alpha. SCCP, TCAP, and MAP APIs are not stable yet. Any commercial integration should isolate those experimental APIs behind application-owned adapters until their encodings are replaced.

## Planned Stabilization Order

1. M3UA binary correctness and public API polish.
2. Production SCTP transport and association lifecycle events.
3. Standards-based SCCP encode/decode.
4. TCAP external interoperability vectors and MAP profile validation.
5. MAP SMS external interoperability vectors and operator-profile validation.
