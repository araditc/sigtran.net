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

## Simulator Scripts

`SigtranSimulatorEndpoint`, `SigtranSimulatorStep`, and `SigtranSimulatorScript` describe deterministic protocol flows without opening sockets.

```csharp
SigtranSimulatorEndpoint asp = new("asp", SigtranSimulatorRole.Asp);
SigtranSimulatorEndpoint sg = new("sg", SigtranSimulatorRole.SignallingGateway);

SigtranSimulatorScript script = new();
script.Add(new SigtranSimulatorStep(asp, sg, "M3UA", payload, "ASP Up"));
```

Script steps can be rendered as trace summaries, making simulator output comparable with captured traffic and golden-vector files.

## MAP SMS Flows

`MapSmsSimulatorFlowBuilder` creates deterministic simulator scripts using the SDK MAP SMS and TCAP encoders.

```csharp
SigtranSimulatorScript script = new MapSmsSimulatorFlowBuilder()
    .AddSendRoutingInfoForShortMessage(msisdn, serviceCentre)
    .AddMobileTerminatedForwardShortMessage(imsi, serviceCentre, tpdu)
    .AddReportShortMessageDeliveryStatus(msisdn, serviceCentre, MapSmsDeliveryStatus.Delivered)
    .Build();
```

The builder covers current MAP SMS foundation operations: MO-ForwardSM, MT-ForwardSM, SendRoutingInfoForSM, ReportSM-DeliveryStatus, and AlertServiceCentre.

## Local Transport Samples

`SigtranTransportSamples.CreateLocalM3uaAspToSg()` describes a development-only TCP scenario for exercising the M3UA stack through the `TcpSctpAdapter`.

```csharp
SigtranLocalTcpScenario scenario = SigtranTransportSamples.CreateLocalM3uaAspToSg();
SctpConnectionOptions options = scenario.ToClientConnectionOptions();
```

The scenario carries M3UA PPID metadata and endpoint names that can be reused by examples, trace summaries, and simulator scripts. It does not make TCP production-ready; native SCTP remains the production transport gate.

## Sample Catalog

`SigtranSampleCatalog.GetSamples()` exposes the supported sample inventory in deterministic order.

| Id | Focus |
| --- | --- |
| `m3ua-asp-to-sg` | ASP startup, traffic activation, DATA, heartbeat, and shutdown |
| `m3ua-ipsp-peer` | Peer-to-peer M3UA lifecycle |
| `sccp-map-sms` | MAP SMS payloads through TCAP and SCCP |
| `local-tcp-m3ua` | Development-only TCP adapter traffic |

The catalog is intentionally metadata-only so documentation, tests, examples, and future CI jobs can share the same stable sample ids.
