using System.Net.Sockets;
using System.Runtime.InteropServices;

using Sigtran.NET.Core.Interfaces;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Provides a simple TCP-backed implementation of the <see cref="ISctpSocket"/>
/// interface.  It frames messages using a four-byte big-endian length
/// prefix and reads exactly one message per call.  This adapter is not
/// suitable for production use but allows development and testing on
/// systems where SCTP is unavailable.
/// </summary>
public sealed class TcpSctpAdapter : ISctpSocket, ISctpMetadataSocket
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private long _sentMessages;
    private long _receivedMessages;
    private bool _disposed;

    /// <summary>
    /// Constructs a new adapter around an existing <see cref="TcpClient"/>.
    /// The client must already be connected and the <see cref="NetworkStream"/>
    /// must be available.
    /// </summary>
    /// <param name="client">The underlying TCP client.</param>
    public TcpSctpAdapter(TcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = _client.GetStream();
    }

    /// <summary>The metadata attached to packets sent through the development TCP adapter.</summary>
    public SctpPayloadMetadata DefaultMetadata { get; } = new(streamId: 0, payloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua);

    /// <summary>The current development adapter association state.</summary>
    public SctpAssociationState AssociationState => _disposed ? SctpAssociationState.Closed : SctpAssociationState.Established;

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        await SendAsync(data, DefaultMetadata, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> data, SctpPayloadMetadata metadata, CancellationToken ct = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TcpSctpAdapter));
        }

        // Prepare a four-byte big-endian length prefix. We intentionally avoid
        // stackalloc/Span here because byref-like types cannot be used as locals
        // in async methods. See CS4009. Instead we allocate a small array and
        // manually encode the integer in big-endian order.
        byte[] prefix = new byte[4];
        int len = data.Length;
        // Write length in network byte order (big-endian)
        prefix[0] = (byte)((len >> 24) & 0xFF);
        prefix[1] = (byte)((len >> 16) & 0xFF);
        prefix[2] = (byte)((len >> 8) & 0xFF);
        prefix[3] = (byte)(len & 0xFF);

        // Write prefix then payload. Use ConfigureAwait(false) to avoid
        // capturing context; we intentionally await sequentially to preserve order.
        await _stream.WriteAsync(prefix, ct).ConfigureAwait(false);
        await _stream.WriteAsync(data, ct).ConfigureAwait(false);
        Interlocked.Increment(ref _sentMessages);
    }

    /// <inheritdoc />
    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        SctpReceiveResult result = await ReceiveWithMetadataAsync(buffer, ct).ConfigureAwait(false);
        return result.BytesReceived;
    }

    /// <inheritdoc />
    async Task<SctpReceiveResult> ISctpMetadataSocket.ReceiveAsync(Memory<byte> buffer, CancellationToken ct)
    {
        return await ReceiveWithMetadataAsync(buffer, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Captures a development transport health snapshot.
    /// </summary>
    /// <param name="remoteEndpoint">The logical remote endpoint to report.</param>
    /// <param name="localEndpoint">The optional logical local endpoint to report.</param>
    /// <returns>An SCTP transport health snapshot.</returns>
    public SctpTransportHealth GetHealthSnapshot(SctpEndpoint remoteEndpoint, SctpEndpoint? localEndpoint = null)
    {
        return new(
            AssociationState,
            remoteEndpoint,
            localEndpoint,
            outboundStreams: 1,
            inboundStreams: 1,
            defaultPayloadProtocolIdentifier: DefaultMetadata.PayloadProtocolIdentifier,
            sentMessages: Interlocked.Read(ref _sentMessages),
            receivedMessages: Interlocked.Read(ref _receivedMessages));
    }

    private async Task<SctpReceiveResult> ReceiveWithMetadataAsync(Memory<byte> buffer, CancellationToken ct)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TcpSctpAdapter));
        }

        // Read 4-byte length prefix
        byte[] prefix = new byte[4];
        int r = await _stream.ReadAsync(prefix.AsMemory(0, 4), ct).ConfigureAwait(false);
        if (r == 0)
        {
            return new(0, DefaultMetadata); // remote closed
        }

        if (r < 4)
        {
            throw new InvalidDataException("Incomplete length prefix");
        }

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(prefix);
        }
        int length = MemoryMarshal.Read<int>(prefix);
        if (length < 0 || length > buffer.Length)
        {
            throw new InvalidDataException($"Invalid frame length: {length}");
        }

        int offset = 0;
        while (offset < length)
        {
            int n = await _stream.ReadAsync(buffer.Slice(offset, length - offset), ct).ConfigureAwait(false);
            if (n <= 0)
            {
                throw new InvalidDataException("Unexpected socket close during payload read");
            }
            offset += n;
        }

        Interlocked.Increment(ref _receivedMessages);
        return new(length, DefaultMetadata);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stream.Dispose();
        _client.Dispose();
    }
}
