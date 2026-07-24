using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Provides Linux SCTP metadata send and receive calls backed by lksctp.
/// </summary>
internal static class NativeSctpInterop
{
    private const int SolSctp = 132;
    private const int SctpInitMsgOption = 2;
    private const int SctpNoDelayOption = 3;
    private const int SctpEventsOption = 11;
    private const int MessageNotificationFlag = 0x8000;
    private const ushort SctpUnorderedFlag = 0x0001;

    public static void ConfigureSocket(
        Socket socket,
        ushort outboundStreams,
        ushort inboundStreams,
        bool enableNoDelay)
    {
        ArgumentNullException.ThrowIfNull(socket);

        SctpInitMessage init = new()
        {
            OutboundStreams = outboundStreams,
            MaxInboundStreams = inboundStreams,
            MaxAttempts = 0,
            MaxInitTimeout = 0
        };

        SetSocketOption(socket, SctpInitMsgOption, ToBytes(init));
        if (enableNoDelay)
        {
            EnableNoDelay(socket);
        }

        EnableReceiveMetadata(socket);
    }

    public static void EnableReceiveMetadata(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        byte[] events = new byte[13];
        events[0] = 1; // sctp_data_io_event: required for sctp_sndrcvinfo on receive.
        SetSocketOption(socket, SctpEventsOption, events);
    }

    public static void EnableNoDelay(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        SetSocketOption(socket, SctpNoDelayOption, BitConverter.GetBytes(1));
    }

    public static int SendMessage(Socket socket, SctpOutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(message);

        byte[]? rented = null;
        byte[] payload;
        if (MemoryMarshal.TryGetArray(
                message.Payload,
                out ArraySegment<byte> segment)
            && segment.Offset == 0)
        {
            payload = segment.Array!;
        }
        else
        {
            rented = ArrayPool<byte>.Shared.Rent(message.Payload.Length);
            message.Payload.CopyTo(rented);
            payload = rented;
        }

        try
        {
            uint networkPpid = HostToNetwork(
                message.Metadata.PayloadProtocolIdentifier);
            uint flags = message.Metadata.Unordered ? SctpUnorderedFlag : 0u;
            nint sent = SctpSendMsg(
                ToFileDescriptor(socket),
                payload,
                (nuint)message.Payload.Length,
                IntPtr.Zero,
                0,
                networkPpid,
                flags,
                message.Metadata.StreamId,
                0,
                0);

            if (sent < 0)
            {
                throw new SocketException(Marshal.GetLastPInvokeError());
            }

            return checked((int)sent);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public static SctpReceiveResult ReceiveMessage(Socket socket, Memory<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(socket);
        if (buffer.IsEmpty)
        {
            throw new ArgumentException("Receive buffer must not be empty.", nameof(buffer));
        }

        byte[]? rented = null;
        byte[] payload;
        ReadOnlyMemory<byte> readOnlyBuffer = buffer;
        if (MemoryMarshal.TryGetArray(
                readOnlyBuffer,
                out ArraySegment<byte> segment)
            && segment.Offset == 0)
        {
            payload = segment.Array!;
        }
        else
        {
            rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            payload = rented;
        }

        try
        {
            while (true)
            {
                SctpSndRcvInfo info = default;
                int messageFlags = 0;
                nint received = SctpRecvMsg(
                    ToFileDescriptor(socket),
                    payload,
                    (nuint)buffer.Length,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out info,
                    out messageFlags);

                if (received < 0)
                {
                    throw new SocketException(Marshal.GetLastPInvokeError());
                }

                if ((messageFlags & MessageNotificationFlag) != 0)
                {
                    continue;
                }

                int byteCount = checked((int)received);
                if (rented is not null)
                {
                    payload.AsMemory(0, byteCount).CopyTo(buffer);
                }

                SctpPayloadMetadata metadata = new(
                    info.Stream,
                    NetworkToHost(info.PayloadProtocolIdentifier),
                    (info.Flags & SctpUnorderedFlag) != 0);
                return new(byteCount, metadata);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static int ToFileDescriptor(Socket socket)
    {
        return checked((int)socket.SafeHandle.DangerousGetHandle().ToInt64());
    }

    private static void SetSocketOption(Socket socket, int optionName, byte[] optionValue)
    {
        int result = SetSocketOptionNative(
            ToFileDescriptor(socket),
            SolSctp,
            optionName,
            optionValue,
            (uint)optionValue.Length);
        if (result != 0)
        {
            throw new SocketException(Marshal.GetLastPInvokeError());
        }
    }

    private static byte[] ToBytes<T>(T value)
        where T : struct
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray();
    }

    private static uint HostToNetwork(uint value)
    {
        return unchecked((uint)IPAddress.HostToNetworkOrder(unchecked((int)value)));
    }

    private static uint NetworkToHost(uint value)
    {
        return unchecked((uint)IPAddress.NetworkToHostOrder(unchecked((int)value)));
    }

    [DllImport("libsctp.so.1", EntryPoint = "sctp_sendmsg", SetLastError = true)]
    private static extern nint SctpSendMsg(
        int socket,
        byte[] message,
        nuint length,
        IntPtr to,
        uint toLength,
        uint payloadProtocolIdentifier,
        uint flags,
        ushort streamNumber,
        uint timeToLive,
        uint context);

    [DllImport("libsctp.so.1", EntryPoint = "sctp_recvmsg", SetLastError = true)]
    private static extern nint SctpRecvMsg(
        int socket,
        byte[] message,
        nuint length,
        IntPtr from,
        IntPtr fromLength,
        out SctpSndRcvInfo info,
        out int messageFlags);

    [DllImport("libc", EntryPoint = "setsockopt", SetLastError = true)]
    private static extern int SetSocketOptionNative(
        int socket,
        int level,
        int optionName,
        byte[] optionValue,
        uint optionLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct SctpInitMessage
    {
        public ushort OutboundStreams;
        public ushort MaxInboundStreams;
        public ushort MaxAttempts;
        public ushort MaxInitTimeout;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SctpSndRcvInfo
    {
        public ushort Stream;
        public ushort StreamSequenceNumber;
        public ushort Flags;
        public uint PayloadProtocolIdentifier;
        public uint Context;
        public uint TimeToLive;
        public uint TransmissionSequenceNumber;
        public uint CumulativeTransmissionSequenceNumber;
        public int AssociationId;
    }
}
