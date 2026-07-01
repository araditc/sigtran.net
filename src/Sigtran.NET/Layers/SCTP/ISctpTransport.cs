namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Sends and receives complete SCTP user messages with stream, PPID, and association lifecycle metadata.
/// </summary>
public interface ISctpTransport : IDisposable, IAsyncDisposable
{
    /// <summary>The SCTP association represented by this transport.</summary>
    ISctpAssociation Association { get; }

    /// <summary>
    /// Sends one SCTP user message with explicit stream and Payload Protocol Identifier metadata.
    /// </summary>
    /// <param name="message">The outbound user message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the message has been queued or sent.</returns>
    ValueTask SendAsync(SctpOutboundMessage message, CancellationToken ct = default);

    /// <summary>
    /// Receives one SCTP user message and exposes the associated stream and Payload Protocol Identifier metadata.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The receive result containing payload length and SCTP metadata.</returns>
    ValueTask<SctpReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default);
}
