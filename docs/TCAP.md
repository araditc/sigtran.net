# TCAP

Phase 4 replaces the early simplified TCAP byte layout with ASN.1 BER-shaped transaction, dialogue, and component primitives.

## BER Primitives

`TcapBerTag`, `TcapBerElement`, and `TcapBer` provide the low-level TLV boundary used by TCAP.

```csharp
TcapBerTag tag = new(
    TcapBerTagClass.ContextSpecific,
    constructed: true,
    number: 1);

Span<byte> buffer = stackalloc byte[32];
bool ok = TcapBer.TryWriteElement(buffer, tag, value, out int written, out string? error);
```

The BER helper supports definite short-form length and definite long-form lengths up to two length octets. Indefinite length is rejected because TCAP messages should be bounded and validation-friendly inside this SDK.
