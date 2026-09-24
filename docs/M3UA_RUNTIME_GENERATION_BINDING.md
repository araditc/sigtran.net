# M3UA Runtime To Transport-Generation Binding

Status: **VERIFIED-DONE on `main` via PR #25, with a post-merge stale-shutdown ordering correction under governed review** under Milestone C / issue #15. The reconnect-generation primitive from PR #24 and runtime-generation binding from PR #25 are admitted on canonical `main`; this correction does not declare Milestone C complete.

## Purpose

`M3uaReconnectFencedAssociationSender` owns transport-generation admission and ambiguous-send fencing. `M3uaHaRuntimeSupervisor` owns independent runtime-lane lifecycle and inbound fan-in. `M3uaRuntimeGenerationFenceBinding` connects those responsibilities without changing node-routing policy or negotiated M3UA Traffic Mode Type.

The binding is attached only while its runtime lane is `Stopped`. This prevents attaching midway through an unknown live SCTP/M3UA session and incorrectly treating it as generation zero or generation one. Attachment also requires the generation sender to be fail-closed and fully drained; a sender that is already accepting dispatch, or still owns in-flight work, is rejected before route-health publication or runtime event subscription.

When a route pool is supplied, the binding also becomes the sole live runtime-health publisher for that association in this composition path. The runtime supervisor must therefore be created without its separate `routePool` binding. This avoids two lifecycle observers racing to publish `Active` before transport-generation ownership is ready.

## Activation contract

The generation sender starts fail-closed. `StateChanged(Active)` does not open admission because `M3uaRuntime` emits that lifecycle transition before the explicit `AspActivated` event. A generation opens only after a matching `AspActivated` event reports `Active` and identifies the same stable association name as the bound lane/sender.

Activation also requires a transport-epoch permit. Attaching the binding while the runtime is stopped grants exactly one initial permit for the first real session. Once that permit is consumed, a later generation can be opened only after the binding observes an explicit `StateChanged(Starting)` or `StateChanged(Reconnecting)` lifecycle boundary. This is the evidence that the runtime is establishing a transport session distinct from the previously activated one.

`M3uaRuntime.StartAsync` publishes its `Starting` event before session-factory or ASP-startup execution is released. This ordering is enforced even when the session factory and handshake complete synchronously, and it applies to a later restart after a stopped/faulted run as well as the initial start. Consequently a generation binding always receives the restart epoch edge before a matching `AspActivated` can consume that permit.

A duplicate or delayed `AspActivated` from the already-current session is therefore always a no-op after its permit has been consumed. This remains true even if the generation sender subsequently fenced that live session as `AmbiguousOutcome`: ambiguity cannot be cleared by another activation event from the same transport. Only an intervening starting/reconnecting transport epoch may arm one replacement activation. `FaultObserved` or `ReconnectScheduled` closes admission, but neither by itself is sufficient to prove a new transport epoch.

If a route pool is bound, route runtime health becomes `Active` only **after** the sender generation has opened. During replacement generation drain, the route remains non-eligible even if the underlying runtime has already reported its new `Active` state. This prevents a dispatcher from selecting a route whose transport-generation sender is still fenced.

Missing or mismatched association identity fails closed and is exposed in binding diagnostics. Transfer and heartbeat diagnostics never establish a replacement generation. A duplicate activation event after a binding-owned generation has consumed its epoch permit is a no-op. By contrast, if the sender is already open when a binding-owned activation task reaches the generation boundary, ownership is unproven: the binding records an error, fences the sender as runtime-unavailable, and keeps route admission closed rather than adopting that generation.

## Loss, reconnect, and drain

`FaultObserved` and `ReconnectScheduled` close new generation admission synchronously with `RuntimeUnavailable` and publish non-eligible route health when the route pool is owned by the binding. `Starting`, `Reconnecting`, and `Faulted` lifecycle states also remain unavailable. `Stopping` and `Stopped` close admission as `AdministrativeDrain`. `ShutdownCompleted` closes the current stopped lifecycle only when both the event snapshot reports `Stopped` **and** the runtime lane is still live-observed as `Stopped` when the binding handles that event.

A completed run can make itself restartable before its final `ShutdownCompleted` callback is observed. The old event can even be constructed with a `Stopped` snapshot before a concurrent replacement changes the live lane to `Starting`; handler scheduling may then deliver that recorded-stopped event only after the replacement lifecycle has advanced. The binding therefore re-reads the lane's live state while handling `ShutdownCompleted`. If the live lane is already `Starting`, `Reconnecting`, `Active`, or `Faulted`, the older final notification is stale and is ignored: it must not revoke a replacement transport-epoch permit, invalidate a pending replacement activation, or re-fence an already-open replacement generation.

`Starting` and `Reconnecting` are additionally the only lifecycle states that arm the next transport-epoch permit. `Faulted`, stopping/shutdown states, disposal, diagnostics, and duplicate activation do not arm a generation. This prevents same-session ambiguity from being mistaken for reconnect completion.

A later valid ASP activation may request a replacement generation, but it cannot activate until every dispatch already admitted to the previous transport generation has released its generation lease. The binding uses a monotonically increasing transition version so a later fault, current-run shutdown, or disposal invalidates an older pending activation. This prevents a reconnect completion racing with teardown from reopening dispatch.

Disposal unsubscribes runtime events, publishes stopping health when applicable, closes new admission, clears any unused transport-epoch permit, and waits for both pending activation work and the current generation drain. Events arriving after detach cannot reopen the sender.

## Ownership boundary

This binding does not claim peer/network acceptance and does not replay work. The generation sender preserves the existing outcome model:

- proven pre-invocation caller cancellation remains known-not-dispatched and does not fault the route;
- proven association pre-dispatch failure remains eligible only for the explicitly reviewed safe failover path;
- post-invocation cancellation, uncertain sender failure, or an explicitly ambiguous outcome fences the generation as ownership-ambiguous;
- ambiguous work is never blindly replayed on a replacement generation;
- a duplicate `AspActivated` cannot clear `AmbiguousOutcome` unless a distinct starting/reconnecting transport epoch was observed first.

Runtime health, local route role, negotiated Traffic Mode Type, and transport-generation ownership remain separate state dimensions. The binding only orders route **eligibility** after generation readiness; it does not promote/demote local node role.

## Deterministic qualification

The dedicated `Sigtran.NET.HaLifecycleTests` executable runs twelve synthetic scenarios after this correction:

1. first matching ASP activation opens exactly one generation, while `StateChanged(Active)` and diagnostics cannot do so;
2. an ambiguous live generation cannot be reopened by duplicate `AspActivated`; an explicit reconnect transport epoch is required before generation rollover;
3. reconnect activation waits for an in-flight prior generation to drain before generation rollover;
4. route admission stays closed through `StateChanged(Active)` and through replacement-generation drain, then opens only after generation readiness;
5. unexpected association identity fails closed and remains diagnostic;
6. missing activation identity fails closed;
7. binding disposal closes admission immediately, waits for admitted work, and ignores late activation after detach;
8. attachment rejects a sender whose dispatch generation was already opened by another owner without mutating or claiming that generation;
9. if an external owner opens the sender after attachment but before ASP activation, the binding detects the unproven ownership, fences the sender, and keeps route admission closed;
10. a production runtime publishes `StateChanged(Starting)` before the session factory is allowed to run on both initial startup and restart, preventing synchronous activation from outrunning epoch arming;
11. mid-session attachment and runtime/sender identity mismatch are rejected;
12. a stale `ShutdownCompleted` carrying an older recorded `Stopped` snapshot cannot revoke a replacement `Starting` epoch or close an already-active replacement generation when the live lane has already advanced.

The repository workflow includes this executable after the existing HA runtime fan-in harness. The corrective source head requires its own successful Actions run and fresh review; historical PR #25 CI/review is evidence for the admitted parent, not for this correction. Any source-head change invalidates the corrective run/review.

These synthetic tests are not operator/vendor acceptance, separate-host qualification, Kubernetes SCTP evidence, or stable-signing evidence. They do not change any stable-release manifest gate.
