namespace sigtran.net.Core.Interfaces;

/// <summary>
/// Represents an abstract transport supporting SCTP‑like semantics.  The
/// SIGTRAN stack uses this interface to send and receive complete
/// protocol data units (e.g. M3UA messages) over a connection‑oriented
/// transport.  A simple TCP adapter is provided for development; a
/// production system would replace it with a proper SCTP implementation.
/// </summary>
public interface ISctpSocket : IDisposable
{
    /// <summary>
    /// Sends a contiguous block of bytes to the remote peer.  The
    /// implementation may frame the data in whatever manner is
    /// appropriate (e.g. by adding length prefixes).  This method
    /// completes when the data has been enqueued for transmission.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="ct">A cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Receives a contiguous block of bytes from the remote peer.  The
    /// implementation must return exactly one protocol data unit per call.
    /// If the buffer is too small an exception should be thrown.  This
    /// method blocks until a complete message has been received.
    /// </summary>
    /// <param name="buffer">A buffer into which the data will be read.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The number of bytes received.</returns>
    Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default);
}