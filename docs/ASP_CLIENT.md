# M3UA ASP Client

`M3uaAspClient` runs common ASP lifecycle handshakes over `M3uaTransportSession`.

## Startup

`StartAsync` performs:

1. Send ASP Up.
2. Receive and process messages until ASP Up Ack is accepted.
3. Send ASP Active.
4. Receive and process messages until ASP Active Ack is accepted.

The inbound processor updates the shared `M3uaAspSession`, so after a successful startup the ASP state is `Active`.

## Heartbeat

`SendHeartbeatAsync` sends BEAT with optional Heartbeat Data and waits for BEAT Ack. The inbound processor applies the Heartbeat acknowledgement to the ASP session. The accepted transition does not change the ASP state.

```csharp
M3uaInboundProcessingResult heartbeat = await client.SendHeartbeatAsync(
    heartbeatData: new byte[] { 0x01, 0x02, 0x03 },
    ct: ct);
```

## Shutdown

`DeactivateAsync` sends ASP Inactive and waits for ASP Inactive Ack. `StopAsync` sends ASP Down and waits for ASP Down Ack. Applications can call both for a graceful Active -> Inactive -> Down shutdown, or call `StopAsync` directly when the peer accepts ASP Down from the current state.

```csharp
await client.DeactivateAsync(ct: ct);
await client.StopAsync(ct: ct);
```

## Example

```csharp
M3uaAspSession aspSession = new();
M3uaInboundProcessor inbound = new(aspSession);
M3uaOutboundProcessor outbound = new(
    aspSession,
    networkAppearance: 7,
    routingContext: 100);

await using M3uaTransportSession transport = new(
    socket,
    inbound,
    outbound);

M3uaAspClient client = new(transport);

M3uaAspStartupResult result = await client.StartAsync(
    new M3uaAspStartupOptions(
        aspIdentifier: 42,
        trafficModeType: M3uaTrafficModeType.Loadshare),
    ct);
```

## Options

| Option | Meaning |
| --- | --- |
| `AspIdentifier` | Optional ASP Identifier sent in ASP Up |
| `TrafficModeType` | Optional Traffic Mode Type sent in ASP Active |
| `AspUpInfoString` | Optional Info String sent in ASP Up |
| `AspActiveInfoString` | Optional Info String sent in ASP Active |
| `MaxHandshakeMessages` | Maximum inbound messages inspected while waiting for each startup acknowledgement |

## Failure Behavior

The client throws when:

- The transport closes before the expected acknowledgement arrives.
- The inbound processor rejects an acknowledgement or state transition.
- The expected acknowledgement is not seen within `MaxHandshakeMessages`.

## Current Scope

Startup, explicit Heartbeat, ASP Inactive, and ASP Down handshakes are modeled here. Reconnect, heartbeat scheduling, and multi-ASP traffic-mode policy belong in later lifecycle work.
