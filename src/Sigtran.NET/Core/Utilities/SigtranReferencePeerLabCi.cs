namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab CI execution policy.
/// </summary>
public sealed class SigtranReferencePeerLabCiProfile
{
    /// <summary>Creates a reference external peer lab CI profile.</summary>
    /// <param name="name">The CI profile name.</param>
    /// <param name="manualDispatchOnly">Whether the lab is manual-dispatch only.</param>
    /// <param name="requiresSelfHostedLinux">Whether a self-hosted Linux runner is required.</param>
    /// <param name="requiredEnvironmentVariables">The required environment variables.</param>
    /// <param name="artifactPatterns">The retained artifact upload patterns.</param>
    public SigtranReferencePeerLabCiProfile(
        string name,
        bool manualDispatchOnly,
        bool requiresSelfHostedLinux,
        IReadOnlyList<string> requiredEnvironmentVariables,
        IReadOnlyList<string> artifactPatterns)
    {
        ArgumentNullException.ThrowIfNull(requiredEnvironmentVariables);
        ArgumentNullException.ThrowIfNull(artifactPatterns);

        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("CI profile name is required.", nameof(name)) : name;
        ManualDispatchOnly = manualDispatchOnly;
        RequiresSelfHostedLinux = requiresSelfHostedLinux;
        RequiredEnvironmentVariables = requiredEnvironmentVariables.Count == 0
            ? throw new ArgumentException("At least one environment variable is required.", nameof(requiredEnvironmentVariables))
            : requiredEnvironmentVariables.ToArray();
        ArtifactPatterns = artifactPatterns.Count == 0
            ? throw new ArgumentException("At least one artifact pattern is required.", nameof(artifactPatterns))
            : artifactPatterns.ToArray();
    }

    /// <summary>The CI profile name.</summary>
    public string Name { get; }

    /// <summary>Whether the lab is manual-dispatch only.</summary>
    public bool ManualDispatchOnly { get; }

    /// <summary>Whether a self-hosted Linux runner is required.</summary>
    public bool RequiresSelfHostedLinux { get; }

    /// <summary>The required environment variables.</summary>
    public IReadOnlyList<string> RequiredEnvironmentVariables { get; }

    /// <summary>The retained artifact upload patterns.</summary>
    public IReadOnlyList<string> ArtifactPatterns { get; }

    /// <summary>Whether this profile is safe to run in default pull request CI.</summary>
    public bool SafeForDefaultCi => !RequiresSelfHostedLinux && !ManualDispatchOnly;

    /// <summary>Formats a compact CI profile summary.</summary>
    /// <returns>The CI profile summary.</returns>
    public string Describe()
    {
        return $"name={Name} manual={ManualDispatchOnly} selfHostedLinux={RequiresSelfHostedLinux} env={RequiredEnvironmentVariables.Count} artifacts={ArtifactPatterns.Count}";
    }
}

/// <summary>
/// Provides reference external peer lab CI profile helpers.
/// </summary>
public static class SigtranReferencePeerLabCi
{
    /// <summary>Creates the default reference external peer lab CI profile.</summary>
    /// <returns>The default reference external peer lab CI profile.</returns>
    public static SigtranReferencePeerLabCiProfile CreateDefault()
    {
        return new(
            "reference-peer-lab",
            manualDispatchOnly: true,
            requiresSelfHostedLinux: true,
            requiredEnvironmentVariables:
            [
                SigtranReferencePeerLabBindings.PeerIdEnvironmentVariable,
                SigtranReferencePeerLabBindings.PackageEnvironmentVariable,
                SigtranReferencePeerLabBindings.PackageVersionEnvironmentVariable,
                SigtranReferencePeerLabBindings.ArtifactRootEnvironmentVariable,
                "LOCAL_IP",
                "LOCAL_SCTP_PORT",
                "REMOTE_IP",
                "REMOTE_SCTP_PORT",
                "ROUTING_CONTEXT"
            ],
            artifactPatterns:
            [
                "artifacts/external-peer/**/pcap/*.pcap",
                "artifacts/external-peer/**/logs/*.log",
                "artifacts/external-peer/**/config/*.env",
                "artifacts/external-peer/**/trace/*.jsonl",
                "artifacts/external-peer/**/comparison/*.md",
                "artifacts/external-peer/**/reports/*.md"
            ]);
    }
}
