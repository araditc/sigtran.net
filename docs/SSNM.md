# M3UA Signalling Network Management

SSNM messages report destination availability, destination audits, restrictions, user-part unavailability, and signalling congestion. SIGTRAN.NET supports the currently modeled SSNM messages through builders, typed parsers, outbound processing, and transport send helpers.

## Common Destination Status

The common SSNM model covers:

- Destination Unavailable, DUNA
- Destination Available, DAVA
- Destination State Audit, DAUD
- Destination Restricted, DRST

Each message carries one or more Affected Point Code entries and may include Network Appearance, Routing Context, and Info String.

```csharp
await transport.SendDestinationUnavailableAsync(
    networkAppearance: 7,
    routingContexts: new uint[] { 100 },
    affectedPointCodes: new[]
    {
        new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)
    },
    infoString: ReadOnlyMemory<byte>.Empty,
    ct: ct);
```

## Specialized SSNM

`SendDestinationUserPartUnavailableAsync` sends DUPU with the required User/Cause parameter. The affected point-code mask must be zero.

```csharp
await transport.SendDestinationUserPartUnavailableAsync(
    networkAppearance: 7,
    routingContexts: new uint[] { 100 },
    affectedPointCode: new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233),
    cause: M3uaUserPartUnavailableCause.InaccessibleRemoteUser,
    userIdentity: M3uaMtp3UserIdentity.Sccp,
    infoString: ReadOnlyMemory<byte>.Empty,
    ct: ct);
```

`SendSignallingCongestionAsync` sends SCON and may include Concerned Destination and Congestion Indications.

```csharp
await transport.SendSignallingCongestionAsync(
    networkAppearance: 7,
    routingContexts: new uint[] { 100 },
    affectedPointCodes: new[]
    {
        new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)
    },
    concernedDestination: new M3uaAffectedPointCode(mask: 0, pointCode: 0x0000AAAA),
    congestionLevel: 2,
    infoString: ReadOnlyMemory<byte>.Empty,
    ct: ct);
```

## Validation

Common SSNM and SCON require at least one Affected Point Code. DUPU requires a single zero-mask Affected Point Code and a valid `M3uaUserPartUnavailableCause` plus `M3uaMtp3UserIdentity`.
