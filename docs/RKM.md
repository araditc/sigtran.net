# M3UA Routing Key Management

Routing Key Management (RKM) lets an ASP dynamically register and deregister Routing Keys with an SG. SIGTRAN.NET now includes the foundational RKM message models, builders, and typed parsers needed for dynamic Routing Context negotiation.

## Implemented Messages

| Message | Builder | Parser |
| --- | --- | --- |
| Registration Request | `BuildRegistrationRequest` | `TryParseRegistrationRequest` |
| Registration Response | `BuildRegistrationResponse` | `TryParseRegistrationResponse` |
| Deregistration Request | `BuildDeregistrationRequest` | `TryParseDeregistrationRequest` |
| Deregistration Response | `BuildDeregistrationResponse` | `TryParseDeregistrationResponse` |

## Registration Request

A Registration Request contains one or more Routing Key parameters. Each Routing Key currently supports:

- Local-RK-Identifier
- Routing Context
- Traffic Mode Type
- Destination Point Code
- Network Appearance
- Service Indicators
- Originating Point Code List

Local-RK-Identifier and Destination Point Code are required. The builder rejects Routing Keys without Destination Point Code so invalid dynamic registration messages are not emitted by default.

```csharp
M3uaRoutingKey[] routingKeys =
[
    new(
        localRoutingKeyIdentifier: 42,
        routingContext: 100,
        trafficModeType: M3uaTrafficModeType.Loadshare,
        destinationPointCodes: [new M3uaAffectedPointCode(0, 0x00112233)],
        networkAppearance: 7,
        serviceIndicators: [3],
        originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
];

Span<byte> buffer = stackalloc byte[256];
bool ok = M3uaMessageBuilder.BuildRegistrationRequest(
    buffer,
    routingKeys,
    out int written,
    out string? error);
```

## Client Helper

`M3uaRkmClient` sends request messages through `M3uaTransportSession` and waits until the matching response type arrives.

```csharp
M3uaRkmClient client = new(transport);

M3uaRegistrationResponseMessage registration = await client.RegisterAsync(
    routingKeys,
    ct: ct);

M3uaDeregistrationResponseMessage deregistration = await client.DeregisterAsync(
    new uint[] { registration.Results[0].RoutingContext },
    ct: ct);
```

## Registration Response

Registration Response carries one or more Registration Result parameters. Each result contains:

- Local-RK-Identifier
- Registration Status
- Routing Context

The status is exposed as `M3uaRegistrationStatus`.

## Deregistration

Deregistration Request carries a non-empty Routing Context list. Deregistration Response carries one or more Deregistration Result parameters with Routing Context and `M3uaDeregistrationStatus`.

## Validation Rules

- RKM messages must use message class `RoutingKeyManagement`.
- Registration Request requires at least one Routing Key.
- Routing Key requires Local-RK-Identifier and Destination Point Code.
- Registration Response requires at least one Registration Result.
- Deregistration Request requires a non-empty Routing Context parameter.
- Deregistration Response requires at least one Deregistration Result.
- Unknown registration and deregistration status values are rejected by typed parsers.
