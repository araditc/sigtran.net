# SCCP

SIGTRAN.NET provides standards-shaped connectionless SCCP codecs and a stateful
service over `IMtp3Network`.

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

The stateful implementation is available for controlled integration. External
peer trace evidence remains required before broad interoperability claims.

## Service Contract

`ISccpService` is the official SCCP service boundary consumed by TCAP. It
depends on `IMtp3Network` and exposes lifecycle, UDT compatibility methods,
general connectionless data requests and indications, and service-return
indications.

`SccpConnectionlessService` owns one lower-layer receive loop. Its bounded data
and return queues provide asynchronous backpressure, and `StopAsync` cancels the
receive owner deterministically.

```csharp
SccpConnectionlessService service = new(network, routingLabel);
await service.StartAsync(ct);

await service.SendAsync(
    new SccpDataRequest(protocolClass, called, calling, tcapPayload),
    ct);

SccpDataIndication indication = await service.ReceiveAsync(ct);
```

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

`SccpReassemblyBuffer` validates descending remaining-segment values and bounds
context count, assembled payload size, and inactivity lifetime. The stateful
service uses it automatically and emits only a complete logical indication.

## Long Unitdata

`SccpLongUnitdataMessage` carries larger connectionless payloads with 16-bit pointer and length fields.

```csharp
SccpLongUnitdataMessage ludt = new(
    new SccpProtocolClass(SccpConnectionlessClass.Class1),
    hopCounter: 9,
    called,
    calling,
    largePayload);

byte[] encoded = ludt.Encode();
```

Use LUDT when the SCCP user data cannot fit inside the one-octet length field used by UDT and XUDT.

## Service Messages

`SccpReturnCause` and `SccpUnitdataServiceMessage` model returned connectionless messages.

```csharp
SccpUnitdataServiceMessage udts = new(
    SccpReturnCause.SubsystemFailure,
    called,
    calling,
    returnedPayload);

byte[] encoded = udts.Encode();
```

Use UDTS when an incoming UDT cannot be delivered and the protocol class asks for return-on-error behavior.

When at least one application route is configured, the stateful service rejects
unresolved traffic. An unroutable UDT with return-on-error set is returned as
UDTS with the original payload and reversed party addresses. Received UDTS
messages are available through `ReceiveReturnAsync`.

## Evidence Vectors

`SccpEvidenceVectors.GetVectors()` exposes deterministic byte-level vectors for UDT, XUDT segmentation, LUDT pointer layout, and UDTS return-cause behavior.

```csharp
IReadOnlyList<SigtranProtocolEvidenceValidationReport> reports =
    SccpEvidenceVectors.ValidateEncoders();
```

Each vector stores literal expected bytes and validates the current encoder output through the shared protocol evidence validator. These vectors are SDK-side evidence and should be compared with retained external traces before SCCP is promoted for commercial interoperability claims.

## Routing

`SccpRouteTable` resolves application routes by subsystem number or global title prefix.

```csharp
SccpRouteTable routes = new();
routes.Add(new SccpRoute("map", SccpRouteSelector.ForSubsystem(SubsystemNumber.MAP)));
routes.Add(new SccpRoute("smsc-uk", SccpRouteSelector.ForGlobalTitlePrefix("44123")));

if (routes.TryResolve(calledParty, out SccpRoute? route))
{
    string name = route.Name;
}
```

Global title routes use longest-prefix matching. SSN routes can optionally include a point code for more specific routing.

`SccpGlobalTitleTranslationTable` performs outbound longest-prefix translation.
Each rule maps a numeric prefix to DPC and SSN and can preserve or remove the
original global title. The translated DPC is applied to the outbound MTP3
routing label.

## Readiness

`SccpReadiness.GetReport()` reports codec and stateful service capabilities. The
SDK foundation is ready when MTP3 routing, party addressing, UDT/XUDT/LUDT
codecs, segmentation, service messages, route APIs, stateful lifecycle,
global-title translation, reassembly, and return policy are present.

```csharp
SccpReadinessSnapshot report = SccpReadiness.GetReport();
bool foundationReady = report.FoundationReady;
```

Production readiness remains false until external SCCP interoperability vectors and network trace validation are added.
