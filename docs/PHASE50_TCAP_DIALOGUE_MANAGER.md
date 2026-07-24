# Phase 50 - TCAP Dialogue Manager

Phase 50 adds a concurrent transaction and component manager over the stateful
`ISccpService` contract.

## Delivered Units

| Unit | Delivery |
| --- | --- |
| 1 | Long-running `TcapDialogueManager` lifecycle |
| 2 | Independent local and remote transaction-id correlation |
| 3 | Bounded concurrent dialogue registry |
| 4 | Begin, Continue, End, Abort, and deterministic cleanup |
| 5 | Inbound Invoke component delivery and duplicate tracking |
| 6 | Outbound invoke handles and asynchronous completion |
| 7 | ReturnResult, ReturnError, and Reject correlation |
| 8 | Shared timer sweep for invoke timeout handling |
| 9 | Bounded event/component queues, snapshots, failure state, and metrics |
| 10 | Paired-stack concurrency, outcome, abort, and timeout tests plus docs |

## Manager Topology

```text
MAP service
    |
ITcapDialogues / TcapDialogueManager
  | dialogue registry | invoke registry | timer sweep |
    |
ISccpService
```

`TcapDialogueManager` implements `ITcapDialogues`. The earlier
`TcapDialogueService` remains available for compatibility with callers that only
need simple transaction submission.

## Transaction Correlation

Every active dialogue has:

- an SDK-local `TcapDialogueHandle`;
- a locally allocated TCAP transaction id;
- a peer transaction id after Begin/Continue exchange;
- local and remote SCCP party addresses;
- pending outbound invokes and active inbound invokes.

Inbound Continue, End, and Abort packages are resolved by destination
transaction id. A network Abort or Continue cannot be sent before the peer
transaction id is known.

## Invoke API

`BeginInvokeAsync` opens a dialogue and returns a `TcapInvokeHandle`.
`InvokeAsync` sends a tracked Invoke on an existing dialogue.
`WaitForInvokeAsync` completes with one of:

- `Result`;
- `Error`;
- `Reject`;
- `TimedOut`;
- `DialogueClosed`.

The manager uses one periodic timeout sweep for all pending invokes. Cancellation
of one waiter does not remove or cancel the protocol invoke.

Inbound components are read through `ReceiveComponentAsync`. Applications reply
with `SendResultAsync`, `SendErrorAsync`, or `SendRejectAsync`, optionally ending
the dialogue in the same package.

## Evidence Boundary

Executable paired-stack tests validate concurrent Begin/Invoke/End flows,
transaction-id correlation, ReturnResult, ReturnError, Reject, timeout, Abort,
and cleanup. Independent peer traces and full MAP dialogue evidence remain in
the end-to-end traffic lab.
