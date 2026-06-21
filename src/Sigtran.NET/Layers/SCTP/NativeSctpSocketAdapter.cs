using System.Net.Sockets;

using Sigtran.NET.Core.Interfaces;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Wraps a native SCTP socket as the SDK packet transport contract.
/// </summary>
public sealed class NativeSctpSocketAdapter : ISctpSocket
{
    private readonly Socket _socket;
    private readonly SctpConnectionOptions _options;
    private long _sentMessages;
    private long _receivedMessages;
    private bool _disposed;
    private SctpAssociationState _associationState;

    /// <summary>Creates a native SCTP socket adapter.</summary>
    /// <param name="socket">The native SCTP socket.</param>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="associationState">The initial association state.</param>
    public NativeSctpSocketAdapter(
        Socket socket,
        SctpConnectionOptions options,
        SctpAssociationState associationState = SctpAssociationState.Closed)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _associationState = associationState;
    }

    /// <summary>The current association state.</summary>
    public SctpAssociationState AssociationState => _disposed ? SctpAssociationState.Closed : _associationState;

    /// <summary>Marks the association as established.</summary>
    public void MarkEstablished()
    {
        ThrowIfDisposed();
        _associationState = SctpAssociationState.Established;
    }

    /// <summary>Marks the association as failed.</summary>
    public void MarkFailed()
    {
        if (!_disposed)
        {
            _associationState = SctpAssociationState.Failed;
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data.IsEmpty)
        {
            throw new ArgumentException("SCTP payload must not be empty.", nameof(data));
        }

        int sent = await _socket.SendAsync(data, SocketFlags.None, ct).ConfigureAwait(false);
        if (sent != data.Length)
        {
            throw new InvalidDataException($"Native SCTP send wrote {sent} bytes for a {data.Length} byte message.");
        }

        Interlocked.Increment(ref _sentMessages);
    }

    /// <inheritdoc />
    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty)
        {
            throw new ArgumentException("Receive buffer must not be empty.", nameof(buffer));
        }

        int received = await _socket.ReceiveAsync(buffer, SocketFlags.None, ct).ConfigureAwait(false);
        if (received > 0)
        {
            Interlocked.Increment(ref _receivedMessages);
        }

        return received;
    }

    /// <summary>Captures a native SCTP transport health snapshot.</summary>
    /// <returns>The transport health snapshot.</returns>
    public SctpTransportHealth GetHealthSnapshot()
    {
        return new(
            AssociationState,
            _options.RemoteEndpoint,
            _options.LocalEndpoint,
            _options.OutboundStreams,
            _options.InboundStreams,
            _options.DefaultPayloadProtocolIdentifier,
            Interlocked.Read(ref _sentMessages),
            Interlocked.Read(ref _receivedMessages));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _associationState = SctpAssociationState.Closed;
        _socket.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeSctpSocketAdapter));
        }
    }
}
