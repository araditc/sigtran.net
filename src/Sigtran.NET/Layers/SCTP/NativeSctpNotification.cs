using System.Buffers.Binary;
using System.Net;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>Identifies the subscribed Linux SCTP notification records.</summary>
internal enum SctpKernelNotificationType : ushort
{
    AssociationChange = 0x8001,
    PeerAddressChange = 0x8002,
    Shutdown = 0x8005,
    PartialDelivery = 0x8006
}

/// <summary>Contains decoded fields from a Linux SCTP kernel notification.</summary>
internal sealed record SctpKernelNotification(
    SctpKernelNotificationType Type, int AssociationId, int StateCode = 0, int ErrorCode = 0,
    SctpEndpoint? PeerEndpoint = null, ushort OutboundStreams = 0, ushort InboundStreams = 0);

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
                    BitConverter.ToUInt16(data[10..]), OutboundStreams: BitConverter.ToUInt16(data[12..]),
                    InboundStreams: BitConverter.ToUInt16(data[14..])),
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
