using System.Net;
using System.Net.Sockets;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Options for a native SCTP listener.
/// </summary>
public sealed class NativeSctpListenerOptions
{
    /// <summary>Creates native SCTP listener options.</summary>
    /// <param name="localEndpoint">The local endpoint to bind.</param>
    /// <param name="backlog">The listen backlog.</param>
    /// <param name="outboundStreams">The outbound stream count to report for accepted associations.</param>
    /// <param name="inboundStreams">The inbound stream count to report for accepted associations.</param>
    /// <param name="defaultPayloadProtocolIdentifier">The default Payload Protocol Identifier.</param>
    public NativeSctpListenerOptions(
        SctpEndpoint localEndpoint,
        int backlog = 100,
        ushort outboundStreams = 1,
        ushort inboundStreams = 1,
        uint defaultPayloadProtocolIdentifier = SctpPayloadProtocolIdentifiers.M3ua)
    {
        if (backlog <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(backlog), "Listen backlog must be positive.");
        }

        if (outboundStreams == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outboundStreams), "Outbound stream count must be positive.");
        }

        if (inboundStreams == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inboundStreams), "Inbound stream count must be positive.");
        }

        LocalEndpoint = localEndpoint ?? throw new ArgumentNullException(nameof(localEndpoint));
        Backlog = backlog;
        OutboundStreams = outboundStreams;
        InboundStreams = inboundStreams;
        DefaultPayloadProtocolIdentifier = defaultPayloadProtocolIdentifier;
    }

    /// <summary>The local endpoint to bind.</summary>
    public SctpEndpoint LocalEndpoint { get; }

    /// <summary>The listen backlog.</summary>
    public int Backlog { get; }

    /// <summary>The outbound stream count to report for accepted associations.</summary>
    public ushort OutboundStreams { get; }

    /// <summary>The inbound stream count to report for accepted associations.</summary>
    public ushort InboundStreams { get; }

    /// <summary>The default Payload Protocol Identifier.</summary>
    public uint DefaultPayloadProtocolIdentifier { get; }
}

/// <summary>
/// Native SCTP server-side listener.
/// </summary>
public sealed class NativeSctpListener : IDisposable
{
    private readonly INativeSctpSocketFactory _socketFactory;
    private readonly NativeSctpEndpointResolver _resolver = new();
    private Socket? _listenSocket;
    private NativeSctpTransportOptions _transportOptions = new();

    /// <summary>Creates a native SCTP listener.</summary>
    /// <param name="socketFactory">The socket factory.</param>
    public NativeSctpListener(INativeSctpSocketFactory? socketFactory = null)
    {
        _socketFactory = socketFactory ?? new NativeSctpSocketFactory();
    }

    /// <summary>Starts listening on the configured local endpoint.</summary>
    /// <param name="options">The listener options.</param>
    /// <param name="transportOptions">The optional production transport behavior options for accepted associations.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task StartAsync(
        NativeSctpListenerOptions options,
        NativeSctpTransportOptions? transportOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_listenSocket is not null)
        {
            throw new InvalidOperationException("Native SCTP listener has already started.");
        }

        _transportOptions = transportOptions ?? new NativeSctpTransportOptions();
        IPEndPoint local = await _resolver.ResolveAsync(options.LocalEndpoint, ct).ConfigureAwait(false);
        Socket socket = _socketFactory.CreateSocket();
        try
        {
            if (_transportOptions.RequireKernelMetadata)
            {
                NativeSctpInterop.ConfigureSocket(socket, options.OutboundStreams, options.InboundStreams);
            }

            socket.Bind(local);
            socket.Listen(options.Backlog);
            _listenSocket = socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>Accepts one native SCTP association.</summary>
    /// <param name="options">The listener options used to describe accepted association defaults.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The accepted native SCTP socket adapter.</returns>
    public async Task<NativeSctpSocketAdapter> AcceptAsync(NativeSctpListenerOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        Socket listenSocket = _listenSocket ?? throw new InvalidOperationException("Native SCTP listener has not started.");
        Socket accepted = await listenSocket.AcceptAsync(ct).ConfigureAwait(false);
        if (_transportOptions.RequireKernelMetadata)
        {
            NativeSctpInterop.EnableReceiveMetadata(accepted);
        }

        SctpEndpoint remote = ToSctpEndpoint(accepted.RemoteEndPoint, "remote");
        SctpConnectionOptions connectionOptions = new(
            remote,
            options.LocalEndpoint,
            options.OutboundStreams,
            options.InboundStreams,
            options.DefaultPayloadProtocolIdentifier);

        return new NativeSctpSocketAdapter(accepted, connectionOptions, SctpAssociationState.Established, _transportOptions);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listenSocket?.Dispose();
        _listenSocket = null;
    }

    private static SctpEndpoint ToSctpEndpoint(EndPoint? endpoint, string label)
    {
        if (endpoint is IPEndPoint ip)
        {
            return new SctpEndpoint(ip.Address.ToString(), ip.Port);
        }

        throw new InvalidOperationException($"Accepted SCTP socket did not expose a valid {label} endpoint.");
    }
}
