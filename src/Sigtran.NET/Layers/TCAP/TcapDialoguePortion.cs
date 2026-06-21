namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Represents an ASN.1 object identifier used by TCAP dialogue portions.
/// </summary>
public sealed class TcapObjectIdentifier
{
    /// <summary>Creates an object identifier from arcs.</summary>
    /// <param name="arcs">The object identifier arcs.</param>
    public TcapObjectIdentifier(params uint[] arcs)
    {
        if (arcs.Length < 2)
        {
            throw new ArgumentException("Object identifier requires at least two arcs.", nameof(arcs));
        }

        if (arcs[0] > 2 || (arcs[0] < 2 && arcs[1] > 39))
        {
            throw new ArgumentOutOfRangeException(nameof(arcs), "Object identifier first arcs are invalid.");
        }

        Arcs = arcs.ToArray();
    }

    /// <summary>The object identifier arcs.</summary>
    public IReadOnlyList<uint> Arcs { get; }

    /// <summary>Encodes the object identifier value bytes.</summary>
    /// <returns>The encoded object identifier value bytes.</returns>
    public byte[] EncodeValue()
    {
        List<byte> result = [(byte)((Arcs[0] * 40) + Arcs[1])];
        for (int i = 2; i < Arcs.Count; i++)
        {
            WriteBase128(result, Arcs[i]);
        }

        return result.ToArray();
    }

    /// <summary>Decodes object identifier value bytes.</summary>
    /// <param name="value">The encoded value bytes.</param>
    /// <returns>The decoded object identifier.</returns>
    public static TcapObjectIdentifier DecodeValue(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentException("Object identifier value is empty.", nameof(value));
        }

        List<uint> arcs = [];
        byte first = value[0];
        arcs.Add((uint)Math.Min(first / 40, 2));
        arcs.Add((uint)(first - (arcs[0] * 40)));
        uint current = 0;
        for (int i = 1; i < value.Length; i++)
        {
            current = (current << 7) | (uint)(value[i] & 0x7F);
            if ((value[i] & 0x80) == 0)
            {
                arcs.Add(current);
                current = 0;
            }
        }

        if (current != 0)
        {
            throw new ArgumentException("Object identifier base-128 value is truncated.", nameof(value));
        }

        return new(arcs.ToArray());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Join(".", Arcs);
    }

    private static void WriteBase128(List<byte> result, uint value)
    {
        Span<byte> stack = stackalloc byte[5];
        int count = 0;
        stack[count++] = (byte)(value & 0x7F);
        value >>= 7;
        while (value > 0)
        {
            stack[count++] = (byte)(0x80 | (value & 0x7F));
            value >>= 7;
        }

        for (int i = count - 1; i >= 0; i--)
        {
            result.Add(stack[i]);
        }
    }
}

/// <summary>
/// Represents a TCAP dialogue portion foundation model.
/// </summary>
public sealed class TcapDialoguePortion
{
    private static readonly TcapBerTag ObjectIdentifierTag = new(TcapBerTagClass.Universal, constructed: false, number: 6);
    private static readonly TcapBerTag UserInformationTag = new(TcapBerTagClass.Universal, constructed: false, number: 4);

    /// <summary>Creates a dialogue portion.</summary>
    /// <param name="applicationContext">The application context object identifier.</param>
    /// <param name="userInformation">The optional user information bytes.</param>
    public TcapDialoguePortion(TcapObjectIdentifier applicationContext, ReadOnlyMemory<byte> userInformation = default)
    {
        ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        UserInformation = userInformation;
    }

    /// <summary>The application context object identifier.</summary>
    public TcapObjectIdentifier ApplicationContext { get; }

    /// <summary>The optional user information bytes.</summary>
    public ReadOnlyMemory<byte> UserInformation { get; }

    /// <summary>Encodes the dialogue portion value bytes.</summary>
    /// <returns>The encoded dialogue portion value bytes.</returns>
    public byte[] Encode()
    {
        byte[] result = new byte[512];
        int offset = 0;
        WriteElement(result, ref offset, ObjectIdentifierTag, ApplicationContext.EncodeValue());
        if (!UserInformation.IsEmpty)
        {
            WriteElement(result, ref offset, UserInformationTag, UserInformation.Span);
        }

        Array.Resize(ref result, offset);
        return result;
    }

    /// <summary>Attempts to decode a dialogue portion.</summary>
    /// <param name="data">The encoded dialogue portion value bytes.</param>
    /// <param name="dialoguePortion">The decoded dialogue portion on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapDialoguePortion? dialoguePortion, out string? error)
    {
        dialoguePortion = null;
        error = null;
        if (!TcapBer.TryReadElement(data, out TcapBerElement oidElement, out error))
        {
            return false;
        }

        if (oidElement.Tag.Encode() != ObjectIdentifierTag.Encode())
        {
            error = "TCAP dialogue portion is missing application context";
            return false;
        }

        TcapObjectIdentifier applicationContext = TcapObjectIdentifier.DecodeValue(oidElement.Value.Span);
        ReadOnlyMemory<byte> userInformation = default;
        ReadOnlySpan<byte> remaining = data[oidElement.TotalLength..];
        if (!remaining.IsEmpty)
        {
            if (!TcapBer.TryReadElement(remaining, out TcapBerElement userElement, out error))
            {
                return false;
            }

            if (userElement.Tag.Encode() != UserInformationTag.Encode())
            {
                error = "TCAP dialogue portion user information tag is invalid";
                return false;
            }

            userInformation = userElement.Value;
        }

        dialoguePortion = new(applicationContext, userInformation);
        return true;
    }

    private static void WriteElement(byte[] destination, ref int offset, TcapBerTag tag, ReadOnlySpan<byte> value)
    {
        if (!TcapBer.TryWriteElement(destination.AsSpan(offset), tag, value, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        offset += written;
    }
}
