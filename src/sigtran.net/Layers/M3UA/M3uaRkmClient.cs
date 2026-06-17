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
        M3uaInboundProcessingResult result = await _session.ReceiveUntilAsync(
            M3uaTypedMessageKind.RegistrationResponse,
            maxResponseMessages,
            ct).ConfigureAwait(false);
        return result.TypedMessage.As<M3uaRegistrationResponseMessage>();
    }

    /// <summary>
    /// Sends a Registration Request, waits for a Registration Response, and requires every result to be successful.
    /// </summary>
    /// <param name="routingKeys">The Routing Key entries to register.</param>
    /// <param name="maxResponseMessages">The maximum inbound messages to inspect while waiting for Registration Response.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The successful Registration Response message.</returns>
    public async Task<M3uaRegistrationResponseMessage> RegisterAndRequireSuccessAsync(
        ReadOnlyMemory<M3uaRoutingKey> routingKeys,
        int maxResponseMessages = 8,
        CancellationToken ct = default)
    {
        M3uaRegistrationResponseMessage response = await RegisterAsync(routingKeys, maxResponseMessages, ct).ConfigureAwait(false);
        RequireRegistrationSuccess(response);
        return response;
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
        M3uaInboundProcessingResult result = await _session.ReceiveUntilAsync(
            M3uaTypedMessageKind.DeregistrationResponse,
            maxResponseMessages,
            ct).ConfigureAwait(false);
        return result.TypedMessage.As<M3uaDeregistrationResponseMessage>();
    }

    /// <summary>
    /// Sends a Deregistration Request, waits for a Deregistration Response, and requires every result to be successful.
    /// </summary>
    /// <param name="routingContexts">The Routing Context values to deregister.</param>
    /// <param name="maxResponseMessages">The maximum inbound messages to inspect while waiting for Deregistration Response.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The successful Deregistration Response message.</returns>
    public async Task<M3uaDeregistrationResponseMessage> DeregisterAndRequireSuccessAsync(
        ReadOnlyMemory<uint> routingContexts,
        int maxResponseMessages = 8,
        CancellationToken ct = default)
    {
        M3uaDeregistrationResponseMessage response = await DeregisterAsync(routingContexts, maxResponseMessages, ct).ConfigureAwait(false);
        RequireDeregistrationSuccess(response);
        return response;
    }

    private static void ValidateMaxResponseMessages(int maxResponseMessages)
    {
        if (maxResponseMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponseMessages), "Maximum response messages must be positive.");
        }
    }

    private static void RequireRegistrationSuccess(M3uaRegistrationResponseMessage response)
    {
        if (response.AllSuccessful)
        {
            return;
        }

        if (response.Results.Length == 0)
        {
            throw new InvalidOperationException("Registration Response did not contain any Registration Result entries.");
        }

        for (int i = 0; i < response.Results.Length; i++)
        {
            M3uaRegistrationResult result = response.Results[i];
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Registration failed for Local-RK-Identifier {result.LocalRoutingKeyIdentifier}: {result.Status}.");
            }
        }
    }

    private static void RequireDeregistrationSuccess(M3uaDeregistrationResponseMessage response)
    {
        if (response.AllSuccessful)
        {
            return;
        }

        if (response.Results.Length == 0)
        {
            throw new InvalidOperationException("Deregistration Response did not contain any Deregistration Result entries.");
        }

        for (int i = 0; i < response.Results.Length; i++)
        {
            M3uaDeregistrationResult result = response.Results[i];
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Deregistration failed for Routing Context {result.RoutingContext}: {result.Status}.");
            }
        }
    }
}
