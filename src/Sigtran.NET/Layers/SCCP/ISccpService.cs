using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Provides connectionless SCCP service primitives over an MTP3 network contract.
/// </summary>
public interface ISccpService
{
    /// <summary>The lower MTP3 network contract used by this SCCP service.</summary>
    IMtp3Network Network { get; }

    /// <summary>
    /// Sends one SCCP Unitdata message.
    /// </summary>
    /// <param name="message">The SCCP Unitdata message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the message has been queued or sent.</returns>
    ValueTask SendUnitdataAsync(SccpUnitdataMessage message, CancellationToken ct = default);

    /// <summary>
    /// Receives one SCCP Unitdata message.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The received SCCP Unitdata message.</returns>
    ValueTask<SccpUnitdataMessage> ReceiveUnitdataAsync(CancellationToken ct = default);
}
