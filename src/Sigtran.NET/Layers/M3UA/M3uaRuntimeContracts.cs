using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Identifies the lifecycle state of an M3UA runtime.
/// </summary>
public enum M3uaRuntimeState
{
    /// <summary>The runtime is not running.</summary>
    Stopped,

    /// <summary>The runtime is opening its first transport session.</summary>
    Starting,

    /// <summary>The ASP is active and can carry MTP3 transfers.</summary>
    Active,

    /// <summary>The runtime is replacing a failed transport session.</summary>
    Reconnecting,

    /// <summary>The runtime is performing graceful shutdown.</summary>
    Stopping,

    /// <summary>The runtime exhausted its recovery policy.</summary>
    Faulted
}

/// <summary>
/// Identifies an observable M3UA runtime event.
/// </summary>
public enum M3uaRuntimeEventKind
{
    /// <summary>The runtime lifecycle state changed.</summary>
    StateChanged,

    /// <summary>An ASP startup handshake completed.</summary>
    AspActivated,

    /// <summary>An MTP3 transfer was sent.</summary>
    TransferSent,

    /// <summary>An MTP3 transfer was received.</summary>
    TransferReceived,

    /// <summary>A heartbeat acknowledgement was received.</summary>
    HeartbeatAcknowledged,

    /// <summary>A transport or protocol fault was observed.</summary>
    FaultObserved,

    /// <summary>A reconnect attempt was scheduled.</summary>
    ReconnectScheduled,

    /// <summary>The runtime completed graceful shutdown.</summary>
    ShutdownCompleted
}

/// <summary>
/// Carries one observable M3UA runtime event.
/// </summary>
public sealed class M3uaRuntimeEventArgs : EventArgs
{
    /// <summary>Creates an M3UA runtime event.</summary>
    /// <param name="kind">The event kind.</param>
    /// <param name="state">The runtime state when the event was recorded.</param>
    /// <param name="observedAtUtc">The UTC observation time.</param>
    /// <param name="associationName">The active association name, when available.</param>
    /// <param name="detail">The optional diagnostic detail.</param>
    public M3uaRuntimeEventArgs(
        M3uaRuntimeEventKind kind,
        M3uaRuntimeState state,
        DateTimeOffset observedAtUtc,
        string? associationName = null,
        string? detail = null)
    {
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Runtime event time must use UTC.", nameof(observedAtUtc));
        }

        Kind = kind;
        State = state;
        ObservedAtUtc = observedAtUtc;
        AssociationName = associationName;
        Detail = detail;
    }

    /// <summary>The event kind.</summary>
    public M3uaRuntimeEventKind Kind { get; }

    /// <summary>The runtime state when the event was recorded.</summary>
    public M3uaRuntimeState State { get; }

    /// <summary>The UTC observation time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>The active association name, when available.</summary>
    public string? AssociationName { get; }

    /// <summary>The optional diagnostic detail.</summary>
    public string? Detail { get; }
}

/// <summary>
/// Represents one opened M3UA transport session selected by a runtime session factory.
/// </summary>
public sealed class M3uaRuntimeSessionLease
{
    /// <summary>Creates an opened runtime session lease.</summary>
    /// <param name="associationName">The stable association name.</param>
    /// <param name="session">The opened M3UA transport session.</param>
    public M3uaRuntimeSessionLease(
        string associationName,
        M3uaTransportSession session)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName)
            ? throw new ArgumentException("Association name is required.", nameof(associationName))
            : associationName;
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>The stable association name.</summary>
    public string AssociationName { get; }

    /// <summary>The opened M3UA transport session.</summary>
    public M3uaTransportSession Session { get; }
}

/// <summary>
/// Opens M3UA transport sessions and can select a different association after a fault.
/// </summary>
public interface IM3uaRuntimeSessionFactory
{
    /// <summary>Opens the next M3UA transport session.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The opened runtime session lease.</returns>
    ValueTask<M3uaRuntimeSessionLease> OpenAsync(CancellationToken ct = default);
}

/// <summary>
/// Adapts an asynchronous delegate to an M3UA runtime session factory.
/// </summary>
public sealed class M3uaDelegateRuntimeSessionFactory : IM3uaRuntimeSessionFactory
{
    private readonly Func<CancellationToken, ValueTask<M3uaRuntimeSessionLease>> _open;

    /// <summary>Creates a delegate-backed runtime session factory.</summary>
    /// <param name="open">The asynchronous session-opening delegate.</param>
    public M3uaDelegateRuntimeSessionFactory(
        Func<CancellationToken, ValueTask<M3uaRuntimeSessionLease>> open)
    {
        _open = open ?? throw new ArgumentNullException(nameof(open));
    }

    /// <inheritdoc />
    public ValueTask<M3uaRuntimeSessionLease> OpenAsync(
        CancellationToken ct = default)
    {
        return _open(ct);
    }
}

/// <summary>
/// Configures a long-running M3UA ASP runtime.
/// </summary>
public sealed class M3uaRuntimeOptions
{
    /// <summary>Creates M3UA runtime options.</summary>
    /// <param name="startupOptions">The ASP startup handshake options.</param>
    /// <param name="reconnectPolicy">The reconnect and association failover policy.</param>
    /// <param name="outboundQueueCapacity">The bounded outbound MTP3 transfer capacity.</param>
    /// <param name="inboundQueueCapacity">The bounded inbound MTP3 transfer capacity.</param>
    /// <param name="heartbeatInterval">The heartbeat interval, or zero to disable active heartbeats.</param>
    /// <param name="heartbeatTimeout">The maximum wait for one heartbeat acknowledgement.</param>
    /// <param name="shutdownTimeout">The maximum graceful shutdown duration.</param>
    public M3uaRuntimeOptions(
        M3uaAspStartupOptions? startupOptions = null,
        SctpReconnectPolicy? reconnectPolicy = null,
        int outboundQueueCapacity = 1024,
        int inboundQueueCapacity = 1024,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? heartbeatTimeout = null,
        TimeSpan? shutdownTimeout = null)
    {
        if (outboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outboundQueueCapacity),
                "Outbound queue capacity must be positive.");
        }

        if (inboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inboundQueueCapacity),
                "Inbound queue capacity must be positive.");
        }

        StartupOptions = startupOptions ?? new M3uaAspStartupOptions(
            trafficModeType: M3uaTrafficModeType.Loadshare);
        ReconnectPolicy = reconnectPolicy ?? new SctpReconnectPolicy();
        OutboundQueueCapacity = outboundQueueCapacity;
        InboundQueueCapacity = inboundQueueCapacity;
        HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30);
        HeartbeatTimeout = heartbeatTimeout ?? TimeSpan.FromSeconds(10);
        ShutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(10);

        if (HeartbeatInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heartbeatInterval),
                "Heartbeat interval must not be negative.");
        }

        if (HeartbeatTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heartbeatTimeout),
                "Heartbeat timeout must be positive.");
        }

        if (ShutdownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shutdownTimeout),
                "Shutdown timeout must be positive.");
        }
    }

    /// <summary>The ASP startup handshake options.</summary>
    public M3uaAspStartupOptions StartupOptions { get; }

    /// <summary>The reconnect and association failover policy.</summary>
    public SctpReconnectPolicy ReconnectPolicy { get; }

    /// <summary>The bounded outbound MTP3 transfer capacity.</summary>
    public int OutboundQueueCapacity { get; }

    /// <summary>The bounded inbound MTP3 transfer capacity.</summary>
    public int InboundQueueCapacity { get; }

    /// <summary>The heartbeat interval, or zero when active heartbeats are disabled.</summary>
    public TimeSpan HeartbeatInterval { get; }

    /// <summary>The maximum wait for one heartbeat acknowledgement.</summary>
    public TimeSpan HeartbeatTimeout { get; }

    /// <summary>The maximum graceful shutdown duration.</summary>
    public TimeSpan ShutdownTimeout { get; }

    /// <summary>Whether active runtime heartbeats are enabled.</summary>
    public bool HeartbeatsEnabled => HeartbeatInterval > TimeSpan.Zero;
}

/// <summary>
/// Captures point-in-time M3UA runtime counters and queue pressure.
/// </summary>
public readonly struct M3uaRuntimeMetrics
{
    /// <summary>Creates an M3UA runtime metrics snapshot.</summary>
    /// <param name="state">The current runtime state.</param>
    /// <param name="outboundQueueDepth">The current outbound queue depth.</param>
    /// <param name="inboundQueueDepth">The current inbound queue depth.</param>
    /// <param name="sentTransfers">The number of sent MTP3 transfers.</param>
    /// <param name="receivedTransfers">The number of received MTP3 transfers.</param>
    /// <param name="heartbeatsSent">The number of sent heartbeats.</param>
    /// <param name="heartbeatsAcknowledged">The number of acknowledged heartbeats.</param>
    /// <param name="heartbeatTimeouts">The number of heartbeat timeouts.</param>
    /// <param name="reconnectAttempts">The number of reconnect attempts.</param>
    /// <param name="faults">The number of observed runtime faults.</param>
    public M3uaRuntimeMetrics(
        M3uaRuntimeState state,
        int outboundQueueDepth,
        int inboundQueueDepth,
        long sentTransfers,
        long receivedTransfers,
        long heartbeatsSent,
        long heartbeatsAcknowledged,
        long heartbeatTimeouts,
        long reconnectAttempts,
        long faults)
    {
        State = state;
        OutboundQueueDepth = outboundQueueDepth;
        InboundQueueDepth = inboundQueueDepth;
        SentTransfers = sentTransfers;
        ReceivedTransfers = receivedTransfers;
        HeartbeatsSent = heartbeatsSent;
        HeartbeatsAcknowledged = heartbeatsAcknowledged;
        HeartbeatTimeouts = heartbeatTimeouts;
        ReconnectAttempts = reconnectAttempts;
        Faults = faults;
    }

    /// <summary>The current runtime state.</summary>
    public M3uaRuntimeState State { get; }

    /// <summary>The current outbound queue depth.</summary>
    public int OutboundQueueDepth { get; }

    /// <summary>The current inbound queue depth.</summary>
    public int InboundQueueDepth { get; }

    /// <summary>The number of sent MTP3 transfers.</summary>
    public long SentTransfers { get; }

    /// <summary>The number of received MTP3 transfers.</summary>
    public long ReceivedTransfers { get; }

    /// <summary>The number of sent heartbeats.</summary>
    public long HeartbeatsSent { get; }

    /// <summary>The number of acknowledged heartbeats.</summary>
    public long HeartbeatsAcknowledged { get; }

    /// <summary>The number of heartbeat timeouts.</summary>
    public long HeartbeatTimeouts { get; }

    /// <summary>The number of reconnect attempts.</summary>
    public long ReconnectAttempts { get; }

    /// <summary>The number of observed runtime faults.</summary>
    public long Faults { get; }
}
