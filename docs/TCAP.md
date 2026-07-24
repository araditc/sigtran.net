# TCAP

SIGTRAN.NET provides ASN.1 BER-shaped transaction, dialogue, and component
primitives together with a concurrent stateful dialogue manager.

## Dialogue Contract

`ITcapDialogues` is the official TCAP service boundary consumed by MAP profiles. It depends on `ISccpService` and exposes Begin, Continue, End, and Receive primitives through `TcapDialogueHandle` and request/event models.

`TcapDialogueManager` is the stateful implementation for concurrent workloads.
It runs over `ISccpService`, correlates local and remote transaction ids, tracks
components, and exposes bounded transaction and component queues.

`TcapDialogueService` remains as a compatibility implementation for simple
submission workflows.

```csharp
TcapDialogueManager manager = new(sccp);
await manager.StartAsync(ct);

TcapInvokeHandle invoke = await manager.BeginInvokeAsync(
    new TcapBeginInvokeRequest(
        called,
        calling,
        TcapOperationCode.MoForwardShortMessage,
        parameters),
    ct);

TcapInvokeOutcome outcome = await manager.WaitForInvokeAsync(invoke, ct);
```

## BER Primitives

`TcapBerTag`, `TcapBerElement`, and `TcapBer` provide the low-level TLV boundary used by TCAP.

```csharp
TcapBerTag tag = new(
    TcapBerTagClass.ContextSpecific,
    constructed: true,
    number: 1);

Span<byte> buffer = stackalloc byte[32];
bool ok = TcapBer.TryWriteElement(buffer, tag, value, out int written, out string? error);
```

The BER helper supports definite short-form length and definite long-form lengths up to two length octets. Indefinite length is rejected because TCAP messages should be bounded and validation-friendly inside this SDK.

## Transaction Portion

`TcapPackageType`, `TcapTransactionId`, and `TcapTransactionTags` model the transaction portion tags used by Begin, Continue, End, Abort, and Unidirectional packages.

```csharp
TcapTransactionId id = TcapTransactionId.FromUInt32(0x010203);

Span<byte> buffer = stackalloc byte[8];
TcapBer.TryWriteElement(
    buffer,
    TcapTransactionTags.TransactionId(originating: true),
    id.ToArray(),
    out int written,
    out string? error);
```

Transaction identifiers are limited to four octets so they remain compatible with common TCAP deployments and compact enough for deterministic logging.

## Components

`TcapBerInvokeComponent` encodes a TCAP Invoke component using BER component tags and nested integer/octet-string fields.

```csharp
TcapBerInvokeComponent invoke = new(
    invokeId: 7,
    TcapOperationCode.MoForwardShortMessage,
    parameters,
    linkedInvokeId: null);

byte[] encoded = invoke.Encode();
```

The existing simplified `TcapInvokeComponent` remains for compatibility during migration. New TCAP work should use the BER component types.

`TcapBerReturnResultComponent`, `TcapBerReturnErrorComponent`, and `TcapBerRejectComponent` complete the basic component outcome set.

```csharp
TcapBerReturnResultComponent result = new(
    invokeId: 7,
    TcapOperationCode.MoForwardShortMessage,
    resultParameters);

TcapBerReturnErrorComponent errorComponent = new(
    invokeId: 7,
    TcapReturnErrorCode.SystemFailure,
    errorParameters);

TcapBerRejectComponent reject = new(
    invokeId: 7,
    TcapRejectProblemCode.DuplicateInvokeId);
```

## Transaction Messages

`TcapTransactionMessage` wraps transaction ids, dialogue portion bytes, and component portion bytes inside a TCAP package.

```csharp
TcapTransactionMessage begin = new(
    TcapPackageType.Begin,
    originatingTransactionId: TcapTransactionId.FromUInt32(0x0102),
    componentPortion: invoke.Encode());

byte[] encoded = begin.Encode();
```

The envelope supports Begin, Continue, End, Abort, and Unidirectional package tags. Dialogue portion bytes are accepted as an optional payload and are modeled more strongly by the dialogue portion APIs.

## Dialogue Portion

`TcapObjectIdentifier` and `TcapDialoguePortion` model the application context and optional user information carried by dialogue-capable TCAP packages.

```csharp
TcapObjectIdentifier mapContext = new(0, 0, 17, 773, 1, 1, 1);
TcapDialoguePortion dialogue = new(mapContext, userInformation);

TcapTransactionMessage begin = new(
    TcapPackageType.Begin,
    originatingTransactionId: TcapTransactionId.FromUInt32(1),
    dialoguePortion: dialogue.Encode(),
    componentPortion: invoke.Encode());
```

MAP-specific application contexts are introduced later, but the BER OID and user-information boundary is now explicit.

## Dialogue State

`TcapDialogueController` tracks the BER transaction dialogue lifecycle independently of the older simplified `TcapDialogue` class.

```csharp
TcapDialogueController dialogue = new(
    dialogueId: 100,
    new TcapInvokeTimeoutPolicy(TimeSpan.FromSeconds(30), maxPendingInvokes: 256));

dialogue.Begin();
dialogue.RegisterInvoke(invokeId: 1, sentAt: DateTimeOffset.UtcNow);
```

The controller validates invalid transitions, duplicate pending invokes, invoke
concurrency limits, and timeout checks. `TcapDialogueManager` supplies the
thread-safe runtime registry and lifecycle around these protocol concepts.

## Concurrent Dialogues

The manager keeps independent local and remote transaction ids for every
dialogue. Begin creates or accepts a new context; Continue updates peer
correlation; End and Abort complete pending invokes and remove the context.

`ReceiveComponentAsync` delivers Invoke, ReturnResult, ReturnError, and Reject
components. Inbound invokes are completed with `SendResultAsync`,
`SendErrorAsync`, or `SendRejectAsync`.

Pending outbound invokes use one shared timer sweep. This avoids creating a
dedicated delay task for every invoke while preserving per-invoke timeout
overrides. `SnapshotDialogues` and `GetMetrics` expose current state without
returning mutable manager internals.

The dialogue event stream is observational and cannot block correlated
ReturnResult, ReturnError, or Reject processing. When its bounded queue is full,
the manager drops the observation event and increments
`TcapDialogueManagerMetrics.DroppedDialogueEvents`. Inbound Invoke components
still use their bounded component queue and retain application backpressure.

## Allocation

`TcapTransactionIdAllocator` and `TcapInvokeRegistry` provide deterministic allocation and duplicate detection for transaction and invoke identifiers.

```csharp
TcapTransactionIdAllocator transactionIds = new();
TcapTransactionId tid = transactionIds.Allocate();

TcapInvokeRegistry invokes = new();
byte invokeId = invokes.Allocate();
```

The allocator deliberately avoids hidden transport or threading behavior. Callers can wrap it with their own synchronization policy where needed.

## Session Builder

`TcapSessionBuilder` creates common Begin/Invoke and End/ReturnResult messages while allocating transaction and invoke identifiers.

```csharp
TcapSessionBuilder builder = new();

TcapBuiltInvoke built = builder.BeginInvoke(
    mapContext,
    TcapOperationCode.MoForwardShortMessage,
    parameters);

byte[] end = builder.EndResult(
    built.OriginatingTransactionId,
    built.InvokeId,
    TcapOperationCode.MoForwardShortMessage,
    resultParameters);
```

## Evidence Vectors

`TcapEvidenceVectors.GetVectors()` exposes deterministic byte-level vectors for Begin/Invoke/Dialogue and End/ReturnResult transaction flows.

```csharp
IReadOnlyList<SigtranProtocolEvidenceValidationReport> reports =
    TcapEvidenceVectors.ValidateEncoders();
```

Each vector stores literal BER expected bytes and validates the current transaction encoder output through the shared protocol evidence validator. These SDK-side vectors should be compared with external TCAP traces before TCAP is promoted for commercial interoperability claims.

## Readiness

`TcapReadiness.GetReport()` reports the current TCAP codec and runtime status.
The foundation includes BER primitives, transaction models, component codecs,
transaction envelopes, dialogue portions, state controls, the session builder,
the concurrent manager, transaction correlation, invoke outcomes, shared timeout
handling, and Abort cleanup.

Phase 52 retains independent C-peer Begin/Invoke and ReturnResult traffic for
the repository profile. Production readiness remains false until
operator/vendor TCAP and MAP profile validation is retained.

`TcapReadiness.GetFoundationCapabilities()` returns the tracked capability names for release checklists and diagnostic surfaces.
