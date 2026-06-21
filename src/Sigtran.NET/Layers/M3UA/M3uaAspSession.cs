namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Tracks local ASP session state from decoded M3UA acknowledgement messages.
/// </summary>
public sealed class M3uaAspSession
{
    private readonly M3uaAspStateMachine _stateMachine;
    private uint[] _routingContexts = Array.Empty<uint>();

    /// <summary>Creates an ASP session with the supplied initial state.</summary>
    /// <param name="initialState">The initial ASP lifecycle state.</param>
    public M3uaAspSession(M3uaAspState initialState = M3uaAspState.Down)
    {
        _stateMachine = new(initialState);
    }

    /// <summary>The current ASP lifecycle state.</summary>
    public M3uaAspState State => _stateMachine.State;

    /// <summary>The ASP Identifier most recently confirmed by ASP Up Ack, when present.</summary>
    public uint? AspIdentifier { get; private set; }

    /// <summary>The Traffic Mode Type most recently confirmed by ASP Active Ack, when present.</summary>
    public M3uaTrafficModeType? TrafficModeType { get; private set; }

    /// <summary>The Routing Context values most recently confirmed by ASP traffic acknowledgements.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>
    /// Determines whether the supplied Routing Context is currently confirmed on this ASP session.
    /// </summary>
    /// <param name="routingContext">The Routing Context value to find.</param>
    /// <returns>True if the Routing Context is present; otherwise false.</returns>
    public bool HasRoutingContext(uint routingContext)
    {
        for (int i = 0; i < _routingContexts.Length; i++)
        {
            if (_routingContexts[i] == routingContext)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies an incoming ASPSM or ASPTM acknowledgement message to this session.
    /// </summary>
    /// <param name="message">The decoded M3UA acknowledgement message.</param>
    /// <param name="transition">The state transition accepted by the session.</param>
    /// <param name="error">An error message when parsing or transition validation fails.</param>
    /// <returns>True if the acknowledgement was accepted; otherwise false.</returns>
    public bool TryApplyAcknowledgement(
        M3uaMessage message,
        out M3uaAspStateTransition transition,
        out string? error)
    {
        return message.MessageClass switch
        {
            M3uaMessageClass.Aspsm => TryApplyAspsmAcknowledgement(message, out transition, out error),
            M3uaMessageClass.Asptm => TryApplyAsptmAcknowledgement(message, out transition, out error),
            _ => Fail($"Expected ASPSM or ASPTM acknowledgement, got {message.MessageClass}", out transition, out error)
        };
    }

    /// <summary>
    /// Applies a transport-loss event and returns the resulting transition.
    /// </summary>
    /// <param name="transition">The state transition accepted by the session.</param>
    /// <param name="error">An error message when transition validation fails.</param>
    /// <returns>True if the transition was accepted; otherwise false.</returns>
    public bool NotifyTransportLost(out M3uaAspStateTransition transition, out string? error)
    {
        bool accepted = _stateMachine.TryApply(M3uaAspEvent.TransportLost, out transition, out error);
        if (accepted)
        {
            AspIdentifier = null;
            TrafficModeType = null;
            _routingContexts = Array.Empty<uint>();
        }

        return accepted;
    }

    /// <summary>
    /// Resets ASP lifecycle state and clears negotiated ASP session values.
    /// </summary>
    /// <param name="state">The ASP state to set after reset.</param>
    public void Reset(M3uaAspState state = M3uaAspState.Down)
    {
        _stateMachine.Reset(state);
        AspIdentifier = null;
        TrafficModeType = null;
        _routingContexts = Array.Empty<uint>();
    }

    private bool TryApplyAspsmAcknowledgement(
        M3uaMessage message,
        out M3uaAspStateTransition transition,
        out string? error)
    {
        if (!M3uaTypedMessageParser.TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? typed, out error))
        {
            transition = default;
            return false;
        }

        M3uaAspEvent? @event = typed!.MessageType switch
        {
            M3uaAspsmMessageType.AspUpAck => M3uaAspEvent.AspUpAcknowledged,
            M3uaAspsmMessageType.AspDownAck => M3uaAspEvent.AspDownAcknowledged,
            M3uaAspsmMessageType.HeartbeatAck => M3uaAspEvent.HeartbeatAcknowledged,
            _ => null
        };

        if (!@event.HasValue)
        {
            return Fail($"ASPSM message type {typed.MessageType} is not an acknowledgement", out transition, out error);
        }

        if (!_stateMachine.TryApply(@event.Value, out transition, out error))
        {
            return false;
        }

        if (typed.MessageType == M3uaAspsmMessageType.AspUpAck && typed.AspIdentifier.HasValue)
        {
            AspIdentifier = typed.AspIdentifier.Value;
        }
        else if (typed.MessageType == M3uaAspsmMessageType.AspDownAck)
        {
            TrafficModeType = null;
            _routingContexts = Array.Empty<uint>();
        }

        return true;
    }

    private bool TryApplyAsptmAcknowledgement(
        M3uaMessage message,
        out M3uaAspStateTransition transition,
        out string? error)
    {
        if (!M3uaTypedMessageParser.TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? typed, out error))
        {
            transition = default;
            return false;
        }

        M3uaAspEvent? @event = typed!.MessageType switch
        {
            M3uaAsptmMessageType.AspActiveAck => M3uaAspEvent.AspActiveAcknowledged,
            M3uaAsptmMessageType.AspInactiveAck => M3uaAspEvent.AspInactiveAcknowledged,
            _ => null
        };

        if (!@event.HasValue)
        {
            return Fail($"ASPTM message type {typed.MessageType} is not an acknowledgement", out transition, out error);
        }

        if (!_stateMachine.TryApply(@event.Value, out transition, out error))
        {
            return false;
        }

        if (typed.MessageType == M3uaAsptmMessageType.AspActiveAck)
        {
            TrafficModeType = typed.TrafficModeType;
        }

        _routingContexts = typed.RoutingContexts.ToArray();
        return true;
    }

    private static bool Fail(string message, out M3uaAspStateTransition transition, out string? error)
    {
        transition = default;
        error = message;
        return false;
    }
}
