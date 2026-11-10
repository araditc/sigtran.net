using System.Buffers.Binary;
using System.Net;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>Identifies the subscribed Linux SCTP notification records.</summary>
public enum SctpKernelNotificationType : ushort
{
    /// <summary>The association lifecycle changed.</summary>
    AssociationChange = 0x8001,
    /// <summary>A peer path changed reachability or primary status.</summary>
    PeerAddressChange = 0x8002,
    /// <summary>The peer initiated graceful shutdown.</summary>
    Shutdown = 0x8005,
    /// <summary>The kernel reported a partial-delivery event.</summary>
    PartialDelivery = 0x8006
}

/// <summary>Contains decoded fields from a Linux SCTP kernel notification.</summary>
public sealed class SctpKernelNotification
{
    internal SctpKernelNotification(SctpKernelNotificationType type, int associationId, int stateCode = 0,
        int errorCode = 0, SctpEndpoint? peerEndpoint = null, ushort outboundStreams = 0, ushort inboundStreams = 0)
    {
        Type = type;
        AssociationId = associationId;
        StateCode = stateCode;
        ErrorCode = errorCode;
        PeerEndpoint = peerEndpoint;
        OutboundStreams = outboundStreams;
        InboundStreams = inboundStreams;
    }

    /// <summary>The Linux notification type.</summary>
    public SctpKernelNotificationType Type { get; }
    /// <summary>The association ID allocated by this host's kernel.</summary>
    public int AssociationId { get; }
    /// <summary>The native state or indication code for this notification type.</summary>
    public int StateCode { get; }
    /// <summary>The native error code, or zero when absent.</summary>
    public int ErrorCode { get; }
    /// <summary>The affected peer endpoint for a path notification.</summary>
    public SctpEndpoint? PeerEndpoint { get; }
    /// <summary>The negotiated outbound streams on an association-change notification.</summary>
    public ushort OutboundStreams { get; }
    /// <summary>The negotiated inbound streams on an association-change notification.</summary>
    public ushort InboundStreams { get; }
}

/// <summary>Reads the Linux SCTP UAPI notification layout, never network wire data.</summary>
internal static class NativeSctpNotification
{
    internal static bool TryDecode(ReadOnlySpan<byte> data, out SctpKernelNotification? notification)
    {
        notification = null;
        if (data.Length < 8 || BitConverter.ToUInt32(data[4..]) != (uint)data.Length)
        {
            return false;
        }

        SctpKernelNotificationType type = (SctpKernelNotificationType)BitConverter.ToUInt16(data);
        notification = type switch
        {
            // Association records can carry extra information after the fixed fields.
            SctpKernelNotificationType.AssociationChange when data.Length >= 20 =>
                new(type, BitConverter.ToInt32(data[16..]), BitConverter.ToUInt16(data[8..]),
                    BitConverter.ToUInt16(data[10..]), outboundStreams: BitConverter.ToUInt16(data[12..]),
                    inboundStreams: BitConverter.ToUInt16(data[14..])),
            // The packed Linux peer change structure embeds a 128-byte sockaddr_storage.
            SctpKernelNotificationType.PeerAddressChange when data.Length == 148 && BitConverter.ToUInt16(data[8..]) == 2 =>
                ReadPeerChange(data),
            SctpKernelNotificationType.Shutdown when data.Length == 12 => new(type, BitConverter.ToInt32(data[8..])),
            SctpKernelNotificationType.PartialDelivery when data.Length == 24 =>
                new(type, BitConverter.ToInt32(data[12..]), BitConverter.ToInt32(data[8..])),
            _ => null
        };
        return notification is not null;
    }

    private static SctpKernelNotification? ReadPeerChange(ReadOnlySpan<byte> data)
    {
        int port = BinaryPrimitives.ReadUInt16BigEndian(data[10..]);
        IPAddress ip = new(data[12..16]);
        if (port == 0 || !NativeSctpAddressSet.IsExplicitIpv4(ip))
        {
            return null;
        }

        return new(SctpKernelNotificationType.PeerAddressChange, BitConverter.ToInt32(data[144..]),
            BitConverter.ToInt32(data[136..]), BitConverter.ToInt32(data[140..]), new(ip.ToString(), port));
    }
}
