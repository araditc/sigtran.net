namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Builds outbound M3UA messages with association defaults and optional ASP state policy.
/// </summary>
public sealed class M3uaOutboundProcessor
{
    /// <summary>Creates an outbound M3UA processor.</summary>
    /// <param name="aspSession">The ASP session used for optional DATA state checks.</param>
    /// <param name="networkAppearance">The default Network Appearance value for outbound DATA.</param>
    /// <param name="routingContext">The default Routing Context value for outbound DATA and ASPTM messages.</param>
    /// <param name="requireActiveAspForPayload">Whether DATA building should fail unless the ASP session is active.</param>
    public M3uaOutboundProcessor(
        M3uaAspSession? aspSession = null,
        uint? networkAppearance = null,
        uint? routingContext = null,
        bool requireActiveAspForPayload = false)
    {
        AspSession = aspSession ?? new M3uaAspSession();
        NetworkAppearance = networkAppearance;
        RoutingContext = routingContext;
        RequireActiveAspForPayload = requireActiveAspForPayload;
    }

    /// <summary>The ASP session used for optional DATA state checks.</summary>
    public M3uaAspSession AspSession { get; }

    /// <summary>The default Network Appearance value for outbound DATA.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The default Routing Context value for outbound DATA and ASPTM messages.</summary>
    public uint? RoutingContext { get; }

    /// <summary>Whether DATA building should fail unless the ASP session is active.</summary>
    public bool RequireActiveAspForPayload { get; }

    /// <summary>
    /// Builds an ASP Up message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildAspUp(
        Span<byte> buffer,
        uint? aspIdentifier,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildAspUp(buffer, aspIdentifier, infoString, out written, out error);
    }

    /// <summary>
    /// Builds an ASP Down message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildAspDown(
        Span<byte> buffer,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildAspDown(buffer, infoString, out written, out error);
    }

    /// <summary>
    /// Builds an ASP Active message using the configured default Routing Context when present.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="trafficModeType">The optional Traffic Mode Type value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildAspActive(
        Span<byte> buffer,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        Span<uint> routingContexts = stackalloc uint[1];
        ReadOnlySpan<uint> contextValues = GetRoutingContextSpan(routingContexts);
        return M3uaMessageBuilder.BuildAspActive(buffer, trafficModeType, contextValues, infoString, out written, out error);
    }

    /// <summary>
    /// Builds an ASP Inactive message using the configured default Routing Context when present.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildAspInactive(
        Span<byte> buffer,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        Span<uint> routingContexts = stackalloc uint[1];
        ReadOnlySpan<uint> contextValues = GetRoutingContextSpan(routingContexts);
        return M3uaMessageBuilder.BuildAspInactive(buffer, contextValues, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Heartbeat message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="heartbeatData">The optional Heartbeat Data value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildHeartbeat(
        Span<byte> buffer,
        ReadOnlySpan<byte> heartbeatData,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildHeartbeat(buffer, heartbeatData, out written, out error);
    }

    /// <summary>
    /// Builds a Heartbeat acknowledgement message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="heartbeatData">The Heartbeat Data value copied from the received Heartbeat message.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildHeartbeatAck(
        Span<byte> buffer,
        ReadOnlySpan<byte> heartbeatData,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildHeartbeatAck(buffer, heartbeatData, out written, out error);
    }

    /// <summary>
    /// Builds an RKM Registration Request message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="routingKeys">The Routing Key entries to register.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildRegistrationRequest(
        Span<byte> buffer,
        ReadOnlySpan<M3uaRoutingKey> routingKeys,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildRegistrationRequest(buffer, routingKeys, out written, out error);
    }

    /// <summary>
    /// Builds an RKM Deregistration Request message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="routingContexts">The Routing Context values to deregister.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDeregistrationRequest(
        Span<byte> buffer,
        ReadOnlySpan<uint> routingContexts,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildDeregistrationRequest(buffer, routingContexts, out written, out error);
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
    public bool TryBuildError(
        Span<byte> buffer,
        M3uaErrorCode errorCode,
        ReadOnlySpan<uint> routingContexts,
        uint? networkAppearance,
        ReadOnlySpan<byte> diagnosticInformation,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildError(buffer, errorCode, routingContexts, networkAppearance, diagnosticInformation, out written, out error);
    }

    /// <summary>
    /// Builds a Management Notify message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="statusType">The Status Type value.</param>
    /// <param name="statusInformation">The Status Information value.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildNotify(
        Span<byte> buffer,
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        uint? aspIdentifier,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildNotify(buffer, statusType, statusInformation, aspIdentifier, routingContexts, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Destination Unavailable SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDestinationUnavailable(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildDestinationUnavailable(buffer, networkAppearance, routingContexts, affectedPointCodes, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Destination Available SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDestinationAvailable(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildDestinationAvailable(buffer, networkAppearance, routingContexts, affectedPointCodes, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Destination State Audit SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDestinationStateAudit(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildDestinationStateAudit(buffer, networkAppearance, routingContexts, affectedPointCodes, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Destination Restricted SSNM message.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDestinationRestricted(
        Span<byte> buffer,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlySpan<byte> infoString,
        out int written,
        out string? error)
    {
        return M3uaMessageBuilder.BuildDestinationRestricted(buffer, networkAppearance, routingContexts, affectedPointCodes, infoString, out written, out error);
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
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildSignallingCongestion(
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
        return M3uaMessageBuilder.BuildSignallingCongestion(buffer, networkAppearance, routingContexts, affectedPointCodes, concernedDestination, congestionLevel, infoString, out written, out error);
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
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildDestinationUserPartUnavailable(
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
        return M3uaMessageBuilder.BuildDestinationUserPartUnavailable(buffer, networkAppearance, routingContexts, affectedPointCode, cause, userIdentity, infoString, out written, out error);
    }

    /// <summary>
    /// Builds a Payload Data message using configured defaults unless explicit values are supplied.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="userPayload">The MTP3-user payload.</param>
    /// <param name="originatingPointCode">The Originating Point Code value.</param>
    /// <param name="destinationPointCode">The Destination Point Code value.</param>
    /// <param name="serviceIndicator">The Service Indicator value.</param>
    /// <param name="networkIndicator">The Network Indicator value.</param>
    /// <param name="messagePriority">The Message Priority value.</param>
    /// <param name="signallingLinkSelection">The Signalling Link Selection value.</param>
    /// <param name="networkAppearance">The optional explicit Network Appearance value.</param>
    /// <param name="routingContext">The optional explicit Routing Context value.</param>
    /// <param name="correlationId">The optional Correlation Id value.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the message cannot be built.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public bool TryBuildPayloadData(
        Span<byte> buffer,
        ReadOnlySpan<byte> userPayload,
        uint originatingPointCode,
        uint destinationPointCode,
        byte serviceIndicator,
        byte networkIndicator,
        byte messagePriority,
        byte signallingLinkSelection,
        uint? networkAppearance,
        uint? routingContext,
        uint? correlationId,
        out int written,
        out string? error)
    {
        written = 0;
        if (RequireActiveAspForPayload && AspSession.State != M3uaAspState.Active)
        {
            error = $"Payload Data cannot be built while ASP is {AspSession.State}";
            return false;
        }

        return M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload,
            originatingPointCode,
            destinationPointCode,
            serviceIndicator,
            networkIndicator,
            messagePriority,
            signallingLinkSelection,
            networkAppearance ?? NetworkAppearance,
            routingContext ?? RoutingContext,
            correlationId,
            out written,
            out error);
    }

    private ReadOnlySpan<uint> GetRoutingContextSpan(Span<uint> scratch)
    {
        if (!RoutingContext.HasValue)
        {
            return ReadOnlySpan<uint>.Empty;
        }

        scratch[0] = RoutingContext.Value;
        return scratch.Slice(0, 1);
    }
}
