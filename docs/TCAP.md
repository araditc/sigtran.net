# TCAP

Phase 4 replaces the early simplified TCAP byte layout with ASN.1 BER-shaped transaction, dialogue, and component primitives.

## Dialogue Contract

`ITcapDialogues` is the official TCAP service boundary consumed by MAP profiles. It depends on `ISccpService` and exposes Begin, Continue, End, and Receive primitives through `TcapDialogueHandle` and request/event models.

`TcapDialogueService` implements this contract over SCCP Unitdata, so MAP code can depend on TCAP dialogue primitives instead of a concrete SCCP implementation.

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

The controller validates invalid transitions, duplicate pending invokes, invoke concurrency limits, and timeout checks. It is intended to become the state core behind higher-level TCAP/MAP APIs.

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

`TcapReadiness.GetReport()` reports the current TCAP BER foundation status. The foundation is complete when BER primitives, transaction models, component codecs, transaction envelopes, dialogue portions, dialogue state controls, and the session builder are present.

Production readiness remains false until external TCAP interoperability vectors and MAP profile validation are added.

`TcapReadiness.GetFoundationCapabilities()` returns the tracked capability names for release checklists and diagnostic surfaces.
