namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Describes the implemented M3UA runtime capabilities.
/// </summary>
public sealed class M3uaRuntimeReadinessSnapshot
{
    /// <summary>Creates an M3UA runtime readiness snapshot.</summary>
    /// <param name="hasAspLifecycle">Whether ASP startup and shutdown lifecycle is implemented.</param>
    /// <param name="hasBoundedQueues">Whether bounded traffic queues are implemented.</param>
    /// <param name="hasHeartbeatSupervision">Whether heartbeat correlation and timeout handling is implemented.</param>
    /// <param name="hasReconnectAndFailover">Whether session replacement and failover is implemented.</param>
    /// <param name="hasCancellation">Whether startup, traffic, and shutdown cancellation is implemented.</param>
    /// <param name="hasRuntimeEvents">Whether runtime events are exposed.</param>
    /// <param name="hasRuntimeMetrics">Whether traffic, queue, heartbeat, reconnect, and fault metrics are exposed.</param>
    /// <param name="hasMtp3Contract">Whether the runtime implements the MTP3 network contract.</param>
    public M3uaRuntimeReadinessSnapshot(
        bool hasAspLifecycle,
        bool hasBoundedQueues,
        bool hasHeartbeatSupervision,
        bool hasReconnectAndFailover,
        bool hasCancellation,
        bool hasRuntimeEvents,
        bool hasRuntimeMetrics,
        bool hasMtp3Contract)
    {
        HasAspLifecycle = hasAspLifecycle;
        HasBoundedQueues = hasBoundedQueues;
        HasHeartbeatSupervision = hasHeartbeatSupervision;
        HasReconnectAndFailover = hasReconnectAndFailover;
        HasCancellation = hasCancellation;
        HasRuntimeEvents = hasRuntimeEvents;
        HasRuntimeMetrics = hasRuntimeMetrics;
        HasMtp3Contract = hasMtp3Contract;
    }

    /// <summary>Whether ASP startup and shutdown lifecycle is implemented.</summary>
    public bool HasAspLifecycle { get; }

    /// <summary>Whether bounded traffic queues are implemented.</summary>
    public bool HasBoundedQueues { get; }

    /// <summary>Whether heartbeat correlation and timeout handling is implemented.</summary>
    public bool HasHeartbeatSupervision { get; }

    /// <summary>Whether session replacement and failover is implemented.</summary>
    public bool HasReconnectAndFailover { get; }

    /// <summary>Whether startup, traffic, and shutdown cancellation is implemented.</summary>
    public bool HasCancellation { get; }

    /// <summary>Whether runtime events are exposed.</summary>
    public bool HasRuntimeEvents { get; }

    /// <summary>Whether traffic, queue, heartbeat, reconnect, and fault metrics are exposed.</summary>
    public bool HasRuntimeMetrics { get; }

    /// <summary>Whether the runtime implements the MTP3 network contract.</summary>
    public bool HasMtp3Contract { get; }

    /// <summary>Whether the M3UA runtime implementation foundation is complete.</summary>
    public bool RuntimeReady => HasAspLifecycle
        && HasBoundedQueues
        && HasHeartbeatSupervision
        && HasReconnectAndFailover
        && HasCancellation
        && HasRuntimeEvents
        && HasRuntimeMetrics
        && HasMtp3Contract;
}

/// <summary>
/// Provides M3UA runtime readiness information.
/// </summary>
public static class M3uaRuntimeReadiness
{
    /// <summary>Returns the current M3UA runtime readiness snapshot.</summary>
    /// <returns>The current M3UA runtime readiness snapshot.</returns>
    public static M3uaRuntimeReadinessSnapshot GetReport()
    {
        return new(
            hasAspLifecycle: true,
            hasBoundedQueues: true,
            hasHeartbeatSupervision: true,
            hasReconnectAndFailover: true,
            hasCancellation: true,
            hasRuntimeEvents: true,
            hasRuntimeMetrics: true,
            hasMtp3Contract: true);
    }
}
