namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the production evidence CI profile.
/// </summary>
public sealed class SigtranReleaseEvidenceCiProfile
{
    /// <summary>Creates a production evidence CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="bundleRootVariable">The bundle root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresEvidenceBundle">Whether a retained evidence bundle is required.</param>
    public SigtranReleaseEvidenceCiProfile(
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
/// Provides production evidence CI profile helpers.
/// </summary>
public static class SigtranReleaseEvidenceCi
{
    /// <summary>Creates the default production evidence CI profile.</summary>
    /// <returns>The default production evidence CI profile.</returns>
    public static SigtranReleaseEvidenceCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_RELEASE_EVIDENCE",
            "SIGTRAN_RELEASE_EVIDENCE_ROOT",
            [
                "dotnet build src/Sigtran.NET.sln --configuration Release",
                "dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release",
                "dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release",
                "dotnet sigtran-evidence-verify artifacts/release-evidence"
            ],
            requiresEvidenceBundle: true);
    }
}
