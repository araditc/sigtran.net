namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a release workflow artifact upload rule.
/// </summary>
public sealed class SigtranReleaseWorkflowArtifactRule
{
    /// <summary>Creates a release workflow artifact upload rule.</summary>
    /// <param name="name">The artifact name.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    public SigtranReleaseWorkflowArtifactRule(string name, string path, int retentionDays)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Artifact name is required.", nameof(name)) : name;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        RetentionDays = retentionDays <= 0 ? throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention must be positive.") : retentionDays;
    }

    /// <summary>The artifact name.</summary>
    public string Name { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The retention period in days.</summary>
    public int RetentionDays { get; }
}

/// <summary>
/// Provides release workflow artifact upload rules.
/// </summary>
public static class SigtranReleaseWorkflowArtifacts
{
    /// <summary>Returns the default release workflow artifact rules.</summary>
    /// <returns>The artifact upload rules.</returns>
    public static IReadOnlyList<SigtranReleaseWorkflowArtifactRule> GetDefaultRules()
    {
        return
        [
            new("nuget-packages", "src/Sigtran.NET/bin/Release/*.nupkg", 90),
            new("symbol-packages", "src/Sigtran.NET/bin/Release/*.snupkg", 90),
            new("supply-chain", "artifacts/supply-chain", 180),
            new("commercial-evidence", "artifacts/commercial-evidence", 180)
        ];
    }

    /// <summary>Returns whether the default rules retain commercial evidence.</summary>
    /// <returns>True when commercial evidence is retained; otherwise false.</returns>
    public static bool RetainsCommercialEvidence()
    {
        return GetDefaultRules().Any(static rule => string.Equals(rule.Name, "commercial-evidence", StringComparison.Ordinal));
    }
}
