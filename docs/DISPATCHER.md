# M3UA Typed Dispatcher

The typed dispatcher gives application code one entry point for converting a decoded `M3uaMessage` into the concrete SDK model for known M3UA message families.

## API

```csharp
if (!M3uaTypedMessageParser.TryParseKnown(
        message,
        out M3uaTypedMessage? typed,
        out string? error))
{
    throw new InvalidOperationException(error);
}

switch (typed.Kind)
{
    case M3uaTypedMessageKind.PayloadData:
        M3uaPayloadDataMessage data = typed.As<M3uaPayloadDataMessage>();
        break;

    case M3uaTypedMessageKind.AspStateMaintenance:
        M3uaAspStateMaintenanceMessage aspsm = typed.As<M3uaAspStateMaintenanceMessage>();
        break;
}
```

## Supported Kinds

| Kind | Concrete model |
| --- | --- |
| `PayloadData` | `M3uaPayloadDataMessage` |
| `AspStateMaintenance` | `M3uaAspStateMaintenanceMessage` |
| `AspTrafficMaintenance` | `M3uaAspTrafficMaintenanceMessage` |
| `Error` | `M3uaErrorMessage` |
| `Notify` | `M3uaNotifyMessage` |
| `Ssnm` | `M3uaSsnmMessage` |
| `SignallingCongestion` | `M3uaSignallingCongestionMessage` |
| `DestinationUserPartUnavailable` | `M3uaDestinationUserPartUnavailableMessage` |
| `RegistrationRequest` | `M3uaRegistrationRequestMessage` |
| `RegistrationResponse` | `M3uaRegistrationResponseMessage` |
| `DeregistrationRequest` | `M3uaDeregistrationRequestMessage` |
| `DeregistrationResponse` | `M3uaDeregistrationResponseMessage` |

## Behavior

- The dispatcher only returns supported, validated typed messages.
- Unsupported message classes or message types return `false` with a descriptive error.
- Message-specific parser validation still applies, including required parameters and duplicate singleton checks.
- The `As<T>()` helper performs a normal cast, so callers should match `Kind` before casting.

## Why This Exists

Production applications usually receive mixed M3UA traffic on the same association: DATA, ASPSM, ASPTM, management, SSNM, and sometimes RKM. Centralizing dispatch in the SDK avoids duplicated switch logic in every application and keeps parser behavior consistent.
