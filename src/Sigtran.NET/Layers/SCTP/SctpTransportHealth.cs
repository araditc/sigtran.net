namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Snapshot of SCTP transport health values.
/// </summary>
public readonly struct SctpTransportHealth
{
    /// <summary>Creates an SCTP transport health snapshot.</summary>
    /// <param name="associationState">The current association state.</param>
    /// <param name="remoteEndpoint">The remote endpoint.</param>
    /// <param name="localEndpoint">The optional local endpoint.</param>
    /// <param name="outboundStreams">The outbound stream count.</param>
    /// <param name="inboundStreams">The inbound stream count.</param>
    /// <param name="defaultPayloadProtocolIdentifier">The default PPID.</param>
    /// <param name="sentMessages">The sent user-message count.</param>
    /// <param name="receivedMessages">The received user-message count.</param>
    public SctpTransportHealth(
        SctpAssociationState associationState,
        SctpEndpoint remoteEndpoint,
        SctpEndpoint? localEndpoint,
        ushort outboundStreams,
        ushort inboundStreams,
        uint defaultPayloadProtocolIdentifier,
        long sentMessages,
        long receivedMessages)
    {
        AssociationState = associationState;
        RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
        LocalEndpoint = localEndpoint;
        OutboundStreams = outboundStreams;
        InboundStreams = inboundStreams;
        DefaultPayloadProtocolIdentifier = defaultPayloadProtocolIdentifier;
        SentMessages = sentMessages;
        ReceivedMessages = receivedMessages;
    }

    /// <summary>The current association state.</summary>
    public SctpAssociationState AssociationState { get; }

    /// <summary>The remote endpoint.</summary>
    public SctpEndpoint RemoteEndpoint { get; }

    /// <summary>The optional local endpoint.</summary>
    public SctpEndpoint? LocalEndpoint { get; }

    /// <summary>The outbound stream count.</summary>
    public ushort OutboundStreams { get; }

    /// <summary>The inbound stream count.</summary>
    public ushort InboundStreams { get; }

    /// <summary>The default PPID.</summary>
    public uint DefaultPayloadProtocolIdentifier { get; }

    /// <summary>The sent user-message count.</summary>
    public long SentMessages { get; }

    /// <summary>The received user-message count.</summary>
    public long ReceivedMessages { get; }

    /// <summary>Whether the association is currently established.</summary>
    public bool IsEstablished => AssociationState == SctpAssociationState.Established;
}
