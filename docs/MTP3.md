# MTP3 Routing

Phase 3 starts by making the MTP3 boundary explicit. SCCP is carried as an MTP3 user part, so the SDK now exposes service information octet and routing label primitives instead of treating OPC, DPC, NI, MP, and SLS as unrelated integers.

## Service Information Octet

`Mtp3ServiceInformationOctet` stores the service indicator, network indicator, and message priority.

```csharp
Mtp3ServiceInformationOctet sio = new(
    Mtp3ServiceIndicator.Sccp,
    networkIndicator: 2,
    messagePriority: 1);

byte encoded = sio.Encode();
```

For SCCP traffic the service indicator is `Mtp3ServiceIndicator.Sccp`.

## Routing Label

`Mtp3RoutingLabel` models an ITU-style routing label with 14-bit DPC, 14-bit OPC, and 4-bit SLS.

```csharp
Mtp3RoutingLabel label = new(
    destinationPointCode: 0x1234,
    originatingPointCode: 0x2345,
    signallingLinkSelection: 0x0A);

Span<byte> buffer = stackalloc byte[4];
label.Encode(buffer);
```

The label validates point-code and SLS ranges before encoding, which keeps SCCP and M3UA call sites from silently producing malformed protocol data.
