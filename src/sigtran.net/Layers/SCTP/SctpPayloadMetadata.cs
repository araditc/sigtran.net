namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Describes SCTP metadata associated with a single user message.
/// </summary>
public readonly struct SctpPayloadMetadata
{
    /// <summary>Creates SCTP payload metadata.</summary>
    /// <param name="streamId">The SCTP stream identifier.</param>
    /// <param name="payloadProtocolIdentifier">The SCTP Payload Protocol Identifier.</param>
    /// <param name="unordered">Whether the SCTP user message should be sent unordered.</param>
    public SctpPayloadMetadata(ushort streamId, uint payloadProtocolIdentifier, bool unordered = false)
    {
        StreamId = streamId;
        PayloadProtocolIdentifier = payloadProtocolIdentifier;
        Unordered = unordered;
    }

    /// <summary>The SCTP stream identifier.</summary>
    public ushort StreamId { get; }

    /// <summary>The SCTP Payload Protocol Identifier.</summary>
    public uint PayloadProtocolIdentifier { get; }

    /// <summary>Whether the SCTP user message should be sent unordered.</summary>
    public bool Unordered { get; }
}

/// <summary>
/// Result of receiving one SCTP user message with metadata.
/// </summary>
public readonly struct SctpReceiveResult
{
    /// <summary>Creates an SCTP receive result.</summary>
    /// <param name="bytesReceived">The number of payload bytes received.</param>
    /// <param name="metadata">The SCTP metadata associated with the payload.</param>
    public SctpReceiveResult(int bytesReceived, SctpPayloadMetadata metadata)
    {
        BytesReceived = bytesReceived;
        Metadata = metadata;
    }

    /// <summary>The number of payload bytes received.</summary>
    public int BytesReceived { get; }

    /// <summary>The SCTP metadata associated with the payload.</summary>
    public SctpPayloadMetadata Metadata { get; }
}

/// <summary>
/// Optional SCTP socket capability for transports that can expose stream and PPID metadata.
/// </summary>
public interface ISctpMetadataSocket
{
    /// <summary>
    /// Sends one SCTP user message with explicit stream and PPID metadata.
    /// </summary>
    /// <param name="data">The user message payload.</param>
    /// <param name="metadata">The SCTP metadata to apply.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the payload has been enqueued for transmission.</returns>
    Task SendAsync(ReadOnlyMemory<byte> data, SctpPayloadMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// Receives one SCTP user message and exposes the associated stream and PPID metadata.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The receive result containing byte count and SCTP metadata.</returns>
    Task<SctpReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default);
}
