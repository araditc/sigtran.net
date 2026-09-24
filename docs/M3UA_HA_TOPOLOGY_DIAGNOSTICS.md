# M3UA HA Topology Diagnostics

Status: **IMPLEMENTING** under Milestone C / issue #15. This slice is deliberately stacked on PR #25 and does not declare Milestone C complete.

## Purpose

`M3uaHaTopologyDiagnostics` projects the already-reviewed HA state dimensions into one deterministic operational snapshot. It does not create a new routing or transport state machine. The snapshot joins:

- `M3uaAssociationPool` node-side route policy, routing-context membership, graceful-drain lease count, live route-health state, and selection counters;
- `M3uaHaRuntimeSupervisor` per-association lifecycle state, receive/fault counters, bounded aggregate inbound pressure, and underlying runtime metrics;
- `M3uaRuntimeGenerationFenceBinding` transport-generation ownership, activation-epoch state, binding errors, and fence reason;
- `M3uaHaDispatchCoordinator` bounded dispatch pressure, aggregate outcomes, and per-association outcome counters.

Node-routing mode and negotiated M3UA Traffic Mode Type remain separate fields. Runtime health, local route role, transport-generation ownership, and dispatch outcomes remain separate dimensions rather than being collapsed into one synthetic health flag.

## Membership, ownership and ordering

Construction fails closed unless the route pool, runtime supervisor, generation-binding registry, and dispatch path describe the exact same association composition. Name equality is necessary but not sufficient: each binding key must match the binding's actual association, the binding must own the exact runtime-lane object supervised by this topology, the binding must publish health into the exact supplied route-pool object, the dispatch coordinator must be backed by a dispatcher that owns that same route-pool object, and the dispatcher must route the association through the exact `M3uaReconnectFencedAssociationSender` owned by the generation binding. Equal names, equivalent configuration, or a separately constructed sender are not ownership evidence.

Production runtime adapters have one additional ownership invariant: two `M3uaRuntimeAssociationLane` instances must not wrap the same underlying `M3uaRuntime`, even when their association names differ. `M3uaHaRuntimeSupervisor` validates exact runtime reference identity during construction and rejects aliases before any lane starts, lifecycle handler is attached, or receive pump is launched. Synthetic `IM3uaAssociationRuntimeLane` implementations remain supported for deterministic tests; the uniqueness rule is specific to the production adapter where one `M3uaRuntime` represents one actual runtime/session owner.

The runtime supervisor must also be composed **without** its optional independent `routePool` binding. In generation-bound topology composition, `M3uaRuntimeGenerationFenceBinding` is the sole route-health publisher because it intentionally keeps the route non-eligible through `StateChanged(Active)` until the matching generation sender has actually opened on `AspActivated`. Allowing the supervisor to publish route health at the same time would create a dual-writer interval in which it could set the route `Active` before transport-generation admission is ready. Diagnostics rejects that composition before any runtime starts.

These checks prevent diagnostics from silently combining route/runtime state from one HA composition with generation/fence state or dispatch counters from another composition. In particular, a same-name second generation sender cannot make a snapshot report `TransportAcceptingDispatch` from one boundary while coordinator outcomes were produced through another, a second route-health publisher cannot make route eligibility race ahead of generation readiness, and one physical/runtime instance cannot be represented as two independently supervised associations. Duplicate or missing binding names are also rejected. Snapshot output is ordered deterministically by association name so retained diagnostics can be compared without relying on dictionary/enumeration order.

## Snapshot semantics

Each association snapshot includes:

- stable association / signaling-gateway identity and priority;
- local policy state and live route-runtime state;
- route dispatch leases, selection count, and routing contexts;
- supervisor-observed runtime state, receive/fault counts, and runtime metrics;
- transport generation, new-dispatch admission, generation in-flight count, fence reason, activation intent/epoch availability, and binding error;
- `Sent`, `NotDispatched`, and `Ambiguous` coordinator outcomes for that association.

The aggregate snapshot includes inbound capacity/pending count plus coordinator pending/admitted/completed/canceled/faulted dispatch counts and all dispatch dispositions.

These values are sampled from existing thread-safe component snapshots. They are intentionally not presented as one globally atomic distributed transaction. Consumers can use them for diagnostics and qualification evidence while retaining the meaning of each underlying counter/state.

Most importantly, coordinator or sender completion is **not** peer/network acceptance. The snapshot never turns local queue acceptance into end-to-end delivery evidence and never changes the non-replayable treatment of ambiguous work.

## Deterministic qualification

The dedicated `Sigtran.NET.HaLifecycleTests` executable now registers nineteen scenarios: the eleven runtime-generation lifecycle scenarios from PR #25 plus eight topology/runtime-composition scenarios.

The positive topology scenario composes two synthetic loadshare associations through the route pool, runtime supervisor, generation bindings, dispatcher, and bounded coordinator. It verifies deterministic association ordering, fail-closed stopped state, generation-one readiness after activation, aggregate/per-association dispatch-counter reconciliation, selection accounting, and independent fault attribution when one lane becomes reconnecting while its peer remains healthy.

Five negative topology scenarios prove construction fails closed when: route/runtime membership and generation-binding membership differ; a binding wraps a different runtime-lane object that merely reuses the same association name; a coordinator belongs to a separately constructed route pool with the same membership; the binding and dispatcher use different generation-fenced sender objects for the same association, lane, and route pool; or the runtime supervisor is independently bound to a route pool while generation bindings also publish route health. These cases protect against false diagnostic attribution and against route eligibility racing ahead of transport-generation readiness.

Two production-runtime composition regressions cover the runtime-alias boundary directly: two differently named production adapters wrapping the exact same stopped `M3uaRuntime` must be rejected before start, while two adapters wrapping two distinct stopped runtimes must be admitted without starting either runtime. Existing synthetic-lane topology coverage confirms synthetic `IM3uaAssociationRuntimeLane` implementations remain valid.

While this PR is stacked on PR #25 rather than canonical `main`, these tests are implementation/review coverage only. No exact-head Actions PASS is claimed until dependencies are governed and this PR can target `main` under the normal repository workflow.

## Evidence boundary

This snapshot is designed to support later authorized-lab qualification, but it is not itself:

- operator/vendor acceptance;
- multi-host or operator-wide capacity evidence;
- Kubernetes SCTP qualification;
- proof of peer/network acceptance or exactly-once delivery;
- trusted-signing evidence;
- authorization to change any stable-release manifest gate.

The stable release remains governed by the machine evaluator and its retained external evidence requirements.
