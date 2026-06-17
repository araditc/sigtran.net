using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents a typed ASP State Maintenance message decoded from M3UA.
/// </summary>
public sealed class M3uaAspStateMaintenanceMessage
{
    /// <summary>Creates a typed ASP State Maintenance message.</summary>
    /// <param name="messageType">The ASP State Maintenance message type.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="heartbeatData">The optional Heartbeat Data value.</param>
    public M3uaAspStateMaintenanceMessage(
        M3uaAspsmMessageType messageType,
        uint? aspIdentifier,
        ReadOnlyMemory<byte> infoString,
        ReadOnlyMemory<byte> heartbeatData)
    {
        MessageType = messageType;
        AspIdentifier = aspIdentifier;
        InfoString = infoString;
        HeartbeatData = heartbeatData;
    }

    /// <summary>The ASP State Maintenance message type.</summary>
    public M3uaAspsmMessageType MessageType { get; }

    /// <summary>The optional ASP Identifier value.</summary>
    public uint? AspIdentifier { get; }

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }

    /// <summary>The optional Heartbeat Data value.</summary>
    public ReadOnlyMemory<byte> HeartbeatData { get; }
}

/// <summary>
/// Represents a typed ASP Traffic Maintenance message decoded from M3UA.
/// </summary>
public sealed class M3uaAspTrafficMaintenanceMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed ASP Traffic Maintenance message.</summary>
    /// <param name="messageType">The ASP Traffic Maintenance message type.</param>
    /// <param name="trafficModeType">The optional Traffic Mode Type value.</param>
    /// <param name="routingContexts">The decoded Routing Context values.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaAspTrafficMaintenanceMessage(
        M3uaAsptmMessageType messageType,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlyMemory<byte> infoString)
    {
        MessageType = messageType;
        TrafficModeType = trafficModeType;
        _routingContexts = routingContexts.ToArray();
        InfoString = infoString;
    }

    /// <summary>The ASP Traffic Maintenance message type.</summary>
    public M3uaAsptmMessageType MessageType { get; }

    /// <summary>The optional Traffic Mode Type value.</summary>
    public M3uaTrafficModeType? TrafficModeType { get; }

    /// <summary>The decoded Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Represents a typed M3UA Payload Data message.
/// </summary>
public sealed class M3uaPayloadDataMessage
{
    private readonly byte[] _userPayload;

    /// <summary>Creates a typed Payload Data message.</summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContext">The optional Routing Context value.</param>
    /// <param name="originatingPointCode">The Originating Point Code value.</param>
    /// <param name="destinationPointCode">The Destination Point Code value.</param>
    /// <param name="serviceIndicator">The Service Indicator value.</param>
    /// <param name="networkIndicator">The Network Indicator value.</param>
    /// <param name="messagePriority">The Message Priority value.</param>
    /// <param name="signallingLinkSelection">The Signalling Link Selection value.</param>
    /// <param name="userPayload">The MTP3-user payload following the routing label fields.</param>
    /// <param name="correlationId">The optional Correlation Id value.</param>
    public M3uaPayloadDataMessage(
        uint? networkAppearance,
        uint? routingContext,
        uint originatingPointCode,
        uint destinationPointCode,
        byte serviceIndicator,
        byte networkIndicator,
        byte messagePriority,
        byte signallingLinkSelection,
        ReadOnlySpan<byte> userPayload,
        uint? correlationId)
    {
        NetworkAppearance = networkAppearance;
        RoutingContext = routingContext;
        OriginatingPointCode = originatingPointCode;
        DestinationPointCode = destinationPointCode;
        ServiceIndicator = serviceIndicator;
        NetworkIndicator = networkIndicator;
        MessagePriority = messagePriority;
        SignallingLinkSelection = signallingLinkSelection;
        _userPayload = userPayload.ToArray();
        CorrelationId = correlationId;
    }

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Routing Context value.</summary>
    public uint? RoutingContext { get; }

    /// <summary>The Originating Point Code value.</summary>
    public uint OriginatingPointCode { get; }

    /// <summary>The Destination Point Code value.</summary>
    public uint DestinationPointCode { get; }

    /// <summary>The Service Indicator value.</summary>
    public byte ServiceIndicator { get; }

    /// <summary>The Network Indicator value.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>The Message Priority value.</summary>
    public byte MessagePriority { get; }

    /// <summary>The Signalling Link Selection value.</summary>
    public byte SignallingLinkSelection { get; }

    /// <summary>The MTP3-user payload following the routing label fields.</summary>
    public ReadOnlySpan<byte> UserPayload => _userPayload;

    /// <summary>The optional Correlation Id value.</summary>
    public uint? CorrelationId { get; }
}

/// <summary>
/// Represents a typed M3UA Error management message.
/// </summary>
public sealed class M3uaErrorMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed M3UA Error management message.</summary>
    /// <param name="errorCode">The decoded Error Code value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="diagnosticInformation">The optional Diagnostic Information value.</param>
    public M3uaErrorMessage(
        M3uaErrorCode errorCode,
        ReadOnlySpan<uint> routingContexts,
        uint? networkAppearance,
        ReadOnlyMemory<byte> diagnosticInformation)
    {
        ErrorCode = errorCode;
        _routingContexts = routingContexts.ToArray();
        NetworkAppearance = networkAppearance;
        DiagnosticInformation = diagnosticInformation;
    }

    /// <summary>The decoded Error Code value.</summary>
    public M3uaErrorCode ErrorCode { get; }

    /// <summary>The optional Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Diagnostic Information value.</summary>
    public ReadOnlyMemory<byte> DiagnosticInformation { get; }
}

/// <summary>
/// Represents a typed M3UA Notify management message.
/// </summary>
public sealed class M3uaNotifyMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed M3UA Notify management message.</summary>
    /// <param name="statusType">The decoded Status Type value.</param>
    /// <param name="statusInformation">The decoded Status Information value.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaNotifyMessage(
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        uint? aspIdentifier,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlyMemory<byte> infoString)
    {
        StatusType = statusType;
        StatusInformation = statusInformation;
        AspIdentifier = aspIdentifier;
        _routingContexts = routingContexts.ToArray();
        InfoString = infoString;
    }

    /// <summary>The decoded Status Type value.</summary>
    public M3uaNotifyStatusType StatusType { get; }

    /// <summary>The decoded Status Information value.</summary>
    public ushort StatusInformation { get; }

    /// <summary>The optional ASP Identifier value.</summary>
    public uint? AspIdentifier { get; }

    /// <summary>The optional Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Represents a single Affected Point Code entry.
/// </summary>
public readonly struct M3uaAffectedPointCode
{
    /// <summary>Creates an Affected Point Code entry.</summary>
    /// <param name="mask">The affected point-code mask.</param>
    /// <param name="pointCode">The affected point code, encoded in the low 24 bits.</param>
    public M3uaAffectedPointCode(byte mask, uint pointCode)
    {
        Mask = mask;
        PointCode = pointCode & 0x00FF_FFFF;
    }

    /// <summary>The affected point-code mask.</summary>
    public byte Mask { get; }

    /// <summary>The affected point code, encoded in the low 24 bits.</summary>
    public uint PointCode { get; }
}

/// <summary>
/// Represents a typed SS7 Signalling Network Management message.
/// </summary>
public sealed class M3uaSsnmMessage
{
    private readonly uint[] _routingContexts;
    private readonly M3uaAffectedPointCode[] _affectedPointCodes;

    /// <summary>Creates a typed SSNM message.</summary>
    /// <param name="messageType">The SSNM message type.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaSsnmMessage(
        M3uaSsnmMessageType messageType,
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        ReadOnlyMemory<byte> infoString)
    {
        MessageType = messageType;
        NetworkAppearance = networkAppearance;
        _routingContexts = routingContexts.ToArray();
        _affectedPointCodes = affectedPointCodes.ToArray();
        InfoString = infoString;
    }

    /// <summary>The SSNM message type.</summary>
    public M3uaSsnmMessageType MessageType { get; }

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The affected point-code entries.</summary>
    public ReadOnlySpan<M3uaAffectedPointCode> AffectedPointCodes => _affectedPointCodes;

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Represents a typed Signalling Congestion message.
/// </summary>
public sealed class M3uaSignallingCongestionMessage
{
    private readonly uint[] _routingContexts;
    private readonly M3uaAffectedPointCode[] _affectedPointCodes;

    /// <summary>Creates a typed Signalling Congestion message.</summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCodes">The affected point-code entries.</param>
    /// <param name="concernedDestination">The optional concerned destination point code.</param>
    /// <param name="congestionLevel">The optional Congestion Indications level.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaSignallingCongestionMessage(
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlySpan<M3uaAffectedPointCode> affectedPointCodes,
        M3uaAffectedPointCode? concernedDestination,
        uint? congestionLevel,
        ReadOnlyMemory<byte> infoString)
    {
        NetworkAppearance = networkAppearance;
        _routingContexts = routingContexts.ToArray();
        _affectedPointCodes = affectedPointCodes.ToArray();
        ConcernedDestination = concernedDestination;
        CongestionLevel = congestionLevel;
        InfoString = infoString;
    }

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The affected point-code entries.</summary>
    public ReadOnlySpan<M3uaAffectedPointCode> AffectedPointCodes => _affectedPointCodes;

    /// <summary>The optional concerned destination point code.</summary>
    public M3uaAffectedPointCode? ConcernedDestination { get; }

    /// <summary>The optional Congestion Indications level.</summary>
    public uint? CongestionLevel { get; }

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Represents a typed Destination User Part Unavailable message.
/// </summary>
public sealed class M3uaDestinationUserPartUnavailableMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed Destination User Part Unavailable message.</summary>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="routingContexts">The optional Routing Context values.</param>
    /// <param name="affectedPointCode">The affected destination point code.</param>
    /// <param name="cause">The unavailability cause.</param>
    /// <param name="userIdentity">The unavailable MTP3-user identity.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaDestinationUserPartUnavailableMessage(
        uint? networkAppearance,
        ReadOnlySpan<uint> routingContexts,
        M3uaAffectedPointCode affectedPointCode,
        M3uaUserPartUnavailableCause cause,
        M3uaMtp3UserIdentity userIdentity,
        ReadOnlyMemory<byte> infoString)
    {
        NetworkAppearance = networkAppearance;
        _routingContexts = routingContexts.ToArray();
        AffectedPointCode = affectedPointCode;
        Cause = cause;
        UserIdentity = userIdentity;
        InfoString = infoString;
    }

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The affected destination point code.</summary>
    public M3uaAffectedPointCode AffectedPointCode { get; }

    /// <summary>The unavailability cause.</summary>
    public M3uaUserPartUnavailableCause Cause { get; }

    /// <summary>The unavailable MTP3-user identity.</summary>
    public M3uaMtp3UserIdentity UserIdentity { get; }

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Represents a Routing Key carried in an RKM Registration Request.
/// </summary>
public sealed class M3uaRoutingKey
{
    private readonly M3uaAffectedPointCode[] _destinationPointCodes;
    private readonly byte[] _serviceIndicators;
    private readonly M3uaAffectedPointCode[] _originatingPointCodes;

    /// <summary>Creates a Routing Key model.</summary>
    /// <param name="localRoutingKeyIdentifier">The Local-RK-Identifier used to correlate the Registration Response.</param>
    /// <param name="routingContext">The optional Routing Context value.</param>
    /// <param name="trafficModeType">The optional Traffic Mode Type value.</param>
    /// <param name="destinationPointCodes">The mandatory Destination Point Code entries.</param>
    /// <param name="networkAppearance">The optional Network Appearance value.</param>
    /// <param name="serviceIndicators">The optional Service Indicator values.</param>
    /// <param name="originatingPointCodes">The optional Originating Point Code entries.</param>
    public M3uaRoutingKey(
        uint localRoutingKeyIdentifier,
        uint? routingContext,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<M3uaAffectedPointCode> destinationPointCodes,
        uint? networkAppearance,
        ReadOnlySpan<byte> serviceIndicators,
        ReadOnlySpan<M3uaAffectedPointCode> originatingPointCodes)
    {
        LocalRoutingKeyIdentifier = localRoutingKeyIdentifier;
        RoutingContext = routingContext;
        TrafficModeType = trafficModeType;
        _destinationPointCodes = destinationPointCodes.ToArray();
        NetworkAppearance = networkAppearance;
        _serviceIndicators = serviceIndicators.ToArray();
        _originatingPointCodes = originatingPointCodes.ToArray();
    }

    /// <summary>The Local-RK-Identifier used to correlate the Registration Response.</summary>
    public uint LocalRoutingKeyIdentifier { get; }

    /// <summary>The optional Routing Context value.</summary>
    public uint? RoutingContext { get; }

    /// <summary>The optional Traffic Mode Type value.</summary>
    public M3uaTrafficModeType? TrafficModeType { get; }

    /// <summary>The mandatory Destination Point Code entries.</summary>
    public ReadOnlySpan<M3uaAffectedPointCode> DestinationPointCodes => _destinationPointCodes;

    /// <summary>The optional Network Appearance value.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional Service Indicator values.</summary>
    public ReadOnlySpan<byte> ServiceIndicators => _serviceIndicators;

    /// <summary>The optional Originating Point Code entries.</summary>
    public ReadOnlySpan<M3uaAffectedPointCode> OriginatingPointCodes => _originatingPointCodes;
}

/// <summary>
/// Represents a typed RKM Registration Request message.
/// </summary>
public sealed class M3uaRegistrationRequestMessage
{
    private readonly M3uaRoutingKey[] _routingKeys;

    /// <summary>Creates a typed Registration Request message.</summary>
    /// <param name="routingKeys">The Routing Key entries to register.</param>
    public M3uaRegistrationRequestMessage(ReadOnlySpan<M3uaRoutingKey> routingKeys)
    {
        _routingKeys = routingKeys.ToArray();
    }

    /// <summary>The Routing Key entries to register.</summary>
    public ReadOnlySpan<M3uaRoutingKey> RoutingKeys => _routingKeys;
}

/// <summary>
/// Represents one Registration Result entry from an RKM Registration Response.
/// </summary>
public readonly struct M3uaRegistrationResult
{
    /// <summary>Creates a Registration Result entry.</summary>
    /// <param name="localRoutingKeyIdentifier">The Local-RK-Identifier from the matching request.</param>
    /// <param name="status">The registration status.</param>
    /// <param name="routingContext">The assigned Routing Context, or zero if registration failed.</param>
    public M3uaRegistrationResult(uint localRoutingKeyIdentifier, M3uaRegistrationStatus status, uint routingContext)
    {
        LocalRoutingKeyIdentifier = localRoutingKeyIdentifier;
        Status = status;
        RoutingContext = routingContext;
    }

    /// <summary>The Local-RK-Identifier from the matching request.</summary>
    public uint LocalRoutingKeyIdentifier { get; }

    /// <summary>The registration status.</summary>
    public M3uaRegistrationStatus Status { get; }

    /// <summary>The assigned Routing Context, or zero if registration failed.</summary>
    public uint RoutingContext { get; }

    /// <summary>Whether the registration result status indicates success.</summary>
    public bool IsSuccess => Status == M3uaRegistrationStatus.SuccessfullyRegistered;
}

/// <summary>
/// Represents a typed RKM Registration Response message.
/// </summary>
public sealed class M3uaRegistrationResponseMessage
{
    private readonly M3uaRegistrationResult[] _results;

    /// <summary>Creates a typed Registration Response message.</summary>
    /// <param name="results">The Registration Result entries.</param>
    public M3uaRegistrationResponseMessage(ReadOnlySpan<M3uaRegistrationResult> results)
    {
        _results = results.ToArray();
    }

    /// <summary>The Registration Result entries.</summary>
    public ReadOnlySpan<M3uaRegistrationResult> Results => _results;

    /// <summary>Whether every registration result indicates success.</summary>
    public bool AllSuccessful
    {
        get
        {
            for (int i = 0; i < _results.Length; i++)
            {
                if (!_results[i].IsSuccess)
                {
                    return false;
                }
            }

            return _results.Length > 0;
        }
    }

    /// <summary>
    /// Finds a Registration Result by Local-RK-Identifier.
    /// </summary>
    /// <param name="localRoutingKeyIdentifier">The Local-RK-Identifier to find.</param>
    /// <param name="result">The matching result on success.</param>
    /// <returns>True if a matching result was found; otherwise false.</returns>
    public bool TryFindResult(uint localRoutingKeyIdentifier, out M3uaRegistrationResult result)
    {
        for (int i = 0; i < _results.Length; i++)
        {
            if (_results[i].LocalRoutingKeyIdentifier == localRoutingKeyIdentifier)
            {
                result = _results[i];
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Finds the assigned Routing Context for a successfully registered Local-RK-Identifier.
    /// </summary>
    /// <param name="localRoutingKeyIdentifier">The Local-RK-Identifier to find.</param>
    /// <param name="routingContext">The assigned Routing Context on success.</param>
    /// <returns>True if a successful registration result was found; otherwise false.</returns>
    public bool TryGetAssignedRoutingContext(uint localRoutingKeyIdentifier, out uint routingContext)
    {
        if (TryFindResult(localRoutingKeyIdentifier, out M3uaRegistrationResult result) && result.IsSuccess)
        {
            routingContext = result.RoutingContext;
            return true;
        }

        routingContext = 0;
        return false;
    }
}

/// <summary>
/// Represents a typed RKM Deregistration Request message.
/// </summary>
public sealed class M3uaDeregistrationRequestMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed Deregistration Request message.</summary>
    /// <param name="routingContexts">The Routing Context values to deregister.</param>
    public M3uaDeregistrationRequestMessage(ReadOnlySpan<uint> routingContexts)
    {
        _routingContexts = routingContexts.ToArray();
    }

    /// <summary>The Routing Context values to deregister.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;
}

/// <summary>
/// Represents one Deregistration Result entry from an RKM Deregistration Response.
/// </summary>
public readonly struct M3uaDeregistrationResult
{
    /// <summary>Creates a Deregistration Result entry.</summary>
    /// <param name="routingContext">The Routing Context from the matching request.</param>
    /// <param name="status">The deregistration status.</param>
    public M3uaDeregistrationResult(uint routingContext, M3uaDeregistrationStatus status)
    {
        RoutingContext = routingContext;
        Status = status;
    }

    /// <summary>The Routing Context from the matching request.</summary>
    public uint RoutingContext { get; }

    /// <summary>The deregistration status.</summary>
    public M3uaDeregistrationStatus Status { get; }

    /// <summary>Whether the deregistration result status indicates success.</summary>
    public bool IsSuccess => Status == M3uaDeregistrationStatus.SuccessfullyDeregistered;
}

/// <summary>
/// Represents a typed RKM Deregistration Response message.
/// </summary>
public sealed class M3uaDeregistrationResponseMessage
{
    private readonly M3uaDeregistrationResult[] _results;

    /// <summary>Creates a typed Deregistration Response message.</summary>
    /// <param name="results">The Deregistration Result entries.</param>
    public M3uaDeregistrationResponseMessage(ReadOnlySpan<M3uaDeregistrationResult> results)
    {
        _results = results.ToArray();
    }

    /// <summary>The Deregistration Result entries.</summary>
    public ReadOnlySpan<M3uaDeregistrationResult> Results => _results;

    /// <summary>Whether every deregistration result indicates success.</summary>
    public bool AllSuccessful
    {
        get
        {
            for (int i = 0; i < _results.Length; i++)
            {
                if (!_results[i].IsSuccess)
                {
                    return false;
                }
            }

            return _results.Length > 0;
        }
    }

    /// <summary>
    /// Finds a Deregistration Result by Routing Context.
    /// </summary>
    /// <param name="routingContext">The Routing Context to find.</param>
    /// <param name="result">The matching result on success.</param>
    /// <returns>True if a matching result was found; otherwise false.</returns>
    public bool TryFindResult(uint routingContext, out M3uaDeregistrationResult result)
    {
        for (int i = 0; i < _results.Length; i++)
        {
            if (_results[i].RoutingContext == routingContext)
            {
                result = _results[i];
                return true;
            }
        }

        result = default;
        return false;
    }
}

/// <summary>
/// Identifies the concrete typed M3UA message model returned by the dispatcher parser.
/// </summary>
public enum M3uaTypedMessageKind
{
    /// <summary>A Transfer Payload Data message.</summary>
    PayloadData,
    /// <summary>An ASP State Maintenance message.</summary>
    AspStateMaintenance,
    /// <summary>An ASP Traffic Maintenance message.</summary>
    AspTrafficMaintenance,
    /// <summary>A Management Error message.</summary>
    Error,
    /// <summary>A Management Notify message.</summary>
    Notify,
    /// <summary>A common SSNM message such as DUNA, DAVA, DAUD, or DRST.</summary>
    Ssnm,
    /// <summary>A Signalling Congestion SSNM message.</summary>
    SignallingCongestion,
    /// <summary>A Destination User Part Unavailable SSNM message.</summary>
    DestinationUserPartUnavailable,
    /// <summary>A Routing Key Management Registration Request message.</summary>
    RegistrationRequest,
    /// <summary>A Routing Key Management Registration Response message.</summary>
    RegistrationResponse,
    /// <summary>A Routing Key Management Deregistration Request message.</summary>
    DeregistrationRequest,
    /// <summary>A Routing Key Management Deregistration Response message.</summary>
    DeregistrationResponse
}

/// <summary>
/// Holds a typed M3UA message produced by the dispatcher parser.
/// </summary>
public sealed class M3uaTypedMessage
{
    internal M3uaTypedMessage(M3uaTypedMessageKind kind, object value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>The kind of typed M3UA message.</summary>
    public M3uaTypedMessageKind Kind { get; }

    /// <summary>The concrete typed message model.</summary>
    public object Value { get; }

    /// <summary>
    /// Gets the concrete typed message model as the requested reference type.
    /// </summary>
    /// <typeparam name="T">The expected typed message model type.</typeparam>
    /// <returns>The typed message model cast to <typeparamref name="T"/>.</returns>
    public T As<T>()
        where T : class
    {
        return (T)Value;
    }
}

/// <summary>
/// Converts generic M3UA messages into typed messages used by session state machines.
/// </summary>
public static class M3uaTypedMessageParser
{
    /// <summary>
    /// Determines whether the dispatcher has typed parser support for a message class and type.
    /// </summary>
    /// <param name="messageClass">The M3UA message class.</param>
    /// <param name="messageType">The message type within the class.</param>
    /// <returns>True if the dispatcher can parse the message into a typed model; otherwise false.</returns>
    public static bool IsSupported(M3uaMessageClass messageClass, byte messageType)
    {
        return messageClass switch
        {
            M3uaMessageClass.Management => messageType is (byte)M3uaManagementMessageType.Error
                or (byte)M3uaManagementMessageType.Notify,
            M3uaMessageClass.Transfer => messageType == (byte)M3uaTransferMessageType.PayloadData,
            M3uaMessageClass.Ssnm => Enum.IsDefined(typeof(M3uaSsnmMessageType), messageType),
            M3uaMessageClass.Aspsm => Enum.IsDefined(typeof(M3uaAspsmMessageType), messageType),
            M3uaMessageClass.Asptm => Enum.IsDefined(typeof(M3uaAsptmMessageType), messageType),
            M3uaMessageClass.RoutingKeyManagement => Enum.IsDefined(typeof(M3uaRoutingKeyManagementMessageType), messageType),
            _ => false
        };
    }

    /// <summary>
    /// Parses any currently supported M3UA message into its concrete typed model.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed dispatcher result on success.</param>
    /// <param name="error">An error message if parsing fails or the message is unsupported.</param>
    /// <returns>True if the message was parsed into a supported typed model; otherwise false.</returns>
    public static bool TryParseKnown(
        M3uaMessage message,
        out M3uaTypedMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        switch (message.MessageClass)
        {
            case M3uaMessageClass.Transfer:
                return TryParseTransferKnown(message, out typedMessage, out error);

            case M3uaMessageClass.Management:
                return TryParseManagementKnown(message, out typedMessage, out error);

            case M3uaMessageClass.Ssnm:
                return TryParseSsnmKnown(message, out typedMessage, out error);

            case M3uaMessageClass.Aspsm:
                if (!TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? aspsm, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.AspStateMaintenance, aspsm!);
                return true;

            case M3uaMessageClass.Asptm:
                if (!TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? asptm, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.AspTrafficMaintenance, asptm!);
                return true;

            case M3uaMessageClass.RoutingKeyManagement:
                return TryParseRoutingKeyManagementKnown(message, out typedMessage, out error);

            default:
                error = $"Unsupported M3UA message class {message.MessageClass}";
                return false;
        }
    }

    /// <summary>
    /// Parses a Payload Data message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParsePayloadData(
        M3uaMessage message,
        out M3uaPayloadDataMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Transfer || message.MessageType != (byte)M3uaTransferMessageType.PayloadData)
        {
            error = $"Expected Payload Data message, got class {message.MessageClass} type {message.MessageType}";
            return false;
        }

        uint? networkAppearance = null;
        uint? routingContext = null;
        uint? correlationId = null;
        uint originatingPointCode = 0;
        uint destinationPointCode = 0;
        byte serviceIndicator = 0;
        byte networkIndicator = 0;
        byte messagePriority = 0;
        byte signallingLinkSelection = 0;
        byte[] userPayload = Array.Empty<byte>();
        bool hasProtocolData = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (routingContext.HasValue)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint context, out error))
                    {
                        return false;
                    }

                    routingContext = context;
                    break;

                case M3uaParameterTag.ProtocolData:
                    if (hasProtocolData)
                    {
                        error = "Duplicate Protocol Data parameter";
                        return false;
                    }

                    if (!TryReadProtocolData(
                            parameter.Value,
                            out originatingPointCode,
                            out destinationPointCode,
                            out serviceIndicator,
                            out networkIndicator,
                            out messagePriority,
                            out signallingLinkSelection,
                            out userPayload,
                            out error))
                    {
                        return false;
                    }

                    hasProtocolData = true;
                    break;

                case M3uaParameterTag.CorrelationId:
                    if (correlationId.HasValue)
                    {
                        error = "Duplicate Correlation Id parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint parsedCorrelationId, out error))
                    {
                        return false;
                    }

                    correlationId = parsedCorrelationId;
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!hasProtocolData)
        {
            error = "Missing Protocol Data parameter";
            return false;
        }

        typedMessage = new(
            networkAppearance,
            routingContext,
            originatingPointCode,
            destinationPointCode,
            serviceIndicator,
            networkIndicator,
            messagePriority,
            signallingLinkSelection,
            userPayload,
            correlationId);
        return true;
    }

    /// <summary>
    /// Parses a Management Error message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseError(
        M3uaMessage message,
        out M3uaErrorMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateManagementType(message, M3uaManagementMessageType.Error, out error))
        {
            return false;
        }

        M3uaErrorCode? errorCode = null;
        uint[] routingContexts = Array.Empty<uint>();
        uint? networkAppearance = null;
        byte[] diagnosticInformation = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasDiagnosticInformation = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.ErrorCode:
                    if (errorCode.HasValue)
                    {
                        error = "Duplicate Error Code parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint rawErrorCode, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaErrorCode), rawErrorCode))
                    {
                        error = $"Unknown Error Code {rawErrorCode}";
                        return false;
                    }

                    errorCode = (M3uaErrorCode)rawErrorCode;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.DiagnosticInformation:
                    if (hasDiagnosticInformation)
                    {
                        error = "Duplicate Diagnostic Information parameter";
                        return false;
                    }

                    hasDiagnosticInformation = true;
                    diagnosticInformation = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!errorCode.HasValue)
        {
            error = "Missing Error Code parameter";
            return false;
        }

        typedMessage = new(errorCode.Value, routingContexts, networkAppearance, diagnosticInformation);
        return true;
    }

    /// <summary>
    /// Parses a Management Notify message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseNotify(
        M3uaMessage message,
        out M3uaNotifyMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateManagementType(message, M3uaManagementMessageType.Notify, out error))
        {
            return false;
        }

        M3uaNotifyStatusType? statusType = null;
        ushort statusInformation = 0;
        uint? aspIdentifier = null;
        uint[] routingContexts = Array.Empty<uint>();
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.Status:
                    if (statusType.HasValue)
                    {
                        error = "Duplicate Status parameter";
                        return false;
                    }

                    if (!TryReadStatus(parameter.Value, out M3uaNotifyStatusType parsedStatusType, out statusInformation, out error))
                    {
                        return false;
                    }

                    statusType = parsedStatusType;
                    break;

                case M3uaParameterTag.AspIdentifier:
                    if (aspIdentifier.HasValue)
                    {
                        error = "Duplicate ASP Identifier parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint id, out error))
                    {
                        return false;
                    }

                    aspIdentifier = id;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!statusType.HasValue)
        {
            error = "Missing Status parameter";
            return false;
        }

        typedMessage = new(statusType.Value, statusInformation, aspIdentifier, routingContexts, infoString);
        return true;
    }

    /// <summary>
    /// Parses a common SSNM message containing Network Appearance, Routing Context,
    /// Affected Point Code, and Info String parameters.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseSsnm(
        M3uaMessage message,
        out M3uaSsnmMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Ssnm)
        {
            error = $"Expected SSNM message class, got {message.MessageClass}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaSsnmMessageType), message.MessageType))
        {
            error = $"Unknown SSNM message type {message.MessageType}";
            return false;
        }

        M3uaSsnmMessageType messageType = (M3uaSsnmMessageType)message.MessageType;
        uint? networkAppearance = null;
        uint[] routingContexts = Array.Empty<uint>();
        M3uaAffectedPointCode[] affectedPointCodes = Array.Empty<M3uaAffectedPointCode>();
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasAffectedPointCode = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.AffectedPointCode:
                    if (hasAffectedPointCode)
                    {
                        error = "Duplicate Affected Point Code parameter";
                        return false;
                    }

                    if (!TryReadAffectedPointCodes(parameter.Value, out affectedPointCodes, out error))
                    {
                        return false;
                    }

                    hasAffectedPointCode = true;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!hasAffectedPointCode)
        {
            error = "Missing Affected Point Code parameter";
            return false;
        }

        typedMessage = new(messageType, networkAppearance, routingContexts, affectedPointCodes, infoString);
        return true;
    }

    /// <summary>
    /// Parses a Destination User Part Unavailable message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseDestinationUserPartUnavailable(
        M3uaMessage message,
        out M3uaDestinationUserPartUnavailableMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Ssnm || message.MessageType != (byte)M3uaSsnmMessageType.DestinationUserPartUnavailable)
        {
            error = $"Expected DUPU message, got class {message.MessageClass} type {message.MessageType}";
            return false;
        }

        uint? networkAppearance = null;
        uint[] routingContexts = Array.Empty<uint>();
        M3uaAffectedPointCode[] affectedPointCodes = Array.Empty<M3uaAffectedPointCode>();
        M3uaUserPartUnavailableCause? cause = null;
        M3uaMtp3UserIdentity? userIdentity = null;
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasAffectedPointCode = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.AffectedPointCode:
                    if (hasAffectedPointCode)
                    {
                        error = "Duplicate Affected Point Code parameter";
                        return false;
                    }

                    if (!TryReadAffectedPointCodes(parameter.Value, out affectedPointCodes, out error))
                    {
                        return false;
                    }

                    hasAffectedPointCode = true;
                    break;

                case M3uaParameterTag.UserCause:
                    if (cause.HasValue)
                    {
                        error = "Duplicate User/Cause parameter";
                        return false;
                    }

                    if (!TryReadUserCause(parameter.Value, out M3uaUserPartUnavailableCause parsedCause, out M3uaMtp3UserIdentity parsedUser, out error))
                    {
                        return false;
                    }

                    cause = parsedCause;
                    userIdentity = parsedUser;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!hasAffectedPointCode || affectedPointCodes.Length != 1)
        {
            error = "DUPU requires exactly one Affected Point Code";
            return false;
        }

        if (affectedPointCodes[0].Mask != 0)
        {
            error = "DUPU Affected Point Code mask must be 0";
            return false;
        }

        if (!cause.HasValue || !userIdentity.HasValue)
        {
            error = "Missing User/Cause parameter";
            return false;
        }

        typedMessage = new(networkAppearance, routingContexts, affectedPointCodes[0], cause.Value, userIdentity.Value, infoString);
        return true;
    }

    /// <summary>
    /// Parses a Signalling Congestion message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseSignallingCongestion(
        M3uaMessage message,
        out M3uaSignallingCongestionMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Ssnm || message.MessageType != (byte)M3uaSsnmMessageType.SignallingCongestion)
        {
            error = $"Expected SCON message, got class {message.MessageClass} type {message.MessageType}";
            return false;
        }

        uint? networkAppearance = null;
        uint[] routingContexts = Array.Empty<uint>();
        M3uaAffectedPointCode[] affectedPointCodes = Array.Empty<M3uaAffectedPointCode>();
        M3uaAffectedPointCode? concernedDestination = null;
        uint? congestionLevel = null;
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasAffectedPointCode = false;
        bool hasConcernedDestination = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.AffectedPointCode:
                    if (hasAffectedPointCode)
                    {
                        error = "Duplicate Affected Point Code parameter";
                        return false;
                    }

                    if (!TryReadAffectedPointCodes(parameter.Value, out affectedPointCodes, out error))
                    {
                        return false;
                    }

                    hasAffectedPointCode = true;
                    break;

                case M3uaParameterTag.ConcernedDestination:
                    if (hasConcernedDestination)
                    {
                        error = "Duplicate Concerned Destination parameter";
                        return false;
                    }

                    if (!TryReadAffectedPointCodes(parameter.Value, out M3uaAffectedPointCode[] concernedDestinations, out error))
                    {
                        return false;
                    }

                    if (concernedDestinations.Length != 1)
                    {
                        error = "Concerned Destination must contain exactly one point code";
                        return false;
                    }

                    hasConcernedDestination = true;
                    concernedDestination = concernedDestinations[0];
                    break;

                case M3uaParameterTag.CongestionIndications:
                    if (congestionLevel.HasValue)
                    {
                        error = "Duplicate Congestion Indications parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint parsedCongestionLevel, out error))
                    {
                        return false;
                    }

                    congestionLevel = parsedCongestionLevel;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!hasAffectedPointCode)
        {
            error = "Missing Affected Point Code parameter";
            return false;
        }

        typedMessage = new(networkAppearance, routingContexts, affectedPointCodes, concernedDestination, congestionLevel, infoString);
        return true;
    }

    /// <summary>
    /// Parses an RKM Registration Request message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseRegistrationRequest(
        M3uaMessage message,
        out M3uaRegistrationRequestMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateRoutingKeyManagementType(message, M3uaRoutingKeyManagementMessageType.RegistrationRequest, out error))
        {
            return false;
        }

        List<M3uaRoutingKey> routingKeys = new();
        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (parameter.Tag != M3uaParameterTag.RoutingKey)
            {
                continue;
            }

            if (!TryReadRoutingKey(parameter.Value, out M3uaRoutingKey? routingKey, out error))
            {
                return false;
            }

            routingKeys.Add(routingKey!);
        }

        if (error is not null)
        {
            return false;
        }

        if (routingKeys.Count == 0)
        {
            error = "Missing Routing Key parameter";
            return false;
        }

        typedMessage = new(CollectionsMarshal.AsSpan(routingKeys));
        return true;
    }

    /// <summary>
    /// Parses an RKM Registration Response message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseRegistrationResponse(
        M3uaMessage message,
        out M3uaRegistrationResponseMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateRoutingKeyManagementType(message, M3uaRoutingKeyManagementMessageType.RegistrationResponse, out error))
        {
            return false;
        }

        List<M3uaRegistrationResult> results = new();
        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (parameter.Tag != M3uaParameterTag.RegistrationResult)
            {
                continue;
            }

            if (!TryReadRegistrationResult(parameter.Value, out M3uaRegistrationResult result, out error))
            {
                return false;
            }

            results.Add(result);
        }

        if (error is not null)
        {
            return false;
        }

        if (results.Count == 0)
        {
            error = "Missing Registration Result parameter";
            return false;
        }

        typedMessage = new(CollectionsMarshal.AsSpan(results));
        return true;
    }

    /// <summary>
    /// Parses an RKM Deregistration Request message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseDeregistrationRequest(
        M3uaMessage message,
        out M3uaDeregistrationRequestMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateRoutingKeyManagementType(message, M3uaRoutingKeyManagementMessageType.DeregistrationRequest, out error))
        {
            return false;
        }

        uint[] routingContexts = Array.Empty<uint>();
        bool hasRoutingContext = false;
        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (parameter.Tag != M3uaParameterTag.RoutingContext)
            {
                continue;
            }

            if (hasRoutingContext)
            {
                error = "Duplicate Routing Context parameter";
                return false;
            }

            if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
            {
                return false;
            }

            hasRoutingContext = true;
        }

        if (error is not null)
        {
            return false;
        }

        if (!hasRoutingContext)
        {
            error = "Missing Routing Context parameter";
            return false;
        }

        typedMessage = new(routingContexts);
        return true;
    }

    /// <summary>
    /// Parses an RKM Deregistration Response message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseDeregistrationResponse(
        M3uaMessage message,
        out M3uaDeregistrationResponseMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (!ValidateRoutingKeyManagementType(message, M3uaRoutingKeyManagementMessageType.DeregistrationResponse, out error))
        {
            return false;
        }

        List<M3uaDeregistrationResult> results = new();
        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (parameter.Tag != M3uaParameterTag.DeregistrationResult)
            {
                continue;
            }

            if (!TryReadDeregistrationResult(parameter.Value, out M3uaDeregistrationResult result, out error))
            {
                return false;
            }

            results.Add(result);
        }

        if (error is not null)
        {
            return false;
        }

        if (results.Count == 0)
        {
            error = "Missing Deregistration Result parameter";
            return false;
        }

        typedMessage = new(CollectionsMarshal.AsSpan(results));
        return true;
    }

    /// <summary>
    /// Parses an ASP State Maintenance message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseAspsm(
        M3uaMessage message,
        out M3uaAspStateMaintenanceMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Aspsm)
        {
            error = $"Expected ASPSM message class, got {message.MessageClass}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaAspsmMessageType), message.MessageType))
        {
            error = $"Unknown ASPSM message type {message.MessageType}";
            return false;
        }

        M3uaAspsmMessageType messageType = (M3uaAspsmMessageType)message.MessageType;
        uint? aspIdentifier = null;
        byte[] infoString = Array.Empty<byte>();
        byte[] heartbeatData = Array.Empty<byte>();
        bool hasInfoString = false;
        bool hasHeartbeatData = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.AspIdentifier:
                    if (aspIdentifier.HasValue)
                    {
                        error = "Duplicate ASP Identifier parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint id, out error))
                    {
                        return false;
                    }

                    aspIdentifier = id;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;

                case M3uaParameterTag.HeartbeatData:
                    if (hasHeartbeatData)
                    {
                        error = "Duplicate Heartbeat Data parameter";
                        return false;
                    }

                    hasHeartbeatData = true;
                    heartbeatData = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        typedMessage = new(messageType, aspIdentifier, infoString, heartbeatData);
        return true;
    }

    /// <summary>
    /// Parses an ASP Traffic Maintenance message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseAsptm(
        M3uaMessage message,
        out M3uaAspTrafficMaintenanceMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Asptm)
        {
            error = $"Expected ASPTM message class, got {message.MessageClass}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaAsptmMessageType), message.MessageType))
        {
            error = $"Unknown ASPTM message type {message.MessageType}";
            return false;
        }

        M3uaAsptmMessageType messageType = (M3uaAsptmMessageType)message.MessageType;
        M3uaTrafficModeType? trafficModeType = null;
        uint[] routingContexts = Array.Empty<uint>();
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.TrafficModeType:
                    if (trafficModeType.HasValue)
                    {
                        error = "Duplicate Traffic Mode Type parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint trafficMode, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaTrafficModeType), trafficMode))
                    {
                        error = $"Unknown Traffic Mode Type {trafficMode}";
                        return false;
                    }

                    trafficModeType = (M3uaTrafficModeType)trafficMode;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        typedMessage = new(messageType, trafficModeType, routingContexts, infoString);
        return true;
    }

    private static bool TryParseTransferKnown(
        M3uaMessage message,
        out M3uaTypedMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        if (message.MessageType != (byte)M3uaTransferMessageType.PayloadData)
        {
            error = $"Unsupported Transfer message type {message.MessageType}";
            return false;
        }

        if (!TryParsePayloadData(message, out M3uaPayloadDataMessage? payloadData, out error))
        {
            return false;
        }

        typedMessage = new(M3uaTypedMessageKind.PayloadData, payloadData!);
        return true;
    }

    private static bool TryParseManagementKnown(
        M3uaMessage message,
        out M3uaTypedMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        switch ((M3uaManagementMessageType)message.MessageType)
        {
            case M3uaManagementMessageType.Error:
                if (!TryParseError(message, out M3uaErrorMessage? errorMessage, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.Error, errorMessage!);
                return true;

            case M3uaManagementMessageType.Notify:
                if (!TryParseNotify(message, out M3uaNotifyMessage? notifyMessage, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.Notify, notifyMessage!);
                return true;

            default:
                error = $"Unsupported Management message type {message.MessageType}";
                return false;
        }
    }

    private static bool TryParseSsnmKnown(
        M3uaMessage message,
        out M3uaTypedMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        switch ((M3uaSsnmMessageType)message.MessageType)
        {
            case M3uaSsnmMessageType.SignallingCongestion:
                if (!TryParseSignallingCongestion(message, out M3uaSignallingCongestionMessage? scon, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.SignallingCongestion, scon!);
                return true;

            case M3uaSsnmMessageType.DestinationUserPartUnavailable:
                if (!TryParseDestinationUserPartUnavailable(message, out M3uaDestinationUserPartUnavailableMessage? dupu, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.DestinationUserPartUnavailable, dupu!);
                return true;

            case M3uaSsnmMessageType.DestinationUnavailable:
            case M3uaSsnmMessageType.DestinationAvailable:
            case M3uaSsnmMessageType.DestinationStateAudit:
            case M3uaSsnmMessageType.DestinationRestricted:
                if (!TryParseSsnm(message, out M3uaSsnmMessage? ssnm, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.Ssnm, ssnm!);
                return true;

            default:
                error = $"Unsupported SSNM message type {message.MessageType}";
                return false;
        }
    }

    private static bool TryParseRoutingKeyManagementKnown(
        M3uaMessage message,
        out M3uaTypedMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        switch ((M3uaRoutingKeyManagementMessageType)message.MessageType)
        {
            case M3uaRoutingKeyManagementMessageType.RegistrationRequest:
                if (!TryParseRegistrationRequest(message, out M3uaRegistrationRequestMessage? registrationRequest, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.RegistrationRequest, registrationRequest!);
                return true;

            case M3uaRoutingKeyManagementMessageType.RegistrationResponse:
                if (!TryParseRegistrationResponse(message, out M3uaRegistrationResponseMessage? registrationResponse, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.RegistrationResponse, registrationResponse!);
                return true;

            case M3uaRoutingKeyManagementMessageType.DeregistrationRequest:
                if (!TryParseDeregistrationRequest(message, out M3uaDeregistrationRequestMessage? deregistrationRequest, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.DeregistrationRequest, deregistrationRequest!);
                return true;

            case M3uaRoutingKeyManagementMessageType.DeregistrationResponse:
                if (!TryParseDeregistrationResponse(message, out M3uaDeregistrationResponseMessage? deregistrationResponse, out error))
                {
                    return false;
                }

                typedMessage = new(M3uaTypedMessageKind.DeregistrationResponse, deregistrationResponse!);
                return true;

            default:
                error = $"Unsupported Routing Key Management message type {message.MessageType}";
                return false;
        }
    }

    private static bool ValidateManagementType(
        M3uaMessage message,
        M3uaManagementMessageType expectedType,
        out string? error)
    {
        error = null;
        if (message.MessageClass != M3uaMessageClass.Management)
        {
            error = $"Expected Management message class, got {message.MessageClass}";
            return false;
        }

        if (message.MessageType != (byte)expectedType)
        {
            error = $"Expected Management message type {expectedType}, got {message.MessageType}";
            return false;
        }

        return true;
    }

    private static bool ValidateRoutingKeyManagementType(
        M3uaMessage message,
        M3uaRoutingKeyManagementMessageType expectedType,
        out string? error)
    {
        error = null;
        if (message.MessageClass != M3uaMessageClass.RoutingKeyManagement)
        {
            error = $"Expected Routing Key Management message class, got {message.MessageClass}";
            return false;
        }

        if (message.MessageType != (byte)expectedType)
        {
            error = $"Expected Routing Key Management message type {expectedType}, got {message.MessageType}";
            return false;
        }

        return true;
    }

    private static bool TryReadRoutingKey(
        ReadOnlySpan<byte> value,
        out M3uaRoutingKey? routingKey,
        out string? error)
    {
        routingKey = null;
        error = null;
        uint? localRoutingKeyIdentifier = null;
        uint? routingContext = null;
        M3uaTrafficModeType? trafficModeType = null;
        List<M3uaAffectedPointCode> destinationPointCodes = new();
        uint? networkAppearance = null;
        byte[] serviceIndicators = Array.Empty<byte>();
        List<M3uaAffectedPointCode> originatingPointCodes = new();
        bool hasServiceIndicators = false;

        M3uaParameterReader reader = new(value);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.LocalRoutingKeyIdentifier:
                    if (localRoutingKeyIdentifier.HasValue)
                    {
                        error = "Duplicate Local-RK-Identifier parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint localId, out error))
                    {
                        return false;
                    }

                    localRoutingKeyIdentifier = localId;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (routingContext.HasValue)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint context, out error))
                    {
                        return false;
                    }

                    routingContext = context;
                    break;

                case M3uaParameterTag.TrafficModeType:
                    if (trafficModeType.HasValue)
                    {
                        error = "Duplicate Traffic Mode Type parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint trafficMode, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaTrafficModeType), trafficMode))
                    {
                        error = $"Unknown Traffic Mode Type {trafficMode}";
                        return false;
                    }

                    trafficModeType = (M3uaTrafficModeType)trafficMode;
                    break;

                case M3uaParameterTag.DestinationPointCode:
                    if (!TryReadAffectedPointCodes(parameter.Value, out M3uaAffectedPointCode[] destinationCodes, out error))
                    {
                        return false;
                    }

                    destinationPointCodes.AddRange(destinationCodes);
                    break;

                case M3uaParameterTag.NetworkAppearance:
                    if (networkAppearance.HasValue)
                    {
                        error = "Duplicate Network Appearance parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint appearance, out error))
                    {
                        return false;
                    }

                    networkAppearance = appearance;
                    break;

                case M3uaParameterTag.ServiceIndicators:
                    if (hasServiceIndicators)
                    {
                        error = "Duplicate Service Indicators parameter";
                        return false;
                    }

                    if (parameter.Value.IsEmpty)
                    {
                        error = "Service Indicators parameter must not be empty";
                        return false;
                    }

                    hasServiceIndicators = true;
                    serviceIndicators = parameter.Value.ToArray();
                    break;

                case M3uaParameterTag.OriginatingPointCodeList:
                    if (!TryReadAffectedPointCodes(parameter.Value, out M3uaAffectedPointCode[] originatingCodes, out error))
                    {
                        return false;
                    }

                    originatingPointCodes.AddRange(originatingCodes);
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!localRoutingKeyIdentifier.HasValue)
        {
            error = "Missing Local-RK-Identifier parameter";
            return false;
        }

        if (destinationPointCodes.Count == 0)
        {
            error = "Missing Destination Point Code parameter";
            return false;
        }

        routingKey = new(
            localRoutingKeyIdentifier.Value,
            routingContext,
            trafficModeType,
            CollectionsMarshal.AsSpan(destinationPointCodes),
            networkAppearance,
            serviceIndicators,
            CollectionsMarshal.AsSpan(originatingPointCodes));
        return true;
    }

    private static bool TryReadRegistrationResult(
        ReadOnlySpan<byte> value,
        out M3uaRegistrationResult result,
        out string? error)
    {
        result = default;
        error = null;
        uint? localRoutingKeyIdentifier = null;
        M3uaRegistrationStatus? status = null;
        uint? routingContext = null;

        M3uaParameterReader reader = new(value);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.LocalRoutingKeyIdentifier:
                    if (localRoutingKeyIdentifier.HasValue)
                    {
                        error = "Duplicate Local-RK-Identifier parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint localId, out error))
                    {
                        return false;
                    }

                    localRoutingKeyIdentifier = localId;
                    break;

                case M3uaParameterTag.RegistrationStatus:
                    if (status.HasValue)
                    {
                        error = "Duplicate Registration Status parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint rawStatus, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaRegistrationStatus), rawStatus))
                    {
                        error = $"Unknown Registration Status {rawStatus}";
                        return false;
                    }

                    status = (M3uaRegistrationStatus)rawStatus;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (routingContext.HasValue)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint context, out error))
                    {
                        return false;
                    }

                    routingContext = context;
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!localRoutingKeyIdentifier.HasValue || !status.HasValue || !routingContext.HasValue)
        {
            error = "Registration Result requires Local-RK-Identifier, Registration Status, and Routing Context";
            return false;
        }

        result = new(localRoutingKeyIdentifier.Value, status.Value, routingContext.Value);
        return true;
    }

    private static bool TryReadDeregistrationResult(
        ReadOnlySpan<byte> value,
        out M3uaDeregistrationResult result,
        out string? error)
    {
        result = default;
        error = null;
        uint? routingContext = null;
        M3uaDeregistrationStatus? status = null;

        M3uaParameterReader reader = new(value);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.RoutingContext:
                    if (routingContext.HasValue)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint context, out error))
                    {
                        return false;
                    }

                    routingContext = context;
                    break;

                case M3uaParameterTag.DeregistrationStatus:
                    if (status.HasValue)
                    {
                        error = "Duplicate Deregistration Status parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint rawStatus, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaDeregistrationStatus), rawStatus))
                    {
                        error = $"Unknown Deregistration Status {rawStatus}";
                        return false;
                    }

                    status = (M3uaDeregistrationStatus)rawStatus;
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        if (!routingContext.HasValue || !status.HasValue)
        {
            error = "Deregistration Result requires Routing Context and Deregistration Status";
            return false;
        }

        result = new(routingContext.Value, status.Value);
        return true;
    }

    private static bool TryReadStatus(
        ReadOnlySpan<byte> value,
        out M3uaNotifyStatusType statusType,
        out ushort statusInformation,
        out string? error)
    {
        statusType = default;
        statusInformation = 0;
        error = null;

        if (value.Length != sizeof(uint))
        {
            error = "Status parameter must contain exactly 4 bytes";
            return false;
        }

        ushort rawStatusType = BinaryPrimitives.ReadUInt16BigEndian(value);
        if (!Enum.IsDefined(typeof(M3uaNotifyStatusType), rawStatusType))
        {
            error = $"Unknown Notify Status Type {rawStatusType}";
            return false;
        }

        statusType = (M3uaNotifyStatusType)rawStatusType;
        statusInformation = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2, 2));
        return ValidateStatusInformation(statusType, statusInformation, out error);
    }

    private static bool ValidateStatusInformation(
        M3uaNotifyStatusType statusType,
        ushort statusInformation,
        out string? error)
    {
        error = null;
        bool valid = statusType switch
        {
            M3uaNotifyStatusType.ApplicationServerStateChange => Enum.IsDefined(typeof(M3uaApplicationServerState), statusInformation),
            M3uaNotifyStatusType.Other => Enum.IsDefined(typeof(M3uaOtherNotifyStatus), statusInformation),
            _ => false
        };

        if (!valid)
        {
            error = $"Unknown Notify Status Information {statusInformation} for Status Type {statusType}";
            return false;
        }

        return true;
    }

    private static bool TryReadUInt32(
        ReadOnlySpan<byte> value,
        M3uaParameterTag tag,
        out uint result,
        out string? error)
    {
        result = 0;
        error = null;
        if (value.Length != sizeof(uint))
        {
            error = $"{tag} parameter must contain exactly 4 bytes";
            return false;
        }

        result = BinaryPrimitives.ReadUInt32BigEndian(value);
        return true;
    }

    private static bool TryReadUInt32List(
        ReadOnlySpan<byte> value,
        M3uaParameterTag tag,
        out uint[] result,
        out string? error)
    {
        result = Array.Empty<uint>();
        error = null;

        if (value.IsEmpty || (value.Length % sizeof(uint)) != 0)
        {
            error = $"{tag} parameter length must be a non-empty multiple of 4 bytes";
            return false;
        }

        result = new uint[value.Length / sizeof(uint)];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(i * sizeof(uint), sizeof(uint)));
        }

        return true;
    }

    private static bool TryReadProtocolData(
        ReadOnlySpan<byte> value,
        out uint originatingPointCode,
        out uint destinationPointCode,
        out byte serviceIndicator,
        out byte networkIndicator,
        out byte messagePriority,
        out byte signallingLinkSelection,
        out byte[] userPayload,
        out string? error)
    {
        originatingPointCode = 0;
        destinationPointCode = 0;
        serviceIndicator = 0;
        networkIndicator = 0;
        messagePriority = 0;
        signallingLinkSelection = 0;
        userPayload = Array.Empty<byte>();
        error = null;

        if (value.Length < 12)
        {
            error = "Protocol Data parameter must contain at least 12 bytes";
            return false;
        }

        originatingPointCode = BinaryPrimitives.ReadUInt32BigEndian(value);
        destinationPointCode = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(4, 4));
        serviceIndicator = value[8];
        networkIndicator = value[9];
        messagePriority = value[10];
        signallingLinkSelection = value[11];
        userPayload = value.Slice(12).ToArray();
        return true;
    }

    private static bool TryReadAffectedPointCodes(
        ReadOnlySpan<byte> value,
        out M3uaAffectedPointCode[] result,
        out string? error)
    {
        result = Array.Empty<M3uaAffectedPointCode>();
        error = null;

        if (value.IsEmpty || (value.Length % sizeof(uint)) != 0)
        {
            error = "Affected Point Code parameter length must be a non-empty multiple of 4 bytes";
            return false;
        }

        result = new M3uaAffectedPointCode[value.Length / sizeof(uint)];
        for (int i = 0; i < result.Length; i++)
        {
            ReadOnlySpan<byte> entry = value.Slice(i * sizeof(uint), sizeof(uint));
            uint pointCode = ((uint)entry[1] << 16) | ((uint)entry[2] << 8) | entry[3];
            result[i] = new(entry[0], pointCode);
        }

        return true;
    }

    private static bool TryReadUserCause(
        ReadOnlySpan<byte> value,
        out M3uaUserPartUnavailableCause cause,
        out M3uaMtp3UserIdentity userIdentity,
        out string? error)
    {
        cause = default;
        userIdentity = default;
        error = null;

        if (value.Length != sizeof(uint))
        {
            error = "User/Cause parameter must contain exactly 4 bytes";
            return false;
        }

        ushort rawCause = BinaryPrimitives.ReadUInt16BigEndian(value);
        ushort rawUser = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2, 2));
        if (!Enum.IsDefined(typeof(M3uaUserPartUnavailableCause), rawCause))
        {
            error = $"Unknown User Part Unavailable Cause {rawCause}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaMtp3UserIdentity), rawUser))
        {
            error = $"Unknown MTP3-User Identity {rawUser}";
            return false;
        }

        cause = (M3uaUserPartUnavailableCause)rawCause;
        userIdentity = (M3uaMtp3UserIdentity)rawUser;
        return true;
    }
}
