namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents the result of processing one inbound M3UA packet.
/// </summary>
public sealed class M3uaInboundProcessingResult
{
    /// <summary>Creates an inbound processing result.</summary>
    /// <param name="message">The decoded generic M3UA message.</param>
    /// <param name="typedMessage">The typed dispatcher result.</param>
    /// <param name="payloadRoute">The resolved DATA route, when a DATA message was routed.</param>
    /// <param name="stateTransition">The accepted ASP state transition, when an acknowledgement updated the session.</param>
    public M3uaInboundProcessingResult(
        M3uaMessage message,
        M3uaTypedMessage typedMessage,
        M3uaPayloadRoute? payloadRoute,
        M3uaAspStateTransition? stateTransition)
    {
        Message = message;
        TypedMessage = typedMessage;
        PayloadRoute = payloadRoute;
        StateTransition = stateTransition;
    }

    /// <summary>The decoded generic M3UA message.</summary>
    public M3uaMessage Message { get; }

    /// <summary>The typed dispatcher result.</summary>
    public M3uaTypedMessage TypedMessage { get; }

    /// <summary>The resolved DATA route, when a DATA message was routed.</summary>
    public M3uaPayloadRoute? PayloadRoute { get; }

    /// <summary>The accepted ASP state transition, when an acknowledgement updated the session.</summary>
    public M3uaAspStateTransition? StateTransition { get; }
}

/// <summary>
/// Decodes, dispatches, updates ASP session state, and routes inbound M3UA messages.
/// </summary>
public sealed class M3uaInboundProcessor
{
    /// <summary>Creates an inbound M3UA processor.</summary>
    /// <param name="aspSession">The ASP session to update from acknowledgement messages.</param>
    /// <param name="payloadRoutes">The optional DATA route table.</param>
    /// <param name="requireActiveAspForPayload">Whether DATA should be rejected unless the ASP session is active.</param>
    public M3uaInboundProcessor(
        M3uaAspSession? aspSession = null,
        M3uaPayloadRouteTable? payloadRoutes = null,
        bool requireActiveAspForPayload = false)
    {
        AspSession = aspSession ?? new M3uaAspSession();
        PayloadRoutes = payloadRoutes ?? new M3uaPayloadRouteTable();
        RequireActiveAspForPayload = requireActiveAspForPayload;
    }

    /// <summary>The ASP session updated from ASPSM and ASPTM acknowledgement messages.</summary>
    public M3uaAspSession AspSession { get; }

    /// <summary>The DATA route table used for typed Payload Data messages.</summary>
    public M3uaPayloadRouteTable PayloadRoutes { get; }

    /// <summary>Whether DATA should be rejected unless the ASP session is active.</summary>
    public bool RequireActiveAspForPayload { get; }

    /// <summary>
    /// Processes one inbound M3UA packet.
    /// </summary>
    /// <param name="packet">The encoded M3UA packet bytes.</param>
    /// <param name="result">The processing result on success.</param>
    /// <param name="error">An error message if decoding, parsing, state update, or routing fails.</param>
    /// <returns>True if the packet was accepted; otherwise false.</returns>
    public bool TryProcess(
        ReadOnlySpan<byte> packet,
        out M3uaInboundProcessingResult? result,
        out string? error)
    {
        result = null;
        M3uaMessage message = new();
        if (!message.TryDecode(packet, out error))
        {
            return false;
        }

        if (!M3uaTypedMessageParser.TryParseKnown(message, out M3uaTypedMessage? typedMessage, out error))
        {
            return false;
        }

        M3uaAspStateTransition? transition = null;
        if (IsAspAcknowledgement(message))
        {
            if (!AspSession.TryApplyAcknowledgement(message, out M3uaAspStateTransition acceptedTransition, out error))
            {
                return false;
            }

            transition = acceptedTransition;
        }

        M3uaPayloadRoute? route = null;
        if (typedMessage!.Kind == M3uaTypedMessageKind.PayloadData)
        {
            if (!TryProcessPayload(typedMessage.As<M3uaPayloadDataMessage>(), out route, out error))
            {
                return false;
            }
        }

        result = new(message, typedMessage, route, transition);
        return true;
    }

    private bool TryProcessPayload(
        M3uaPayloadDataMessage payloadData,
        out M3uaPayloadRoute? route,
        out string? error)
    {
        route = null;
        error = null;

        if (RequireActiveAspForPayload && AspSession.State != M3uaAspState.Active)
        {
            error = $"Payload Data cannot be accepted while ASP is {AspSession.State}";
            return false;
        }

        if (PayloadRoutes.Routes.Count == 0)
        {
            return true;
        }

        return PayloadRoutes.TryResolve(payloadData, out route, out error);
    }

    private static bool IsAspAcknowledgement(M3uaMessage message)
    {
        return message.MessageClass switch
        {
            M3uaMessageClass.Aspsm => message.MessageType is (byte)M3uaAspsmMessageType.AspUpAck
                or (byte)M3uaAspsmMessageType.AspDownAck
                or (byte)M3uaAspsmMessageType.HeartbeatAck,
            M3uaMessageClass.Asptm => message.MessageType is (byte)M3uaAsptmMessageType.AspActiveAck
                or (byte)M3uaAsptmMessageType.AspInactiveAck,
            _ => false
        };
    }
}
