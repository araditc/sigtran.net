namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the external peer interoperability execution environment.
/// </summary>
public sealed class SigtranExternalPeerInteropEnvironment
{
    /// <summary>Creates an external peer interoperability environment.</summary>
    /// <param name="name">The environment name.</param>
    /// <param name="requiresLinux">Whether Linux is required.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresExternalPeer">Whether an external SIGTRAN peer is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture is required.</param>
    /// <param name="artifactRoot">The artifact root used by the lab run.</param>
    /// <param name="requiredTools">The required lab tools.</param>
    /// <param name="requiresSdkTrace">Whether SDK trace capture is required.</param>
    /// <param name="requiresPeerConfiguration">Whether peer configuration capture is required.</param>
    /// <param name="requiresPeerLog">Whether peer log capture is required.</param>
    public SigtranExternalPeerInteropEnvironment(
        string name,
        bool requiresLinux,
        bool requiresNativeSctp,
        bool requiresExternalPeer,
        bool requiresPacketCapture,
        string artifactRoot = "artifacts/external-peer",
        IReadOnlyList<string>? requiredTools = null,
        bool requiresSdkTrace = true,
        bool requiresPeerConfiguration = true,
        bool requiresPeerLog = true)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Environment name is required.", nameof(name)) : name;
        RequiresLinux = requiresLinux;
        RequiresNativeSctp = requiresNativeSctp;
        RequiresExternalPeer = requiresExternalPeer;
        RequiresPacketCapture = requiresPacketCapture;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        RequiredTools = requiredTools is null || requiredTools.Count == 0
            ? ["dotnet", "tcpdump", "tshark", "sctp-tools"]
            : requiredTools.ToArray();
        RequiresSdkTrace = requiresSdkTrace;
        RequiresPeerConfiguration = requiresPeerConfiguration;
        RequiresPeerLog = requiresPeerLog;
    }

    /// <summary>The environment name.</summary>
    public string Name { get; }

    /// <summary>Whether Linux is required.</summary>
    public bool RequiresLinux { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether an external SIGTRAN peer is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Whether packet capture is required.</summary>
    public bool RequiresPacketCapture { get; }

    /// <summary>The artifact root used by the lab run.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The required lab tools.</summary>
    public IReadOnlyList<string> RequiredTools { get; }

    /// <summary>Whether SDK trace capture is required.</summary>
    public bool RequiresSdkTrace { get; }

    /// <summary>Whether peer configuration capture is required.</summary>
    public bool RequiresPeerConfiguration { get; }

    /// <summary>Whether peer log capture is required.</summary>
    public bool RequiresPeerLog { get; }

    /// <summary>Whether the environment has the minimum external peer lab prerequisites.</summary>
    public bool HasMinimumLabPrerequisites => RequiresLinux && RequiresNativeSctp && RequiresExternalPeer && RequiresPacketCapture;

    /// <summary>Whether the environment can produce production interop evidence artifacts.</summary>
    public bool CanProduceProductionArtifacts => HasMinimumLabPrerequisites
        && RequiresSdkTrace
        && RequiresPeerConfiguration
        && RequiresPeerLog
        && RequiredTools.Count >= 4;
}

/// <summary>
/// Provides external peer interoperability environment helpers.
/// </summary>
public static class SigtranExternalPeerInteropEnvironments
{
    /// <summary>Creates the default external peer lab environment.</summary>
    /// <returns>The default external peer lab environment.</returns>
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
