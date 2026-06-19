# MAP SMS Profile

Phase 5 builds MAP SMS operation models and BER bindings on top of the TCAP foundation.

## Operation Catalog

`MapSmsOperationCatalog` exposes the MAP SMS operations targeted by the SDK profile:

- `mt-ForwardSM`
- `sendRoutingInfoForSM`
- `mo-ForwardSM`
- `reportSM-DeliveryStatus`
- `alertServiceCentre`

```csharp
bool known = MapSmsOperationCatalog.TryGet(
    MapSmsOperationCode.MoForwardShortMessage,
    out MapSmsOperationMetadata metadata);
```

## Parameter Set

`MapSmsParameterSet` is the shared BER context-specific parameter container used by the operation models.

```csharp
MapSmsParameterSet parameters = new();
parameters.Add(tagNumber: 0, smRpDa);
parameters.Add(tagNumber: 1, smRpOa);

byte[] encoded = parameters.Encode();
```

Operation-specific models build on this container so the public APIs can stay typed while still preserving deterministic BER payloads.

## Address Primitives

`MapSmsAddress` represents MSISDN, IMSI, and service-centre identities with TBCD digit encoding.

```csharp
MapSmsAddress msisdn = new(
    MapSmsAddressKind.Msisdn,
    "+44123456789");

byte[] encoded = msisdn.Encode();
```

The address payload stores address kind, nature of address, numbering plan, and TBCD digits. Operation-specific models reuse this primitive instead of duplicating digit encoding rules.

## MO-ForwardSM

`MapMoForwardShortMessage` models the required MO-ForwardSM SMS profile parameters:

- SM-RP-DA
- SM-RP-OA
- SM-RP-UI

```csharp
MapMoForwardShortMessage mo = new(
    smRpDa,
    smRpOa,
    tpdu);

byte[] parameters = mo.Encode();
```

`MapSmsOperations.CreateMoForwardSm(smRpDa, smRpOa, userData)` is a compatibility helper that produces the same BER-shaped parameter payload.

## MT-ForwardSM

`MapMtForwardShortMessage` models MT-ForwardSM with the same shared SMS profile fields:

- SM-RP-DA
- SM-RP-OA
- SM-RP-UI

```csharp
MapMtForwardShortMessage mt = new(
    smRpDa,
    smRpOa,
    tpdu);

byte[] parameters = mt.Encode();
```

`MapSmsOperations.CreateMtForwardSm(smRpDa, smRpOa, userData)` produces the typed BER-shaped payload.

## SendRoutingInfoForSM

`MapSendRoutingInfoForShortMessage` models the route lookup request used before MT delivery.

```csharp
MapSendRoutingInfoForShortMessage sri = new(
    msisdn,
    serviceCentreAddress,
    gprsSupportIndicator: true);

byte[] parameters = sri.Encode();
```

## ReportSM-DeliveryStatus

`MapReportShortMessageDeliveryStatus` models delivery status reports with MSISDN, service centre address, and `MapSmsDeliveryStatus`.

```csharp
MapReportShortMessageDeliveryStatus report = new(
    msisdn,
    serviceCentreAddress,
    MapSmsDeliveryStatus.MemoryCapacityExceeded);

byte[] parameters = report.Encode();
```

## AlertServiceCentre

`MapAlertServiceCentre` models the alert sent when a subscriber becomes reachable again.

```csharp
MapAlertServiceCentre alert = new(
    msisdn,
    serviceCentreAddress);

byte[] parameters = alert.Encode();
```

## Errors And Extensions

`MapSmsErrorMapper` maps MAP SMS errors into delivery-status categories. `MapSmsExtensionContainer` preserves extension parameters as BER context-specific TLVs.

```csharp
MapSmsDeliveryStatus status = MapSmsErrorMapper.ToDeliveryStatus(
    MapSmsErrorCode.AbsentSubscriberForShortMessage);

MapSmsExtensionContainer extensions = new();
extensions.Add(tagNumber: 5, value);
```

## TCAP Client Facade

`MapSmsTcapClient` builds TCAP Begin/Invoke transactions for SMS operations.

```csharp
MapSmsTcapClient client = new();

TcapBuiltInvoke built = client.BeginMoForwardShortMessage(mo);
byte[] tcapMessage = built.EncodedMessage;
```

The facade hides TCAP transaction-id, invoke-id, dialogue portion, and component wrapping while keeping the encoded transaction available for lower-level routing.

## Readiness

`MapSmsReadiness.GetReport()` reports the current MAP SMS profile status. The foundation is complete when operation metadata, address primitives, ForwardSM codecs, SRI-SM, delivery status, AlertServiceCentre, errors/extensions, and the TCAP client facade are present.

Production readiness remains false until external MAP SMS interoperability vectors and operator-profile validation are added.
