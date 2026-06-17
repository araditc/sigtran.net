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

    /// <summary>
    /// Builds a Management Error message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="errorCode">The Error Code value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="diagnosticInformation">The optional Diagnostic Information value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildError(
        Span<byte> buffer,
        M3uaErrorCode errorCode,
        ReadOnlySpan<uint> routingContexts,
        uint? networkAppearance,
        ReadOnlySpan<byte> diagnosticInformation,
        out int written,
        out string? error)
    {
        int parameterLength = M3uaParameterWriter.GetPaddedLength(sizeof(uint))
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + (networkAppearance.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalBytesParameterLength(diagnosticInformation);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Management, (byte)M3uaManagementMessageType.Error, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.ErrorCode, (uint)errorCode, out int paramWritten, out error))
        {
            written = 0;
            return false;
        }

        offset += paramWritten;
        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        if (networkAppearance.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.NetworkAppearance, networkAppearance.Value, out paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.DiagnosticInformation, diagnosticInformation, ref offset, out error);
    }

    /// <summary>
    /// Builds a Management Notify message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="statusType">The Status Type value.</param>
    /// <param name="statusInformation">The Status Information value.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildNotify(
        Span<byte> buffer,
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        uint? aspIdentifier,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        int parameterLength = M3uaParameterWriter.GetPaddedLength(sizeof(uint))
                              + (aspIdentifier.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Management, (byte)M3uaManagementMessageType.Notify, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (!TryWriteStatusParameter(buffer.Slice(offset), statusType, statusInformation, out int paramWritten, out error))
        {
            written = 0;
            return false;
        }

        offset += paramWritten;
        if (aspIdentifier.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.AspIdentifier, aspIdentifier.Value, out paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    /// <summary>
    /// Builds a Destination Unavailable SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildDestinationUnavailable(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildCommonSsnmMessage(
            buffer,
            M3uaSsnmMessageType.DestinationUnavailable,
            networkAppearance,
            routingContexts,
            affectedPointCodes,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Destination Available SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildDestinationAvailable(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildCommonSsnmMessage(
            buffer,
            M3uaSsnmMessageType.DestinationAvailable,
            networkAppearance,
            routingContexts,
            affectedPointCodes,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Destination State Audit SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildDestinationStateAudit(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildCommonSsnmMessage(
            buffer,
            M3uaSsnmMessageType.DestinationStateAudit,
            networkAppearance,
            routingContexts,
            affectedPointCodes,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Destination Restricted SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildDestinationRestricted(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildCommonSsnmMessage(
            buffer,
            M3uaSsnmMessageType.DestinationRestricted,
            networkAppearance,
            routingContexts,
            affectedPointCodes,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds a Signalling Congestion SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="concernedDestination">The optional concerned destination point code.</param>
    /// <param name="congestionLevel">The optional Congestion Indications level.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildSignallingCongestion(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        M3uaAffectedPointCode? concernedDestination,
        uint? congestionLevel,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        written = 0;
        if (affectedPointCodes.IsEmpty)
        {
            error = "At least one Affected Point Code is required";
            return false;
        }

        error = null;
        int parameterLength = (networkAppearance.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + M3uaParameterWriter.GetPaddedLength(affectedPointCodes.Length * sizeof(uint))
                              + (concernedDestination.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + (congestionLevel.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Ssnm, (byte)M3uaSsnmMessageType.SignallingCongestion, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (networkAppearance.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.NetworkAppearance, networkAppearance.Value, out int paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        if (!TryWriteAffectedPointCodeParameter(buffer, affectedPointCodes, ref offset, out error))
        {
            written = 0;
            return false;
        }

        if (concernedDestination.HasValue)
        {
            ReadOnlySpan<M3uaAffectedPointCode> concernedDestinations = stackalloc[] { concernedDestination.Value };
            if (!TryWriteAffectedPointCodeParameter(buffer, M3uaParameterTag.ConcernedDestination, concernedDestinations, ref offset, out error))
            {
                written = 0;
                return false;
            }
        }

        if (congestionLevel.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.CongestionIndications, congestionLevel.Value, out int congestionWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += congestionWritten;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    /// <summary>
    /// Builds a Destination User Part Unavailable SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCode">The affected point-code entry. The mask must be zero.</param>
    /// <param name="cause">The unavailability cause.</param>
    /// <param name="userIdentity">The unavailable MTP3-user identity.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildDestinationUserPartUnavailable(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        M3uaAffectedPointCode affectedPointCode,
        M3uaUserPartUnavailableCause cause,
        M3uaMtp3UserIdentity userIdentity,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        written = 0;
        if (affectedPointCode.Mask != 0)
        {
            error = "DUPU Affected Point Code mask must be 0";
            return false;
        }

        int parameterLength = (networkAppearance.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + M3uaParameterWriter.GetPaddedLength(sizeof(uint))
                              + M3uaParameterWriter.GetPaddedLength(sizeof(uint))
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Ssnm, (byte)M3uaSsnmMessageType.DestinationUserPartUnavailable, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (networkAppearance.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.NetworkAppearance, networkAppearance.Value, out int paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes = stackalloc[] { affectedPointCode };
        if (!TryWriteAffectedPointCodeParameter(buffer, affectedPointCodes, ref offset, out error))
        {
            written = 0;
            return false;
        }

        if (!TryWriteUserCauseParameter(buffer.Slice(offset), cause, userIdentity, out int userCauseWritten, out error))
        {
            written = 0;
            return false;
        }

        offset += userCauseWritten;
        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    /// <summary>
    /// Builds an ASP Active message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="trafficModeType">The optional traffic mode type.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspActive(
        Span<byte> buffer,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildTrafficModeRoutingContextInfoMessage(
            buffer,
            (byte)M3uaAsptmMessageType.AspActive,
            trafficModeType,
            routingContexts,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Active acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="trafficModeType">The optional traffic mode type.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspActiveAck(
        Span<byte> buffer,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildTrafficModeRoutingContextInfoMessage(
            buffer,
            (byte)M3uaAsptmMessageType.AspActiveAck,
            trafficModeType,
            routingContexts,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Inactive message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspInactive(
        Span<byte> buffer,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildRoutingContextInfoMessage(
            buffer,
            (byte)M3uaAsptmMessageType.AspInactive,
            routingContexts,
            infoString,
            out written,
            out error);
    }

    /// <summary>
    /// Builds an ASP Inactive acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value encoded as bytes.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildAspInactiveAck(
        Span<byte> buffer,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return BuildRoutingContextInfoMessage(
            buffer,
            (byte)M3uaAsptmMessageType.AspInactiveAck,
            routingContexts,
            infoString,
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

    private static bool BuildTrafficModeRoutingContextInfoMessage(
        Span<byte> buffer,
        byte messageType,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        int parameterLength = (trafficModeType.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Asptm, messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (trafficModeType.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.TrafficModeType, (uint)trafficModeType.Value, out int paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    private static bool BuildCommonSsnmMessage(
        Span<byte> buffer,
        M3uaSsnmMessageType messageType,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        written = 0;
        if (affectedPointCodes.IsEmpty)
        {
            error = "At least one Affected Point Code is required";
            return false;
        }

        error = null;
        int parameterLength = (networkAppearance.HasValue ? M3uaParameterWriter.GetPaddedLength(sizeof(uint)) : 0)
                              + GetOptionalUInt32ListParameterLength(routingContexts)
                              + M3uaParameterWriter.GetPaddedLength(affectedPointCodes.Length * sizeof(uint))
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Ssnm, (byte)messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (networkAppearance.HasValue)
        {
            if (!TryWriteUInt32Parameter(buffer.Slice(offset), M3uaParameterTag.NetworkAppearance, networkAppearance.Value, out int paramWritten, out error))
            {
                written = 0;
                return false;
            }

            offset += paramWritten;
        }

        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        if (!TryWriteAffectedPointCodeParameter(buffer, affectedPointCodes, ref offset, out error))
        {
            written = 0;
            return false;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
    }

    private static bool BuildRoutingContextInfoMessage(
        Span<byte> buffer,
        byte messageType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        int parameterLength = GetOptionalUInt32ListParameterLength(routingContexts)
                              + GetOptionalBytesParameterLength(infoString);
        if (!TryWriteMessageHeader(buffer, M3uaMessageClass.Asptm, messageType, parameterLength, out written, out error))
        {
            return false;
        }

        int offset = M3uaProtocol.HeaderLength;
        if (!TryWriteOptionalUInt32ListParameter(buffer, M3uaParameterTag.RoutingContext, routingContexts, ref offset, out error))
        {
            written = 0;
            return false;
        }

        return TryWriteOptionalBytesParameter(buffer, M3uaParameterTag.InfoString, infoString, ref offset, out error);
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

    private static int GetOptionalUInt32ListParameterLength(ReadOnlySpan<uint> values)
    {
        return values.IsEmpty ? 0 : M3uaParameterWriter.GetPaddedLength(values.Length * sizeof(uint));
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

    private static bool TryWriteStatusParameter(
        Span<byte> buffer,
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        out int written,
        out string? error)
    {
        Span<byte> valueBuffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt16BigEndian(valueBuffer, (ushort)statusType);
        BinaryPrimitives.WriteUInt16BigEndian(valueBuffer.Slice(2, 2), statusInformation);
        return M3uaParameterWriter.TryWrite(buffer, M3uaParameterTag.Status, valueBuffer, out written, out error);
    }

    private static bool TryWriteUserCauseParameter(
        Span<byte> buffer,
        M3uaUserPartUnavailableCause cause,
        M3uaMtp3UserIdentity userIdentity,
        out int written,
        out string? error)
    {
        Span<byte> valueBuffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt16BigEndian(valueBuffer, (ushort)cause);
        BinaryPrimitives.WriteUInt16BigEndian(valueBuffer.Slice(2, 2), (ushort)userIdentity);
        return M3uaParameterWriter.TryWrite(buffer, M3uaParameterTag.UserCause, valueBuffer, out written, out error);
    }

    private static bool TryWriteOptionalUInt32ListParameter(
        Span<byte> buffer,
        M3uaParameterTag tag,
        ReadOnlySpan<uint> values,
        ref int offset,
        out string? error)
    {
        error = null;
        if (values.IsEmpty)
        {
            return true;
        }

        int valueLength = values.Length * sizeof(uint);
        if (!M3uaParameterWriter.TryWriteHeader(buffer.Slice(offset), tag, valueLength, out int written, out error))
        {
            return false;
        }

        Span<byte> valueBuffer = buffer.Slice(offset + M3uaProtocol.ParameterHeaderLength, valueLength);
        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(valueBuffer.Slice(i * sizeof(uint), sizeof(uint)), values[i]);
        }

        offset += written;
        return true;
    }

    private static bool TryWriteAffectedPointCodeParameter(
        Span<byte> buffer,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ref int offset,
        out string? error)
    {
        return TryWriteAffectedPointCodeParameter(buffer, M3uaParameterTag.AffectedPointCode, affectedPointCodes, ref offset, out error);
    }

    private static bool TryWriteAffectedPointCodeParameter(
        Span<byte> buffer,
        M3uaParameterTag tag,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ref int offset,
        out string? error)
    {
        int valueLength = affectedPointCodes.Length * sizeof(uint);
        if (!M3uaParameterWriter.TryWriteHeader(buffer.Slice(offset), tag, valueLength, out int written, out error))
        {
            return false;
        }

        Span<byte> valueBuffer = buffer.Slice(offset + M3uaProtocol.ParameterHeaderLength, valueLength);
        for (int i = 0; i < affectedPointCodes.Length; i++)
        {
            M3uaAffectedPointCode affectedPointCode = affectedPointCodes[i];
            int entryOffset = i * sizeof(uint);
            valueBuffer[entryOffset] = affectedPointCode.Mask;
            valueBuffer[entryOffset + 1] = (byte)((affectedPointCode.PointCode >> 16) & 0xFF);
            valueBuffer[entryOffset + 2] = (byte)((affectedPointCode.PointCode >> 8) & 0xFF);
            valueBuffer[entryOffset + 3] = (byte)(affectedPointCode.PointCode & 0xFF);
        }

        offset += written;
        return true;
    }
}
