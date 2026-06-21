namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// ASN.1 BER tag classes used by TCAP transaction and component portions.
/// </summary>
public enum TcapBerTagClass : byte
{
    /// <summary>Universal ASN.1 tag class.</summary>
    Universal = 0x00,

    /// <summary>Application ASN.1 tag class.</summary>
    Application = 0x40,

    /// <summary>Context-specific ASN.1 tag class.</summary>
    ContextSpecific = 0x80,

    /// <summary>Private ASN.1 tag class.</summary>
    Private = 0xC0
}

/// <summary>
/// Represents one BER tag octet used by TCAP.
/// </summary>
public readonly struct TcapBerTag
{
    private const byte ConstructedMask = 0x20;
    private const byte NumberMask = 0x1F;

    /// <summary>Creates a BER tag.</summary>
    /// <param name="tagClass">The BER tag class.</param>
    /// <param name="constructed">Whether the value is constructed.</param>
    /// <param name="number">The short-form tag number.</param>
    public TcapBerTag(TcapBerTagClass tagClass, bool constructed, byte number)
    {
        if (number > NumberMask)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Only BER short-form tags are supported.");
        }

        TagClass = tagClass;
        Constructed = constructed;
        Number = number;
    }

    /// <summary>The BER tag class.</summary>
    public TcapBerTagClass TagClass { get; }

    /// <summary>Whether the value is constructed.</summary>
    public bool Constructed { get; }

    /// <summary>The short-form tag number.</summary>
    public byte Number { get; }

    /// <summary>Encodes the tag as one octet.</summary>
    /// <returns>The encoded tag octet.</returns>
    public byte Encode()
    {
        return (byte)((byte)TagClass | (Constructed ? ConstructedMask : 0x00) | (Number & NumberMask));
    }

    /// <summary>Decodes one BER tag octet.</summary>
    /// <param name="value">The encoded tag octet.</param>
    /// <returns>The decoded BER tag.</returns>
    public static TcapBerTag Decode(byte value)
    {
        return new(
            (TcapBerTagClass)(value & 0xC0),
            (value & ConstructedMask) != 0,
            (byte)(value & NumberMask));
    }
}

/// <summary>
/// Represents one decoded BER TLV element.
/// </summary>
public readonly struct TcapBerElement
{
    /// <summary>Creates a BER element view.</summary>
    /// <param name="tag">The decoded tag.</param>
    /// <param name="value">The value bytes.</param>
    /// <param name="totalLength">The full encoded TLV length.</param>
    public TcapBerElement(TcapBerTag tag, ReadOnlyMemory<byte> value, int totalLength)
    {
        Tag = tag;
        Value = value;
        TotalLength = totalLength;
    }

    /// <summary>The decoded tag.</summary>
    public TcapBerTag Tag { get; }

    /// <summary>The value bytes.</summary>
    public ReadOnlyMemory<byte> Value { get; }

    /// <summary>The full encoded TLV length.</summary>
    public int TotalLength { get; }
}

/// <summary>
/// Provides BER TLV encode/decode helpers for TCAP.
/// </summary>
public static class TcapBer
{
    /// <summary>Writes a BER TLV element to the caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="tag">The BER tag.</param>
    /// <param name="value">The value bytes.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if writing succeeded; otherwise false.</returns>
    public static bool TryWriteElement(
        Span<byte> destination,
        TcapBerTag tag,
        ReadOnlySpan<byte> value,
        out int written,
        out string? error)
    {
        written = 0;
        error = null;
        int lengthOctets = GetLengthOctetCount(value.Length);
        int required = 1 + lengthOctets + value.Length;
        if (destination.Length < required)
        {
            error = "BER destination buffer is too small";
            return false;
        }

        destination[0] = tag.Encode();
        WriteLength(destination.Slice(1, lengthOctets), value.Length);
        value.CopyTo(destination.Slice(1 + lengthOctets));
        written = required;
        return true;
    }

    /// <summary>Attempts to read the first BER TLV element from a buffer.</summary>
    /// <param name="data">The encoded BER bytes.</param>
    /// <param name="element">The decoded BER element on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryReadElement(ReadOnlySpan<byte> data, out TcapBerElement element, out string? error)
    {
        element = default;
        error = null;
        if (data.Length < 2)
        {
            error = "BER element is too short";
            return false;
        }

        TcapBerTag tag = TcapBerTag.Decode(data[0]);
        if (!TryReadLength(data[1..], out int valueLength, out int lengthOctets, out error))
        {
            return false;
        }

        int valueOffset = 1 + lengthOctets;
        if (data.Length < valueOffset + valueLength)
        {
            error = "BER value length exceeds available data";
            return false;
        }

        element = new(tag, data.Slice(valueOffset, valueLength).ToArray(), valueOffset + valueLength);
        return true;
    }

    private static int GetLengthOctetCount(int length)
    {
        if (length < 0x80)
        {
            return 1;
        }

        if (length <= 0xFF)
        {
            return 2;
        }

        return 3;
    }

    private static void WriteLength(Span<byte> destination, int length)
    {
        if (length < 0x80)
        {
            destination[0] = (byte)length;
            return;
        }

        if (length <= 0xFF)
        {
            destination[0] = 0x81;
            destination[1] = (byte)length;
            return;
        }

        destination[0] = 0x82;
        destination[1] = (byte)(length >> 8);
        destination[2] = (byte)length;
    }

    private static bool TryReadLength(ReadOnlySpan<byte> data, out int length, out int lengthOctets, out string? error)
    {
        length = 0;
        lengthOctets = 0;
        error = null;
        if (data.IsEmpty)
        {
            error = "BER length is missing";
            return false;
        }

        byte first = data[0];
        if ((first & 0x80) == 0)
        {
            length = first;
            lengthOctets = 1;
            return true;
        }

        int count = first & 0x7F;
        if (count is 0 or > 2)
        {
            error = "Unsupported BER length form";
            return false;
        }

        if (data.Length < 1 + count)
        {
            error = "BER length octets are truncated";
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            length = (length << 8) | data[1 + i];
        }

        lengthOctets = 1 + count;
        return true;
    }
}
