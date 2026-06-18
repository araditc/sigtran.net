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
