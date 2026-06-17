namespace sigtran.net.Layers.M3UA;

/// <summary>
/// M3UA protocol constants and helpers shared by encoders and decoders.
/// </summary>
public static class M3uaProtocol
{
    /// <summary>The M3UA Release 1.0 protocol version.</summary>
    public const byte Version = 1;

    /// <summary>The fixed common message header length in octets.</summary>
    public const int HeaderLength = 8;

    /// <summary>The fixed TLV parameter header length in octets.</summary>
    public const int ParameterHeaderLength = 4;

    /// <summary>Rounds a byte length up to the next 32-bit boundary.</summary>
    public static int AlignToUInt32(int length)
    {
        int remainder = length & 0x3;
        return remainder == 0 ? length : length + (4 - remainder);
    }
}

/// <summary>M3UA message classes defined by RFC 4666 section 3.1.2.</summary>
public enum M3uaMessageClass : byte
{
    /// <summary>Management messages.</summary>
    Management = 0,
    /// <summary>Transfer messages.</summary>
    Transfer = 1,
    /// <summary>SS7 signalling network management messages.</summary>
    Ssnm = 2,
    /// <summary>ASP state maintenance messages.</summary>
    Aspsm = 3,
    /// <summary>ASP traffic maintenance messages.</summary>
    Asptm = 4,
    /// <summary>Routing key management messages.</summary>
    RoutingKeyManagement = 9
}

/// <summary>Management message types.</summary>
public enum M3uaManagementMessageType : byte
{
    /// <summary>Error message.</summary>
    Error = 0,
    /// <summary>Notify message.</summary>
    Notify = 1
}

/// <summary>Transfer message types.</summary>
public enum M3uaTransferMessageType : byte
{
    /// <summary>Payload Data message.</summary>
    PayloadData = 1
}

/// <summary>SS7 signalling network management message types.</summary>
public enum M3uaSsnmMessageType : byte
{
    /// <summary>Destination Unavailable.</summary>
    DestinationUnavailable = 1,
    /// <summary>Destination Available.</summary>
    DestinationAvailable = 2,
    /// <summary>Destination State Audit.</summary>
    DestinationStateAudit = 3,
    /// <summary>Signalling Congestion.</summary>
    SignallingCongestion = 4,
    /// <summary>Destination User Part Unavailable.</summary>
    DestinationUserPartUnavailable = 5,
    /// <summary>Destination Restricted.</summary>
    DestinationRestricted = 6
}

/// <summary>ASP state maintenance message types.</summary>
public enum M3uaAspsmMessageType : byte
{
    /// <summary>ASP Up.</summary>
    AspUp = 1,
    /// <summary>ASP Down.</summary>
    AspDown = 2,
    /// <summary>Heartbeat.</summary>
    Heartbeat = 3,
    /// <summary>ASP Up acknowledgement.</summary>
    AspUpAck = 4,
    /// <summary>ASP Down acknowledgement.</summary>
    AspDownAck = 5,
    /// <summary>Heartbeat acknowledgement.</summary>
    HeartbeatAck = 6
}

/// <summary>ASP traffic maintenance message types.</summary>
public enum M3uaAsptmMessageType : byte
{
    /// <summary>ASP Active.</summary>
    AspActive = 1,
    /// <summary>ASP Inactive.</summary>
    AspInactive = 2,
    /// <summary>ASP Active acknowledgement.</summary>
    AspActiveAck = 3,
    /// <summary>ASP Inactive acknowledgement.</summary>
    AspInactiveAck = 4
}

/// <summary>Routing key management message types.</summary>
public enum M3uaRoutingKeyManagementMessageType : byte
{
    /// <summary>Registration Request.</summary>
    RegistrationRequest = 1,
    /// <summary>Registration Response.</summary>
    RegistrationResponse = 2,
    /// <summary>Deregistration Request.</summary>
    DeregistrationRequest = 3,
    /// <summary>Deregistration Response.</summary>
    DeregistrationResponse = 4
}

/// <summary>M3UA and common SIGTRAN parameter tags used by RFC 4666.</summary>
public enum M3uaParameterTag : ushort
{
    /// <summary>Reserved tag.</summary>
    Reserved = 0x0000,
    /// <summary>Info String parameter.</summary>
    InfoString = 0x0004,
    /// <summary>Routing Context parameter.</summary>
    RoutingContext = 0x0006,
    /// <summary>Diagnostic Information parameter.</summary>
    DiagnosticInformation = 0x0007,
    /// <summary>Heartbeat Data parameter.</summary>
    HeartbeatData = 0x0009,
    /// <summary>Traffic Mode Type parameter.</summary>
    TrafficModeType = 0x000B,
    /// <summary>Error Code parameter.</summary>
    ErrorCode = 0x000C,
    /// <summary>Status parameter.</summary>
    Status = 0x000D,
    /// <summary>ASP Identifier parameter.</summary>
    AspIdentifier = 0x0011,
    /// <summary>Affected Point Code parameter.</summary>
    AffectedPointCode = 0x0012,
    /// <summary>Correlation Id parameter.</summary>
    CorrelationId = 0x0013,
    /// <summary>Network Appearance parameter.</summary>
    NetworkAppearance = 0x0200,
    /// <summary>User/Cause parameter.</summary>
    UserCause = 0x0204,
    /// <summary>Congestion Indications parameter.</summary>
    CongestionIndications = 0x0205,
    /// <summary>Concerned Destination parameter.</summary>
    ConcernedDestination = 0x0206,
    /// <summary>Routing Key parameter.</summary>
    RoutingKey = 0x0207,
    /// <summary>Registration Result parameter.</summary>
    RegistrationResult = 0x0208,
    /// <summary>Deregistration Result parameter.</summary>
    DeregistrationResult = 0x0209,
    /// <summary>Local Routing Key Identifier parameter.</summary>
    LocalRoutingKeyIdentifier = 0x020A,
    /// <summary>Destination Point Code parameter.</summary>
    DestinationPointCode = 0x020B,
    /// <summary>Service Indicators parameter.</summary>
    ServiceIndicators = 0x020C,
    /// <summary>Originating Point Code List parameter.</summary>
    OriginatingPointCodeList = 0x020E,
    /// <summary>Protocol Data parameter.</summary>
    ProtocolData = 0x0210,
    /// <summary>Registration Status parameter.</summary>
    RegistrationStatus = 0x0212,
    /// <summary>Deregistration Status parameter.</summary>
    DeregistrationStatus = 0x0213
}
