# M3UA Reconnect and Generation Fencing

Status: **IMPLEMENTING** under Milestone C / issue #15. Parent health-driven Active/Standby PR #23 is merged; this slice now targets canonical main for exact-head CI and independent review. It is not Milestone C completion evidence.

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

## Deterministic coverage and execution

Seven reconnect scenarios from `ReconnectFenceRegression` and `ReconnectAdmissionRaceRegression` are explicitly registered in the existing HA dispatch executable's normal async entry point. Each runs once through the existing PASS/FAIL runner. They cover:

1. closed generations reject before invoking the underlying sender;
2. explicit runtime/admin fence closes admission while already-admitted work drains, prevents replacement overlap, and allows explicit generation advancement only after drain;
3. ambiguous send failure fences the generation, blocks new dispatch, and retains precedence over a weaker runtime fence;
4. proven pre-dispatch failure leaves the current generation open for an explicit higher-level retry decision;
5. cancellation injected synchronously at the exact internal pre-admission seam preserves caller-owned known-not-dispatched classification, zero transport calls, unchanged/open generation and reconciled leases;
6. a dispatcher-level regression cancels only after the route lease is already admitted but before the generation-aware sender receives control, then proves the route remains `Active`, route/generation leases reconcile, selection accounting remains one, and the generation remains open;
7. a worker starts with an uncancelled token and is held at that same pre-admission seam; the controlling async test injects cancellation while the worker is held, then releases it. This crosses the historical early-check location and requires the post-admission recheck to stop transport invocation.

The pre-admission callback is an internal deterministic test seam on an internal class; production construction leaves it null and the public SDK surface remains unchanged. The obsolete reflection/`Monitor.LockContentionCount` test has been replaced by per-sender deterministic injection: a process-wide contention count cannot establish which lock a specific send reached.

Reconnect tests do not run blocking async work in a `ModuleInitializer`. Module initialization precedes other module execution; waiting on workers/continuations from inside initialization can prevent those workers from running. See Microsoft's [module initializer reference](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/module-initializer) and [initialization deadlock explanation](https://devblogs.microsoft.com/dotnet/static-constructor-deadlocks/). Gated tests release and join their workers in `finally`, with bounded waits, so assertion failures cannot leave admitted work awaiting an unreleased test gate.

Run with the existing command; no new test project or runner policy is required:

```text
dotnet run --project src/Sigtran.NET.HaDispatchTests/Sigtran.NET.HaDispatchTests.csproj --configuration Release
```

Exact source/base/merge-ref, executed test counts and retained artifacts belong to PR #24 and the durable issue #15 checkpoint, not to an assumed future PASS. These are synthetic in-process concurrency/ownership tests. They do not satisfy operator/vendor, multi-host, Kubernetes, trusted-signing or stable-release gates.
