using System.Net.Sockets;
using System.Runtime.InteropServices;
using Sigtran.NET.Layers.SCTP;

/// <summary>Applies an opt-in LAN workload profile to owned sockets, never SDK or host defaults.</summary>
internal sealed class CapacitySocketFactory(bool lanProfile) : INativeSctpSocketFactory
{
    internal const string LanProfile = "lan-bounded-v1";
    internal const string DefaultProfile = "system-default";
    private Socket? _socket;
    internal int LocalPort => _socket?.LocalEndPoint is System.Net.IPEndPoint endpoint
        ? endpoint.Port : throw new InvalidOperationException("No connected local endpoint exists.");

    /// <inheritdoc />
    public Socket CreateSocket()
    {
        Socket socket = new NativeSctpSocketFactory().CreateSocket();
        try
        {
            if (lanProfile)
            {
                socket.SendBufferSize = 1024 * 1024;
                socket.ReceiveBufferSize = 1024 * 1024;
                byte[] value = EncodeRto(300, 200, 1000);
                WithHandle(socket, fd => SetOption(fd, 132, 0, value, (uint)value.Length));
            }
            _socket = socket;
            return socket;
        }
        catch { socket.Dispose(); throw; }
    }

    /// <summary>Reads effective kernel values after connect; requests may be clamped by host limits.</summary>
    internal CapacitySocketProfile Snapshot()
    {
        Socket socket = _socket ?? throw new InvalidOperationException("No owned socket exists.");
        byte[] value = new byte[16];
        uint length = (uint)value.Length;
        WithHandle(socket, fd => GetOption(fd, 132, 0, value, ref length));
        if (length != value.Length) throw new InvalidOperationException("Unexpected Linux SCTP RTO ABI.");
        if (lanProfile && (BitConverter.ToUInt32(value, 4) != 300 || BitConverter.ToUInt32(value, 8) != 1000
            || BitConverter.ToUInt32(value, 12) != 200))
            throw new InvalidOperationException("Requested LAN RTO profile was not effective.");
        return new(lanProfile ? LanProfile : DefaultProfile, socket.SendBufferSize, socket.ReceiveBufferSize,
            BitConverter.ToUInt32(value, 4), BitConverter.ToUInt32(value, 12), BitConverter.ToUInt32(value, 8));
    }

    /// <summary>Linux sctp_rtoinfo consists of four native-endian 32-bit fields; association zero is ignored for one-to-one sockets.</summary>
    internal static byte[] EncodeRto(uint initial, uint minimum, uint maximum)
    {
        if (minimum == 0 || minimum > initial || initial > maximum || maximum > 60_000)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        byte[] value = new byte[16];
        BitConverter.TryWriteBytes(value.AsSpan(4), initial);
        BitConverter.TryWriteBytes(value.AsSpan(8), maximum);
        BitConverter.TryWriteBytes(value.AsSpan(12), minimum);
        return value;
    }

    private static void WithHandle(Socket socket, Func<int, int> action)
    {
        bool held = false;
        try
        {
            socket.SafeHandle.DangerousAddRef(ref held);
            if (action(socket.SafeHandle.DangerousGetHandle().ToInt32()) != 0)
                throw new SocketException(Marshal.GetLastPInvokeError());
        }
        finally { if (held) socket.SafeHandle.DangerousRelease(); }
    }

    [DllImport("libc", EntryPoint = "setsockopt", SetLastError = true)]
    private static extern int SetOption(int fd, int level, int option, byte[] value, uint length);

    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    private static extern int GetOption(int fd, int level, int option, [Out] byte[] value, ref uint length);
}

/// <summary>Observed per-socket settings, not inferred sysctl values or a public-network recommendation.</summary>
internal sealed record CapacitySocketProfile(string Name, int SendBufferBytes, int ReceiveBufferBytes,
    uint RtoInitialMilliseconds, uint RtoMinimumMilliseconds, uint RtoMaximumMilliseconds);
