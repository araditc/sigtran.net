# M3UA Reconnect and Generation Fencing

Status: **IMPLEMENTING** under Milestone C / issue #15. This is a dependency-stacked slice on the health-driven Active/Standby work in PR #23; it is not Milestone C completion evidence.

## Problem boundary

An association name is a routing identity, not proof that every transport session opened under that name is the same dispatch generation. After transport loss or reconnect, work accepted by an older session generation must not be silently treated as work owned by the replacement session. In particular, an outcome that may already have reached the transport/peer remains ambiguous and must not be replayed merely because a replacement association becomes healthy.

`M3uaReconnectFencedAssociationSender` adds a generation-aware admission boundary around an existing `IM3uaAssociationSender` without changing the existing dispatcher outcome taxonomy:

- every transport generation is opened explicitly with `ActivateNextGeneration()`;
- a new sender starts fail-closed until a generation is activated;
- `FenceAsync` closes new admission immediately and waits only for sends already admitted to that generation;
- a replacement generation cannot be activated until the previous generation has no in-flight work;
- a proven pre-dispatch `M3uaAssociationSendException` (`DispatchMayHaveOccurred == false`) does not manufacture an association-generation fence; higher-level routing policy may perform its already-governed safe retry/failover decision;
- ambiguous `M3uaAssociationSendException`, cancellation after inner-sender invocation, and unknown sender exceptions fence the current generation as `AmbiguousOutcome` before the exception is rethrown;
- cancellation is rechecked after generation admission and immediately before inner-sender invocation. If already canceled at that boundary it is converted to a known-not-dispatched send exception, the temporary generation lease is released, and the healthy generation stays open;
- an ambiguity fence has precedence over weaker runtime/admin fence reasons until an explicit replacement generation is activated.

This component does not claim peer acknowledgement, exactly-once delivery, or transaction replay safety. It only defines when a particular local association transport generation may accept new dispatch calls.

## Relationship to route policy

The generation fence is separate from `M3uaAssociationPool` node-routing policy and from negotiated M3UA Traffic Mode Type. The existing dispatcher remains responsible for mapping sender exceptions to `Sent`, `NotDispatched`, or `Ambiguous`, and the existing route pool remains responsible for Active/Standby, Override, Loadshare, Broadcast, routing-context membership, graceful drain and policy fencing.

A later composition step must coordinate runtime/session activation with `ActivateNextGeneration()` and route-health admission. It must not automatically clear an `Ambiguous` route fence or reinterpret `M3uaRuntime.SendAsync` queue acceptance as transport/peer acceptance.

## Deterministic coverage

`ReconnectFenceRegression` is part of the existing HA dispatch executable and covers:

1. closed generations reject before invoking the underlying sender;
2. explicit runtime/admin fence closes admission while already-admitted work drains;
3. replacement activation is blocked while prior-generation work remains in flight;
4. ambiguous send failure fences the generation and blocks blind replay;
5. a weaker runtime fence cannot erase an ambiguity fence;
6. proven pre-dispatch failure leaves the current generation open for an explicit higher-level retry decision;
7. cancellation observed after generation lease admission but before inner-sender invocation remains known-not-dispatched, releases the admitted lease, does not call the transport sender, and does not fence the healthy generation.

These are synthetic in-process concurrency/ownership tests. They do not satisfy operator/vendor, multi-host, Kubernetes, trusted-signing or stable-release gates.
