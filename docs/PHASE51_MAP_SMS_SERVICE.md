# Phase 51 - MAP SMS Service

## Objective

Move MAP SMS from deterministic codecs and transaction builders to a stateful,
interface-driven application service over the concurrent TCAP dialogue manager.

## Delivered Runtime

- `ITcapComponentDialogues` separates correlated component operations from the
  concrete `TcapDialogueManager`.
- `MapSmsService` executes and correlates SRI-SM, MO/MT ForwardSM,
  ReportSM-DeliveryStatus, and AlertServiceCentre.
- `MapSmsOperationResult` exposes Result, MAP Error, Reject, timeout, and
  dialogue-close outcomes without leaking TCAP internals into application code.
- `MapSmsServer` validates and decodes inbound invokes, dispatches typed async
  handlers, and sends ReturnResult, ReturnError, or Reject.
- `MapSmsOperationProfiles` supplies the TS 29.002 operation values,
  application contexts, and default timeouts.
- Cancellation aborts an outstanding dialogue where possible. TCAP timeout
  correlation and bounded component queues remain owned by the lower layer.
- Server metrics cover received, completed, errored, rejected, malformed, and
  failed-handler operations.

## Compatibility

The original `Send*Async` methods and `MapSmsTcapClient` remain available.
Legacy TCAP operation enum tokens retain their values for source and byte-vector
compatibility. MAP profiles place their actual local operation values on the
wire through explicit application-profile conversion.

## Verification

The paired-stack test composes:

`MapSmsService -> TcapDialogueManager -> SccpConnectionlessService -> IMtp3Network`

It sends all five operations across two independent stack instances, validates
typed server decoding, checks four successful results, and verifies
`absentSubscriberForSM` ReturnError mapping. The full solution build and test
suite pass with zero warnings.

## Remaining Gate

This phase proves application behavior inside the SDK. Independent
MAP/TCAP/SCCP/M3UA/SCTP peer traffic, PCAP field comparison, and operator
profile evidence are intentionally assigned to the end-to-end traffic lab.
