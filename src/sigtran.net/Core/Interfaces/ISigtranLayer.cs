namespace sigtran.net.Core.Interfaces;

/// <summary>
/// A minimal contract for a protocol layer in the SIGTRAN stack.  Layers
/// connect to an adjacent lower layer (e.g. M3UA connects to SCTP) and
/// an adjacent upper layer (e.g. M3UA connects to SCCP) via an
/// <see cref="ILayerConnector"/>.  Implementations should be
/// stateless where possible and delegate state tracking to dialogue or
/// session objects.
/// </summary>
public interface ISigtranLayer
{
    /// <summary>
    /// Called by the lower layer when a new payload has been received.  The
    /// data will be an entire protocol data unit appropriate to this
    /// layer.  Implementations should parse and validate the data and
    /// forward it to the upper layer via the <see cref="ILayerConnector"/>.
    /// </summary>
    /// <param name="data">The raw bytes received from the lower layer.</param>
    void ReceiveFromLower(ReadOnlySpan<byte> data);

    /// <summary>
    /// Called by the upper layer when a new payload should be transmitted
    /// downwards.  The data provided is the upper layer’s user payload.  It
    /// is the responsibility of the implementation to encapsulate this
    /// payload in an appropriate protocol data unit and deliver it to the
    /// lower layer via the <see cref="ILayerConnector"/>.
    /// </summary>
    /// <param name="data">The raw payload to transmit downwards.</param>
    void ReceiveFromUpper(ReadOnlySpan<byte> data);
}