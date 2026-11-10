# MAP SMS Service

SIGTRAN.NET provides typed MAP SMS codecs, correlated client workflows, and an
asynchronous inbound operation server over TCAP.

## Service Contract

`IMapSmsService` is the official SMS-oriented MAP service boundary. It depends
on `ITcapDialogues` and exposes all five supported operations:

- `sendRoutingInfoForSM`
- `mo-ForwardSM`
- `mt-ForwardSM`
- `reportSM-DeliveryStatus`
- `alertServiceCentre`

The `Send*Async` methods retain fire-and-forget compatibility. The
`Invoke*Async` methods require `ITcapComponentDialogues`, correlate the invoke
with ReturnResult, ReturnError, Reject, timeout, or dialogue closure, and return
`MapSmsOperationResult`.

```csharp
IMapSmsService map = new MapSmsService(
    tcapDialogueManager,
    remoteMapAddress,
    localMapAddress);

MapSmsOperationResult result =
    await map.InvokeRoutingInfoForShortMessageAsync(request, ct: cancellationToken);

if (result.ErrorCode == MapSmsErrorCode.UnknownSubscriber)
{
    // Apply the operator's subscriber-not-found policy.
}

if (result.RoutingInfo is { } routing)
{
    // Use the decoded IMSI and network-node ISDN address for the next step.
}
```

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

`MapSmsOperationProfiles` binds each operation to its standardized local
operation value, application-context-name, and default timeout. The built-in
profiles use the TS 29.002 v3/v2 contexts:

| Operation | Local code | Application context |
| --- | ---: | --- |
| `mt-ForwardSM` | 44 | `0.4.0.0.1.0.25.3` |
| `sendRoutingInfoForSM` | 45 | `0.4.0.0.1.0.20.3` |
| `mo-ForwardSM` | 46 | `0.4.0.0.1.0.21.3` |
| `reportSM-DeliveryStatus` | 47 | `0.4.0.0.1.0.20.3` |
| `alertServiceCentre` | 64 | `0.4.0.0.1.0.23.2` |

The assignments follow
[ETSI TS 129 002](https://www.etsi.org/deliver/etsi_ts/129000_129099/129002/03.06.00_60/ts_129002v030600p.pdf).

## Parameter Set

`MapSmsParameterSet` is a generic SDK utility for a SEQUENCE of context-specific
values. It is not used to encode the five standardized operation arguments.

```csharp
MapSmsParameterSet parameters = new();
parameters.Add(tagNumber: 0, smRpDa);
parameters.Add(tagNumber: 1, smRpOa);

byte[] encoded = parameters.Encode();
```

Use the operation-specific models below to encode TS 29.002 arguments.

## Address Primitives

`MapSmsAddress` represents MSISDN, IMSI, and service-centre identities with TBCD digit encoding.

```csharp
MapSmsAddress msisdn = new(
    MapSmsAddressKind.Msisdn,
    "+44123456789");

byte[] encoded = msisdn.Encode();
```

The standalone payload stores address kind, nature, numbering plan and TBCD
digits. Operation codecs instead write an ASN.1 AddressString (one
type/numbering-plan octet followed by TBCD) or IMSI (TBCD only). Use MAP
nature value 1 for an international number, not the SCCP nature value 4.

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

`MapSmsOperations.CreateMoForwardSm(smRpDa, smRpOa, userData)` produces the
same ASN.1 argument: service-centre DA, MSISDN OA and SignalInfo.

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

`MapSmsOperations.CreateMtForwardSm(smRpDa, smRpOa, userData)` produces the
same ASN.1 argument: IMSI DA, service-centre OA and SignalInfo.

## SendRoutingInfoForSM

`MapSendRoutingInfoForShortMessage` models the route lookup request used before MT delivery.

```csharp
MapSendRoutingInfoForShortMessage sri = new(
    msisdn,
    serviceCentreAddress,
    gprsSupportIndicator: true);

byte[] parameters = sri.Encode();
```

The priority BOOLEAN defaults to false; the four-argument constructor allows
an explicit value. A successful result contains IMSI and network-node ISDN
address; an empty or malformed result is not reported as success. A server
handler can return `MapSmsOperationResponse.Result(new MapSmsRoutingInfo(imsi, node).Encode())`.

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

## Inbound Operation Server

`MapSmsServer` dispatches decoded inbound invokes to operation-specific async
handlers. It validates the operation and payload before calling application
code, returns `MistypedComponent` for malformed parameters, returns
`UnrecognizedComponent` when no profile or handler exists, maps handler
responses to ReturnResult, ReturnError, or Reject, and reports processing
metrics.

```csharp
MapSmsServer server = new(tcapDialogueManager);
server.RegisterHandler(
    MapSmsOperationCode.MtForwardShortMessage,
    (request, ct) =>
    {
        MapMtForwardShortMessage mt =
            request.GetMessage<MapMtForwardShortMessage>();
        return ValueTask.FromResult(MapSmsOperationResponse.Result());
    });

await server.RunAsync(stoppingToken);
```

Only one application consumer should own the inbound TCAP component stream for
an endpoint. `TcapDialogueManager` keeps correlated outbound outcomes on their
invoke completion path, so they cannot accumulate in the inbound server queue.

## Evidence Vectors

`MapSmsEvidenceVectors.GetVectors()` exposes deterministic byte-level vectors for MO-ForwardSM, MT-ForwardSM, SendRoutingInfoForSM, ReportSM-DeliveryStatus, and AlertServiceCentre operation parameters.

```csharp
IReadOnlyList<SigtranProtocolEvidenceValidationReport> reports =
    MapSmsEvidenceVectors.ValidateEncoders();
```

Each vector stores literal BER-shaped expected bytes and validates the current operation encoder output through the shared protocol evidence validator. These SDK-side vectors should be compared with external MAP SMS traces before MAP is promoted for commercial interoperability claims.

## Readiness

`MapSmsReadiness.GetReport()` reports twelve implemented service capabilities:
operation metadata, address primitives, five operation codecs, errors and
extensions, the TCAP facade, operation profiles, correlated client workflows,
typed server dispatch, and operational controls.

The in-process paired-stack tests exercise all five workflows and MAP error
mapping. Phase 52 adds independent C-peer traffic for all five operations and
retains PCAP and trace comparison evidence for the repository profile.
Production readiness remains false until operator-profile validation is
retained.
