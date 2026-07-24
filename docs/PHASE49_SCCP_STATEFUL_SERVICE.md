# Phase 49 - SCCP Stateful Service Layer

Phase 49 upgrades SCCP from codec-only building blocks to a long-running
connectionless service over `IMtp3Network`.

## Delivered Units

| Unit | Delivery |
| --- | --- |
| 1 | Stateful `ISccpService` lifecycle and receive ownership |
| 2 | Bounded data and service-return queues |
| 3 | Unified request and indication models for UDT, XUDT, and LUDT |
| 4 | Automatic connectionless message-format selection |
| 5 | Longest-prefix global-title translation to DPC and SSN |
| 6 | Thread-safe application route registration and resolution |
| 7 | Bounded, expiring, ordered XUDT reassembly |
| 8 | Return-on-error handling for unroutable UDT traffic |
| 9 | Cancellation, deterministic stop, failure state, and metrics |
| 10 | Loopback traffic tests, readiness integration, and documentation |

## Service Flow

```text
TCAP or application
        |
    ISccpService
        |
SccpConnectionlessService
  | translation | routing | reassembly | return policy |
        |
   IMtp3Network
```

`SccpConnectionlessService.StartAsync` creates the single owner of
`IMtp3Network.ReceiveAsync`. Decoded messages are delivered to bounded channels,
which apply backpressure instead of allowing unbounded allocation.

## Outbound Selection

`SccpDataRequest` can select UDT, XUDT, LUDT, or automatic mode. Automatic mode
tries UDT first, uses segmented XUDT when configured and the payload fits within
the 16-segment protocol limit, then falls back to LUDT.

When a global-title translation matches, the called party is rewritten to a
route-on-subsystem address and the translated point code becomes the MTP3 DPC.
Rules use deterministic longest-prefix matching.

## Inbound Policy

Inbound UDT, XUDT, and LUDT messages are decoded into
`SccpDataIndication`. Segmented XUDT contexts are keyed by source point code,
segmentation reference, and encoded called/calling addresses. Context count,
payload size, ordering, and inactivity lifetime are bounded.

When routes are configured, unresolved traffic is not delivered. UDT traffic
with return-on-error set is returned as UDTS with
`NoTranslationForThisSpecificAddress`.

## Evidence Boundary

The executable tests validate stateful traffic and policy behavior through an
independent in-process `IMtp3Network` peer. External SCCP traces, XUDTS/LUDTS
coverage, and independent end-to-end MAP traffic remain part of the later
traffic-lab evidence gate.
