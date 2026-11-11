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
    private const int SctpBindxAddAddress = 1;
    private const int Interrupted = 4;
    private const int WouldBlock = 11;
    private const int ConnectAlreadyInProgress = 114;
    private const int ConnectInProgress = 115;
    private const ushort SctpUnorderedFlag = 0x0001;

    internal static void BindAddresses(Socket socket, IReadOnlyList<IPEndPoint> addresses)
    {
        byte[] packed = NativeSctpAddressSet.PackLinuxIpv4(addresses);
        using NativeSctpHandle handle = new(socket);
        if (SctpBindx(handle.FileDescriptor, packed, addresses.Count, SctpBindxAddAddress) != 0)
        {
            throw new SocketException(Marshal.GetLastPInvokeError());
        }
    }

    internal static void BindLocal(Socket socket, IPEndPoint[] addresses)
    {
        if (addresses.Length == 0)
        {
            return;
        }

        // Managed bind establishes the endpoint type required by Socket.AcceptAsync.
        socket.Bind(addresses[0]);
        if (addresses.Length > 1)
        {
            BindAddresses(socket, addresses[1..]);
        }
    }

    internal static async Task ConnectAddressesAsync(Socket socket, IReadOnlyList<IPEndPoint> addresses, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        byte[] packed = NativeSctpAddressSet.PackLinuxIpv4(addresses);
        socket.Blocking = false;
        if (StartConnect(socket, packed, addresses.Count))
        {
            return;
        }

        // A writable nonblocking socket can indicate either success or failure.
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (socket.Poll(0, SelectMode.SelectWrite) || socket.Poll(0, SelectMode.SelectError))
            {
                int error = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error)!;
                if (error != 0)
                {
                    throw new SocketException(error);
                }

                return;
            }

            await Task.Delay(10, ct).ConfigureAwait(false);
        }
    }

    private static bool StartConnect(Socket socket, byte[] packed, int addressCount)
    {
        using NativeSctpHandle handle = new(socket);
        if (SctpConnectx(handle.FileDescriptor, packed, addressCount, IntPtr.Zero) == 0)
        {
            return true;
        }

        int error = Marshal.GetLastPInvokeError();
        return error is Interrupted or ConnectAlreadyInProgress or ConnectInProgress
            ? false
            : throw new SocketException(error);
    }

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
        events[1] = 1; // Association state and negotiated streams.
        events[2] = 1; // Peer address reachability.
        events[5] = 1; // Peer shutdown.
        events[6] = 1; // Partial delivery aborted.
        SetSocketOption(socket, SctpEventsOption, events);
    }

    public static void EnableNoDelay(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        SetSocketOption(socket, SctpNoDelayOption, BitConverter.GetBytes(1));
    }

    internal static bool TrySendMessage(Socket socket, SctpOutboundMessage message, out int byteCount)
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
            byteCount = 0;
            uint networkPpid = HostToNetwork(
                message.Metadata.PayloadProtocolIdentifier);
            uint flags = message.Metadata.Unordered ? SctpUnorderedFlag : 0u;
            using NativeSctpHandle handle = new(socket);
            int sent = SctpSendMsg(
                handle.FileDescriptor,
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
                int error = Marshal.GetLastPInvokeError();
                return error is WouldBlock or Interrupted ? false : throw new SocketException(error);
            }

            byteCount = sent;
            return true;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static bool TryReceiveFrame(Socket socket, Memory<byte> buffer, out NativeSctpReceiveFrame frame)
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
            using NativeSctpHandle handle = new(socket);
            int received = SctpRecvMsg(
                handle.FileDescriptor, payload, (nuint)buffer.Length, IntPtr.Zero, IntPtr.Zero,
                out SctpSndRcvInfo info, out int messageFlags);
            if (received < 0)
            {
                frame = default;
                int error = Marshal.GetLastPInvokeError();
                return error is WouldBlock or Interrupted ? false : throw new SocketException(error);
            }

            int byteCount = received;
            SctpKernelNotification? notification = null;
            if ((messageFlags & NativeSctpReceiveFrame.NotificationFlag) != 0)
            {
                NativeSctpNotification.TryDecode(payload.AsSpan(0, byteCount), out notification);
            }
            else if (rented is not null)
            {
                payload.AsMemory(0, byteCount).CopyTo(buffer);
            }

            SctpPayloadMetadata metadata = new(info.Stream, NetworkToHost(info.PayloadProtocolIdentifier),
                (info.Flags & SctpUnorderedFlag) != 0);
            frame = new(byteCount, metadata, messageFlags, notification);
            return true;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void SetSocketOption(Socket socket, int optionName, byte[] optionValue)
    {
        using NativeSctpHandle handle = new(socket);
        int result = SetSocketOptionNative(
            handle.FileDescriptor,
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

    [DllImport("libsctp.so.1", EntryPoint = "sctp_bindx", SetLastError = true)]
    private static extern int SctpBindx(int socket, byte[] addresses, int addressCount, int flags);

    [DllImport("libsctp.so.1", EntryPoint = "sctp_connectx", SetLastError = true)]
    private static extern int SctpConnectx(int socket, byte[] addresses, int addressCount, IntPtr associationId);

    [DllImport("libsctp.so.1", EntryPoint = "sctp_sendmsg", SetLastError = true)]
    private static extern int SctpSendMsg(
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
    private static extern int SctpRecvMsg(
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

/// <summary>Represents one recvmsg return, which may be a notification or part of a user record.</summary>
internal readonly record struct NativeSctpReceiveFrame(int ByteCount, SctpPayloadMetadata Metadata,
    int Flags, SctpKernelNotification? Notification)
{
    internal const int NotificationFlag = 0x8000;
    internal bool IsNotification => (Flags & NotificationFlag) != 0;
    internal bool IsEndOfRecord => (Flags & 0x80) != 0; // Linux MSG_EOR.
    internal bool IsTruncated => (Flags & 0x20) != 0; // Linux MSG_TRUNC.
}

/// <summary>Keeps an owning socket handle alive for the duration of a synchronous native call.</summary>
internal readonly ref struct NativeSctpHandle
{
    private readonly SafeSocketHandle _handle;

    internal NativeSctpHandle(Socket socket)
    {
        _handle = socket.SafeHandle;
        bool referenced = false;
        _handle.DangerousAddRef(ref referenced);
    }

    internal int FileDescriptor => _handle.DangerousGetHandle().ToInt32();

    public void Dispose() => _handle.DangerousRelease();
}
