using System.Buffers.Binary;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Represents an SCCP Long Unitdata message with 16-bit pointers and a 16-bit data length.
/// </summary>
public sealed class SccpLongUnitdataMessage
{
    private const int MaximumUserData = 3952;
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
        if (hopCounter is 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(hopCounter), "SCCP LUDT hop counter must be 1..15.");
        }

        if (userData.IsEmpty || userData.Length > MaximumUserData)
        {
            throw new ArgumentOutOfRangeException(nameof(userData), "SCCP LUDT user data must be 1..3952 octets.");
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
        int totalLength = 11 + 1 + called.Length + 1 + calling.Length + 2 + UserData.Length;
        byte[] result = new byte[totalLength];
        result[0] = (byte)SccpMessageType.LongUnitdata;
        result[1] = ProtocolClass.Encode();
        result[2] = HopCounter;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(3), 7);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(5), checked((ushort)(6 + called.Length)));
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(7), checked((ushort)(5 + called.Length + calling.Length)));

        int offset = 11;
        result[offset++] = checked((byte)called.Length);
        called.CopyTo(result.AsSpan(offset));
        offset += called.Length;
        result[offset++] = checked((byte)calling.Length);
        calling.CopyTo(result.AsSpan(offset));
        offset += calling.Length;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset), (ushort)UserData.Length);
        UserData.Span.CopyTo(result.AsSpan(offset + 2));
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

        if (data[2] is 0 or > 15)
        {
            error = "SCCP LUDT hop counter must be 1..15.";
            return false;
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(data[9..]) != 0)
        {
            error = "SCCP LUDT optional parameters are not supported by this decoder";
            return false;
        }

        if (!SccpProtocolClass.TryDecodeConnectionless(data[1], out var protocolClass, out error)
            || !SccpConnectionlessLayout.TryRead(data, 11, 3, true, out var calledBytes, out var callingBytes,
                out var userData, out _, out error))
        {
            return false;
        }

        if (userData.Length > MaximumUserData)
        {
            error = "SCCP LUDT payload exceeds 3952 octets.";
            return false;
        }

        if (!SccpPartyAddress.TryDecode(calledBytes, out SccpPartyAddress? calledParty, out error)
            || !SccpPartyAddress.TryDecode(callingBytes, out SccpPartyAddress? callingParty, out error))
        {
            return false;
        }

        message = new(
            protocolClass,
            data[2],
            calledParty!,
            callingParty!,
            userData.ToArray());
        return true;
    }

}
