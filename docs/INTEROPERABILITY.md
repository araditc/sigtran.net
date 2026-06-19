# Interoperability And Tooling

Phase 6 adds tooling for trace comparison, conformance vectors, simulators, samples, CI, and release readiness checks.

## Trace Frames

`SigtranTraceFrame` and `SigtranTraceFormatter` provide logger-neutral trace output for packet-capture comparison.

```csharp
SigtranTraceFrame frame = new(
    DateTimeOffset.UtcNow,
    "M3UA",
    SigtranTraceDirection.Outbound,
    "asp:2905",
    "sg:2905",
    payload);

string dump = SigtranTraceFormatter.FormatHexDump(frame);
```

The formatter emits a compact summary plus offset-based hex rows that can be compared with Wireshark exports, SG logs, or golden-vector files.

## Conformance Vectors

`SigtranConformanceVector` and `SigtranConformanceRegistry` store stable golden vectors by id.

```csharp
SigtranConformanceRegistry registry = new();
registry.Add(new SigtranConformanceVector(
    "m3ua/asp-up",
    "M3UA",
    "ASP Up message",
    payload,
    "RFC 4666"));
```

Registries preserve deterministic snapshot ordering so vector inventories can be compared in CI and release reviews.

`SigtranBuiltInVectors.CreateRegistry()` returns SDK-generated baseline vectors for current protocol foundations, including M3UA ASP Up and MAP MO-ForwardSM payloads.
