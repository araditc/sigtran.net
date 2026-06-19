# Phase 8 Native SCTP

Phase 8 implements the native SCTP production transport path, starting with Linux.

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
