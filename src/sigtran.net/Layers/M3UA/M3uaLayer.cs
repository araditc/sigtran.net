using System.Buffers;

using sigtran.net.Core.Interfaces;
using sigtran.net.Core.Utilities;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents the M3UA layer in the SIGTRAN stack.  It sits on top of an
/// SCTP transport and below a higher layer (typically SCCP).  Its job
/// is to parse and validate M3UA messages received from SCTP and
/// forward the contained Protocol Data to the next layer up.  It also
/// builds M3UA messages from upper layer payloads and sends them down
/// through the SCTP adapter.
/// </summary>
public sealed class M3uaLayer : ISigtranLayer
{
    private readonly ISctpSocket _socket;
    private readonly ILayerConnector _connector;

    /// <summary>
    /// Constructs a new M3UA layer using the given transport and
    /// connector.  The connector is used to pass Protocol Data up or
    /// down without exposing the concrete type of the neighbouring
    /// layers.
    /// </summary>
    /// <param name="socket">The underlying SCTP transport.</param>
    /// <param name="connector">The connector bridging to SCCP.</param>
    public M3uaLayer(ISctpSocket socket, ILayerConnector connector)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
    }

    /// <inheritdoc />
    public void ReceiveFromLower(ReadOnlySpan<byte> data)
    {
        // Parse M3UA message
        M3uaMessage msg = new();
        if (!msg.TryDecode(data, out string? err))
        {
            MetricsExporter.ErrorsTotal++;
            throw new InvalidOperationException(err);
        }
        if (!msg.TryGetProtocolData(out ReadOnlySpan<byte> proto, out err))
        {
            MetricsExporter.ErrorsTotal++;
            throw new InvalidOperationException(err);
        }
        // Forward the protocol data (MTP3/SCCP bytes) upward
        _connector.SendUp(proto);
        MetricsExporter.M3uaMessagesTotal++;
    }

    /// <inheritdoc />
    public void ReceiveFromUpper(ReadOnlySpan<byte> data)
    {
        // The upper layer (SCCP) passes raw MTP3/SCCP bytes.  We need to
        // encapsulate them into a Payload Data message and send down.
        ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        byte[] rented = pool.Rent(4096);
        try
        {
            if (!M3uaMessageBuilder.BuildPayloadData(
                    rented.AsSpan(), data, opc: 1, dpc: 2, si: 3, ni: 2, mp: 0, sls: 0,
                    out int written, out string? err))
            {
                MetricsExporter.ErrorsTotal++;
                throw new InvalidOperationException(err);
            }
            // Send down synchronously; for a real implementation this
            // should be fully asynchronous and consider flow control.
            _socket.SendAsync(rented.AsMemory(0, written), CancellationToken.None).GetAwaiter().GetResult();
            MetricsExporter.M3uaMessagesTotal++;
        }
        finally
        {
            pool.Return(rented);
        }
    }
}