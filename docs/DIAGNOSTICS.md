# M3UA Diagnostics

`M3uaDiagnostics` provides logger-neutral formatting helpers for troubleshooting M3UA traffic. The helpers return strings so applications can send the output to console logs, structured logging, test failure messages, or trace files.

## Hex Dump

`FormatHexDump` writes offset-prefixed hexadecimal lines without changing protocol state.

```csharp
string dump = M3uaDiagnostics.FormatHexDump(packet, bytesPerLine: 16);
```

## Header Summary

`TryFormatSummary` validates the M3UA common header and returns a compact one-line summary.

```csharp
if (M3uaDiagnostics.TryFormatSummary(packet, out string summary, out string? error))
{
    logger.LogDebug("{Summary}", summary);
}
```

The summary includes version, message class, message type, total length, and parameter length. Malformed packets return false with the same decode error used by `M3uaMessage`.

## Typed Summary

`TryFormatTypedSummary` validates the common header and then runs the typed dispatcher. It is useful for production traces where unsupported or malformed message types should be visible before an application handler runs.

```csharp
if (M3uaDiagnostics.TryFormatTypedSummary(packet, out string typed, out string? error))
{
    logger.LogInformation("{M3uaPacket}", typed);
}
```

The typed summary adds `kind`, such as `PayloadData`, `AspStateMaintenance`, `Error`, or `RegistrationResponse`. Unsupported messages return false with the dispatcher error.

## Parameter Inventory

`TryFormatParameterInventory` decodes the common header and walks the TLV parameter block. It reports the parameter count plus each tag, encoded length, value length, and padded length.

```csharp
if (M3uaDiagnostics.TryFormatParameterInventory(packet, out string inventory, out string? error))
{
    logger.LogTrace("{M3uaParameters}", inventory);
}
```

This helper is useful when comparing SDK output with packet captures or SG traces, especially before a full typed parser exists for a message variant.
