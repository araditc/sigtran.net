namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Represents an SCCP Extended Unitdata message without optional parameters.
/// </summary>
public sealed class SccpExtendedUnitdataMessage
{
    /// <summary>Creates an SCCP XUDT message.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="hopCounter">The hop counter.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The SCCP user data.</param>
    public SccpExtendedUnitdataMessage(
        SccpProtocolClass protocolClass,
        byte hopCounter,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData)
    {
        if (hopCounter == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopCounter), "SCCP XUDT hop counter must be positive.");
        }

        if (userData.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(userData), "SCCP XUDT user data must fit in one length octet.");
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

    /// <summary>Encodes this message as SCCP XUDT bytes.</summary>
    /// <returns>The encoded SCCP XUDT bytes.</returns>
    public byte[] Encode()
    {
        byte[] called = CalledParty.Encode();
        byte[] calling = CallingParty.Encode();
        if (called.Length > byte.MaxValue || calling.Length > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP XUDT addresses must fit in one length octet.");
        }

        int totalLength = 7 + 1 + called.Length + 1 + calling.Length + 1 + UserData.Length;
        if (totalLength > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP XUDT total length must fit in one message.");
        }

        byte[] result = new byte[totalLength];
        result[0] = (byte)SccpMessageType.ExtendedUnitdata;
        result[1] = ProtocolClass.Encode();
        result[2] = HopCounter;
        result[3] = 4;
        result[4] = checked((byte)(3 + 1 + called.Length));
        result[5] = checked((byte)(2 + 1 + called.Length + 1 + calling.Length));
        result[6] = 0;

        int offset = 7;
        WriteVariable(result, ref offset, called);
        WriteVariable(result, ref offset, calling);
        WriteVariable(result, ref offset, UserData.Span);
        return result;
    }

    /// <summary>Attempts to decode an SCCP XUDT message.</summary>
    /// <param name="data">The SCCP XUDT bytes.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out SccpExtendedUnitdataMessage? message, out string? error)
    {
        message = null;
        error = null;
        if (data.Length < 10)
        {
            error = "SCCP XUDT buffer is too short";
            return false;
        }

        if (data[0] != (byte)SccpMessageType.ExtendedUnitdata)
        {
            error = $"Unexpected SCCP message type 0x{data[0]:X2}";
            return false;
        }

        if (data[2] == 0)
        {
            error = "SCCP XUDT hop counter is zero";
            return false;
        }

        if (data[6] != 0)
        {
            error = "SCCP XUDT optional parameters are not supported by this decoder";
            return false;
        }

        if (!TryReadVariable(data, pointerIndex: 3, out ReadOnlySpan<byte> calledBytes, out error)
            || !TryReadVariable(data, pointerIndex: 4, out ReadOnlySpan<byte> callingBytes, out error)
            || !TryReadVariable(data, pointerIndex: 5, out ReadOnlySpan<byte> userData, out error))
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
            error = "SCCP XUDT variable parameter pointer is outside the message";
            return false;
        }

        int length = data[start];
        if (start + 1 + length > data.Length)
        {
            error = "SCCP XUDT variable parameter length exceeds the message";
            return false;
        }

        value = data.Slice(start + 1, length);
        return true;
    }
}
