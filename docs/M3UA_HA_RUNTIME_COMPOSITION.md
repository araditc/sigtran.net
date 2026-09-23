# M3UA Multi-Association Runtime Composition

Status: **IMPLEMENTING** under roadmap Milestone C and tracking issue #15.

This document describes the bounded runtime-composition slice implemented after the HA routing, dispatch, coordinator, and graceful-drain slices. It does not declare Milestone C complete and does not promote any stable-release evidence gate.

## Responsibility boundary

`M3uaHaRuntimeSupervisor` composes independently managed `M3uaRuntime` instances for association lifecycle isolation and inbound MTP3 fan-in. Each production lane delegates to the existing `M3uaRuntime` contract through `M3uaRuntimeAssociationLane`; the supervisor does not replace SCTP/session startup, ASP state handling, reconnect policy, heartbeat processing, or the existing bounded per-runtime queues.

The aggregate inbound channel is bounded and uses wait-based backpressure. Each delivered transfer retains its source association name. Per-lane snapshots expose the observed runtime state, received-transfer count, fault count, and the underlying `M3uaRuntimeMetrics`.

Association startup is independent. The supervisor becomes usable when the first lane reaches its initial active state; a slow or reconnecting peer is not a readiness barrier for a healthy association. If every configured lane fails before any lane activates, startup fails closed. A terminal fault cancels only that lane's receive pump; healthy lanes remain available.

The supervisor is intentionally one-way for this slice: after it has stopped, the same supervisor instance is not restartable. Association runtime reconnect behavior remains owned by each `M3uaRuntime` according to its configured `SctpReconnectPolicy`.

## Outbound semantic boundary

This slice does **not** route HA outbound traffic through `M3uaRuntime.SendAsync`. That method admits a transfer to a bounded runtime queue and therefore cannot, by itself, prove SCTP/peer/network acceptance.

HA outbound delivery remains owned by the reviewed association-pool, dispatcher, and coordinator path, which distinguishes `Sent`, `NotDispatched`, `Ambiguous`, and `NoRoute` outcomes and does not blindly replay a transfer whose network acceptance may already be uncertain. A later composition slice may unify lifecycle ownership with that dispatch path only if those acceptance semantics remain explicit.

## Deterministic qualification

`src/Sigtran.NET.HaRuntimeTests` executes deterministic synthetic-lane scenarios for:

- duplicate association identity rejection;
- all-lane startup failure;
- one failed lane with a healthy peer;
- a slow-starting lane with a healthy peer;
- capacity-one aggregate backpressure with source-association preservation;
- terminal lane-fault isolation while a healthy peer continues receiving.

Run locally with:

```text
dotnet run --project src/Sigtran.NET.HaRuntimeTests/Sigtran.NET.HaRuntimeTests.csproj --configuration Release
```

The repository CI runs the same executable. These tests establish deterministic SDK behavior only. They are not multi-host, operator/vendor, Kubernetes, or production-network evidence.

## Remaining Milestone C work

This slice does not yet complete the full multi-association runtime milestone. Remaining integration includes reviewed ownership between route state and live association health, reconnect/fencing coordination across the routing and runtime layers, graceful drain of live sessions, and final composition of inbound runtime state with the existing HA outbound coordinator without weakening ambiguous-send handling.

Milestone D multi-host qualification must use genuinely separate authorized hosts/peers and retained topology-specific evidence; same-host deterministic tests do not satisfy that gate.
