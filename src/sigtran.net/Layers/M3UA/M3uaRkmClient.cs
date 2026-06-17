namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Runs Routing Key Management request/response handshakes over an M3UA transport session.
/// </summary>
public sealed class M3uaRkmClient
{
    private readonly M3uaTransportSession _session;

    /// <summary>Creates an RKM client over an existing transport session.</summary>
    /// <param name="session">The transport session used for sending and receiving M3UA messages.</param>
    public M3uaRkmClient(M3uaTransportSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Sends a Registration Request and waits for a Registration Response.
    /// </summary>
    /// <param name="routingKeys">The Routing Key entries to register.</param>
    /// <param name="maxResponseMessages">The maximum inbound messages to inspect while waiting for Registration Response.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The accepted Registration Response message.</returns>
    public async Task<M3uaRegistrationResponseMessage> RegisterAsync(
        ReadOnlyMemory<M3uaRoutingKey> routingKeys,
        int maxResponseMessages = 8,
        CancellationToken ct = default)
    {
        ValidateMaxResponseMessages(maxResponseMessages);

        await _session.SendRegistrationRequestAsync(routingKeys, ct).ConfigureAwait(false);
        M3uaInboundProcessingResult result = await ReceiveUntilKindAsync(
            M3uaTypedMessageKind.RegistrationResponse,
            maxResponseMessages,
            ct).ConfigureAwait(false);
        return result.TypedMessage.As<M3uaRegistrationResponseMessage>();
    }

    /// <summary>
    /// Sends a Deregistration Request and waits for a Deregistration Response.
    /// </summary>
    /// <param name="routingContexts">The Routing Context values to deregister.</param>
    /// <param name="maxResponseMessages">The maximum inbound messages to inspect while waiting for Deregistration Response.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The accepted Deregistration Response message.</returns>
    public async Task<M3uaDeregistrationResponseMessage> DeregisterAsync(
        ReadOnlyMemory<uint> routingContexts,
        int maxResponseMessages = 8,
        CancellationToken ct = default)
    {
        ValidateMaxResponseMessages(maxResponseMessages);

        await _session.SendDeregistrationRequestAsync(routingContexts, ct).ConfigureAwait(false);
        M3uaInboundProcessingResult result = await ReceiveUntilKindAsync(
            M3uaTypedMessageKind.DeregistrationResponse,
            maxResponseMessages,
            ct).ConfigureAwait(false);
        return result.TypedMessage.As<M3uaDeregistrationResponseMessage>();
    }

    private async Task<M3uaInboundProcessingResult> ReceiveUntilKindAsync(
        M3uaTypedMessageKind expectedKind,
        int maxMessages,
        CancellationToken ct)
    {
        for (int i = 0; i < maxMessages; i++)
        {
            M3uaInboundProcessingResult? result = await _session.ReceiveAsync(ct).ConfigureAwait(false);
            if (result is null)
            {
                throw new InvalidOperationException($"Transport closed before {expectedKind} was received.");
            }

            if (result.TypedMessage.Kind == expectedKind)
            {
                return result;
            }
        }

        throw new InvalidOperationException($"Did not receive {expectedKind} within {maxMessages} inbound messages.");
    }

    private static void ValidateMaxResponseMessages(int maxResponseMessages)
    {
        if (maxResponseMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponseMessages), "Maximum response messages must be positive.");
        }
    }
}
