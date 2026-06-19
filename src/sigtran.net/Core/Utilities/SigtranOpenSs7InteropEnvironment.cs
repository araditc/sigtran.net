namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the OpenSS7/IPSS7 interoperability execution environment.
/// </summary>
public sealed class SigtranOpenSs7InteropEnvironment
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability environment.</summary>
    /// <param name="name">The environment name.</param>
    /// <param name="requiresLinux">Whether Linux is required.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresOpenSs7Peer">Whether an OpenSS7/IPSS7 peer is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture is required.</param>
    public SigtranOpenSs7InteropEnvironment(
        string name,
        bool requiresLinux,
        bool requiresNativeSctp,
        bool requiresOpenSs7Peer,
        bool requiresPacketCapture)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Environment name is required.", nameof(name)) : name;
        RequiresLinux = requiresLinux;
        RequiresNativeSctp = requiresNativeSctp;
        RequiresOpenSs7Peer = requiresOpenSs7Peer;
        RequiresPacketCapture = requiresPacketCapture;
    }

    /// <summary>The environment name.</summary>
    public string Name { get; }

    /// <summary>Whether Linux is required.</summary>
    public bool RequiresLinux { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether an OpenSS7/IPSS7 peer is required.</summary>
    public bool RequiresOpenSs7Peer { get; }

    /// <summary>Whether packet capture is required.</summary>
    public bool RequiresPacketCapture { get; }

    /// <summary>Whether the environment has the minimum OpenSS7/IPSS7 lab prerequisites.</summary>
    public bool HasMinimumLabPrerequisites => RequiresLinux && RequiresNativeSctp && RequiresOpenSs7Peer && RequiresPacketCapture;
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability environment helpers.
/// </summary>
public static class SigtranOpenSs7InteropEnvironments
{
    /// <summary>Creates the default OpenSS7/IPSS7 lab environment.</summary>
    /// <returns>The default OpenSS7/IPSS7 lab environment.</returns>
    public static SigtranOpenSs7InteropEnvironment CreateDefault()
    {
        return new(
            "openss7-ipss7-linux-lab",
            requiresLinux: true,
            requiresNativeSctp: true,
            requiresOpenSs7Peer: true,
            requiresPacketCapture: true);
    }
}
