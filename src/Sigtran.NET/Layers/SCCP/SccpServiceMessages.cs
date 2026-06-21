namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// SCCP return causes used by connectionless service messages.
/// </summary>
public enum SccpReturnCause : byte
{
    /// <summary>No translation for an address of this nature.</summary>
    NoTranslationForAddressOfSuchNature = 0x00,

    /// <summary>No translation for this specific address.</summary>
    NoTranslationForThisSpecificAddress = 0x01,

    /// <summary>Subsystem congestion.</summary>
    SubsystemCongestion = 0x02,

    /// <summary>Subsystem failure.</summary>
    SubsystemFailure = 0x03,

    /// <summary>Unequipped user.</summary>
    UnequippedUser = 0x04,

    /// <summary>MTP failure.</summary>
    MtpFailure = 0x05,

    /// <summary>Network congestion.</summary>
    NetworkCongestion = 0x06,

    /// <summary>Unqualified return cause.</summary>
    Unqualified = 0x0F
}

/// <summary>
/// Represents an SCCP Unitdata Service message.
/// </summary>
public sealed class SccpUnitdataServiceMessage
{
    /// <summary>Creates an SCCP UDTS message.</summary>
    /// <param name="returnCause">The return cause.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The returned SCCP user data.</param>
    public SccpUnitdataServiceMessage(
        SccpReturnCause returnCause,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData)
    {
        if (userData.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(userData), "SCCP UDTS user data must fit in one length octet.");
        }

        ReturnCause = returnCause;
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        UserData = userData;
    }

    /// <summary>The return cause.</summary>
    public SccpReturnCause ReturnCause { get; }

    /// <summary>The called party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The returned SCCP user data.</summary>
    public ReadOnlyMemory<byte> UserData { get; }

    /// <summary>Encodes this message as SCCP UDTS bytes.</summary>
    /// <returns>The encoded SCCP UDTS bytes.</returns>
    public byte[] Encode()
    {
        byte[] called = CalledParty.Encode();
        byte[] calling = CallingParty.Encode();
        if (called.Length > byte.MaxValue || calling.Length > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP UDTS addresses must fit in one length octet.");
        }

        int totalLength = 5 + 1 + called.Length + 1 + calling.Length + 1 + UserData.Length;
        if (totalLength > byte.MaxValue)
        {
            throw new InvalidOperationException("SCCP UDTS total length must fit in one message.");
        }

        byte[] result = new byte[totalLength];
        result[0] = (byte)SccpMessageType.UnitdataService;
        result[1] = (byte)ReturnCause;
        result[2] = 3;
        result[3] = checked((byte)(2 + 1 + called.Length));
        result[4] = checked((byte)(1 + 1 + called.Length + 1 + calling.Length));

        int offset = 5;
        WriteVariable(result, ref offset, called);
        WriteVariable(result, ref offset, calling);
        WriteVariable(result, ref offset, UserData.Span);
        return result;
    }

    /// <summary>Attempts to decode an SCCP UDTS message.</summary>
    /// <param name="data">The SCCP UDTS bytes.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out SccpUnitdataServiceMessage? message, out string? error)
    {
        message = null;
        error = null;
        if (data.Length < 8)
        {
            error = "SCCP UDTS buffer is too short";
            return false;
        }

        if (data[0] != (byte)SccpMessageType.UnitdataService)
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
            (SccpReturnCause)data[1],
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
            error = "SCCP UDTS variable parameter pointer is outside the message";
            return false;
        }

        int length = data[start];
        if (start + 1 + length > data.Length)
        {
            error = "SCCP UDTS variable parameter length exceeds the message";
            return false;
        }

        value = data.Slice(start + 1, length);
        return true;
    }
}
