# M3UA

M3UA is the first production-focused layer in SIGTRAN.NET. The implementation follows RFC 4666 message structure and uses strict network byte order for message headers and TLV parameters.

## Implemented Message Families

| Family | Message types |
| --- | --- |
| Transfer | DATA |
| ASPSM | ASP Up, ASP Down, BEAT, ASP Up Ack, ASP Down Ack, BEAT Ack |
| ASPTM | ASP Active, ASP Inactive, ASP Active Ack, ASP Inactive Ack |
| Management | Error, Notify |
| SSNM common | DUNA, DAVA, DAUD, DRST |
| SSNM specialized | DUPU, SCON |

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

## ASP State

`M3uaAspStateMachine` models the local ASP lifecycle:

- `Down`
- `Inactive`
- `Active`

`M3uaAspSession` applies acknowledgement messages and records ASP Identifier, Traffic Mode, and Routing Context state.

## SSNM Notes

Common SSNM messages require at least one Affected Point Code. DUPU is modeled separately because it requires exactly one Affected Point Code with mask zero and a mandatory User/Cause parameter. SCON is modeled separately because it can include Concerned Destination and Congestion Indications.

## Validation Rules

- Message lengths must match the common header length.
- TLV lengths must include the parameter header and stay inside the parameter buffer.
- Parameter padding is skipped but not exposed as value bytes.
- UInt32 lists must be non-empty and aligned to four bytes.
- Duplicate singleton parameters are rejected.
- Required parameters are checked before typed messages are returned.
