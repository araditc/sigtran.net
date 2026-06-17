# M3UA

M3UA is the first production-focused layer in SIGTRAN.NET. The implementation follows RFC 4666 message structure and uses strict network byte order for message headers and TLV parameters.

`M3uaProtocol` exposes public protocol metadata through `Name`, `Specification`, `PackageName`, `Version`, `HeaderLength`, and `ParameterHeaderLength`. Use these constants for diagnostics, generated capability reports, and compatibility checks instead of duplicating literal values.

## Implemented Message Families

| Family | Message types |
| --- | --- |
| Transfer | DATA with optional Network Appearance, Routing Context, and Correlation Id |
| ASPSM | ASP Up, ASP Down, BEAT, ASP Up Ack, ASP Down Ack, BEAT Ack |
| ASPTM | ASP Active, ASP Inactive, ASP Active Ack, ASP Inactive Ack |
| Management | Error, Notify with builders, parsers, and transport send helpers |
| SSNM common | DUNA, DAVA, DAUD, DRST with builders, parsers, and transport send helpers |
| SSNM specialized | DUPU, SCON with builders, parsers, and transport send helpers |
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

Common framing also exposes `M3uaMessage.HasParameter(tag)` and `M3uaMessage.TryGetParameter(tag, out value, out error)` for callers that need raw TLV checks before or outside typed parsing. `TryGetProtocolData` is a convenience wrapper over the same lookup path.

`M3uaInboundProcessor` combines decode, typed dispatch, optional ASP active-state enforcement for DATA, route resolution, and ASP acknowledgement state updates behind one processing call.

`M3uaOutboundProcessor` provides state-aware outbound builders for ASP lifecycle messages and DATA, including configured Network Appearance and Routing Context defaults.

`M3uaTransportSession` connects processors to an `ISctpSocket` so applications can receive one complete M3UA PDU or send common outbound M3UA messages asynchronously.

`M3uaAspClient` runs common lifecycle handshakes: startup with ASP Up and ASP Active, Heartbeat with Heartbeat Ack, and shutdown with ASP Inactive or ASP Down acknowledgements.

## Transfer Notes

DATA messages are modeled with a typed parser so callers can access Network Appearance, Routing Context, OPC, DPC, SI, NI, MP, SLS, user payload, and Correlation Id without manually decoding the Protocol Data parameter.

The original `BuildPayloadData` overload remains available for simple DATA messages. A newer overload adds Network Appearance, Routing Context, and Correlation Id while preserving the same Protocol Data byte layout.

`M3uaPayloadRouteTable` can resolve parsed DATA messages to application routes by Network Appearance, Routing Context, Destination Point Code, and Service Indicator. The table chooses the most specific matching route and rejects ambiguous equal-specificity matches.

## ASP State

`M3uaAspStateMachine` models the local ASP lifecycle:

- `Down`
- `Inactive`
- `Active`

`M3uaAspSession` applies acknowledgement messages and records ASP Identifier, Traffic Mode, and Routing Context state.

`M3uaAspSession.Reset` clears negotiated ASP Identifier, Traffic Mode, and Routing Context values and returns the lifecycle state to `Down` unless a different state is supplied. `NotifyTransportLost` also clears negotiated values after a transport-loss transition.

## SSNM Notes

Common SSNM messages require at least one Affected Point Code. DUPU is modeled separately because it requires exactly one Affected Point Code with mask zero and a mandatory User/Cause parameter. SCON is modeled separately because it can include Concerned Destination and Congestion Indications.

`M3uaTransportSession` can send common SSNM messages, DUPU, and SCON through dedicated methods.

## Management Notes

Management Error and Notify messages are available through builders, typed parsers, outbound processing, and `M3uaTransportSession` send helpers.

## RKM Notes

Routing Key Management support covers the first dynamic registration path:

- Registration Request with one or more Routing Key parameters.
- Registration Response with one or more Registration Result parameters.
- Deregistration Request with a Routing Context list.
- Deregistration Response with one or more Deregistration Result parameters.

Routing Keys require a Local-RK-Identifier and at least one Destination Point Code. Routing Context, Traffic Mode Type, Network Appearance, Service Indicators, and Originating Point Code List are optional fields.

`M3uaRkmClient` can send Registration and Deregistration requests over `M3uaTransportSession` and wait for the corresponding response type.

## Validation Rules

- Message lengths must match the common header length.
- TLV lengths must include the parameter header and stay inside the parameter buffer.
- Parameter padding is skipped but not exposed as value bytes.
- UInt32 lists must be non-empty and aligned to four bytes.
- Duplicate singleton parameters are rejected.
- Required parameters are checked before typed messages are returned.
