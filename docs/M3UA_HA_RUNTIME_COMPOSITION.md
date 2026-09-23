# M3UA Multi-Association Runtime Composition

Status: **IMPLEMENTING** under roadmap Milestone C and tracking issue #15.

This document describes the bounded runtime-composition slice implemented after the HA routing, dispatch, coordinator, and graceful-drain slices. It does not declare Milestone C complete and does not promote any stable-release evidence gate.

## Responsibility boundary

`M3uaHaRuntimeSupervisor` composes independently managed `M3uaRuntime` instances for association lifecycle isolation and inbound MTP3 fan-in. Each production lane delegates to the existing `M3uaRuntime` contract through `M3uaRuntimeAssociationLane`; the supervisor does not replace SCTP/session startup, ASP state handling, reconnect policy, heartbeat processing, or the existing bounded per-runtime queues.

The aggregate inbound channel is bounded and uses wait-based backpressure. Each delivered transfer retains its source association name. Per-lane snapshots expose the observed runtime state, received-transfer count, fault count, and the underlying `M3uaRuntimeMetrics`.

Association startup is independent. The supervisor becomes usable when the first lane reaches its initial active state; a slow or reconnecting peer is not a readiness barrier for a healthy association. If every configured lane fails before any lane activates, startup fails closed. A terminal fault cancels only that lane's receive pump; healthy lanes remain available.

The supervisor may also bind to the existing `M3uaAssociationPool`. Binding requires exact association-name membership between runtime lanes and the route pool; partial binding fails closed. Once bound, live `M3uaRuntimeState` is tracked separately from local `M3uaAssociationOperationalState`: node role remains policy, while runtime health controls whether that role is currently eligible for new dispatch. A stopped, starting, stopping, reconnecting, or faulted bound runtime is not selectable; an active runtime is eligible subject to the existing role, drain, routing-context, and traffic-mode rules.

This separation is deliberate. Recoverable runtime health loss must not silently turn an Active route into Standby/Faulted policy state or reinterpret negotiated M3UA Traffic Mode Type. `M3uaRuntime` raises `FaultObserved` before its later reconnect transition, so the supervisor treats a recoverable fault observation as immediately reconnecting for route-admission purposes. This closes the interval in which a known-faulting association could otherwise receive new dispatches. Explicit runtime recovery to `Active` restores eligibility without changing node role.

Only lifecycle/recovery events (`StateChanged`, `AspActivated`, `ShutdownCompleted`, `FaultObserved`, and `ReconnectScheduled`) update observed route health. Transfer and heartbeat diagnostics can carry a pre-fault `Active` snapshot; they do not prove recovery and cannot reopen a reconnecting or terminally faulted route. Unknown event kinds also do not change route health. A delayed first-activation completion updates initial health only while the supervisor still observes `Starting`; it must not overwrite a newer fault already observed before that continuation resumes. Successful `StartAsync` means first activation occurred, not that every lane is currently healthy; current admission always uses bound route health.

The supervisor is intentionally one-way for this slice: after it has stopped, the same supervisor instance is not restartable. Association runtime reconnect behavior remains owned by each `M3uaRuntime` according to its configured `SctpReconnectPolicy`.

## Outbound semantic boundary

This slice does **not** route HA outbound traffic through `M3uaRuntime.SendAsync`. That method admits a transfer to a bounded runtime queue and therefore cannot, by itself, prove SCTP/peer/network acceptance.

HA outbound delivery remains owned by the reviewed association-pool, dispatcher, and coordinator path, which distinguishes `Sent`, `NotDispatched`, `Ambiguous`, and `NoRoute` outcomes and does not blindly replay a transfer whose network acceptance may already be uncertain. Live runtime health narrows route admission; it does not change transaction ownership or convert an ambiguous send into a retryable pre-dispatch failure.

## Deterministic qualification

`src/Sigtran.NET.HaRuntimeTests` contains seventeen deterministic synthetic-lane scenarios covering:

1. duplicate association identity rejection;
2. exact runtime-lane/route-pool membership enforcement;
3. all-lane startup failure;
4. one startup failure with a healthy peer;
5. a slow-starting lane with a healthy peer;
6. concurrent startup callers sharing one readiness boundary;
7. cancellation of one startup waiter without cancelling shared startup;
8. capacity-one aggregate backpressure with source-association preservation;
9. terminal lane-fault isolation while a healthy peer continues receiving;
10. live runtime-health binding to route admission, including immediate exclusion on `FaultObserved` and eligibility restoration after explicit recovery;
11. concurrent shutdown callers sharing one shutdown operation;
12. cancellation of one shutdown waiter without cancelling shared cleanup;
13. peer shutdown continuation and diagnostics after a synchronous lane-stop failure;
14. rejection of a late runtime callback captured before event-handler detach;
15. stale transfer/heartbeat/unknown diagnostics cannot clear a recoverable fault, while explicit ASP activation restores eligibility;
16. stale diagnostics cannot clear terminal fault state or divert traffic from the healthy peer;
17. first-activation completion cannot clear a fault reported before startup returns.

`src/Sigtran.NET.HaTests` additionally contains synchronous route-policy health regressions that verify reconnect exclusion/recovery and override fallback/recovery without conflating local policy with protocol traffic-mode negotiation.

Run the async runtime-composition harness locally with:

```text
dotnet run --project src/Sigtran.NET.HaRuntimeTests/Sigtran.NET.HaRuntimeTests.csproj --configuration Release
```

The repository CI is configured to run the same executable. Until an exact-head workflow run exists for a changed source head, these scenarios are implementation coverage rather than CI evidence. These tests establish deterministic SDK behavior only; they are not multi-host, operator/vendor, Kubernetes, or production-network evidence.

## Remaining Milestone C work

This slice does not yet complete the full multi-association runtime milestone. Remaining integration includes deterministic active/standby failover behavior driven by bound runtime health, reviewed reconnect/fencing coordination with dispatch outcomes, graceful drain of live sessions, and final composition of live runtime health with the existing HA outbound coordinator without weakening `Ambiguous` send handling or introducing blind replay.

Milestone D multi-host qualification must use genuinely separate authorized hosts/peers and retained topology-specific evidence; same-host deterministic tests do not satisfy that gate.
