using System.Buffers.Binary;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Builds M3UA messages into caller-provided buffers.
/// </summary>
public static class M3uaMessageBuilder
{
    /// <summary>
    /// Constructs a Payload Data message into the provided buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="userPayload">The user payload, usually SCCP or another MTP3-user payload.</param>
    /// <param name="opc">Originating Point Code.</param>
    /// <param name="dpc">Destination Point Code.</param>
    /// <param name="si">Service Indicator, e.g. 3 for SCCP.</param>
    /// <param name="ni">Network Indicator.</param>
    /// <param name="mp">Message Priority.</param>
    /// <param name="sls">Signalling Link Selection.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildPayloadData(
        Span<byte> buffer,
        ReadOnlySpan<byte> userPayload,
        uint opc,
        uint dpc,
        byte si,
        byte ni,
        byte mp,
        byte sls,
        out int written,
        out string? error)
    {
        written = 0;
        error = null;

        int protocolDataValueLength = 12 + userPayload.Length;
        int protocolDataParameterLength = M3uaParameterWriter.GetPaddedLength(protocolDataValueLength);
        int total = M3uaProtocol.HeaderLength + protocolDataParameterLength;

        if (buffer.Length < total)
        {
            error = $"Insufficient buffer size: need {total}, have {buffer.Length}";
            return false;
        }

        buffer[0] = M3uaProtocol.Version;
        buffer[1] = 0;
        buffer[2] = (byte)M3uaMessageClass.Transfer;
        buffer[3] = (byte)M3uaTransferMessageType.PayloadData;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4, 4), (uint)total);

        Span<byte> protocolDataParameter = buffer.Slice(M3uaProtocol.HeaderLength, protocolDataParameterLength);
        if (!M3uaParameterWriter.TryWriteHeader(
                protocolDataParameter,
                M3uaParameterTag.ProtocolData,
                protocolDataValueLength,
                out int parameterWritten,
                out error))
        {
            written = 0;
            return false;
        }

        if (parameterWritten != protocolDataParameterLength)
        {
            written = 0;
            error = $"Unexpected Protocol Data parameter length {parameterWritten}";
            return false;
        }

        Span<byte> protocolData = protocolDataParameter.Slice(M3uaProtocol.ParameterHeaderLength, protocolDataValueLength);
        BinaryPrimitives.WriteUInt32BigEndian(protocolData.Slice(0, 4), opc);
        BinaryPrimitives.WriteUInt32BigEndian(protocolData.Slice(4, 4), dpc);
        protocolData[8] = si;
        protocolData[9] = ni;
        protocolData[10] = mp;
        protocolData[11] = sls;
        userPayload.CopyTo(protocolData.Slice(12));

        written = total;
        return true;
    }
}
