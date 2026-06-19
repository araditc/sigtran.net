namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the commercial evidence CI profile.
/// </summary>
public sealed class SigtranCommercialEvidenceCiProfile
{
    /// <summary>Creates a commercial evidence CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="bundleRootVariable">The bundle root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresEvidenceBundle">Whether a retained evidence bundle is required.</param>
    public SigtranCommercialEvidenceCiProfile(
        string enableVariable,
        string bundleRootVariable,
        IReadOnlyList<string> commands,
        bool requiresEvidenceBundle)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        BundleRootVariable = string.IsNullOrWhiteSpace(bundleRootVariable) ? throw new ArgumentException("Bundle root variable is required.", nameof(bundleRootVariable)) : bundleRootVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresEvidenceBundle = requiresEvidenceBundle;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The bundle root variable.</summary>
    public string BundleRootVariable { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether a retained evidence bundle is required.</summary>
    public bool RequiresEvidenceBundle { get; }

    /// <summary>Returns whether the profile is enabled by environment variables.</summary>
    /// <param name="environment">The environment variables.</param>
    /// <returns>True when enabled; otherwise false.</returns>
    public bool IsEnabled(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.TryGetValue(EnableVariable, out string? value)
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Provides commercial evidence CI profile helpers.
/// </summary>
public static class SigtranCommercialEvidenceCi
{
    /// <summary>Creates the default commercial evidence CI profile.</summary>
    /// <returns>The default commercial evidence CI profile.</returns>
    public static SigtranCommercialEvidenceCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_COMMERCIAL_EVIDENCE",
            "SIGTRAN_COMMERCIAL_EVIDENCE_ROOT",
            [
                "dotnet build src/sigtran.net.sln --configuration Release",
                "dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release",
                "dotnet pack src/sigtran.net/sigtran.net.csproj --configuration Release",
                "dotnet sigtran-evidence-verify artifacts/commercial-evidence"
            ],
            requiresEvidenceBundle: true);
    }
}
