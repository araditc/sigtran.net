namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes one outbound SCTP user message with validated stream and PPID metadata.
/// </summary>
public sealed class SctpOutboundMessage
{
    /// <summary>Creates an outbound SCTP user message.</summary>
    /// <param name="payload">The user message payload.</param>
    /// <param name="metadata">The SCTP metadata for the payload.</param>
    public SctpOutboundMessage(ReadOnlyMemory<byte> payload, SctpPayloadMetadata metadata)
    {
        if (payload.IsEmpty)
        {
            throw new ArgumentException("SCTP outbound payload must not be empty.", nameof(payload));
        }

        Payload = payload;
        Metadata = metadata;
    }

    /// <summary>The user message payload.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>The SCTP metadata for the payload.</summary>
    public SctpPayloadMetadata Metadata { get; }

    /// <summary>Whether the PPID is recognized by the SDK as a SIGTRAN PPID.</summary>
    public bool HasKnownSigtranPpid => SctpPayloadProtocolIdentifiers.IsKnown(Metadata.PayloadProtocolIdentifier);

    /// <summary>Formats a compact outbound message summary.</summary>
    /// <returns>The outbound message summary.</returns>
    public string Describe()
    {
        return $"bytes={Payload.Length} stream={Metadata.StreamId} ppid={Metadata.PayloadProtocolIdentifier} unordered={Metadata.Unordered} knownPpid={HasKnownSigtranPpid}";
    }
}

/// <summary>
/// Builds outbound SCTP user messages from connection options and stream selection policy.
/// </summary>
public static class SctpOutboundMessageBuilder
{
    /// <summary>Creates an outbound SCTP user message when stream and PPID metadata are valid.</summary>
    /// <param name="payload">The user message payload.</param>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="streamPolicy">The stream selection policy.</param>
    /// <param name="sequence">The caller-provided sequence value used by round-robin stream selection.</param>
    /// <param name="message">The outbound message when creation succeeds.</param>
    /// <param name="error">The validation error when creation fails.</param>
    /// <param name="payloadProtocolIdentifier">The optional PPID override.</param>
    /// <param name="unordered">Whether to send the user message unordered.</param>
    /// <returns><c>true</c> when the outbound message is valid; otherwise <c>false</c>.</returns>
    public static bool TryCreate(
        ReadOnlyMemory<byte> payload,
        SctpConnectionOptions options,
        SctpStreamSelectionPolicy streamPolicy,
        uint sequence,
        out SctpOutboundMessage? message,
        out string? error,
        uint? payloadProtocolIdentifier = null,
        bool unordered = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(streamPolicy);

        message = null;
        if (payload.IsEmpty)
        {
            error = "SCTP outbound payload must not be empty.";
            return false;
        }

        uint ppid = payloadProtocolIdentifier ?? options.DefaultPayloadProtocolIdentifier;
        if (!SctpPayloadProtocolIdentifiers.TryRequireKnown(ppid, out error))
        {
            return false;
        }

        ushort streamId = streamPolicy.SelectStream(sequence);
        if (streamId >= options.OutboundStreams)
        {
            error = $"Selected SCTP stream {streamId} is outside the negotiated outbound stream count {options.OutboundStreams}.";
            return false;
        }

        message = new(payload, new SctpPayloadMetadata(streamId, ppid, unordered));
        error = null;
        return true;
    }
}
