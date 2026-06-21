namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a commercial deployment profile.
/// </summary>
public sealed class SigtranDeploymentProfile
{
    /// <summary>Creates a deployment profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="operatingSystem">The operating system family.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresExternalEvidence">Whether external interoperability evidence is required.</param>
    /// <param name="requiresObservability">Whether observability is required.</param>
    /// <param name="requiresSecurityPolicy">Whether a security policy is required.</param>
    public SigtranDeploymentProfile(
        string name,
        SigtranOperatingSystemFamily operatingSystem,
        bool requiresNativeSctp,
        bool requiresExternalEvidence,
        bool requiresObservability,
        bool requiresSecurityPolicy)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Deployment profile name is required.", nameof(name)) : name;
        OperatingSystem = operatingSystem;
        RequiresNativeSctp = requiresNativeSctp;
        RequiresExternalEvidence = requiresExternalEvidence;
        RequiresObservability = requiresObservability;
        RequiresSecurityPolicy = requiresSecurityPolicy;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The operating system family.</summary>
    public SigtranOperatingSystemFamily OperatingSystem { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether external interoperability evidence is required.</summary>
    public bool RequiresExternalEvidence { get; }

    /// <summary>Whether observability is required.</summary>
    public bool RequiresObservability { get; }

    /// <summary>Whether a security policy is required.</summary>
    public bool RequiresSecurityPolicy { get; }

    /// <summary>Formats a compact deployment summary.</summary>
    /// <returns>The deployment summary.</returns>
    public string Describe()
    {
        return $"{Name}: os={OperatingSystem} nativeSctp={RequiresNativeSctp} externalEvidence={RequiresExternalEvidence} observability={RequiresObservability} security={RequiresSecurityPolicy}";
    }
}

/// <summary>
/// Provides deployment profiles for SDK consumers.
/// </summary>
public static class SigtranDeploymentProfiles
{
    /// <summary>Creates the recommended commercial deployment profile.</summary>
    /// <returns>The commercial deployment profile.</returns>
    public static SigtranDeploymentProfile CreateCommercialLinuxProfile()
    {
        return new(
            "commercial-linux",
            SigtranOperatingSystemFamily.Linux,
            requiresNativeSctp: true,
            requiresExternalEvidence: true,
            requiresObservability: true,
            requiresSecurityPolicy: true);
    }

    /// <summary>Creates the local development deployment profile.</summary>
    /// <returns>The local development deployment profile.</returns>
    public static SigtranDeploymentProfile CreateLocalDevelopmentProfile()
    {
        return new(
            "local-development",
            SigtranOperatingSystemFamily.Windows,
            requiresNativeSctp: false,
            requiresExternalEvidence: false,
            requiresObservability: false,
            requiresSecurityPolicy: true);
    }
}
