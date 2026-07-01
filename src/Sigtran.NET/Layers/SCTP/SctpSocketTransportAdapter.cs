using Sigtran.NET.Core.Interfaces;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Adapts the legacy packet socket interface to the official SCTP transport contract.
/// </summary>
public sealed class SctpSocketTransportAdapter : ISctpTransport
{
    private readonly ISctpSocket _socket;
    private readonly ISctpMetadataSocket? _metadataSocket;
    private readonly bool _leaveOpen;
    private readonly SctpAssociationSnapshot _association;
    private bool _disposed;

    /// <summary>Creates an SCTP transport adapter around an existing packet socket.</summary>
    /// <param name="socket">The packet socket to adapt.</param>
    /// <param name="metadataSocket">The optional metadata-capable socket facade.</param>
    /// <param name="association">The optional association state provider.</param>
    /// <param name="defaultMetadata">The default metadata used when the socket cannot expose SCTP metadata.</param>
    /// <param name="leaveOpen">Whether disposing this adapter should leave the wrapped socket open.</param>
    public SctpSocketTransportAdapter(
        ISctpSocket socket,
        ISctpMetadataSocket? metadataSocket = null,
        ISctpAssociation? association = null,
        SctpPayloadMetadata? defaultMetadata = null,
        bool leaveOpen = false)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _metadataSocket = metadataSocket ?? socket as ISctpMetadataSocket;
        _leaveOpen = leaveOpen;
        DefaultMetadata = defaultMetadata ?? new SctpPayloadMetadata(streamId: 0, payloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua);
        _association = SctpAssociationSnapshot.From(association, SctpAssociationState.Established);
    }

    /// <summary>The metadata returned when the wrapped socket cannot provide SCTP metadata.</summary>
    public SctpPayloadMetadata DefaultMetadata { get; }

    /// <inheritdoc />
    public ISctpAssociation Association => _association;

    /// <inheritdoc />
    public async ValueTask SendAsync(SctpOutboundMessage message, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        if (_metadataSocket is not null)
        {
            await _metadataSocket.SendAsync(message.Payload, message.Metadata, ct).ConfigureAwait(false);
            return;
        }

        await _socket.SendAsync(message.Payload, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<SctpReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (_metadataSocket is not null)
        {
            return await _metadataSocket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
        }

        int received = await _socket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
        return new(received, DefaultMetadata);
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SctpSocketTransportAdapter));
        }
    }

    private sealed class SctpAssociationSnapshot : ISctpAssociation
    {
        private readonly IReadOnlyList<SctpAssociationJournalEntry> _events;

        private SctpAssociationSnapshot(SctpAssociationState state, IReadOnlyList<SctpAssociationJournalEntry> events)
        {
            State = state;
            _events = events;
        }

        public SctpAssociationState State { get; }

        public static SctpAssociationSnapshot From(ISctpAssociation? association, SctpAssociationState fallbackState)
        {
            if (association is null)
            {
                return new(
                    fallbackState,
                    [new SctpAssociationJournalEntry(DateTimeOffset.UtcNow, new SctpAssociationEvent(SctpAssociationEventType.Established, fallbackState, "legacy socket adapter"))]);
            }

            return new(association.State, association.SnapshotEvents());
        }

        public IReadOnlyList<SctpAssociationJournalEntry> SnapshotEvents()
        {
            return _events;
        }
    }
}
