namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Describes a host and port used by an SCTP association.
/// </summary>
public sealed class SctpEndpoint
{
    /// <summary>Creates an SCTP endpoint.</summary>
    /// <param name="host">The DNS name or IP address.</param>
    /// <param name="port">The SCTP port.</param>
    public SctpEndpoint(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("SCTP endpoint host must not be empty.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "SCTP endpoint port must be between 1 and 65535.");
        }

        Host = host;
        Port = port;
    }

    /// <summary>The DNS name or IP address.</summary>
    public string Host { get; }

    /// <summary>The SCTP port.</summary>
    public int Port { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Host}:{Port}";
    }
}

/// <summary>
/// Connection options for a production SCTP transport.
/// </summary>
public sealed class SctpConnectionOptions
{
    /// <summary>Creates SCTP connection options.</summary>
    /// <param name="remoteEndpoint">The remote SCTP endpoint.</param>
    /// <param name="localEndpoint">The optional local SCTP endpoint.</param>
    /// <param name="outboundStreams">The requested outbound stream count.</param>
    /// <param name="inboundStreams">The requested inbound stream count.</param>
    /// <param name="defaultPayloadProtocolIdentifier">The default Payload Protocol Identifier.</param>
    /// <param name="connectTimeout">The association connect timeout.</param>
    public SctpConnectionOptions(
        SctpEndpoint remoteEndpoint,
        SctpEndpoint? localEndpoint = null,
        ushort outboundStreams = 1,
        ushort inboundStreams = 1,
        uint defaultPayloadProtocolIdentifier = 3,
        TimeSpan? connectTimeout = null)
    {
        if (outboundStreams == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outboundStreams), "Outbound stream count must be positive.");
        }

        if (inboundStreams == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inboundStreams), "Inbound stream count must be positive.");
        }

        RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
        LocalEndpoint = localEndpoint;
        OutboundStreams = outboundStreams;
        InboundStreams = inboundStreams;
        DefaultPayloadProtocolIdentifier = defaultPayloadProtocolIdentifier;
        ConnectTimeout = connectTimeout ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>The remote SCTP endpoint.</summary>
    public SctpEndpoint RemoteEndpoint { get; }

    /// <summary>The optional local SCTP endpoint.</summary>
    public SctpEndpoint? LocalEndpoint { get; }

    /// <summary>The requested outbound stream count.</summary>
    public ushort OutboundStreams { get; }

    /// <summary>The requested inbound stream count.</summary>
    public ushort InboundStreams { get; }

    /// <summary>The default Payload Protocol Identifier.</summary>
    public uint DefaultPayloadProtocolIdentifier { get; }

    /// <summary>The association connect timeout.</summary>
    public TimeSpan ConnectTimeout { get; }
}
