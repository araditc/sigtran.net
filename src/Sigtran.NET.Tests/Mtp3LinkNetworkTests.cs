using Sigtran.NET.Layers.MTP3;

/// <summary>Verifies ITU MTP3 wire format and the MTP2 service boundary.</summary>
internal static class Mtp3LinkNetworkTests
{
    internal static void MsuWire()
    {
        byte[] fixture = [0x83, 0x34, 0x52, 0xD1, 0xA8, 0x09, 0x01];
        Mtp3TransferMessage transfer = new(new(Mtp3ServiceIndicator.Sccp, 2), new(0x1234, 0x2345, 10), new byte[] { 9, 1 });
        Require(Mtp3Msu.Encode(transfer).SequenceEqual(fixture), "ITU SIO, routing label and payload wire fixture");
        for (int size = 0; size <= 5; size++)
        {
            Require(!Mtp3Msu.TryDecode(fixture.AsSpan(0, size), out _), "Short or empty MSU must be rejected");
        }

        Require(Mtp3Msu.TryDecode(fixture, out Mtp3TransferMessage? decoded)
            && decoded!.RoutingLabel.OriginatingPointCode == 0x2345
            && decoded.RoutingLabel.DestinationPointCode == 0x1234
            && decoded.RoutingLabel.SignallingLinkSelection == 10, "Decode ITU routing label");
        fixture[5] = 0;
        Require(decoded!.UserPayload.Span[0] == 9, "The receive buffer must not alias the transfer payload");

        try
        {
            _ = Mtp3Msu.Encode(new(transfer.ServiceInformation, transfer.RoutingLabel, transfer.UserPayload, routingContext: 1));
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException("Adaptation parameters must not be silently dropped");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
