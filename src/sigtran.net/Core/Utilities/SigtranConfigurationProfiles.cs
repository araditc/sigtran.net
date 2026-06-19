namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a configuration profile kind.
/// </summary>
public enum SigtranConfigurationProfileKind
{
    /// <summary>Local development profile.</summary>
    Development,

    /// <summary>External lab profile.</summary>
    InteropLab,

    /// <summary>Production-like profile.</summary>
    Production
}

/// <summary>
/// Describes a developer-facing configuration profile.
/// </summary>
public sealed class SigtranConfigurationProfile
{
    /// <summary>Creates a configuration profile.</summary>
    /// <param name="kind">The profile kind.</param>
    /// <param name="transport">The intended transport.</param>
    /// <param name="requiresExternalEvidence">Whether external evidence is required.</param>
    public SigtranConfigurationProfile(SigtranConfigurationProfileKind kind, string transport, bool requiresExternalEvidence)
    {
        Kind = kind;
        Transport = string.IsNullOrWhiteSpace(transport) ? throw new ArgumentException("Transport is required.", nameof(transport)) : transport;
        RequiresExternalEvidence = requiresExternalEvidence;
    }

    /// <summary>The profile kind.</summary>
    public SigtranConfigurationProfileKind Kind { get; }

    /// <summary>The intended transport.</summary>
    public string Transport { get; }

    /// <summary>Whether external evidence is required.</summary>
    public bool RequiresExternalEvidence { get; }
}

/// <summary>
/// Provides official developer-facing configuration profiles.
/// </summary>
public static class SigtranConfigurationProfiles
{
    /// <summary>Returns the official configuration profiles.</summary>
    /// <returns>The official configuration profiles.</returns>
    public static IReadOnlyList<SigtranConfigurationProfile> GetProfiles()
    {
        return
        [
            new SigtranConfigurationProfile(SigtranConfigurationProfileKind.Development, "tcp-adapter", requiresExternalEvidence: false),
            new SigtranConfigurationProfile(SigtranConfigurationProfileKind.InteropLab, "native-sctp", requiresExternalEvidence: true),
            new SigtranConfigurationProfile(SigtranConfigurationProfileKind.Production, "native-sctp", requiresExternalEvidence: true)
        ];
    }
}
