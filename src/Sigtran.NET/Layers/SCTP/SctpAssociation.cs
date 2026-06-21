namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Lifecycle states for an SCTP association.
/// </summary>
public enum SctpAssociationState
{
    /// <summary>No association is established.</summary>
    Closed,
    /// <summary>An association attempt is in progress.</summary>
    Connecting,
    /// <summary>The association is established and can carry user messages.</summary>
    Established,
    /// <summary>The association is reconnecting after a failure.</summary>
    Reconnecting,
    /// <summary>The association is shutting down gracefully.</summary>
    ShuttingDown,
    /// <summary>The association failed and requires external handling.</summary>
    Failed
}

/// <summary>
/// Event types emitted by an SCTP association lifecycle.
/// </summary>
public enum SctpAssociationEventType
{
    /// <summary>An association attempt started.</summary>
    ConnectStarted,
    /// <summary>The association became established.</summary>
    Established,
    /// <summary>The association started reconnecting.</summary>
    ReconnectStarted,
    /// <summary>The association shutdown sequence started.</summary>
    ShutdownStarted,
    /// <summary>The association closed.</summary>
    Closed,
    /// <summary>The association failed.</summary>
    Failed
}

/// <summary>
/// Describes an SCTP association lifecycle event.
/// </summary>
public readonly struct SctpAssociationEvent
{
    /// <summary>Creates an SCTP association event.</summary>
    /// <param name="eventType">The lifecycle event type.</param>
    /// <param name="state">The state after the event.</param>
    /// <param name="reason">The optional diagnostic reason.</param>
    public SctpAssociationEvent(SctpAssociationEventType eventType, SctpAssociationState state, string? reason = null)
    {
        EventType = eventType;
        State = state;
        Reason = reason;
    }

    /// <summary>The lifecycle event type.</summary>
    public SctpAssociationEventType EventType { get; }

    /// <summary>The state after the event.</summary>
    public SctpAssociationState State { get; }

    /// <summary>The optional diagnostic reason.</summary>
    public string? Reason { get; }
}
