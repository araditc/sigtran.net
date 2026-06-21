namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Represents an SCCP Unitdata message with Q.713-style variable parameter pointers.
/// </summary>
public sealed class SccpUnitdataMessage
{
    /// <summary>Creates an SCCP Unitdata message.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The SCCP user data.</param>
    public SccpUnitdataMessage(
        SccpProtocolClass protocolClass,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData)
    {
        if (userData.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(userData), "SCCP UDT user data must fit in one length octet.");
        }

        ProtocolClass = protocolClass;
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        UserData = userData;
    }

    /// <summary>The SCCP protocol class.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <summary>The called party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The SCCP user data.</summary>
    public ReadOnlyMemory<byte> UserData { get; }

    /// <summary>Encodes this message as SCCP UDT bytes.</summary>
    /// <returns>The encoded SCCP UDT bytes.</returns>
    public byte[] Encode()
    {
        byte[] called = CalledParty.Encode();
        byte[] calling = CallingParty.Encode();
        if (called.Length > byte.MaxValue || calling.Length > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP UDT addresses must fit in one length octet.");
        }

        int totalLength = 5 + 1 + called.Length + 1 + calling.Length + 1 + UserData.Length;
        if (totalLength > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP UDT total length must fit in one message.");
        }

        byte[] result = new byte[totalLength];
        result[0] = (byte)SccpMessageType.Unitdata;
        result[1] = ProtocolClass.Encode();
        result[2] = 3;
        result[3] = checked((byte)(2 + 1 + called.Length));
        result[4] = checked((byte)(1 + 1 + called.Length + 1 + calling.Length));

        int offset = 5;
        WriteVariable(result, ref offset, called);
        WriteVariable(result, ref offset, calling);
        WriteVariable(result, ref offset, UserData.Span);
        return result;
    }

    /// <summary>Attempts to decode an SCCP UDT message.</summary>
    /// <param name="data">The SCCP UDT bytes.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out SccpUnitdataMessage? message, out string? error)
    {
        message = null;
        error = null;
        if (data.Length < 8)
        {
            error = "SCCP UDT buffer is too short";
            return false;
        }

        if (data[0] != (byte)SccpMessageType.Unitdata)
        {
            error = $"Unexpected SCCP message type 0x{data[0]:X2}";
            return false;
        }

        if (!TryReadVariable(data, pointerIndex: 2, out ReadOnlySpan<byte> calledBytes, out error)
            || !TryReadVariable(data, pointerIndex: 3, out ReadOnlySpan<byte> callingBytes, out error)
            || !TryReadVariable(data, pointerIndex: 4, out ReadOnlySpan<byte> userData, out error))
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
            calledParty!,
            callingParty!,
            userData.ToArray());
        return true;
    }

    private static void WriteVariable(byte[] destination, ref int offset, ReadOnlySpan<byte> value)
    {
        destination[offset++] = checked((byte)value.Length);
        value.CopyTo(destination.AsSpan(offset));
        offset += value.Length;
    }

    private static bool TryReadVariable(ReadOnlySpan<byte> data, int pointerIndex, out ReadOnlySpan<byte> value, out string? error)
    {
        value = default;
        error = null;
        int start = pointerIndex + data[pointerIndex];
        if (start >= data.Length)
        {
            error = "SCCP UDT variable parameter pointer is outside the message";
            return false;
        }

        int length = data[start];
        if (start + 1 + length > data.Length)
        {
            error = "SCCP UDT variable parameter length exceeds the message";
            return false;
        }

        value = data.Slice(start + 1, length);
        return true;
    }
}
