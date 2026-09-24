# M3UA HA Live Outbound Ownership

Status: **IMPLEMENTING** under Milestone C / issue #15. This slice is dependency-stacked after the runtime-generation binding and topology-diagnostics work. It is not release, operator, multi-host, or Kubernetes evidence.

## Why the existing runtime queue is not an HA send acknowledgement

`M3uaRuntime.SendAsync` implements the public `IMtp3Network` contract by applying bounded backpressure and admitting an MTP3 transfer to the runtime's outbound queue. The active session loop later performs the M3UA transport write. Queue admission therefore cannot be used as the HA dispatcher's `Sent` boundary: the active transport may fail after admission, and an unread queue item can survive into a replacement session under the historical runtime contract.

The public queue-oriented API remains unchanged. Milestone C adds a separate **internal tracked path** for the HA composition.

## Tracked HA path

`M3uaRuntimeAssociationSender` adapts one live `M3uaRuntime` to `IM3uaAssociationSender` and waits for `M3uaRuntime.SendTrackedAsync` to resolve ownership.

For tracked work:

- admission is allowed only while the expected association is the runtime's current `Active` association;
- each successfully activated ASP transport session receives a monotonically increasing internal session generation;
- tracked work captures that generation before entering the same bounded outbound channel used by the runtime;
- caller cancellation can abort while bounded queue admission is still pending and remains known-not-dispatched;
- after queue admission succeeds, the caller waits for the runtime to resolve the item rather than treating admission as a send or abandoning ownership on later caller cancellation;
- the current session loop completes the item only after the local `M3uaTransportSession.SendPayloadDataAsync` call completes;
- cancellation/failure before transport invocation is known-not-dispatched;
- cancellation/failure after transport invocation begins is ownership-ambiguous;
- when an active session retires, its per-session admission boundary is canceled **before reconnect backoff/open**; blocked writers and queued-but-unclaimed tracked work are completed immediately as known-not-dispatched instead of waiting for generation N+1 to consume them;
- queue-admission cancellation **signals** are arbitrated once per tracked work item, but retirement does not finalize from that signal alone: it cancels the retiring session's admission boundary and waits for the actual `WriteAsync` outcome to settle; caller-first cancellation is caller-owned only when admission really canceled, while a successful bounded-channel write supersedes a racing caller/session cancellation signal;
- transport invocation is claimed atomically against session retirement; retirement may classify only work that has not claimed transport, while claimed work keeps transport ownership and resolves from the transport path;
- a replacement session still refuses any stale tracked queue entry captured for an earlier runtime session generation. Such entries are never replayed on the replacement transport.

The runtime's historical untracked `IMtp3Network.SendAsync` queue behavior is intentionally preserved for compatibility. HA composition must use the tracked association sender, not infer HA ownership from the public queue call.

## Meaning of transport-write completion

`TransportWriteCompleted` is deliberately narrower than a delivery acknowledgement. The lower `ISctpTransport.SendAsync` contract may complete when a user message is queued or sent by that transport implementation. The tracked HA path therefore establishes only a local lower-layer ownership boundary suitable for routing/fencing decisions. It does **not** prove that the remote SG, SCCP peer, TCAP peer, or MAP application accepted the transaction, and it does not create an exactly-once guarantee.

Ambiguous MAP/TCAP transactions remain non-replayable without a higher-level reconciliation decision.

## Relationship to generation fencing

The runtime session generation and `M3uaReconnectFencedAssociationSender` generation are complementary boundaries:

- the runtime session generation prevents an item already admitted for session N from being transmitted by session N+1;
- the reconnect-fenced sender prevents a new HA dispatch from entering a route generation that runtime lifecycle has fenced;
- `M3uaRuntimeGenerationFenceBinding` opens the outer generation only after the matching runtime reports `AspActivated` and closes it on transport/runtime loss;
- route policy and negotiated M3UA Traffic Mode remain separate from both generation counters.

The parent topology diagnostics still require one route-health publisher and exact route/runtime/binding/dispatch ownership. Production runtime composition now adds the live-transport invariant at the generation-binding boundary: when a `M3uaRuntimeAssociationLane` is paired with a runtime-backed fenced sender, both must reference the exact same `M3uaRuntime` object. Equal association names are insufficient. Synthetic lanes/senders remain supported because this exact-runtime rule is applied only when both sides expose production runtime identity.

## Deterministic coverage

`Sigtran.NET.HaLifecycleTests` registers eleven synthetic live-runtime scenarios on this branch:

1. HA send remains incomplete while the local transport-write gate is held, proving bounded runtime queue admission is not the `Sent` boundary;
2. association identity mismatch fails known-not-dispatched before any payload transport invocation;
3. a payload transport failure after invocation begins is surfaced as ambiguous;
4. association loss with one invoked transfer and one queued tracked transfer makes the invoked transfer ambiguous, activates a replacement runtime session, and proves the replacement transport receives **zero** payload calls for the prior-generation queued item;
5. with outbound capacity one, one transport-claimed item, one queued item, and a third writer blocked on admission, session retirement completes the queued and blocked work before a deliberately gated replacement `OpenAsync`, drains the outer generation fence, and never invokes those payloads on generation 2;
6. session-retirement cancellation winning a blocked admission remains runtime-owned even if the caller token is canceled later;
7. caller cancellation winning a blocked admission remains caller-owned even when runtime cleanup follows;
8. two distinct stopped production runtimes using the same association name are rejected when the supervised lane references runtime A but the live sender references runtime B, before either runtime starts;
9. the positive production composition using the exact same runtime object for lane and live sender is accepted while remaining fail-closed and generation zero;
10. a deterministic admission-state regression records a caller cancellation signal first and then a successful channel admission, proving the settled outcome is `Succeeded` and caller ownership is false;
11. caller cancellation after transport claim cannot relabel the send as safe pre-dispatch work; session loss resolves it as ambiguous transport-owned work.

These tests use an in-process synthetic SCTP transport and ASP acknowledgements. They prove local ownership semantics only. They do not close `operator-profile`, `multi-host-soak`, `kubernetes-sctp`, or `trusted-signing`.
