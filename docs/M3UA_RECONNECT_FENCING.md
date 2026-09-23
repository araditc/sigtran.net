# M3UA Reconnect and Generation Fencing

Status: **IMPLEMENTING** under Milestone C / issue #15. This is a dependency-stacked slice on the health-driven Active/Standby work in PR #23; it is not Milestone C completion evidence.

## Problem boundary

An association name is a routing identity, not proof that every transport session opened under that name is the same dispatch generation. After transport loss or reconnect, work accepted by an older session generation must not be silently treated as work owned by the replacement session. In particular, an outcome that may already have reached the transport/peer remains ambiguous and must not be replayed merely because a replacement association becomes healthy.

`M3uaReconnectFencedAssociationSender` adds a generation-aware admission boundary around an existing `IM3uaAssociationSender` without changing the public SDK surface:

- every transport generation is opened explicitly with `ActivateNextGeneration()`;
- a new sender starts fail-closed until a generation is activated;
- `FenceAsync` closes new admission immediately and waits only for sends already admitted to that generation;
- a replacement generation cannot be activated until the previous generation has no in-flight work;
- a proven pre-dispatch `M3uaAssociationSendException` (`DispatchMayHaveOccurred == false`) does not manufacture an association-generation fence; higher-level routing policy may perform its already-governed safe retry/failover decision;
- ambiguous `M3uaAssociationSendException`, cancellation after inner-sender invocation, and unknown sender exceptions fence the current generation as `AmbiguousOutcome` before the exception is rethrown;
- cancellation is rechecked after generation admission and immediately before inner-sender invocation. If already canceled at that boundary it is classified explicitly as caller cancellation and known-not-dispatched, the temporary generation lease is released, and the healthy generation stays open;
- the dispatcher preserves that caller-cancellation classification through its internal outcome, releases the already-acquired route lease, and **does not** call `ApplyDispatchFailureState` for the healthy association. Ordinary association pre-dispatch failures remain eligible for the governed failover path;
- an ambiguity fence has precedence over weaker runtime/admin fence reasons until an explicit replacement generation is activated.

This component does not claim peer acknowledgement, exactly-once delivery, or transaction replay safety. It only defines when a particular local association transport generation may accept new dispatch calls.

## Relationship to route policy

The generation fence is separate from `M3uaAssociationPool` node-routing policy and from negotiated M3UA Traffic Mode Type. The existing dispatcher remains responsible for mapping sender exceptions to `Sent`, `NotDispatched`, or `Ambiguous`, and the existing route pool remains responsible for Active/Standby, Override, Loadshare, Broadcast, routing-context membership, graceful drain and policy fencing.

A caller cancellation that is observed before underlying sender invocation is not evidence that the association failed. It may occur after the dispatcher has already acquired a route lease, so the cancellation identity must survive the sender boundary until that lease is released. Conversely, once the underlying sender has been invoked, cancellation can no longer prove non-delivery and remains ownership-ambiguous.

A later composition step must coordinate runtime/session activation with `ActivateNextGeneration()` and route-health admission. It must not automatically clear an `Ambiguous` route fence or reinterpret `M3uaRuntime.SendAsync` queue acceptance as transport/peer acceptance.

## Deterministic coverage

`ReconnectFenceRegression` and `ReconnectAdmissionRaceRegression` are part of the existing HA dispatch executable and cover:

1. closed generations reject before invoking the underlying sender;
2. explicit runtime/admin fence closes admission while already-admitted work drains;
3. replacement activation is blocked while prior-generation work remains in flight;
4. ambiguous send failure fences the generation and blocks blind replay;
5. a weaker runtime fence cannot erase an ambiguity fence;
6. proven pre-dispatch failure leaves the current generation open for an explicit higher-level retry decision;
7. a deterministic internal pre-admission gate is positioned exactly after the historical early-cancellation-check location and before generation admission. The send starts with an uncancelled token, reaches that gate, is cancelled while held there, then resumes; the post-admission cancellation check must prevent the underlying sender, reconcile the generation lease, and leave the generation unfenced. This directly distinguishes the historical implementation instead of relying on scheduler/thread-state timing;
8. a secondary monitor-contention regression exercises the real generation lock under contention and verifies the same no-send/no-fence boundary;
9. a dispatcher-level regression cancels only after the route lease is already admitted but before the generation-aware sender receives control, then proves the route remains `Active`, route/generation leases reconcile, the transport sender is not invoked, and the transport generation remains open.

The pre-admission callback is an internal deterministic test seam on an internal class; production construction leaves it null and the public SDK surface remains unchanged.

These are synthetic in-process concurrency/ownership tests. They do not satisfy operator/vendor, multi-host, Kubernetes, trusted-signing or stable-release gates.
