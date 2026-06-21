# Phase 8 Native SCTP

Phase 8 implements the native SCTP production transport path, starting with Linux.

See [Phase 8 Summary](PHASE8_SUMMARY.md) for the completed unit inventory and remaining Linux verification gate.

## Platform Probe

`NativeSctpPlatform.Probe()` checks whether the current runtime can create an SCTP socket.

```csharp
NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();
bool canCreateSocket = capability.CanCreateSocket;
```

The SDK uses Linux `IPPROTO_SCTP` protocol number `132` with `SocketType.Seqpacket`. Windows and macOS remain contract-only until a verified production provider is selected.

The probe is intentionally separate from readiness. Socket creation is necessary, but production readiness also requires metadata handling, lifecycle integration, integration tests, and external interoperability evidence.

## Socket Factory

`NativeSctpSocketFactory` creates unconnected native SCTP sockets after checking `NativeSctpPlatform.Probe()`.

```csharp
INativeSctpSocketFactory factory = new NativeSctpSocketFactory();
using Socket socket = factory.CreateSocket();
```

When native SCTP is unavailable, the factory throws `NativeSctpUnavailableException` with the platform capability result attached. This keeps unsupported Windows and macOS environments explicit.

## Connection Planning

`NativeSctpConnectionPlanner` resolves `SctpConnectionOptions` into a native connection plan.

```csharp
NativeSctpConnectionPlan plan = await new NativeSctpConnectionPlanner()
    .BuildAsync(options);
```

The planner resolves the remote endpoint and optional local endpoint before a socket attempts bind/connect. Phase 8 currently resolves IPv4 endpoints because the Linux native SCTP path starts with IPv4 verification.

## Socket Adapter

`NativeSctpSocketAdapter` wraps a native SCTP socket behind the SDK `ISctpSocket` contract.

```csharp
using Socket socket = factory.CreateSocket();
using NativeSctpSocketAdapter adapter = new(socket, options);
```

The adapter exposes lifecycle state and `SctpTransportHealth` snapshots. It currently sends and receives complete socket messages through the native socket API; SCTP ancillary metadata handling is tracked separately before production readiness can be claimed.

## Connector

`NativeSctpConnector` builds a connection plan, optionally binds the local endpoint, applies `SctpConnectionOptions.ConnectTimeout`, and returns an established `NativeSctpSocketAdapter`.

```csharp
NativeSctpConnector connector = new();
NativeSctpSocketAdapter socket = await connector.ConnectAsync(options);
```

The connector is Linux-native only through `NativeSctpSocketFactory`; unsupported platforms fail before attempting network I/O.

## Listener

`NativeSctpListener` provides the server-side bind/listen/accept path for native SCTP.

```csharp
NativeSctpListenerOptions options = new(new SctpEndpoint("0.0.0.0", 2905));
using NativeSctpListener listener = new();
await listener.StartAsync(options);
NativeSctpSocketAdapter association = await listener.AcceptAsync(options);
```

The listener shares the same socket factory and unsupported-platform behavior as the connector. Real accept/send/receive verification belongs in Linux SCTP lab runs.

## Lab Profile

Native SCTP lab tests are opt-in through `SIGTRAN_NATIVE_SCTP_LAB=1`.

```powershell
$env:SIGTRAN_NATIVE_SCTP_LAB = "1"
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
```

`NativeSctpLab.CreateFromEnvironment()` exposes the current lab profile. The default loopback endpoint is `127.0.0.1:2905`.

## Readiness

`NativeSctpReadiness.GetReport()` summarizes Phase 8 native SCTP status.

```csharp
NativeSctpReadinessReport report = NativeSctpReadiness.GetReport();
bool foundationReady = report.FoundationReady;
bool productionReady = report.IsProductionReady;
```

The native SCTP foundation is ready when platform probe, socket factory, connection planner, socket adapter, connector, listener, and lab profile are present. Production readiness remains false until Linux SCTP verification passes.

`SigtranNativeSctpSupport.IsImplementationFoundationReady()` now reflects this Phase 8 foundation status, while commercial readiness remains tied to `NativeSctpReadinessReport.IsProductionReady`.
