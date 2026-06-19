namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies an SDK runtime environment.
/// </summary>
public enum SigtranRuntimeEnvironment
{
    /// <summary>Local development environment.</summary>
    Development,

    /// <summary>External interoperability lab environment.</summary>
    InteropLab,

    /// <summary>Production environment.</summary>
    Production
}

/// <summary>
/// Describes environment-specific configuration requirements.
/// </summary>
public sealed class SigtranEnvironmentMatrixEntry
{
    /// <summary>Creates an environment matrix entry.</summary>
    /// <param name="environment">The runtime environment.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresEvidenceRoot">Whether an evidence root is required.</param>
    /// <param name="requiresExternalSecretProvider">Whether an external secret provider is required.</param>
    public SigtranEnvironmentMatrixEntry(
        SigtranRuntimeEnvironment environment,
        bool requiresNativeSctp,
        bool requiresEvidenceRoot,
        bool requiresExternalSecretProvider)
    {
        Environment = environment;
        RequiresNativeSctp = requiresNativeSctp;
        RequiresEvidenceRoot = requiresEvidenceRoot;
        RequiresExternalSecretProvider = requiresExternalSecretProvider;
    }

    /// <summary>The runtime environment.</summary>
    public SigtranRuntimeEnvironment Environment { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether an evidence root is required.</summary>
    public bool RequiresEvidenceRoot { get; }

    /// <summary>Whether an external secret provider is required.</summary>
    public bool RequiresExternalSecretProvider { get; }
}

/// <summary>
/// Provides environment matrix helpers.
/// </summary>
public static class SigtranEnvironmentMatrix
{
    private static readonly SigtranEnvironmentMatrixEntry[] Entries =
    [
        new(SigtranRuntimeEnvironment.Development, requiresNativeSctp: false, requiresEvidenceRoot: false, requiresExternalSecretProvider: false),
        new(SigtranRuntimeEnvironment.InteropLab, requiresNativeSctp: true, requiresEvidenceRoot: true, requiresExternalSecretProvider: false),
        new(SigtranRuntimeEnvironment.Production, requiresNativeSctp: true, requiresEvidenceRoot: true, requiresExternalSecretProvider: true)
    ];

    /// <summary>Returns the runtime environment matrix.</summary>
    /// <returns>The runtime environment matrix.</returns>
    public static IReadOnlyList<SigtranEnvironmentMatrixEntry> GetEntries()
    {
        return Entries.ToArray();
    }
}
