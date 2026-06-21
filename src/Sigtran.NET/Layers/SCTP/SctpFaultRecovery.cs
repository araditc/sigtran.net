namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Identifies transport-level SCTP fault categories.
/// </summary>
public enum SctpTransportFaultKind
{
    /// <summary>A connect operation exceeded its timeout.</summary>
    ConnectTimeout,

    /// <summary>A send operation exceeded its timeout.</summary>
    SendTimeout,

    /// <summary>A receive operation exceeded its timeout.</summary>
    ReceiveTimeout,

    /// <summary>A reconnect operation exceeded its timeout.</summary>
    ReconnectTimeout,

    /// <summary>A graceful shutdown operation exceeded its timeout.</summary>
    ShutdownTimeout,

    /// <summary>The caller cancelled the operation.</summary>
    CallerCancelled,

    /// <summary>The peer closed the association.</summary>
    PeerClosed,

    /// <summary>The peer reset the association.</summary>
    PeerReset,

    /// <summary>The platform SCTP socket is unavailable.</summary>
    SocketUnavailable,

    /// <summary>The platform SCTP socket reported an error.</summary>
    SocketError,

    /// <summary>The send queue rejected an outbound message because of backpressure limits.</summary>
    BackpressureRejected,

    /// <summary>The transport detected invalid protocol behavior.</summary>
    ProtocolError,

    /// <summary>The fault does not map to a more specific category.</summary>
    Unknown
}

/// <summary>
/// Recovery actions available after an SCTP transport fault.
/// </summary>
public enum SctpRecoveryAction
{
    /// <summary>No recovery action is required.</summary>
    None,

    /// <summary>Retry the failed operation without recreating the association.</summary>
    RetryOperation,

    /// <summary>Reconnect the SCTP association.</summary>
    ReconnectAssociation,

    /// <summary>Close the SCTP association.</summary>
    CloseAssociation,

    /// <summary>Fail the operation immediately and surface the fault to the caller.</summary>
    FailFast
}

/// <summary>
/// Describes an SCTP transport fault.
/// </summary>
public sealed class SctpTransportFault
{
    /// <summary>Creates an SCTP transport fault.</summary>
    /// <param name="kind">The fault category.</param>
    /// <param name="reason">The diagnostic reason.</param>
    /// <param name="exception">The optional source exception.</param>
    public SctpTransportFault(SctpTransportFaultKind kind, string reason, Exception? exception = null)
    {
        Kind = kind;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("SCTP transport fault reason is required.", nameof(reason)) : reason;
        Exception = exception;
    }

    /// <summary>The fault category.</summary>
    public SctpTransportFaultKind Kind { get; }

    /// <summary>The diagnostic reason.</summary>
    public string Reason { get; }

    /// <summary>The optional source exception.</summary>
    public Exception? Exception { get; }

    /// <summary>Formats a compact fault summary.</summary>
    /// <returns>The fault summary.</returns>
    public string Describe()
    {
        string exceptionName = Exception?.GetType().Name ?? "-";
        return $"kind={Kind} reason={Reason} exception={exceptionName}";
    }
}

/// <summary>
/// Describes the recovery decision for an SCTP transport fault.
/// </summary>
public sealed class SctpRecoveryDecision
{
    /// <summary>Creates an SCTP recovery decision.</summary>
    /// <param name="fault">The source transport fault.</param>
    /// <param name="action">The selected recovery action.</param>
    /// <param name="reason">The diagnostic decision reason.</param>
    /// <param name="nextReconnectAttempt">The next reconnect attempt, if a reconnect should be scheduled.</param>
    public SctpRecoveryDecision(
        SctpTransportFault fault,
        SctpRecoveryAction action,
        string reason,
        SctpReconnectScheduleEntry? nextReconnectAttempt = null)
    {
        Fault = fault ?? throw new ArgumentNullException(nameof(fault));
        Action = action;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("SCTP recovery decision reason is required.", nameof(reason)) : reason;
        NextReconnectAttempt = nextReconnectAttempt;
    }

    /// <summary>The source transport fault.</summary>
    public SctpTransportFault Fault { get; }

    /// <summary>The selected recovery action.</summary>
    public SctpRecoveryAction Action { get; }

    /// <summary>The diagnostic decision reason.</summary>
    public string Reason { get; }

    /// <summary>The next reconnect attempt, if a reconnect should be scheduled.</summary>
    public SctpReconnectScheduleEntry? NextReconnectAttempt { get; }

    /// <summary>Whether the decision schedules a reconnect attempt.</summary>
    public bool ShouldReconnect => Action == SctpRecoveryAction.ReconnectAssociation && NextReconnectAttempt.HasValue;

    /// <summary>Whether the fault should be surfaced to the caller immediately.</summary>
    public bool ShouldFailFast => Action == SctpRecoveryAction.FailFast;

    /// <summary>Formats a compact recovery decision summary.</summary>
    /// <returns>The recovery decision summary.</returns>
    public string Describe()
    {
        string reconnect = NextReconnectAttempt?.Describe() ?? "-";
        return $"action={Action} reconnect={ShouldReconnect} failFast={ShouldFailFast} reason={Reason} fault=({Fault.Describe()}) next=({reconnect})";
    }
}

/// <summary>
/// Provides deterministic SCTP fault recovery decisions.
/// </summary>
public static class SctpFaultRecovery
{
    /// <summary>Determines the recovery action for an SCTP transport fault.</summary>
    /// <param name="fault">The transport fault.</param>
    /// <param name="reconnectSchedule">The current reconnect schedule.</param>
    /// <param name="completedReconnectAttempts">The number of reconnect attempts already completed.</param>
    /// <returns>The recovery decision.</returns>
    public static SctpRecoveryDecision Decide(
        SctpTransportFault fault,
        SctpReconnectSchedule reconnectSchedule,
        int completedReconnectAttempts = 0)
    {
        ArgumentNullException.ThrowIfNull(fault);
        ArgumentNullException.ThrowIfNull(reconnectSchedule);

        return fault.Kind switch
        {
            SctpTransportFaultKind.BackpressureRejected => new(fault, SctpRecoveryAction.RetryOperation, "retry-after-backpressure-drain"),
            SctpTransportFaultKind.CallerCancelled => new(fault, SctpRecoveryAction.CloseAssociation, "caller-cancelled-operation"),
            SctpTransportFaultKind.ShutdownTimeout => new(fault, SctpRecoveryAction.CloseAssociation, "shutdown-timeout-close-association"),
            SctpTransportFaultKind.ProtocolError => new(fault, SctpRecoveryAction.FailFast, "protocol-error-requires-operator-attention"),
            SctpTransportFaultKind.Unknown => new(fault, SctpRecoveryAction.FailFast, "unknown-fault"),
            _ => DecideReconnectableFault(fault, reconnectSchedule, completedReconnectAttempts)
        };
    }

    private static SctpRecoveryDecision DecideReconnectableFault(
        SctpTransportFault fault,
        SctpReconnectSchedule reconnectSchedule,
        int completedReconnectAttempts)
    {
        SctpReconnectScheduleEntry? next = reconnectSchedule.GetNextAttempt(completedReconnectAttempts);
        if (next.HasValue)
        {
            return new(fault, SctpRecoveryAction.ReconnectAssociation, "schedule-reconnect-attempt", next);
        }

        return new(fault, SctpRecoveryAction.FailFast, "reconnect-attempts-exhausted");
    }
}
