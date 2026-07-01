namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Represents the operational state of an MTP2-compatible signalling link.
/// </summary>
public enum Mtp2LinkState
{
    /// <summary>The link is not aligned and cannot carry signalling units.</summary>
    OutOfService,

    /// <summary>The link is aligning or reconnecting.</summary>
    Aligning,

    /// <summary>The link is aligned and can carry signalling units.</summary>
    InService,

    /// <summary>The link is draining or shutting down.</summary>
    ShuttingDown,

    /// <summary>The link failed and needs recovery handling.</summary>
    Failed
}

/// <summary>
/// Provides the MTP2-compatible link contract consumed by an MTP3 network layer.
/// </summary>
public interface IMtp2Link
{
    /// <summary>The current link state.</summary>
    Mtp2LinkState State { get; }

    /// <summary>
    /// Sends one MTP2 service data unit to the lower adaptation or transport layer.
    /// </summary>
    /// <param name="payload">The service data unit payload.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the payload has been queued or sent.</returns>
    ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default);

    /// <summary>
    /// Receives one MTP2 service data unit from the lower adaptation or transport layer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The number of payload bytes received.</returns>
    ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default);
}
