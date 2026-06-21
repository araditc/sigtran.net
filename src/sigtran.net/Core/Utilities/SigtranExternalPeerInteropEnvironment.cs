namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the OpenSS7/IPSS7 interoperability execution environment.
/// </summary>
public sealed class SigtranExternalPeerInteropEnvironment
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability environment.</summary>
    /// <param name="name">The environment name.</param>
    /// <param name="requiresLinux">Whether Linux is required.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresExternalPeer">Whether an OpenSS7/IPSS7 peer is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture is required.</param>
    public SigtranExternalPeerInteropEnvironment(
        string name,
        bool requiresLinux,
        bool requiresNativeSctp,
        bool requiresExternalPeer,
        bool requiresPacketCapture)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Environment name is required.", nameof(name)) : name;
        RequiresLinux = requiresLinux;
        RequiresNativeSctp = requiresNativeSctp;
        RequiresExternalPeer = requiresExternalPeer;
        RequiresPacketCapture = requiresPacketCapture;
    }

    /// <summary>The environment name.</summary>
    public string Name { get; }

    /// <summary>Whether Linux is required.</summary>
    public bool RequiresLinux { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether an OpenSS7/IPSS7 peer is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Whether packet capture is required.</summary>
    public bool RequiresPacketCapture { get; }

    /// <summary>Whether the environment has the minimum OpenSS7/IPSS7 lab prerequisites.</summary>
    public bool HasMinimumLabPrerequisites => RequiresLinux && RequiresNativeSctp && RequiresExternalPeer && RequiresPacketCapture;
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability environment helpers.
/// </summary>
public static class SigtranExternalPeerInteropEnvironments
{
    /// <summary>Creates the default OpenSS7/IPSS7 lab environment.</summary>
    /// <returns>The default OpenSS7/IPSS7 lab environment.</returns>
    public static SigtranExternalPeerInteropEnvironment CreateDefault()
    {
        return new(
            "external-sigtran-peer-linux-lab",
            requiresLinux: true,
            requiresNativeSctp: true,
            requiresExternalPeer: true,
            requiresPacketCapture: true);
    }
}
