# M3UA

M3UA is the first production-focused layer in SIGTRAN.NET. The implementation follows RFC 4666 message structure and uses strict network byte order for message headers and TLV parameters.

## Implemented Message Families

| Family | Message types |
| --- | --- |
| Transfer | DATA with optional Network Appearance, Routing Context, and Correlation Id |
| ASPSM | ASP Up, ASP Down, BEAT, ASP Up Ack, ASP Down Ack, BEAT Ack |
| ASPTM | ASP Active, ASP Inactive, ASP Active Ack, ASP Inactive Ack |
| Management | Error, Notify |
| SSNM common | DUNA, DAVA, DAUD, DRST |
| SSNM specialized | DUPU, SCON |
| RKM | REG REQ, REG RSP, DEREG REQ, DEREG RSP |

## Builder Pattern

Builders write encoded messages into a caller-owned `Span<byte>` and return the number of bytes written.

```csharp
Span<byte> buffer = stackalloc byte[256];

bool ok = M3uaMessageBuilder.BuildAspActive(
    buffer,
    M3uaTrafficModeType.Loadshare,
    routingContexts: [100, 200],
    infoString: ReadOnlySpan<byte>.Empty,
    out int written,
    out string? error);
```

This pattern avoids hidden allocations in hot paths and lets callers control buffer ownership.

## Typed Parsing

`M3uaMessage` handles common framing. Typed parsers then validate message-specific parameter rules.

```csharp
M3uaMessage message = new();
if (!message.TryDecode(buffer[..written], out string? error))
{
    throw new InvalidOperationException(error);
}

if (!M3uaTypedMessageParser.TryParseAsptm(
        message,
        out M3uaAspTrafficMaintenanceMessage? active,
        out error))
{
    throw new InvalidOperationException(error);
}
```

For application code that handles multiple M3UA message families, `M3uaTypedMessageParser.TryParseKnown` dispatches supported messages into a `M3uaTypedMessage` result with a `Kind` discriminator and concrete typed model.

## Transfer Notes

DATA messages are modeled with a typed parser so callers can access Network Appearance, Routing Context, OPC, DPC, SI, NI, MP, SLS, user payload, and Correlation Id without manually decoding the Protocol Data parameter.

The original `BuildPayloadData` overload remains available for simple DATA messages. A newer overload adds Network Appearance, Routing Context, and Correlation Id while preserving the same Protocol Data byte layout.

## ASP State

`M3uaAspStateMachine` models the local ASP lifecycle:

- `Down`
- `Inactive`
- `Active`

`M3uaAspSession` applies acknowledgement messages and records ASP Identifier, Traffic Mode, and Routing Context state.

## SSNM Notes

Common SSNM messages require at least one Affected Point Code. DUPU is modeled separately because it requires exactly one Affected Point Code with mask zero and a mandatory User/Cause parameter. SCON is modeled separately because it can include Concerned Destination and Congestion Indications.

## RKM Notes

Routing Key Management support covers the first dynamic registration path:

- Registration Request with one or more Routing Key parameters.
- Registration Response with one or more Registration Result parameters.
- Deregistration Request with a Routing Context list.
- Deregistration Response with one or more Deregistration Result parameters.

Routing Keys require a Local-RK-Identifier and at least one Destination Point Code. Routing Context, Traffic Mode Type, Network Appearance, Service Indicators, and Originating Point Code List are optional fields.

## Validation Rules

- Message lengths must match the common header length.
- TLV lengths must include the parameter header and stay inside the parameter buffer.
- Parameter padding is skipped but not exposed as value bytes.
- UInt32 lists must be non-empty and aligned to four bytes.
- Duplicate singleton parameters are rejected.
- Required parameters are checked before typed messages are returned.
