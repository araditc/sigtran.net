namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// ASP lifecycle states used by the M3UA ASP state machine.
/// </summary>
public enum M3uaAspState
{
    /// <summary>The ASP is administratively or transport-down.</summary>
    Down,
    /// <summary>The ASP is up but not carrying traffic.</summary>
    Inactive,
    /// <summary>The ASP is active and may carry traffic.</summary>
    Active
}

/// <summary>
/// Events accepted by the M3UA ASP state machine.
/// </summary>
public enum M3uaAspEvent
{
    /// <summary>An ASP Up acknowledgement was received or accepted.</summary>
    AspUpAcknowledged,
    /// <summary>An ASP Active acknowledgement was received or accepted.</summary>
    AspActiveAcknowledged,
    /// <summary>An ASP Inactive acknowledgement was received or accepted.</summary>
    AspInactiveAcknowledged,
    /// <summary>An ASP Down acknowledgement was received or accepted.</summary>
    AspDownAcknowledged,
    /// <summary>A Heartbeat acknowledgement was received or accepted.</summary>
    HeartbeatAcknowledged,
    /// <summary>The SCTP association or underlying transport failed.</summary>
    TransportLost
}

/// <summary>
/// Describes the result of applying an event to an ASP state machine.
/// </summary>
public readonly struct M3uaAspStateTransition
{
    /// <summary>Creates a transition result.</summary>
    /// <param name="from">The state before applying the event.</param>
    /// <param name="to">The state after applying the event.</param>
    /// <param name="event">The event that was applied.</param>
    /// <param name="changed">Whether the state changed.</param>
    public M3uaAspStateTransition(M3uaAspState from, M3uaAspState to, M3uaAspEvent @event, bool changed)
    {
        From = from;
        To = to;
        Event = @event;
        Changed = changed;
    }

    /// <summary>The state before applying the event.</summary>
    public M3uaAspState From { get; }

    /// <summary>The state after applying the event.</summary>
    public M3uaAspState To { get; }

    /// <summary>The event that was applied.</summary>
    public M3uaAspEvent Event { get; }

    /// <summary>Whether the state changed.</summary>
    public bool Changed { get; }
}

/// <summary>
/// Implements the local ASP lifecycle transitions needed by M3UA session logic.
/// </summary>
public sealed class M3uaAspStateMachine
{
    /// <summary>Creates a state machine with the supplied initial state.</summary>
    /// <param name="initialState">The initial ASP state.</param>
    public M3uaAspStateMachine(M3uaAspState initialState = M3uaAspState.Down)
    {
        State = initialState;
    }

    /// <summary>The current ASP state.</summary>
    public M3uaAspState State { get; private set; }

    /// <summary>
    /// Attempts to apply an event and update the ASP state.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    /// <param name="transition">The transition result on success.</param>
    /// <param name="error">An error message when the event is invalid for the current state.</param>
    /// <returns>True if the event was accepted; otherwise false.</returns>
    public bool TryApply(M3uaAspEvent @event, out M3uaAspStateTransition transition, out string? error)
    {
        M3uaAspState from = State;
        error = null;

        M3uaAspState? to = (State, @event) switch
        {
            (_, M3uaAspEvent.TransportLost) => M3uaAspState.Down,
            (_, M3uaAspEvent.HeartbeatAcknowledged) => State,
            (M3uaAspState.Down, M3uaAspEvent.AspUpAcknowledged) => M3uaAspState.Inactive,
            (M3uaAspState.Inactive, M3uaAspEvent.AspActiveAcknowledged) => M3uaAspState.Active,
            (M3uaAspState.Active, M3uaAspEvent.AspInactiveAcknowledged) => M3uaAspState.Inactive,
            (M3uaAspState.Inactive, M3uaAspEvent.AspDownAcknowledged) => M3uaAspState.Down,
            (M3uaAspState.Active, M3uaAspEvent.AspDownAcknowledged) => M3uaAspState.Down,
            _ => null
        };

        if (!to.HasValue)
        {
            transition = default;
            error = $"Cannot apply {@event} while ASP is {State}";
            return false;
        }

        State = to.Value;
        transition = new(from, State, @event, from != State);
        return true;
    }

    /// <summary>
    /// Resets the state machine to a known ASP state without applying a protocol event.
    /// </summary>
    /// <param name="state">The state to set.</param>
    public void Reset(M3uaAspState state = M3uaAspState.Down)
    {
        State = state;
    }
}
