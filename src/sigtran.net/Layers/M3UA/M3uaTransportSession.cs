using System.Buffers;

using sigtran.net.Core.Interfaces;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Snapshot of transport-session packet counters.
/// </summary>
public readonly struct M3uaTransportSessionCounters
{
    /// <summary>Creates a counter snapshot.</summary>
    /// <param name="sentPdus">The number of successfully sent M3UA PDUs.</param>
    /// <param name="receivedPdus">The number of successfully received and processed M3UA PDUs.</param>
    /// <param name="sendFailures">The number of outbound build or send failures.</param>
    /// <param name="receiveFailures">The number of inbound receive or processing failures.</param>
    public M3uaTransportSessionCounters(long sentPdus, long receivedPdus, long sendFailures, long receiveFailures)
    {
        SentPdus = sentPdus;
        ReceivedPdus = receivedPdus;
        SendFailures = sendFailures;
        ReceiveFailures = receiveFailures;
    }

    /// <summary>The number of successfully sent M3UA PDUs.</summary>
    public long SentPdus { get; }

    /// <summary>The number of successfully received and processed M3UA PDUs.</summary>
    public long ReceivedPdus { get; }

    /// <summary>The number of outbound build or send failures.</summary>
    public long SendFailures { get; }

    /// <summary>The number of inbound receive or processing failures.</summary>
    public long ReceiveFailures { get; }
}

/// <summary>
/// Connects M3UA processors to an SCTP-like transport.
/// </summary>
public sealed class M3uaTransportSession : IAsyncDisposable, IDisposable
{
    private readonly ISctpSocket _socket;
    private readonly bool _leaveOpen;
    private long _sentPdus;
    private long _receivedPdus;
    private long _sendFailures;
    private long _receiveFailures;
    private bool _disposed;

    /// <summary>Creates a transport-backed M3UA session.</summary>
    /// <param name="socket">The SCTP-like socket that reads and writes complete M3UA PDUs.</param>
    /// <param name="inboundProcessor">The inbound processor used for received packets.</param>
    /// <param name="outboundProcessor">The outbound processor used for sent packets.</param>
    /// <param name="maxPduSize">The maximum inbound or outbound M3UA PDU size in bytes.</param>
    /// <param name="leaveOpen">Whether disposing this session should leave the socket open.</param>
    public M3uaTransportSession(
        ISctpSocket socket,
        M3uaInboundProcessor? inboundProcessor = null,
        M3uaOutboundProcessor? outboundProcessor = null,
        int maxPduSize = ushort.MaxValue,
        bool leaveOpen = false)
    {
        if (maxPduSize < M3uaProtocol.HeaderLength)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPduSize), "Maximum PDU size must fit an M3UA header.");
        }

        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        InboundProcessor = inboundProcessor ?? new M3uaInboundProcessor();
        OutboundProcessor = outboundProcessor ?? new M3uaOutboundProcessor(InboundProcessor.AspSession);
        MaxPduSize = maxPduSize;
        _leaveOpen = leaveOpen;
    }

    /// <summary>The inbound processor used for received packets.</summary>
    public M3uaInboundProcessor InboundProcessor { get; }

    /// <summary>The outbound processor used for sent packets.</summary>
    public M3uaOutboundProcessor OutboundProcessor { get; }

    /// <summary>The maximum inbound or outbound M3UA PDU size in bytes.</summary>
    public int MaxPduSize { get; }

    /// <summary>A snapshot of packet counters for this transport session.</summary>
    public M3uaTransportSessionCounters Counters => new(
        Interlocked.Read(ref _sentPdus),
        Interlocked.Read(ref _receivedPdus),
        Interlocked.Read(ref _sendFailures),
        Interlocked.Read(ref _receiveFailures));

    /// <summary>
    /// Resets all session-local packet counters to zero.
    /// </summary>
    public void ResetCounters()
    {
        Interlocked.Exchange(ref _sentPdus, 0);
        Interlocked.Exchange(ref _receivedPdus, 0);
        Interlocked.Exchange(ref _sendFailures, 0);
        Interlocked.Exchange(ref _receiveFailures, 0);
    }

    /// <summary>
    /// Notifies the shared ASP session that the underlying transport association was lost.
    /// </summary>
    /// <param name="transition">The accepted ASP state transition.</param>
    /// <param name="error">An error message when the transition is rejected.</param>
    /// <returns>True if the ASP session accepted the transport-loss transition; otherwise false.</returns>
    public bool TryNotifyTransportLost(out M3uaAspStateTransition transition, out string? error)
    {
        ThrowIfDisposed();
        return InboundProcessor.AspSession.NotifyTransportLost(out transition, out error);
    }

    /// <summary>
    /// Receives and processes one complete M3UA PDU.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound processing result, or null when the remote peer closes cleanly.</returns>
    public async Task<M3uaInboundProcessingResult?> ReceiveAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] rented = ArrayPool<byte>.Shared.Rent(MaxPduSize);
        bool failureCounted = false;
        try
        {
            int received = await _socket.ReceiveAsync(rented.AsMemory(0, MaxPduSize), ct).ConfigureAwait(false);
            if (received == 0)
            {
                return null;
            }

            if (!InboundProcessor.TryProcess(rented.AsSpan(0, received), out M3uaInboundProcessingResult? result, out string? error))
            {
                Interlocked.Increment(ref _receiveFailures);
                failureCounted = true;
                throw new InvalidOperationException(error);
            }

            Interlocked.Increment(ref _receivedPdus);
            return result;
        }
        catch
        {
            if (!failureCounted)
            {
                Interlocked.Increment(ref _receiveFailures);
            }

            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Receives and processes one complete M3UA PDU, automatically sending Heartbeat Ack for inbound Heartbeat messages.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound processing result, or null when the remote peer closes cleanly.</returns>
    public async Task<M3uaInboundProcessingResult?> ReceiveAndAcknowledgeHeartbeatAsync(CancellationToken ct = default)
    {
        M3uaInboundProcessingResult? result = await ReceiveAsync(ct).ConfigureAwait(false);
        if (result?.TypedMessage.Kind != M3uaTypedMessageKind.AspStateMaintenance)
        {
            return result;
        }

        M3uaAspStateMaintenanceMessage aspsm = result.TypedMessage.As<M3uaAspStateMaintenanceMessage>();
        if (aspsm.MessageType == M3uaAspsmMessageType.Heartbeat)
        {
            await SendHeartbeatAckAsync(aspsm.HeartbeatData, ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Receives and processes inbound M3UA PDUs until a specific typed message kind is accepted.
    /// </summary>
    /// <param name="expectedKind">The typed message kind to wait for.</param>
    /// <param name="maxMessages">The maximum inbound messages to inspect.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound processing result with the expected typed message kind.</returns>
    public async Task<M3uaInboundProcessingResult> ReceiveUntilAsync(
        M3uaTypedMessageKind expectedKind,
        int maxMessages = 8,
        CancellationToken ct = default)
    {
        if (maxMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages), "Maximum messages must be positive.");
        }

        for (int i = 0; i < maxMessages; i++)
        {
            M3uaInboundProcessingResult? result = await ReceiveAsync(ct).ConfigureAwait(false);
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

    /// <summary>
    /// Receives and processes inbound M3UA PDUs until a specific ASP state event is accepted.
    /// </summary>
    /// <param name="expectedEvent">The ASP state event to wait for.</param>
    /// <param name="maxMessages">The maximum inbound messages to inspect.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound processing result with the expected ASP state transition.</returns>
    public async Task<M3uaInboundProcessingResult> ReceiveUntilTransitionAsync(
        M3uaAspEvent expectedEvent,
        int maxMessages = 8,
        CancellationToken ct = default)
    {
        if (maxMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages), "Maximum messages must be positive.");
        }

        for (int i = 0; i < maxMessages; i++)
        {
            M3uaInboundProcessingResult? result = await ReceiveAsync(ct).ConfigureAwait(false);
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

    /// <summary>
    /// Builds and sends an ASP Up message.
    /// </summary>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendAspUpAsync(uint? aspIdentifier, ReadOnlyMemory<byte> infoString, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildAspUp(buffer, aspIdentifier, infoString.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends an ASP Down message.
    /// </summary>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendAspDownAsync(ReadOnlyMemory<byte> infoString, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildAspDown(buffer, infoString.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends an ASP Active message.
    /// </summary>
    /// <param name="trafficModeType">The optional Traffic Mode Type value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendAspActiveAsync(M3uaTrafficModeType? trafficModeType, ReadOnlyMemory<byte> infoString, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildAspActive(buffer, trafficModeType, infoString.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends an ASP Inactive message.
    /// </summary>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendAspInactiveAsync(ReadOnlyMemory<byte> infoString, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildAspInactive(buffer, infoString.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Heartbeat message.
    /// </summary>
    /// <param name="heartbeatData">The optional Heartbeat Data value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendHeartbeatAsync(ReadOnlyMemory<byte> heartbeatData, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildHeartbeat(buffer, heartbeatData.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Heartbeat acknowledgement message.
    /// </summary>
    /// <param name="heartbeatData">The Heartbeat Data value copied from the received Heartbeat message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendHeartbeatAckAsync(ReadOnlyMemory<byte> heartbeatData, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildHeartbeatAck(buffer, heartbeatData.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends an RKM Registration Request message.
    /// </summary>
    /// <param name="routingKeys">The Routing Key entries to register.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendRegistrationRequestAsync(ReadOnlyMemory<M3uaRoutingKey> routingKeys, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildRegistrationRequest(buffer, routingKeys.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends an RKM Deregistration Request message.
    /// </summary>
    /// <param name="routingContexts">The Routing Context values to deregister.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDeregistrationRequestAsync(ReadOnlyMemory<uint> routingContexts, CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDeregistrationRequest(buffer, routingContexts.Span, out written, out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Management Error message.
    /// </summary>
    /// <param name="errorCode">The Error Code value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="diagnosticInformation">The optional Diagnostic Information value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendErrorAsync(
        M3uaErrorCode errorCode,
        ReadOnlyMemory<uint> routingContexts,
        uint? networkAppearance,
        ReadOnlyMemory<byte> diagnosticInformation,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildError(
                buffer,
                errorCode,
                routingContexts.Span,
                networkAppearance,
                diagnosticInformation.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Management Notify message.
    /// </summary>
    /// <param name="statusType">The Status Type value.</param>
    /// <param name="statusInformation">The Status Information value.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendNotifyAsync(
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        uint? aspIdentifier,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildNotify(
                buffer,
                statusType,
                statusInformation,
                aspIdentifier,
                routingContexts.Span,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Destination Unavailable SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDestinationUnavailableAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDestinationUnavailable(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCodes.Span,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Destination Available SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDestinationAvailableAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDestinationAvailable(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCodes.Span,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Destination State Audit SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDestinationStateAuditAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDestinationStateAudit(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCodes.Span,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Destination Restricted SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDestinationRestrictedAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDestinationRestricted(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCodes.Span,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Signalling Congestion SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="concernedDestination">The optional concerned destination point code.</param>
    /// <param name="congestionLevel">The optional Congestion Indications level.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendSignallingCongestionAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<M3uaAffectedPointCode> affectedPointCodes,
        M3uaAffectedPointCode? concernedDestination,
        uint? congestionLevel,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildSignallingCongestion(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCodes.Span,
                concernedDestination,
                congestionLevel,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Destination User Part Unavailable SSNM message.
    /// </summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCode">The affected point-code entry. The mask must be zero.</param>
    /// <param name="cause">The unavailability cause.</param>
    /// <param name="userIdentity">The unavailable MTP3-user identity.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendDestinationUserPartUnavailableAsync(
        uint? networkAppearance,
        ReadOnlyMemory<uint> routingContexts,
        M3uaAffectedPointCode affectedPointCode,
        M3uaUserPartUnavailableCause cause,
        M3uaMtp3UserIdentity userIdentity,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildDestinationUserPartUnavailable(
                buffer,
                networkAppearance,
                routingContexts.Span,
                affectedPointCode,
                cause,
                userIdentity,
                infoString.Span,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Payload Data message.
    /// </summary>
    /// <param name="userPayload">The MTP3-user payload.</param>
    /// <param name="originatingPointCode">The Originating Point Code value.</param>
    /// <param name="destinationPointCode">The Destination Point Code value.</param>
    /// <param name="serviceIndicator">The Service Indicator value.</param>
    /// <param name="networkIndicator">The Network Indicator value.</param>
    /// <param name="messagePriority">The Message Priority value.</param>
    /// <param name="signallingLinkSelection">The Signalling Link Selection value.</param>
    /// <param name="networkAppearance">The optional explicit Network Appearance value.</param>
    /// <param name="routingContext">The optional explicit Routing Context value.</param>
    /// <param name="correlationId">The optional Correlation Id value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendPayloadDataAsync(
        ReadOnlyMemory<byte> userPayload,
        uint originatingPointCode,
        uint destinationPointCode,
        byte serviceIndicator,
        byte networkIndicator,
        byte messagePriority,
        byte signallingLinkSelection,
        uint? networkAppearance = null,
        uint? routingContext = null,
        uint? correlationId = null,
        CancellationToken ct = default)
    {
        return BuildAndSendAsync(
            (Span<byte> buffer, out int written, out string? error) => OutboundProcessor.TryBuildPayloadData(
                buffer,
                userPayload.Span,
                originatingPointCode,
                destinationPointCode,
                serviceIndicator,
                networkIndicator,
                messagePriority,
                signallingLinkSelection,
                networkAppearance,
                routingContext,
                correlationId,
                out written,
                out error),
            ct);
    }

    /// <summary>
    /// Builds and sends a Payload Data message from a typed Payload Data model.
    /// </summary>
    /// <param name="message">The typed Payload Data message to send.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the packet has been sent.</returns>
    public Task SendPayloadDataAsync(M3uaPayloadDataMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendPayloadDataAsync(
            message.UserPayload.ToArray(),
            message.OriginatingPointCode,
            message.DestinationPointCode,
            message.ServiceIndicator,
            message.NetworkIndicator,
            message.MessagePriority,
            message.SignallingLinkSelection,
            message.NetworkAppearance,
            message.RoutingContext,
            message.CorrelationId,
            ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_leaveOpen)
        {
            _socket.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task BuildAndSendAsync(PacketBuilder builder, CancellationToken ct)
    {
        ThrowIfDisposed();
        byte[] rented = ArrayPool<byte>.Shared.Rent(MaxPduSize);
        bool failureCounted = false;
        try
        {
            if (!builder(rented.AsSpan(0, MaxPduSize), out int written, out string? error))
            {
                Interlocked.Increment(ref _sendFailures);
                failureCounted = true;
                throw new InvalidOperationException(error);
            }

            await _socket.SendAsync(rented.AsMemory(0, written), ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sentPdus);
        }
        catch
        {
            if (!failureCounted)
            {
                Interlocked.Increment(ref _sendFailures);
            }

            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(M3uaTransportSession));
        }
    }

    private delegate bool PacketBuilder(Span<byte> buffer, out int written, out string? error);
}
