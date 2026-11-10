using System.Buffers;

namespace Sigtran.NET.Layers.MTP2;

/// <summary>Adapts the raw RFC 4165 User Data body to an ITU MTP2 service data unit.</summary>
internal sealed class M2paMtp2ServiceLink(M2paLink link, int maximumMessageSize) : IMtp2LinkRecovery
{
    private const int MsuOffset = 1;
    private M2paChangeoverSnapshot? _frozen;

    /// <inheritdoc />
    public Mtp2LinkState State => link.State;

    /// <inheritdoc />
    public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (payload.IsEmpty || payload.Length > maximumMessageSize - M2paProtocol.MinimumMessageLength - MsuOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "The MSU must fit in the configured M2PA message size.");
        }

        ct.ThrowIfCancellationRequested();
        // RFC 4165 2.3.1: the first User Data octet is spare for ITU MTP.
        byte[] body = new byte[MsuOffset + payload.Length];
        payload.Span.CopyTo(body.AsSpan(MsuOffset));
        return link.SendAsync(body, ct);
    }

    /// <inheritdoc />
    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        int size = Math.Min(buffer.Length, maximumMessageSize - M2paProtocol.MinimumMessageLength - MsuOffset) + MsuOffset;
        byte[] body = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            int count = await link.ReceiveAsync(body.AsMemory(0, size), ct).ConfigureAwait(false);
            if (count <= MsuOffset)
            {
                throw new InvalidDataException("M2PA User Data does not contain an ITU MSU.");
            }

            body.AsSpan(MsuOffset, count - MsuOffset).CopyTo(buffer.Span);
            return count - MsuOffset;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(body);
        }
    }

    /// <inheritdoc />
    public async ValueTask<(uint? LastAcceptedSequence, long AcceptedMsuCount)> FreezeForChangeoverAsync(CancellationToken ct = default)
    {
        M2paChangeoverSnapshot snapshot = await link.FreezeForChangeoverAsync(ct).ConfigureAwait(false);
        Volatile.Write(ref _frozen, snapshot);
        return (snapshot.LastAccepted, snapshot.AcceptedCount);
    }

    /// <inheritdoc />
    public bool TryRetrieveAfter(uint peerLastAcceptedSequence, out IReadOnlyList<ReadOnlyMemory<byte>> messages)
    {
        messages = [];
        M2paChangeoverSnapshot? snapshot = Volatile.Read(ref _frozen);
        if (snapshot?.LastAccepted is null)
        {
            return false;
        }

        int start = 0;
        if (peerLastAcceptedSequence != snapshot.LastAcknowledged)
        {
            int boundary = Array.FindIndex(snapshot.Entries, entry => entry.ForwardSequenceNumber == peerLastAcceptedSequence);
            if (boundary < 0)
            {
                return false;
            }

            start = boundary + 1;
        }

        ReadOnlyMemory<byte>[] retained = new ReadOnlyMemory<byte>[snapshot.Entries.Length - start];
        for (int index = start; index < snapshot.Entries.Length; index++)
        {
            ReadOnlyMemory<byte> body = snapshot.Entries[index].Payload;
            if (body.Length <= MsuOffset)
            {
                return false;
            }

            retained[index - start] = body[MsuOffset..];
        }

        messages = retained;
        return true;
    }
}

/// <summary>Owns the sequence and acknowledgement boundaries of one stopped M2PA generation.</summary>
internal sealed record M2paChangeoverSnapshot(uint? LastAccepted, long AcceptedCount, uint LastAcknowledged, M2paRetrievalEntry[] Entries);
