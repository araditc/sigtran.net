using System.Net.Sockets;

namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Exception thrown when native SCTP is not available on the current platform.
/// </summary>
public sealed class NativeSctpUnavailableException : InvalidOperationException
{
    /// <summary>Creates a native SCTP unavailable exception.</summary>
    /// <param name="capability">The platform capability.</param>
    public NativeSctpUnavailableException(NativeSctpPlatformCapability capability)
        : base($"Native SCTP is unavailable: {capability.Describe()}")
    {
        Capability = capability;
    }

    /// <summary>The platform capability that caused the failure.</summary>
    public NativeSctpPlatformCapability Capability { get; }
}

/// <summary>
/// Creates native SCTP sockets.
/// </summary>
public interface INativeSctpSocketFactory
{
    /// <summary>Creates an unconnected native SCTP socket.</summary>
    /// <returns>The native SCTP socket.</returns>
    Socket CreateSocket();
}

/// <summary>
/// Default native SCTP socket factory.
/// </summary>
public sealed class NativeSctpSocketFactory : INativeSctpSocketFactory
{
    /// <inheritdoc />
    public Socket CreateSocket()
    {
        NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();
        if (!capability.CanCreateSocket)
        {
            throw new NativeSctpUnavailableException(capability);
        }

        return new Socket(AddressFamily.InterNetwork, NativeSctpPlatform.SctpSocketType, NativeSctpPlatform.SctpProtocolType);
    }
}
