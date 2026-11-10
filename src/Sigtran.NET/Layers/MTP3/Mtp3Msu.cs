namespace Sigtran.NET.Layers.MTP3;

/// <summary>Encodes the ITU MTP3 octets carried across the MTP2 service boundary.</summary>
internal static class Mtp3Msu
{
    private const int HeaderSize = 5;

    /// <summary>Encodes SIO, the four-byte routing label and the user-part or management bytes.</summary>
    internal static byte[] Encode(Mtp3TransferMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.NetworkAppearance is not null || message.RoutingContext is not null || message.CorrelationId is not null)
        {
            throw new ArgumentException("MTP3 MSUs cannot carry adaptation-layer parameters.", nameof(message));
        }

        if ((byte)message.ServiceInformation.ServiceIndicator > 15)
        {
            throw new ArgumentException("The service indicator must fit in four bits.", nameof(message));
        }

        byte[] bytes = new byte[checked(HeaderSize + message.UserPayload.Length)];
        bytes[0] = message.ServiceInformation.Encode();
        message.RoutingLabel.Encode(bytes.AsSpan(1, 4));
        message.UserPayload.Span.CopyTo(bytes.AsSpan(HeaderSize));
        return bytes;
    }

    /// <summary>Decodes a complete MSU and copies its payload away from the receive buffer.</summary>
    internal static bool TryDecode(ReadOnlySpan<byte> source, out Mtp3TransferMessage? message)
    {
        if (source.Length <= HeaderSize)
        {
            message = null;
            return false;
        }

        message = new(Mtp3ServiceInformationOctet.Decode(source[0]),
            Mtp3RoutingLabel.Decode(source.Slice(1, 4)), source[HeaderSize..].ToArray());
        return true;
    }
}
