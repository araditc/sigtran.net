# Phase 47 M3UA Runtime

## Objective

Provide a long-running M3UA ASP service that upper layers can consume through
`IMtp3Network` without coordinating transport reads, ASP handshakes, heartbeat
responses, reconnects, or queue pressure themselves.

## Runtime API

`M3uaRuntime` implements `IMtp3Network` and owns:

- ASP Up and ASP Active startup.
- ASP Inactive and ASP Down graceful shutdown sends.
- A single inbound M3UA receive loop.
- Bounded inbound and outbound MTP3 transfer channels.
- Automatic response to peer heartbeats.
- Correlated heartbeat supervision with timeout handling.
- Session replacement and failover through `IM3uaRuntimeSessionFactory`.
- Reconnect backoff through `SctpReconnectPolicy`.
- Runtime lifecycle, traffic, heartbeat, reconnect, shutdown, and fault events.
- Queue, transfer, heartbeat, reconnect, and fault metrics.
- Cancellation for startup, traffic waits, reconnect delays, and shutdown.

## Session Factory

`IM3uaRuntimeSessionFactory.OpenAsync` returns an
`M3uaRuntimeSessionLease`. The factory owns endpoint selection and can rotate
between primary and secondary associations after a fault.

The returned `M3uaTransportSession` should share one `M3uaAspSession` between its
inbound and outbound processors. Production payload policy should require an
active ASP.

```csharp
M3uaAspSession aspSession = new();
M3uaInboundProcessor inbound = new(
    aspSession,
    requireActiveAspForPayload: true);
M3uaOutboundProcessor outbound = new(
    aspSession,
    networkAppearance: 7,
    routingContext: 100,
    requireActiveAspForPayload: true);

IM3uaRuntimeSessionFactory factory = new M3uaDelegateRuntimeSessionFactory(
    async ct =>
    {
        ISctpTransport transport = await OpenSctpTransportAsync(ct);
        M3uaTransportSession session = new(
            transport,
            inbound,
            outbound);
        return new M3uaRuntimeSessionLease("primary-sg", session);
    });

await using M3uaRuntime runtime = new(
    factory,
    new M3uaRuntimeOptions(
        startupOptions: new M3uaAspStartupOptions(
            aspIdentifier: 42,
            trafficModeType: M3uaTrafficModeType.Loadshare)));

await runtime.StartAsync();
IMtp3Network network = runtime;
```

The session factory delegate should create new processor instances for each
replacement session when reconnect and failover are enabled.

## Backpressure

Both runtime channels use `BoundedChannelFullMode.Wait`. Producers are suspended
when the outbound queue is full and the receiver loop is suspended when an upper
layer does not drain the inbound queue. Both waits honor caller cancellation.

`M3uaRuntimeMetrics` reports both queue depths so operators can alert before
traffic latency becomes unacceptable.

## Heartbeats

Only the runtime receive loop reads M3UA messages. Heartbeat requests carry a
monotonic 64-bit token, and the receive loop correlates the echoed
`Heartbeat Ack`. A timeout faults the active session and activates the reconnect
policy.

Inbound peer heartbeats are acknowledged automatically.

## Completion Criteria

Phase 47 is complete because:

- The runtime implements `IMtp3Network`.
- ASP activation, heartbeat supervision, traffic, and shutdown are covered by an
  executable loopback test.
- Queue pressure, events, and metrics are public and documented.
- Reconnect/failover is delegated through a replaceable session factory.
- Product readiness no longer reports the M3UA runtime implementation blocker.
- Build, tests, and package validation pass.

