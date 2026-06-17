namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Options used when starting an ASP over an M3UA transport session.
/// </summary>
public sealed class M3uaAspStartupOptions
{
    /// <summary>Creates ASP startup options.</summary>
    /// <param name="aspIdentifier">The optional ASP Identifier sent in ASP Up.</param>
    /// <param name="trafficModeType">The optional Traffic Mode Type sent in ASP Active.</param>
    /// <param name="aspUpInfoString">The optional Info String sent in ASP Up.</param>
    /// <param name="aspActiveInfoString">The optional Info String sent in ASP Active.</param>
    /// <param name="maxHandshakeMessages">The maximum inbound messages to inspect while waiting for each acknowledgement.</param>
    public M3uaAspStartupOptions(
        uint? aspIdentifier = null,
        M3uaTrafficModeType? trafficModeType = null,
        ReadOnlyMemory<byte> aspUpInfoString = default,
        ReadOnlyMemory<byte> aspActiveInfoString = default,
        int maxHandshakeMessages = 8)
    {
        if (maxHandshakeMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHandshakeMessages), "Maximum handshake messages must be positive.");
        }

        AspIdentifier = aspIdentifier;
        TrafficModeType = trafficModeType;
        AspUpInfoString = aspUpInfoString;
        AspActiveInfoString = aspActiveInfoString;
        MaxHandshakeMessages = maxHandshakeMessages;
    }

    /// <summary>The optional ASP Identifier sent in ASP Up.</summary>
    public uint? AspIdentifier { get; }

    /// <summary>The optional Traffic Mode Type sent in ASP Active.</summary>
    public M3uaTrafficModeType? TrafficModeType { get; }

    /// <summary>The optional Info String sent in ASP Up.</summary>
    public ReadOnlyMemory<byte> AspUpInfoString { get; }

    /// <summary>The optional Info String sent in ASP Active.</summary>
    public ReadOnlyMemory<byte> AspActiveInfoString { get; }

    /// <summary>The maximum inbound messages to inspect while waiting for each acknowledgement.</summary>
    public int MaxHandshakeMessages { get; }
}

/// <summary>
/// Represents a completed ASP startup handshake.
/// </summary>
public sealed class M3uaAspStartupResult
{
    /// <summary>Creates an ASP startup result.</summary>
    /// <param name="aspUpAcknowledgement">The inbound result for ASP Up Ack.</param>
    /// <param name="aspActiveAcknowledgement">The inbound result for ASP Active Ack.</param>
    public M3uaAspStartupResult(
        M3uaInboundProcessingResult aspUpAcknowledgement,
        M3uaInboundProcessingResult aspActiveAcknowledgement)
    {
        AspUpAcknowledgement = aspUpAcknowledgement;
        AspActiveAcknowledgement = aspActiveAcknowledgement;
    }

    /// <summary>The inbound result for ASP Up Ack.</summary>
    public M3uaInboundProcessingResult AspUpAcknowledgement { get; }

    /// <summary>The inbound result for ASP Active Ack.</summary>
    public M3uaInboundProcessingResult AspActiveAcknowledgement { get; }
}

/// <summary>
/// Runs common ASP lifecycle handshakes over an M3UA transport session.
/// </summary>
public sealed class M3uaAspClient
{
    private readonly M3uaTransportSession _session;

    /// <summary>Creates an ASP client over an existing transport session.</summary>
    /// <param name="session">The transport session used for sending and receiving M3UA messages.</param>
    public M3uaAspClient(M3uaTransportSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Sends ASP Up, waits for ASP Up Ack, sends ASP Active, and waits for ASP Active Ack.
    /// </summary>
    /// <param name="options">The optional startup settings.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The accepted acknowledgement results.</returns>
    public async Task<M3uaAspStartupResult> StartAsync(
        M3uaAspStartupOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new M3uaAspStartupOptions();

        await _session.SendAspUpAsync(options.AspIdentifier, options.AspUpInfoString, ct).ConfigureAwait(false);
        M3uaInboundProcessingResult upAck = await ReceiveUntilTransitionAsync(
            M3uaAspEvent.AspUpAcknowledged,
            options.MaxHandshakeMessages,
            ct).ConfigureAwait(false);

        await _session.SendAspActiveAsync(options.TrafficModeType, options.AspActiveInfoString, ct).ConfigureAwait(false);
        M3uaInboundProcessingResult activeAck = await ReceiveUntilTransitionAsync(
            M3uaAspEvent.AspActiveAcknowledged,
            options.MaxHandshakeMessages,
            ct).ConfigureAwait(false);

        return new(upAck, activeAck);
    }

    /// <summary>
    /// Sends a Heartbeat message and waits for a Heartbeat Ack.
    /// </summary>
    /// <param name="heartbeatData">The optional Heartbeat Data value to echo through the peer.</param>
    /// <param name="maxHandshakeMessages">The maximum inbound messages to inspect while waiting for Heartbeat Ack.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound result that accepted the Heartbeat Ack.</returns>
    public async Task<M3uaInboundProcessingResult> SendHeartbeatAsync(
        ReadOnlyMemory<byte> heartbeatData = default,
        int maxHandshakeMessages = 8,
        CancellationToken ct = default)
    {
        if (maxHandshakeMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHandshakeMessages), "Maximum handshake messages must be positive.");
        }

        await _session.SendHeartbeatAsync(heartbeatData, ct).ConfigureAwait(false);
        return await ReceiveUntilTransitionAsync(
            M3uaAspEvent.HeartbeatAcknowledged,
            maxHandshakeMessages,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends ASP Inactive and waits for ASP Inactive Ack.
    /// </summary>
    /// <param name="infoString">The optional Info String value sent in ASP Inactive.</param>
    /// <param name="maxHandshakeMessages">The maximum inbound messages to inspect while waiting for ASP Inactive Ack.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound result that accepted the ASP Inactive Ack.</returns>
    public async Task<M3uaInboundProcessingResult> DeactivateAsync(
        ReadOnlyMemory<byte> infoString = default,
        int maxHandshakeMessages = 8,
        CancellationToken ct = default)
    {
        if (maxHandshakeMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHandshakeMessages), "Maximum handshake messages must be positive.");
        }

        await _session.SendAspInactiveAsync(infoString, ct).ConfigureAwait(false);
        return await ReceiveUntilTransitionAsync(
            M3uaAspEvent.AspInactiveAcknowledged,
            maxHandshakeMessages,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends ASP Down and waits for ASP Down Ack.
    /// </summary>
    /// <param name="infoString">The optional Info String value sent in ASP Down.</param>
    /// <param name="maxHandshakeMessages">The maximum inbound messages to inspect while waiting for ASP Down Ack.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound result that accepted the ASP Down Ack.</returns>
    public async Task<M3uaInboundProcessingResult> StopAsync(
        ReadOnlyMemory<byte> infoString = default,
        int maxHandshakeMessages = 8,
        CancellationToken ct = default)
    {
        if (maxHandshakeMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHandshakeMessages), "Maximum handshake messages must be positive.");
        }

        await _session.SendAspDownAsync(infoString, ct).ConfigureAwait(false);
        return await ReceiveUntilTransitionAsync(
            M3uaAspEvent.AspDownAcknowledged,
            maxHandshakeMessages,
            ct).ConfigureAwait(false);
    }

    private async Task<M3uaInboundProcessingResult> ReceiveUntilTransitionAsync(
        M3uaAspEvent expectedEvent,
        int maxMessages,
        CancellationToken ct)
    {
        for (int i = 0; i < maxMessages; i++)
        {
            M3uaInboundProcessingResult? result = await _session.ReceiveAsync(ct).ConfigureAwait(false);
            if (result is null)
            {
                throw new InvalidOperationException($"Transport closed before {expectedEvent} was received.");
            }

            if (result.StateTransition?.Event == expectedEvent)
            {
                return result;
            }
        }

        throw new InvalidOperationException($"Did not receive {expectedEvent} within {maxMessages} inbound messages.");
    }
}
