using sigtran.net.Core.Interfaces;
using sigtran.net.Core.Utilities;

namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Implements a minimal SCCP layer supporting only the UDT format.  It
/// bridges between TCAP (or the application layer) and M3UA.  When
/// receiving data from below (M3UA), it parses a UDT and forwards its
/// user data upward.  When receiving data from above (TCAP), it builds a
/// UDT around the payload and passes it down.
/// </summary>
public sealed class SccpLayer : ISigtranLayer
{
    private readonly ILayerConnector _connector;

    /// <summary>
    /// Constructs a new SCCP layer with the given connector.  The
    /// connector bridges to the TCAP layer above and the M3UA layer
    /// below.
    /// </summary>
    /// <param name="connector">The layer connector.</param>
    public SccpLayer(ILayerConnector connector)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
    }

    /// <inheritdoc />
    public void ReceiveFromLower(ReadOnlySpan<byte> data)
    {
        if (!SccpMessage.TryParseUdt(data, out SccpMessage? msg, out string? err))
        {
            MetricsExporter.ErrorsTotal++;
            throw new InvalidOperationException(err);
        }
        if (msg!.UserData.IsEmpty)
        {
            return; // nothing to forward
        }
        _connector.SendUp(msg.UserData.Span);
    }

    /// <inheritdoc />
    public void ReceiveFromUpper(ReadOnlySpan<byte> data)
    {
        // Build a UDT with MAP SSN on both ends.  In real use the SSNs
        // would depend on the actual service and peer.
        SccpMessage udt = new()
                          {
                              ProtocolClass = 0,
                              CalledParty = new() { Subsystem = SubsystemNumber.MAP },
                              CallingParty = new() { Subsystem = SubsystemNumber.MAP },
                              UserData = data.ToArray()
                          };
        byte[] bytes = udt.EncodeUdt();
        _connector.SendDown(bytes);
    }
}