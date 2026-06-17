using System.Buffers;

using sigtran.net.Core.Interfaces;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Connects M3UA processors to an SCTP-like transport.
/// </summary>
public sealed class M3uaTransportSession : IAsyncDisposable, IDisposable
{
    private readonly ISctpSocket _socket;
    private readonly bool _leaveOpen;
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

    /// <summary>
    /// Receives and processes one complete M3UA PDU.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound processing result, or null when the remote peer closes cleanly.</returns>
    public async Task<M3uaInboundProcessingResult?> ReceiveAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] rented = ArrayPool<byte>.Shared.Rent(MaxPduSize);
        try
        {
            int received = await _socket.ReceiveAsync(rented.AsMemory(0, MaxPduSize), ct).ConfigureAwait(false);
            if (received == 0)
            {
                return null;
            }

            if (!InboundProcessor.TryProcess(rented.AsSpan(0, received), out M3uaInboundProcessingResult? result, out string? error))
            {
                throw new InvalidOperationException(error);
            }

            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
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
        try
        {
            if (!builder(rented.AsSpan(0, MaxPduSize), out int written, out string? error))
            {
                throw new InvalidOperationException(error);
            }

            await _socket.SendAsync(rented.AsMemory(0, written), ct).ConfigureAwait(false);
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
