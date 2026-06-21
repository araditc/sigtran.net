namespace Sigtran.NET.Layers.MTP3;

/// <summary>
/// Identifies an MTP3 user part carried by the service information octet.
/// </summary>
public enum Mtp3ServiceIndicator : byte
{
    /// <summary>Signalling network management messages.</summary>
    SignallingNetworkManagement = 0,

    /// <summary>Signalling network testing and maintenance messages.</summary>
    SignallingNetworkTesting = 1,

    /// <summary>Signalling connection control part.</summary>
    Sccp = 3,

    /// <summary>Telephone user part.</summary>
    Tup = 4,

    /// <summary>ISDN user part.</summary>
    Isup = 5
}

/// <summary>
/// Represents the MTP3 service information octet.
/// </summary>
public readonly struct Mtp3ServiceInformationOctet
{
    /// <summary>Creates an MTP3 service information octet.</summary>
    /// <param name="serviceIndicator">The MTP3 user part.</param>
    /// <param name="networkIndicator">The two-bit network indicator.</param>
    /// <param name="messagePriority">The two-bit message priority.</param>
    public Mtp3ServiceInformationOctet(Mtp3ServiceIndicator serviceIndicator, byte networkIndicator, byte messagePriority = 0)
    {
        if (networkIndicator > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(networkIndicator), "Network indicator must fit in two bits.");
        }

        if (messagePriority > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(messagePriority), "Message priority must fit in two bits.");
        }

        ServiceIndicator = serviceIndicator;
        NetworkIndicator = networkIndicator;
        MessagePriority = messagePriority;
    }

    /// <summary>The MTP3 user part.</summary>
    public Mtp3ServiceIndicator ServiceIndicator { get; }

    /// <summary>The two-bit network indicator.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>The two-bit message priority.</summary>
    public byte MessagePriority { get; }

    /// <summary>Encodes this value as one service information octet.</summary>
    /// <returns>The encoded service information octet.</returns>
    public byte Encode()
    {
        return (byte)(((NetworkIndicator & 0x03) << 6) | ((MessagePriority & 0x03) << 4) | ((byte)ServiceIndicator & 0x0F));
    }

    /// <summary>Decodes one service information octet.</summary>
    /// <param name="value">The encoded service information octet.</param>
    /// <returns>The decoded service information octet.</returns>
    public static Mtp3ServiceInformationOctet Decode(byte value)
    {
        return new(
            (Mtp3ServiceIndicator)(value & 0x0F),
            networkIndicator: (byte)((value >> 6) & 0x03),
            messagePriority: (byte)((value >> 4) & 0x03));
    }
}

/// <summary>
/// Represents the ITU-style MTP3 routing label used by SCCP over M3UA.
/// </summary>
public readonly struct Mtp3RoutingLabel
{
    /// <summary>Creates an MTP3 routing label.</summary>
    /// <param name="destinationPointCode">The 14-bit destination point code.</param>
    /// <param name="originatingPointCode">The 14-bit originating point code.</param>
    /// <param name="signallingLinkSelection">The four-bit signalling link selection value.</param>
    public Mtp3RoutingLabel(uint destinationPointCode, uint originatingPointCode, byte signallingLinkSelection)
    {
        if (destinationPointCode > 0x3FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationPointCode), "Destination point code must fit in 14 bits.");
        }

        if (originatingPointCode > 0x3FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(originatingPointCode), "Originating point code must fit in 14 bits.");
        }

        if (signallingLinkSelection > 0x0F)
        {
            throw new ArgumentOutOfRangeException(nameof(signallingLinkSelection), "SLS must fit in four bits.");
        }

        DestinationPointCode = destinationPointCode;
        OriginatingPointCode = originatingPointCode;
        SignallingLinkSelection = signallingLinkSelection;
    }

    /// <summary>The 14-bit destination point code.</summary>
    public uint DestinationPointCode { get; }

    /// <summary>The 14-bit originating point code.</summary>
    public uint OriginatingPointCode { get; }

    /// <summary>The four-bit signalling link selection value.</summary>
    public byte SignallingLinkSelection { get; }

    /// <summary>Encodes the routing label into the caller-provided buffer.</summary>
    /// <param name="destination">A buffer with at least four bytes.</param>
    public void Encode(Span<byte> destination)
    {
        if (destination.Length < 4)
        {
            throw new ArgumentException("MTP3 routing label requires four bytes.", nameof(destination));
        }

        uint label = DestinationPointCode | (OriginatingPointCode << 14) | ((uint)SignallingLinkSelection << 28);
        destination[0] = (byte)label;
        destination[1] = (byte)(label >> 8);
        destination[2] = (byte)(label >> 16);
        destination[3] = (byte)(label >> 24);
    }

    /// <summary>Decodes a four-byte routing label.</summary>
    /// <param name="source">The encoded routing label bytes.</param>
    /// <returns>The decoded routing label.</returns>
    public static Mtp3RoutingLabel Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
        {
            throw new ArgumentException("MTP3 routing label requires four bytes.", nameof(source));
        }

        uint label = source[0] | ((uint)source[1] << 8) | ((uint)source[2] << 16) | ((uint)source[3] << 24);
        return new(
            destinationPointCode: label & 0x3FFF,
            originatingPointCode: (label >> 14) & 0x3FFF,
            signallingLinkSelection: (byte)((label >> 28) & 0x0F));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"DPC={DestinationPointCode} OPC={OriginatingPointCode} SLS={SignallingLinkSelection}";
    }
}
