namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Represents an SCCP Long Unitdata message with 16-bit pointers and variable lengths.
/// </summary>
public sealed class SccpLongUnitdataMessage
{
    /// <summary>Creates an SCCP LUDT message.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="hopCounter">The hop counter.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The SCCP user data.</param>
    public SccpLongUnitdataMessage(
        SccpProtocolClass protocolClass,
        byte hopCounter,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData)
    {
        if (hopCounter == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopCounter), "SCCP LUDT hop counter must be positive.");
        }

        if (userData.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(userData), "SCCP LUDT user data must fit in two length octets.");
        }

        ProtocolClass = protocolClass;
        HopCounter = hopCounter;
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        UserData = userData;
    }

    /// <summary>The SCCP protocol class.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <summary>The hop counter.</summary>
    public byte HopCounter { get; }

    /// <summary>The called party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The SCCP user data.</summary>
    public ReadOnlyMemory<byte> UserData { get; }

    /// <summary>Encodes this message as SCCP LUDT bytes.</summary>
    /// <returns>The encoded SCCP LUDT bytes.</returns>
    public byte[] Encode()
    {
        byte[] called = CalledParty.Encode();
        byte[] calling = CallingParty.Encode();
        int totalLength = 11 + 2 + called.Length + 2 + calling.Length + 2 + UserData.Length;
        byte[] result = new byte[totalLength];
        result[0] = (byte)SccpMessageType.LongUnitdata;
        result[1] = ProtocolClass.Encode();
        result[2] = HopCounter;
        WriteUInt16(result.AsSpan(3), 8);
        WriteUInt16(result.AsSpan(5), 8 + called.Length);
        WriteUInt16(result.AsSpan(7), 8 + called.Length + calling.Length);
        WriteUInt16(result.AsSpan(9), 0);

        int offset = 11;
        WriteLongVariable(result, ref offset, called);
        WriteLongVariable(result, ref offset, calling);
        WriteLongVariable(result, ref offset, UserData.Span);
        return result;
    }

    /// <summary>Attempts to decode an SCCP LUDT message.</summary>
    /// <param name="data">The SCCP LUDT bytes.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out SccpLongUnitdataMessage? message, out string? error)
    {
        message = null;
        error = null;
        if (data.Length < 17)
        {
            error = "SCCP LUDT buffer is too short";
            return false;
        }

        if (data[0] != (byte)SccpMessageType.LongUnitdata)
        {
            error = $"Unexpected SCCP message type 0x{data[0]:X2}";
            return false;
        }

        if (data[2] == 0)
        {
            error = "SCCP LUDT hop counter is zero";
            return false;
        }

        if (ReadUInt16(data.Slice(9, 2)) != 0)
        {
            error = "SCCP LUDT optional parameters are not supported by this decoder";
            return false;
        }

        if (!TryReadLongVariable(data, pointerIndex: 3, out ReadOnlySpan<byte> calledBytes, out error)
            || !TryReadLongVariable(data, pointerIndex: 5, out ReadOnlySpan<byte> callingBytes, out error)
            || !TryReadLongVariable(data, pointerIndex: 7, out ReadOnlySpan<byte> userData, out error))
        {
            return false;
        }

        if (!SccpPartyAddress.TryDecode(calledBytes, out SccpPartyAddress? calledParty, out error)
            || !SccpPartyAddress.TryDecode(callingBytes, out SccpPartyAddress? callingParty, out error))
        {
            return false;
        }

        message = new(
            SccpProtocolClass.Decode(data[1]),
            data[2],
            calledParty!,
            callingParty!,
            userData.ToArray());
        return true;
    }

    private static void WriteLongVariable(byte[] destination, ref int offset, ReadOnlySpan<byte> value)
    {
        WriteUInt16(destination.AsSpan(offset), checked((ushort)value.Length));
        offset += 2;
        value.CopyTo(destination.AsSpan(offset));
        offset += value.Length;
    }

    private static bool TryReadLongVariable(ReadOnlySpan<byte> data, int pointerIndex, out ReadOnlySpan<byte> value, out string? error)
    {
        value = default;
        error = null;
        int start = pointerIndex + ReadUInt16(data.Slice(pointerIndex, 2));
        if (start >= data.Length)
        {
            error = "SCCP LUDT variable parameter pointer is outside the message";
            return false;
        }

        int length = ReadUInt16(data.Slice(start, 2));
        if (start + 2 + length > data.Length)
        {
            error = "SCCP LUDT variable parameter length exceeds the message";
            return false;
        }

        value = data.Slice(start + 2, length);
        return true;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> source)
    {
        return (ushort)((source[0] << 8) | source[1]);
    }

    private static void WriteUInt16(Span<byte> destination, int value)
    {
        destination[0] = (byte)(value >> 8);
        destination[1] = (byte)value;
    }
}
