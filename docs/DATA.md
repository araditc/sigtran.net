# M3UA Payload Data

Payload Data (DATA) carries the original SS7 MTP3-user message through M3UA. SIGTRAN.NET exposes both a low-level encoded `M3uaMessage` view and a typed `M3uaPayloadDataMessage` parser for application code.

## Parameters

| Parameter | Status | SDK model |
| --- | --- | --- |
| Network Appearance | Optional | `NetworkAppearance` |
| Routing Context | Conditional | `RoutingContext` |
| Protocol Data | Mandatory | OPC, DPC, SI, NI, MP, SLS, `UserPayload` |
| Correlation Id | Optional | `CorrelationId` |

## Building DATA

The basic overload builds a DATA message with only Protocol Data:

```csharp
Span<byte> buffer = stackalloc byte[256];

bool ok = M3uaMessageBuilder.BuildPayloadData(
    buffer,
    userPayload: [0x01, 0x02, 0x03],
    opc: 0x00010203,
    dpc: 0x00040506,
    si: 3,
    ni: 2,
    mp: 0,
    sls: 7,
    out int written,
    out string? error);
```

The extended overload also writes Network Appearance, Routing Context, and Correlation Id:

```csharp
bool ok = M3uaMessageBuilder.BuildPayloadData(
    buffer,
    userPayload: [0x01, 0x02, 0x03],
    opc: 0x00010203,
    dpc: 0x00040506,
    si: 3,
    ni: 2,
    mp: 0,
    sls: 7,
    networkAppearance: 7,
    routingContext: 100,
    correlationId: 42,
    out int written,
    out string? error);
```

## Parsing DATA

```csharp
M3uaMessage message = new();
if (!message.TryDecode(buffer[..written], out string? error))
{
    throw new InvalidOperationException(error);
}

if (!M3uaTypedMessageParser.TryParsePayloadData(
        message,
        out M3uaPayloadDataMessage? data,
        out error))
{
    throw new InvalidOperationException(error);
}
```

## Validation Rules

- DATA must use the Transfer message class and Payload Data message type.
- Protocol Data is mandatory.
- Network Appearance, Routing Context, Protocol Data, and Correlation Id are singleton parameters.
- Protocol Data must contain at least 12 bytes for OPC, DPC, SI, NI, MP, and SLS before user payload bytes.
- UInt32 optional fields must contain exactly four value bytes.
