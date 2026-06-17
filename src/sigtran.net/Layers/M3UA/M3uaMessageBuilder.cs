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

    /// <summary>
    /// Builds an ASP Up message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspUp(
        Span<byte> buffer,
        uint? aspIdentifier,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildAspIdentifierAndInfoMessage(
            buffer,
            M3uaMessageClass.Aspsm,
            (byte)M3uaAspsmMessageType.AspUp,
            aspIdentifier,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Up acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspUpAck(
        Span<byte> buffer,
        uint? aspIdentifier,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildAspIdentifierAndInfoMessage(
            buffer,
            M3uaMessageClass.Aspsm,
            (byte)M3uaAspsmMessageType.AspUpAck,
            aspIdentifier,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Down message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspDown(
        Span<byte> buffer,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildInfoOnlyMessage(
            buffer,
            M3uaMessageClass.Aspsm,
            (byte)M3uaAspsmMessageType.AspDown,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Down acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspDownAck(
        Span<byte> buffer,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildInfoOnlyMessage(
            buffer,
            M3uaMessageClass.Aspsm,
            (byte)M3uaAspsmMessageType.AspDownAck,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Heartbeat message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="heartbeatData">The optional Heartbeat Data value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildHeartbeat(
        Span<byte> buffer,
        ReadOnlySpan<byte> heartbeatData,
        out int written,
        out string? error)
    {
        return BuildHeartbeatMessage(
            buffer,
            (byte)M3uaAspsmMessageType.Heartbeat,
            heartbeatData,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Heartbeat acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="heartbeatData">The Heartbeat Data value copied from the received Heartbeat message.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildHeartbeatAck(
        Span<byte> buffer,
        ReadOnlySpan<byte> heartbeatData,
        out int written,
        out string? error)
    {
        return BuildHeartbeatMessage(
            buffer,
            (byte)M3uaAspsmMessageType.HeartbeatAck,
            heartbeatData,
            out written,
            out error);
    }

    private static bool BuildAspIdentifierAndInfoMessage(
        Span<byte> buffer,
        M3uaMessageClass messageClass,
        byte messageType,
        uint? aspIdentifier,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        int parameterLength = (aspIdentifier.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, messageClass, messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (aspIdentifier.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.AspIdentifier, aspIdentifier.Value, out int paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    private static bool BuildInfoOnlyMessage(
        Span<byte> buffer,
        M3uaMessageClass messageClass,
        byte messageType,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        int parameterLength = GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, messageClass, messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    private static bool BuildHeartbeatMessage(
        Span<byte> buffer,
        byte messageType,
        ReadOnlySpan<byte> heartbeatData,
        out int written,
        out string? error)
    {
        int parameterLength = GetOptionalBytesParameterLength(heartbeatData);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Aspsm, messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.HeartbeatData, heartbeatData, ref offset, out error);
    }

    private static bool TryWriteMessageHeader(
        Span<byte> buffer,
        M3uaMessageClass messageClass,
        byte messageType,
        int parameterLength,
        out int written,
        out string? error)
    {
        written = 0;
        error = null;

        int total = M3uaProtocol.HeaderLength + parameterLength;
        if (buffer.Length < total)
        {
            error = $"Insufficient buffer size: need {total}, have {buffer.Length}";
            return false;
        }

        buffer[0] = M3uaProtocol.Version;
        buffer[1] = 0;
        buffer[2] = (byte)messageClass;
        buffer[3] = messageType;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4, 4), (uint)total);
        written = total;
        return true;
    }

    private static int GetOptionalBytesParameterLength(ReadOnlySpan<byte> value)
    {
        return value.IsEmpty ? 0 : M3uaParameterWriter.GetPaddedLength(value.Length);
    }

    private static bool TryWriteOptionalBytesParameter(
        Span<byte> buffer,
        M3uaParameterTag tag,
        ReadOnlySpan<byte> value,
        ref int offset,
        out string? error)
    {
        error = null;
        if (value.IsEmpty)
        {
            return true;
        }

        if (!M3uaParameterWriter.TryWrite(buffer.Slice(offset), tag, value, out int written, out error))
        {
            return false;
        }

        offset += written;
        return true;
    }

    private static bool TryWriteUInt32Parameter(
        Span<byte> buffer,
        M3uaParameterTag tag,
        uint value,
        out int written,
        out string? error)
    {
        Span<byte> valueBuffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(valueBuffer, value);
        return M3uaParameterWriter.TryWrite(buffer, tag, valueBuffer, out written, out error);
    }
}
