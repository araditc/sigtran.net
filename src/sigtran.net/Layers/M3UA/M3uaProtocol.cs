using System.Buffers.Binary;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// M3UA protocol constants and helpers shared by encoders and decoders.
/// </summary>
public static class M3uaProtocol
{
    /// <summary>The protocol short name exposed by this layer.</summary>
    public const string Name = "M3UA";

    /// <summary>The primary specification used for this protocol layer.</summary>
    public const string Specification = "RFC 4666";

    /// <summary>The NuGet package identity used by this SDK.</summary>
    public const string PackageName = "Sigtran.Net";

    /// <summary>The M3UA Release 1.0 protocol version.</summary>
    public const byte Version = 1;

    /// <summary>The fixed common message header length in octets.</summary>
    public const int HeaderLength = 8;

    /// <summary>The fixed TLV parameter header length in octets.</summary>
    public const int ParameterHeaderLength = 4;

    /// <summary>The current M3UA capability snapshot exposed by this SDK build.</summary>
    public static M3uaProtocolCapabilities Capabilities => new(
        supportsPayloadData: true,
        supportsAspLifecycle: true,
        supportsManagement: true,
        supportsSsnm: true,
        supportsRkm: true,
        supportsTransportSession: true);

    /// <summary>Rounds a byte length up to the next 32-bit boundary.</summary>
    /// <param name="length">The byte length to align.</param>
    /// <returns>The aligned byte length.</returns>
    public static int AlignToUInt32(int length)
    {
        int remainder = length & 0x3;
        return remainder == 0 ? length : length + (4 - remainder);
    }

    /// <summary>
    /// Reads the fixed M3UA common header without validating protocol support.
    /// </summary>
    /// <param name="packet">The encoded packet bytes.</param>
    /// <param name="header">The decoded common-header preview on success.</param>
    /// <param name="error">An error message if the packet is too short for the common header.</param>
    /// <returns>True if the common header was read; otherwise false.</returns>
    public static bool TryReadHeader(ReadOnlySpan<byte> packet, out M3uaHeaderPreview header, out string? error)
    {
        header = default;
        if (packet.Length < HeaderLength)
        {
            error = "M3UA buffer too short for header";
            return false;
        }

        header = new(
            version: packet[0],
            reserved: packet[1],
            messageClass: (M3uaMessageClass)packet[2],
            messageType: packet[3],
            messageLength: BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(4, 4)));
        error = null;
        return true;
    }

    /// <summary>
    /// Validates an M3UA message length against the common header, available bytes, and 32-bit alignment rules.
    /// </summary>
    /// <param name="messageLength">The message length from the M3UA common header.</param>
    /// <param name="availableLength">The number of available packet bytes.</param>
    /// <param name="error">An error message when the length is invalid.</param>
    /// <returns>True if the message length is valid; otherwise false.</returns>
    public static bool TryValidateMessageLength(uint messageLength, int availableLength, out string? error)
    {
        if (availableLength < 0)
        {
            error = "Available packet length must not be negative";
            return false;
        }

        if (messageLength < HeaderLength || messageLength > (uint)availableLength)
        {
            error = $"Invalid M3UA length {messageLength}";
            return false;
        }

        if ((messageLength & 0x3) != 0)
        {
            error = $"M3UA message length {messageLength} is not 32-bit aligned";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>
/// A lightweight preview of the fixed M3UA common header.
/// </summary>
public readonly struct M3uaHeaderPreview
{
    /// <summary>Creates a common-header preview.</summary>
    /// <param name="version">The raw M3UA version byte.</param>
    /// <param name="reserved">The raw reserved header byte.</param>
    /// <param name="messageClass">The raw message class value.</param>
    /// <param name="messageType">The raw message type value.</param>
    /// <param name="messageLength">The raw message length value.</param>
    public M3uaHeaderPreview(byte version, byte reserved, M3uaMessageClass messageClass, byte messageType, uint messageLength)
    {
        Version = version;
        Reserved = reserved;
        MessageClass = messageClass;
        MessageType = messageType;
        MessageLength = messageLength;
    }

    /// <summary>The raw M3UA version byte.</summary>
    public byte Version { get; }

    /// <summary>The raw reserved header byte.</summary>
    public byte Reserved { get; }

    /// <summary>The raw message class value.</summary>
    public M3uaMessageClass MessageClass { get; }

    /// <summary>The raw message type value.</summary>
    public byte MessageType { get; }

    /// <summary>The raw message length value.</summary>
    public uint MessageLength { get; }
}

/// <summary>
/// Describes the M3UA feature families supported by the current SDK build.
/// </summary>
public readonly struct M3uaProtocolCapabilities
{
    /// <summary>Creates a capability snapshot.</summary>
    /// <param name="supportsPayloadData">Whether Payload Data builders and parsers are available.</param>
    /// <param name="supportsAspLifecycle">Whether ASP lifecycle messages and client helpers are available.</param>
    /// <param name="supportsManagement">Whether Management Error and Notify messages are available.</param>
    /// <param name="supportsSsnm">Whether Signalling Network Management messages are available.</param>
    /// <param name="supportsRkm">Whether Routing Key Management messages and client helpers are available.</param>
    /// <param name="supportsTransportSession">Whether the async M3UA transport session facade is available.</param>
    public M3uaProtocolCapabilities(
        bool supportsPayloadData,
        bool supportsAspLifecycle,
        bool supportsManagement,
        bool supportsSsnm,
        bool supportsRkm,
        bool supportsTransportSession)
    {
        SupportsPayloadData = supportsPayloadData;
        SupportsAspLifecycle = supportsAspLifecycle;
        SupportsManagement = supportsManagement;
        SupportsSsnm = supportsSsnm;
        SupportsRkm = supportsRkm;
        SupportsTransportSession = supportsTransportSession;
    }

    /// <summary>Whether Payload Data builders and parsers are available.</summary>
    public bool SupportsPayloadData { get; }

    /// <summary>Whether ASP lifecycle messages and client helpers are available.</summary>
    public bool SupportsAspLifecycle { get; }

    /// <summary>Whether Management Error and Notify messages are available.</summary>
    public bool SupportsManagement { get; }

    /// <summary>Whether Signalling Network Management messages are available.</summary>
    public bool SupportsSsnm { get; }

    /// <summary>Whether Routing Key Management messages and client helpers are available.</summary>
    public bool SupportsRkm { get; }

    /// <summary>Whether the async M3UA transport session facade is available.</summary>
    public bool SupportsTransportSession { get; }
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

/// <summary>Error Code values used by M3UA Error messages.</summary>
public enum M3uaErrorCode : uint
{
    /// <summary>Invalid Version.</summary>
    InvalidVersion = 0x01,
    /// <summary>Unsupported Message Class.</summary>
    UnsupportedMessageClass = 0x03,
    /// <summary>Unsupported Message Type.</summary>
    UnsupportedMessageType = 0x04,
    /// <summary>Unsupported Traffic Mode Type.</summary>
    UnsupportedTrafficModeType = 0x05,
    /// <summary>Unexpected Message.</summary>
    UnexpectedMessage = 0x06,
    /// <summary>Protocol Error.</summary>
    ProtocolError = 0x07,
    /// <summary>Invalid Stream Identifier.</summary>
    InvalidStreamIdentifier = 0x09,
    /// <summary>Refused - Management Blocking.</summary>
    RefusedManagementBlocking = 0x0D,
    /// <summary>ASP Identifier Required.</summary>
    AspIdentifierRequired = 0x0E,
    /// <summary>Invalid ASP Identifier.</summary>
    InvalidAspIdentifier = 0x0F,
    /// <summary>Invalid Parameter Value.</summary>
    InvalidParameterValue = 0x11,
    /// <summary>Parameter Field Error.</summary>
    ParameterFieldError = 0x12,
    /// <summary>Unexpected Parameter.</summary>
    UnexpectedParameter = 0x13,
    /// <summary>Destination Status Unknown.</summary>
    DestinationStatusUnknown = 0x14,
    /// <summary>Invalid Network Appearance.</summary>
    InvalidNetworkAppearance = 0x15,
    /// <summary>Missing Parameter.</summary>
    MissingParameter = 0x16,
    /// <summary>Invalid Routing Context.</summary>
    InvalidRoutingContext = 0x19,
    /// <summary>No configured Application Server for ASP.</summary>
    NoConfiguredApplicationServerForAsp = 0x1A
}

/// <summary>Status Type values used by M3UA Notify messages.</summary>
public enum M3uaNotifyStatusType : ushort
{
    /// <summary>Application Server state change.</summary>
    ApplicationServerStateChange = 1,
    /// <summary>Other status information.</summary>
    Other = 2
}

/// <summary>Status Information values for Application Server state-change notifications.</summary>
public enum M3uaApplicationServerState : ushort
{
    /// <summary>Reserved value.</summary>
    Reserved = 1,
    /// <summary>Application Server Inactive.</summary>
    Inactive = 2,
    /// <summary>Application Server Active.</summary>
    Active = 3,
    /// <summary>Application Server Pending.</summary>
    Pending = 4
}

/// <summary>Status Information values for Notify messages with Status Type Other.</summary>
public enum M3uaOtherNotifyStatus : ushort
{
    /// <summary>Insufficient ASP resources are active in the Application Server.</summary>
    InsufficientAspResourcesActiveInApplicationServer = 1,
    /// <summary>An alternate ASP is active.</summary>
    AlternateAspActive = 2,
    /// <summary>An ASP failure was detected.</summary>
    AspFailure = 3
}

/// <summary>Unavailability Cause values used by DUPU User/Cause parameters.</summary>
public enum M3uaUserPartUnavailableCause : ushort
{
    /// <summary>The unavailability reason is unknown.</summary>
    Unknown = 0,
    /// <summary>The remote user is unequipped.</summary>
    UnequippedRemoteUser = 1,
    /// <summary>The remote user is inaccessible.</summary>
    InaccessibleRemoteUser = 2
}

/// <summary>Common MTP3-User Identity values used by DUPU User/Cause parameters.</summary>
public enum M3uaMtp3UserIdentity : ushort
{
    /// <summary>SCCP user identity.</summary>
    Sccp = 3,
    /// <summary>TUP user identity.</summary>
    Tup = 4,
    /// <summary>ISUP user identity.</summary>
    Isup = 5,
    /// <summary>Broadband ISUP user identity.</summary>
    BroadbandIsup = 9,
    /// <summary>Satellite ISUP user identity.</summary>
    SatelliteIsup = 10,
    /// <summary>AAL type 2 signalling user identity.</summary>
    AalType2Signalling = 12,
    /// <summary>Bearer Independent Call Control user identity.</summary>
    Bicc = 13,
    /// <summary>Gateway Control Protocol user identity.</summary>
    GatewayControlProtocol = 14
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

/// <summary>Traffic mode values used by ASP Traffic Maintenance messages.</summary>
public enum M3uaTrafficModeType : uint
{
    /// <summary>Override mode, where one ASP handles all traffic for an Application Server.</summary>
    Override = 1,
    /// <summary>Loadshare mode, where traffic is distributed across active ASPs.</summary>
    Loadshare = 2,
    /// <summary>Broadcast mode, where each active ASP receives the same traffic.</summary>
    Broadcast = 3
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

/// <summary>Registration Status values used by RKM Registration Result parameters.</summary>
public enum M3uaRegistrationStatus : uint
{
    /// <summary>The Routing Key was registered successfully.</summary>
    SuccessfullyRegistered = 0,
    /// <summary>An unknown registration error occurred.</summary>
    ErrorUnknown = 1,
    /// <summary>The Destination Point Code is invalid.</summary>
    ErrorInvalidDestinationPointCode = 2,
    /// <summary>The Network Appearance is invalid.</summary>
    ErrorInvalidNetworkAppearance = 3,
    /// <summary>The Routing Key is invalid.</summary>
    ErrorInvalidRoutingKey = 4,
    /// <summary>Registration permission was denied.</summary>
    ErrorPermissionDenied = 5,
    /// <summary>The peer cannot support unique routing for the requested key.</summary>
    ErrorCannotSupportUniqueRouting = 6,
    /// <summary>The Routing Key is not currently provisioned.</summary>
    ErrorRoutingKeyNotCurrentlyProvisioned = 7,
    /// <summary>The peer has insufficient resources to register the Routing Key.</summary>
    ErrorInsufficientResources = 8,
    /// <summary>The Routing Key includes an unsupported parameter field.</summary>
    ErrorUnsupportedRoutingKeyParameterField = 9,
    /// <summary>The traffic handling mode is unsupported or invalid.</summary>
    ErrorUnsupportedOrInvalidTrafficHandlingMode = 10,
    /// <summary>The Routing Key change was refused.</summary>
    ErrorRoutingKeyChangeRefused = 11,
    /// <summary>The Routing Key is already registered.</summary>
    ErrorRoutingKeyAlreadyRegistered = 12
}

/// <summary>Deregistration Status values used by RKM Deregistration Result parameters.</summary>
public enum M3uaDeregistrationStatus : uint
{
    /// <summary>The Routing Context was deregistered successfully.</summary>
    SuccessfullyDeregistered = 0,
    /// <summary>An unknown deregistration error occurred.</summary>
    ErrorUnknown = 1,
    /// <summary>The Routing Context is invalid.</summary>
    ErrorInvalidRoutingContext = 2,
    /// <summary>Deregistration permission was denied.</summary>
    ErrorPermissionDenied = 3,
    /// <summary>The Routing Context is not registered.</summary>
    ErrorNotRegistered = 4,
    /// <summary>The ASP is currently active for the Routing Context.</summary>
    ErrorAspCurrentlyActiveForRoutingContext = 5
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
