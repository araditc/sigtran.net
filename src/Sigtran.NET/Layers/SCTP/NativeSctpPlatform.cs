using System.Net.Sockets;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Identifies native SCTP platform capability status.
/// </summary>
public enum NativeSctpPlatformStatus
{
    /// <summary>The operating system is not a supported native SCTP target.</summary>
    UnsupportedOperatingSystem,

    /// <summary>The operating system is supported, but socket creation failed.</summary>
    SocketCreationUnavailable,

    /// <summary>The operating system supports creating an SCTP socket.</summary>
    SocketCreationSupported
}

/// <summary>
/// Describes native SCTP platform capability.
/// </summary>
public sealed class NativeSctpPlatformCapability
{
    /// <summary>Creates a native SCTP platform capability result.</summary>
    /// <param name="status">The capability status.</param>
    /// <param name="reason">The diagnostic reason.</param>
    public NativeSctpPlatformCapability(NativeSctpPlatformStatus status, string reason)
    {
        Status = status;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("Native SCTP capability reason is required.", nameof(reason)) : reason;
    }

    /// <summary>The capability status.</summary>
    public NativeSctpPlatformStatus Status { get; }

    /// <summary>The diagnostic reason.</summary>
    public string Reason { get; }

    /// <summary>Whether native SCTP socket creation is supported.</summary>
    public bool CanCreateSocket => Status == NativeSctpPlatformStatus.SocketCreationSupported;

    /// <summary>Formats a compact capability summary.</summary>
    /// <returns>The capability summary.</returns>
    public string Describe()
    {
        return $"nativeSctp status={Status} canCreateSocket={CanCreateSocket} reason={Reason}";
    }
}

/// <summary>
/// Provides native SCTP platform constants and capability probing.
/// </summary>
public static class NativeSctpPlatform
{
    /// <summary>The Linux IPPROTO_SCTP protocol number.</summary>
    public const int IpProtocolSctp = 132;

    /// <summary>The socket type used by SCTP one-to-one style associations.</summary>
    public const SocketType SctpSocketType = SocketType.Stream;

    /// <summary>Gets the protocol type value used when creating native SCTP sockets.</summary>
    public static ProtocolType SctpProtocolType => (ProtocolType)IpProtocolSctp;

    /// <summary>Returns whether the current operating system is the supported native SCTP target.</summary>
    /// <returns>True on Linux; otherwise false.</returns>
    public static bool IsSupportedOperatingSystem()
    {
        return OperatingSystem.IsLinux();
    }

    /// <summary>Probes whether a native SCTP socket can be created.</summary>
    /// <returns>The native SCTP platform capability.</returns>
    public static NativeSctpPlatformCapability Probe()
    {
        if (!IsSupportedOperatingSystem())
        {
            return new(NativeSctpPlatformStatus.UnsupportedOperatingSystem, RuntimeInformation.OSDescription);
        }

        if (TryCreateSocket(out Socket? socket, out string reason))
        {
            socket!.Dispose();
            return new(NativeSctpPlatformStatus.SocketCreationSupported, "SCTP socket creation succeeded.");
        }

        return new(NativeSctpPlatformStatus.SocketCreationUnavailable, reason);
    }

    internal static bool TryCreateSocket(out Socket? socket, out string reason)
    {
        socket = null;
        if (!IsSupportedOperatingSystem())
        {
            reason = RuntimeInformation.OSDescription;
            return false;
        }

        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SctpSocketType, SctpProtocolType);
            reason = "managed-socket";
            return true;
        }
        catch (Exception managedException) when (managedException is SocketException or PlatformNotSupportedException)
        {
            try
            {
                int handle = LibcSocket(AddressFamilyInterNetwork, SocketTypeStream, IpProtocolSctp);
                if (handle < 0)
                {
                    reason = $"managed={managedException.Message}; libc-errno={Marshal.GetLastPInvokeError()}";
                    return false;
                }

                socket = new Socket(new SafeSocketHandle(new IntPtr(handle), ownsHandle: true));
                reason = "libc-socket";
                return true;
            }
            catch (Exception nativeException)
            {
                reason = $"managed={managedException.Message}; libc={nativeException.Message}";
                return false;
            }
        }
    }

    private const int AddressFamilyInterNetwork = 2;
    private const int SocketTypeStream = 1;

    [DllImport("libc", EntryPoint = "socket", SetLastError = true)]
    private static extern int LibcSocket(int domain, int type, int protocol);
}
