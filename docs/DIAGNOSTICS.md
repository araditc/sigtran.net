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
