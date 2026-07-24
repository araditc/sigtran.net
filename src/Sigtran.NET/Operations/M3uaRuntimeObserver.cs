using Sigtran.NET.Layers.M3UA;

namespace Sigtran.NET.Operations;

/// <summary>
/// Projects M3UA runtime events into structured logs and .NET diagnostics.
/// </summary>
public sealed class M3uaRuntimeObserver : IDisposable
{
    private readonly M3uaRuntime _runtime;
    private readonly ISigtranEventSink _sink;
    private bool _disposed;

    /// <summary>Creates and attaches an M3UA runtime observer.</summary>
    /// <param name="runtime">The runtime to observe.</param>
    /// <param name="sink">The structured event destination.</param>
    public M3uaRuntimeObserver(
        M3uaRuntime runtime,
        ISigtranEventSink sink)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _runtime.RuntimeEvent += OnRuntimeEvent;
    }

    /// <summary>Detaches the observer from the runtime.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _runtime.RuntimeEvent -= OnRuntimeEvent;
        _disposed = true;
    }

    private void OnRuntimeEvent(object? sender, M3uaRuntimeEventArgs eventArgs)
    {
        switch (eventArgs.Kind)
        {
            case M3uaRuntimeEventKind.TransferSent:
                SigtranTelemetry.RecordTransfer(
                    "m3ua",
                    SigtranTransferDirection.Outbound,
                    eventArgs.AssociationName);
                break;
            case M3uaRuntimeEventKind.TransferReceived:
                SigtranTelemetry.RecordTransfer(
                    "m3ua",
                    SigtranTransferDirection.Inbound,
                    eventArgs.AssociationName);
                break;
            case M3uaRuntimeEventKind.ReconnectScheduled:
                SigtranTelemetry.RecordReconnect(
                    "m3ua",
                    eventArgs.AssociationName);
                break;
            case M3uaRuntimeEventKind.FaultObserved:
                SigtranTelemetry.RecordFault(
                    "m3ua",
                    "runtime",
                    eventArgs.AssociationName);
                break;
            case M3uaRuntimeEventKind.AspActivated:
                if (eventArgs.AssociationName is not null)
                {
                    SigtranTelemetry.RecordAssociationState(
                        "m3ua",
                        eventArgs.AssociationName,
                        active: true);
                }

                break;
            case M3uaRuntimeEventKind.ShutdownCompleted:
                if (eventArgs.AssociationName is not null)
                {
                    SigtranTelemetry.RecordAssociationState(
                        "m3ua",
                        eventArgs.AssociationName,
                        active: false);
                }

                break;
        }

        M3uaRuntimeMetrics metrics = _runtime.GetMetrics();
        SigtranTelemetry.RecordQueueDepth(
            "m3ua",
            "outbound",
            metrics.OutboundQueueDepth);
        SigtranTelemetry.RecordQueueDepth(
            "m3ua",
            "inbound",
            metrics.InboundQueueDepth);

        _sink.Write(new(
            eventArgs.ObservedAtUtc,
            $"m3ua.{ToEventName(eventArgs.Kind)}",
            ToSeverity(eventArgs.Kind),
            "m3ua",
            eventArgs.Detail,
            eventArgs.AssociationName,
            new Dictionary<string, string>
            {
                ["state"] = eventArgs.State.ToString(),
                ["queue.outbound.depth"] =
                    metrics.OutboundQueueDepth.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                ["queue.inbound.depth"] =
                    metrics.InboundQueueDepth.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
            }));
    }

    private static string ToEventName(M3uaRuntimeEventKind kind)
    {
        return kind switch
        {
            M3uaRuntimeEventKind.StateChanged => "state.changed",
            M3uaRuntimeEventKind.AspActivated => "asp.activated",
            M3uaRuntimeEventKind.TransferSent => "transfer.sent",
            M3uaRuntimeEventKind.TransferReceived => "transfer.received",
            M3uaRuntimeEventKind.HeartbeatAcknowledged =>
                "heartbeat.acknowledged",
            M3uaRuntimeEventKind.FaultObserved => "fault.observed",
            M3uaRuntimeEventKind.ReconnectScheduled =>
                "reconnect.scheduled",
            M3uaRuntimeEventKind.ShutdownCompleted =>
                "shutdown.completed",
            _ => "event"
        };
    }

    private static SigtranEventSeverity ToSeverity(
        M3uaRuntimeEventKind kind)
    {
        return kind switch
        {
            M3uaRuntimeEventKind.FaultObserved => SigtranEventSeverity.Error,
            M3uaRuntimeEventKind.ReconnectScheduled =>
                SigtranEventSeverity.Warning,
            M3uaRuntimeEventKind.TransferSent
                or M3uaRuntimeEventKind.TransferReceived =>
                SigtranEventSeverity.Debug,
            _ => SigtranEventSeverity.Information
        };
    }
}
