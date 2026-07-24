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

    /// <summary>Starts the stateful lower-layer receive loop.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the receive loop has started.</returns>
    ValueTask StartAsync(CancellationToken ct = default);

    /// <summary>Stops the stateful lower-layer receive loop.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the receive loop has stopped.</returns>
    ValueTask StopAsync(CancellationToken ct = default);

    /// <summary>Sends one SCCP connectionless data request.</summary>
    /// <param name="request">The outbound SCCP request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when all encoded messages have been sent.</returns>
    ValueTask SendAsync(SccpDataRequest request, CancellationToken ct = default);

    /// <summary>Receives one decoded and routed SCCP data indication.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The next SCCP data indication.</returns>
    ValueTask<SccpDataIndication> ReceiveAsync(CancellationToken ct = default);

    /// <summary>Receives one SCCP service-return indication.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The next SCCP service-return indication.</returns>
    ValueTask<SccpReturnIndication> ReceiveReturnAsync(CancellationToken ct = default);
}
