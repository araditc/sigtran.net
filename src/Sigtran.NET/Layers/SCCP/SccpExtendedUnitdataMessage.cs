namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Represents an SCCP Extended Unitdata message with optional segmentation.
/// </summary>
public sealed class SccpExtendedUnitdataMessage
{
    /// <summary>Creates an SCCP XUDT message.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="hopCounter">The hop counter.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The SCCP user data.</param>
    /// <param name="segmentation">The optional segmentation parameter.</param>
    public SccpExtendedUnitdataMessage(
        SccpProtocolClass protocolClass,
        byte hopCounter,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData,
        SccpSegmentationParameter? segmentation = null)
    {
        if (hopCounter is 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(hopCounter), "SCCP XUDT hop counter must be 1..15.");
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
        Segmentation = segmentation;
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

    /// <summary>The optional segmentation parameter.</summary>
    public SccpSegmentationParameter? Segmentation { get; }

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

        int optionalLength = Segmentation.HasValue ? 1 + 1 + SccpSegmentationParameter.EncodedLength + 1 : 0;
        int totalLength = 7 + 1 + called.Length + 1 + calling.Length + 1 + UserData.Length + optionalLength;
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
        result[6] = Segmentation.HasValue
            ? checked((byte)(1 + 1 + called.Length + 1 + calling.Length + 1 + UserData.Length))
            : (byte)0;

        int offset = 7;
        WriteVariable(result, ref offset, called);
        WriteVariable(result, ref offset, calling);
        WriteVariable(result, ref offset, UserData.Span);
        if (Segmentation.HasValue)
        {
            result[offset++] = (byte)SccpOptionalParameterName.Segmentation;
            result[offset++] = SccpSegmentationParameter.EncodedLength;
            Segmentation.Value.Encode(result.AsSpan(offset, SccpSegmentationParameter.EncodedLength));
            offset += SccpSegmentationParameter.EncodedLength;
            result[offset] = (byte)SccpOptionalParameterName.EndOfOptionalParameters;
        }

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

        if (data[2] is 0 or > 15)
        {
            error = "SCCP XUDT hop counter must be 1..15.";
            return false;
        }

        if (!SccpProtocolClass.TryDecodeConnectionless(data[1], out var protocolClass, out error)
            || !SccpConnectionlessLayout.TryRead(data, 7, 3, false, out var calledBytes, out var callingBytes,
                out var userData, out var optional, out error))
        {
            return false;
        }

        if (!SccpPartyAddress.TryDecode(calledBytes, out SccpPartyAddress? calledParty, out error)
            || !SccpPartyAddress.TryDecode(callingBytes, out SccpPartyAddress? callingParty, out error))
        {
            return false;
        }

        SccpSegmentationParameter? segmentation = null;
        if (!optional.IsEmpty && !TryReadOptionalParameters(optional, out segmentation, out error))
        {
            return false;
        }

        message = new(
            protocolClass,
            data[2],
            calledParty!,
            callingParty!,
            userData.ToArray(),
            segmentation);
        return true;
    }

    private static void WriteVariable(byte[] destination, ref int offset, ReadOnlySpan<byte> value)
    {
        destination[offset++] = checked((byte)value.Length);
        value.CopyTo(destination.AsSpan(offset));
        offset += value.Length;
    }

    private static bool TryReadOptionalParameters(
        ReadOnlySpan<byte> data,
        out SccpSegmentationParameter? segmentation,
        out string? error)
    {
        segmentation = null;
        error = null;
        int offset = 0;

        while (offset < data.Length)
        {
            SccpOptionalParameterName name = (SccpOptionalParameterName)data[offset++];
            if (name == SccpOptionalParameterName.EndOfOptionalParameters)
            {
                if (offset == data.Length && offset > 1)
                {
                    return true;
                }

                error = "Invalid SCCP XUDT optional parameter terminator.";
                return false;
            }

            if (offset >= data.Length)
            {
                error = "SCCP XUDT optional parameter is missing length";
                return false;
            }

            int length = data[offset++];
            if (offset + length > data.Length)
            {
                error = "SCCP XUDT optional parameter length exceeds the message";
                return false;
            }

            if (name == SccpOptionalParameterName.Segmentation)
            {
                if (segmentation.HasValue || length != SccpSegmentationParameter.EncodedLength)
                {
                    error = "Invalid or duplicate SCCP segmentation parameter.";
                    return false;
                }

                segmentation = SccpSegmentationParameter.Decode(data.Slice(offset, length));
            }

            offset += length;
        }

        error = "SCCP XUDT optional parameters are missing the end marker";
        return false;
    }
}
