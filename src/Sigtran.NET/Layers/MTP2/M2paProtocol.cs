using System.Buffers.Binary;

using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Identifies an RFC 4165 M2PA message type.
/// </summary>
public enum M2paMessageType : byte
{
    /// <summary>An MTP3 service data unit or an acknowledgement-only message.</summary>
    UserData = 1,

    /// <summary>An M2PA link status message.</summary>
    LinkStatus = 2
}

/// <summary>
/// Identifies an RFC 4165 M2PA link status value.
/// </summary>
public enum M2paLinkStatus : uint
{
    /// <summary>Link alignment has started.</summary>
    Alignment = 1,

    /// <summary>Normal proving is in progress.</summary>
    ProvingNormal = 2,

    /// <summary>Emergency proving is in progress.</summary>
    ProvingEmergency = 3,

    /// <summary>The link or processor-outage recovery handshake is ready.</summary>
    Ready = 4,

    /// <summary>The local processor is unavailable.</summary>
    ProcessorOutage = 5,

    /// <summary>The local processor recovered.</summary>
    ProcessorRecovered = 6,

    /// <summary>The receiver is congested.</summary>
    Busy = 7,

    /// <summary>The receiver congestion condition ended.</summary>
    BusyEnded = 8,

    /// <summary>The link is out of service.</summary>
    OutOfService = 9
}

/// <summary>
/// Identifies why a Ready link-status message is being sent.
/// </summary>
public enum M2paReadyContext
{
    /// <summary>Ready completes link alignment and uses the link-status stream.</summary>
    Alignment,

    /// <summary>Ready completes processor-outage recovery and uses the user-data stream.</summary>
    ProcessorRecovery
}

/// <summary>
/// Exposes RFC 4165 M2PA constants and sequence arithmetic.
/// </summary>
public static class M2paProtocol
{
    /// <summary>The supported M2PA protocol version.</summary>
    public const byte Version = 1;

    /// <summary>The registered SIGTRAN M2PA message class.</summary>
    public const byte MessageClass = 11;

    /// <summary>The common SIGTRAN header size in bytes.</summary>
    public const int CommonHeaderLength = 8;

    /// <summary>The M2PA sequence header size in bytes.</summary>
    public const int SequenceHeaderLength = 8;

    /// <summary>The minimum M2PA message size in bytes.</summary>
    public const int MinimumMessageLength = CommonHeaderLength + SequenceHeaderLength;

    /// <summary>The highest 24-bit M2PA sequence number.</summary>
    public const uint MaximumSequenceNumber = 0x00FF_FFFF;

    /// <summary>The SCTP stream reserved for link-status traffic.</summary>
    public const ushort LinkStatusStream = 0;

    /// <summary>The SCTP stream reserved for user data and in-sequence status traffic.</summary>
    public const ushort UserDataStream = 1;

    /// <summary>Returns the next 24-bit sequence number with wraparound.</summary>
    /// <param name="sequenceNumber">The current sequence number.</param>
    /// <returns>The next sequence number.</returns>
    public static uint NextSequenceNumber(uint sequenceNumber)
    {
        ValidateSequenceNumber(sequenceNumber, nameof(sequenceNumber));
        return sequenceNumber == MaximumSequenceNumber ? 0 : sequenceNumber + 1;
    }

    /// <summary>Returns the SCTP stream required for a link-status message.</summary>
    /// <param name="status">The link-status value.</param>
    /// <param name="readyContext">The Ready message context.</param>
    /// <returns>The required SCTP stream identifier.</returns>
    public static ushort GetStream(
        M2paLinkStatus status,
        M2paReadyContext readyContext = M2paReadyContext.Alignment)
    {
        return status switch
        {
            M2paLinkStatus.ProcessorOutage => UserDataStream,
            M2paLinkStatus.ProcessorRecovered => UserDataStream,
            M2paLinkStatus.Ready when readyContext == M2paReadyContext.ProcessorRecovery
                => UserDataStream,
            _ => LinkStatusStream
        };
    }

    /// <summary>Validates SCTP metadata for a decoded M2PA message.</summary>
    /// <param name="message">The decoded M2PA message.</param>
    /// <param name="metadata">The received SCTP metadata.</param>
    /// <param name="error">The validation error.</param>
    /// <returns>True when the PPID, ordering, and stream are valid.</returns>
    public static bool TryValidateSctpMetadata(
        M2paMessage message,
        SctpPayloadMetadata metadata,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (metadata.PayloadProtocolIdentifier != SctpPayloadProtocolIdentifiers.M2pa)
        {
            error = $"M2PA requires SCTP PPID {SctpPayloadProtocolIdentifiers.M2pa}.";
            return false;
        }

        if (metadata.Unordered)
        {
            error = "M2PA messages require ordered SCTP delivery.";
            return false;
        }

        if (message.MessageType == M2paMessageType.UserData)
        {
            if (metadata.StreamId != UserDataStream)
            {
                error = $"M2PA User Data requires SCTP stream {UserDataStream}.";
                return false;
            }

            error = null;
            return true;
        }

        ushort expected = GetStream(message.LinkStatus!.Value);
        if (message.LinkStatus == M2paLinkStatus.Ready)
        {
            if (metadata.StreamId is LinkStatusStream or UserDataStream)
            {
                error = null;
                return true;
            }
        }
        else if (metadata.StreamId == expected)
        {
            error = null;
            return true;
        }

        error = $"M2PA Link Status {message.LinkStatus} is invalid on SCTP stream {metadata.StreamId}.";
        return false;
    }

    internal static void ValidateSequenceNumber(uint value, string parameterName)
    {
        if (value > MaximumSequenceNumber)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"M2PA sequence number must not exceed {MaximumSequenceNumber}.");
        }
    }
}

/// <summary>
/// Represents one decoded RFC 4165 M2PA message.
/// </summary>
public sealed class M2paMessage
{
    private readonly byte[] _payload;

    private M2paMessage(
        M2paMessageType messageType,
        uint backwardSequenceNumber,
        uint forwardSequenceNumber,
        byte[] payload,
        M2paLinkStatus? linkStatus)
    {
        MessageType = messageType;
        BackwardSequenceNumber = backwardSequenceNumber;
        ForwardSequenceNumber = forwardSequenceNumber;
        _payload = payload;
        LinkStatus = linkStatus;
    }

    /// <summary>The decoded message type.</summary>
    public M2paMessageType MessageType { get; }

    /// <summary>The FSN most recently accepted from the peer.</summary>
    public uint BackwardSequenceNumber { get; }

    /// <summary>The local forward sequence number carried by the message.</summary>
    public uint ForwardSequenceNumber { get; }

    /// <summary>The user-data payload or proving filler bytes.</summary>
    public ReadOnlyMemory<byte> Payload => _payload;

    /// <summary>The decoded link status, when the message is Link Status.</summary>
    public M2paLinkStatus? LinkStatus { get; }

    /// <summary>Whether this is an acknowledgement-only User Data message.</summary>
    public bool IsAcknowledgementOnly =>
        MessageType == M2paMessageType.UserData && _payload.Length == 0;

    /// <summary>Decodes one complete M2PA message.</summary>
    /// <param name="encoded">The encoded M2PA bytes.</param>
    /// <param name="message">The decoded message.</param>
    /// <param name="error">The decode error.</param>
    /// <returns>True when the message is valid.</returns>
    public static bool TryDecode(
        ReadOnlySpan<byte> encoded,
        out M2paMessage? message,
        out string? error)
    {
        message = null;
        if (encoded.Length < M2paProtocol.MinimumMessageLength)
        {
            error = $"M2PA message requires at least {M2paProtocol.MinimumMessageLength} bytes.";
            return false;
        }

        if (encoded[0] != M2paProtocol.Version || encoded[1] != 0)
        {
            error = "Unsupported M2PA version or non-zero spare byte.";
            return false;
        }

        if (encoded[2] != M2paProtocol.MessageClass)
        {
            error = $"Invalid M2PA message class {encoded[2]}.";
            return false;
        }

        if (!Enum.IsDefined(typeof(M2paMessageType), encoded[3]))
        {
            error = $"Unsupported M2PA message type {encoded[3]}.";
            return false;
        }

        uint length = BinaryPrimitives.ReadUInt32BigEndian(encoded.Slice(4, 4));
        if (length != encoded.Length)
        {
            error = $"M2PA message length {length} does not match received bytes {encoded.Length}.";
            return false;
        }

        if (encoded[8] != 0 || encoded[12] != 0)
        {
            error = "M2PA sequence-number unused octets must be zero.";
            return false;
        }

        uint bsn = ReadUInt24(encoded.Slice(9, 3));
        uint fsn = ReadUInt24(encoded.Slice(13, 3));
        M2paMessageType type = (M2paMessageType)encoded[3];
        ReadOnlySpan<byte> data = encoded.Slice(M2paProtocol.MinimumMessageLength);

        if (type == M2paMessageType.LinkStatus)
        {
            if (data.Length < sizeof(uint))
            {
                error = "M2PA Link Status requires a 4-byte state value.";
                return false;
            }

            uint stateValue = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(0, 4));
            if (!Enum.IsDefined(typeof(M2paLinkStatus), stateValue))
            {
                error = $"Unsupported M2PA Link Status value {stateValue}.";
                return false;
            }

            M2paLinkStatus status = (M2paLinkStatus)stateValue;
            if (data.Length > sizeof(uint)
                && status is not M2paLinkStatus.ProvingNormal
                    and not M2paLinkStatus.ProvingEmergency)
            {
                error = "Only M2PA proving status messages may carry filler bytes.";
                return false;
            }

            message = new(type, bsn, fsn, data.Slice(4).ToArray(), status);
        }
        else
        {
            message = new(type, bsn, fsn, data.ToArray(), linkStatus: null);
        }

        error = null;
        return true;
    }

    /// <summary>Encodes one User Data or acknowledgement-only message.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="backwardSequenceNumber">The FSN most recently accepted from the peer.</param>
    /// <param name="forwardSequenceNumber">The local forward sequence number.</param>
    /// <param name="payload">The optional MTP3 service data unit.</param>
    /// <param name="written">The number of encoded bytes.</param>
    /// <param name="error">The encode error.</param>
    /// <returns>True when the message was encoded.</returns>
    public static bool TryEncodeUserData(
        Span<byte> destination,
        uint backwardSequenceNumber,
        uint forwardSequenceNumber,
        ReadOnlySpan<byte> payload,
        out int written,
        out string? error)
    {
        return TryEncode(
            destination,
            M2paMessageType.UserData,
            backwardSequenceNumber,
            forwardSequenceNumber,
            payload,
            out written,
            out error);
    }

    /// <summary>Encodes one Link Status message.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="backwardSequenceNumber">The FSN most recently accepted from the peer.</param>
    /// <param name="forwardSequenceNumber">The local forward sequence number.</param>
    /// <param name="status">The link status.</param>
    /// <param name="provingFiller">Optional proving filler bytes.</param>
    /// <param name="written">The number of encoded bytes.</param>
    /// <param name="error">The encode error.</param>
    /// <returns>True when the message was encoded.</returns>
    public static bool TryEncodeLinkStatus(
        Span<byte> destination,
        uint backwardSequenceNumber,
        uint forwardSequenceNumber,
        M2paLinkStatus status,
        ReadOnlySpan<byte> provingFiller,
        out int written,
        out string? error)
    {
        written = 0;
        if (!provingFiller.IsEmpty
            && status is not M2paLinkStatus.ProvingNormal
                and not M2paLinkStatus.ProvingEmergency)
        {
            error = "Only proving status messages may carry filler bytes.";
            return false;
        }

        byte[] data = new byte[sizeof(uint) + provingFiller.Length];
        BinaryPrimitives.WriteUInt32BigEndian(data, (uint)status);
        provingFiller.CopyTo(data.AsSpan(sizeof(uint)));
        return TryEncode(
            destination,
            M2paMessageType.LinkStatus,
            backwardSequenceNumber,
            forwardSequenceNumber,
            data,
            out written,
            out error);
    }

    private static bool TryEncode(
        Span<byte> destination,
        M2paMessageType messageType,
        uint backwardSequenceNumber,
        uint forwardSequenceNumber,
        ReadOnlySpan<byte> data,
        out int written,
        out string? error)
    {
        written = 0;
        if (backwardSequenceNumber > M2paProtocol.MaximumSequenceNumber
            || forwardSequenceNumber > M2paProtocol.MaximumSequenceNumber)
        {
            error = "M2PA BSN and FSN must fit in 24 bits.";
            return false;
        }

        int length = M2paProtocol.MinimumMessageLength + data.Length;
        if (destination.Length < length)
        {
            error = $"M2PA destination requires {length} bytes.";
            return false;
        }

        destination[0] = M2paProtocol.Version;
        destination[1] = 0;
        destination[2] = M2paProtocol.MessageClass;
        destination[3] = (byte)messageType;
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), (uint)length);
        destination[8] = 0;
        WriteUInt24(destination.Slice(9, 3), backwardSequenceNumber);
        destination[12] = 0;
        WriteUInt24(destination.Slice(13, 3), forwardSequenceNumber);
        data.CopyTo(destination.Slice(M2paProtocol.MinimumMessageLength));
        written = length;
        error = null;
        return true;
    }

    private static uint ReadUInt24(ReadOnlySpan<byte> bytes)
    {
        return ((uint)bytes[0] << 16) | ((uint)bytes[1] << 8) | bytes[2];
    }

    private static void WriteUInt24(Span<byte> bytes, uint value)
    {
        bytes[0] = (byte)(value >> 16);
        bytes[1] = (byte)(value >> 8);
        bytes[2] = (byte)value;
    }
}
