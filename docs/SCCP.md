# SCCP

Phase 3 replaces the early SCCP proof-of-concept with standards-shaped connectionless SCCP primitives.

## Protocol Vocabulary

`SccpMessageType` exposes the connectionless message type codes used by UDT, XUDT, LUDT, and their service-message forms.

```csharp
byte udt = (byte)SccpMessageType.Unitdata;
byte xudt = (byte)SccpMessageType.ExtendedUnitdata;
```

`SccpProtocolClass` models the protocol class octet and the return-message-on-error option.

```csharp
SccpProtocolClass protocolClass = new(
    SccpConnectionlessClass.Class1,
    returnMessageOnError: true);

byte encoded = protocolClass.Encode();
```

The current SCCP work is still a foundation for standards-based encode/decode. Interoperability-sensitive consumers should wait for the Phase 3 readiness report before treating SCCP APIs as stable.

## Party Addresses

`SccpPartyAddress` models the called/calling party address boundary with routing indicator, optional point code, optional subsystem number, and optional global title.

```csharp
SccpPartyAddress called = new(
    SccpRoutingIndicator.RouteOnGlobalTitle,
    subsystemNumber: SubsystemNumber.MAP,
    pointCode: 0x1234,
    globalTitle: new SccpGlobalTitle("44123456789"));

byte[] encoded = called.Encode();
```

`SccpGlobalTitle` currently supports numeric TBCD digits with translation type, numbering plan, and nature of address fields. Unsupported global title indicator forms are rejected during decode instead of being silently misread.

## Unitdata

`SccpUnitdataMessage` encodes and decodes UDT messages with the Q.713-style fixed part and variable parameter pointers:

- message type
- protocol class
- called party address pointer
- calling party address pointer
- data pointer
- length-prefixed called party address
- length-prefixed calling party address
- length-prefixed user data

```csharp
SccpUnitdataMessage udt = new(
    new SccpProtocolClass(SccpConnectionlessClass.Class0),
    called,
    calling,
    userData);

byte[] encoded = udt.Encode();
```

The older `SccpMessage` class remains for compatibility during migration, but new code should use `SccpUnitdataMessage`.

## Extended Unitdata

`SccpExtendedUnitdataMessage` adds the XUDT fixed fields, including hop counter and the optional-parameter pointer slot.

```csharp
SccpExtendedUnitdataMessage xudt = new(
    new SccpProtocolClass(SccpConnectionlessClass.Class1, returnMessageOnError: true),
    hopCounter: 12,
    called,
    calling,
    userData);
```

When `Segmentation` is present, the XUDT codec writes an optional parameter list with the segmentation parameter and end marker. Unknown optional parameters are skipped during decode; malformed optional parameter lengths are rejected.

## Segmentation

`SccpSegmentationParameter` represents the SCCP segmentation optional parameter value with local reference, remaining segment count, and first-segment flag.

```csharp
SccpSegmentationParameter segmentation = new(
    localReference: 0x00A1B2C3,
    remainingSegments: 3,
    firstSegment: true);

Span<byte> value = stackalloc byte[SccpSegmentationParameter.EncodedLength];
segmentation.Encode(value);
```

The parameter can be used directly with `SccpExtendedUnitdataMessage`:

```csharp
SccpExtendedUnitdataMessage segmented = new(
    new SccpProtocolClass(SccpConnectionlessClass.Class1),
    hopCounter: 10,
    called,
    calling,
    segmentPayload,
    segmentation);
```
