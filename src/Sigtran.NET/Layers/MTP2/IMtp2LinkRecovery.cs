namespace Sigtran.NET.Layers.MTP2;

/// <summary>Optional extended changeover primitives for an MTP2 service endpoint.</summary>
public interface IMtp2LinkRecovery : IMtp2Link
{
    /// <summary>Quiesces the link and freezes its receive and retrieval boundaries.</summary>
    /// <param name="ct">A token to cancel before freezing the link.</param>
    /// <returns>
    /// The last accepted 24-bit FSN (null when unavailable) and the cumulative number of
    /// complete MSUs made available to the exclusive receiver since link creation.
    /// </returns>
    ValueTask<(uint? LastAcceptedSequence, long AcceptedMsuCount)> FreezeForChangeoverAsync(CancellationToken ct = default);

    /// <summary>Retrieves frozen MSUs strictly after a peer-confirmed sequence boundary, in send order.</summary>
    /// <param name="peerLastAcceptedSequence">The peer's last accepted 24-bit FSN.</param>
    /// <param name="messages">The owned MTP3 MSUs, without adaptation-layer headers.</param>
    /// <returns>False when the peer value cannot be reconciled with the frozen window.</returns>
    bool TryRetrieveAfter(uint peerLastAcceptedSequence, out IReadOnlyList<ReadOnlyMemory<byte>> messages);
}
