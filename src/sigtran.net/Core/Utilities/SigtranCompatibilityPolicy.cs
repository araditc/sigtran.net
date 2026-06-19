namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes SDK compatibility policy.
/// </summary>
public sealed class SigtranCompatibilityPolicy
{
    /// <summary>Creates a compatibility policy.</summary>
    /// <param name="targetFramework">The supported target framework.</param>
    /// <param name="usesSemanticVersioning">Whether SemVer is used.</param>
    /// <param name="allowsBreakingChangesBeforeStable">Whether breaking API changes are allowed before stable release.</param>
    /// <param name="stableApiRequiresMajorVersion">Whether stable API breaking changes require a major version.</param>
    public SigtranCompatibilityPolicy(
        string targetFramework,
        bool usesSemanticVersioning,
        bool allowsBreakingChangesBeforeStable,
        bool stableApiRequiresMajorVersion)
    {
        TargetFramework = string.IsNullOrWhiteSpace(targetFramework) ? throw new ArgumentException("Target framework is required.", nameof(targetFramework)) : targetFramework;
        UsesSemanticVersioning = usesSemanticVersioning;
        AllowsBreakingChangesBeforeStable = allowsBreakingChangesBeforeStable;
        StableApiRequiresMajorVersion = stableApiRequiresMajorVersion;
    }

    /// <summary>The supported target framework.</summary>
    public string TargetFramework { get; }

    /// <summary>Whether SemVer is used.</summary>
    public bool UsesSemanticVersioning { get; }

    /// <summary>Whether breaking API changes are allowed before stable release.</summary>
    public bool AllowsBreakingChangesBeforeStable { get; }

    /// <summary>Whether stable API breaking changes require a major version.</summary>
    public bool StableApiRequiresMajorVersion { get; }

    /// <summary>Formats a compact compatibility summary.</summary>
    /// <returns>The compatibility summary.</returns>
    public string Describe()
    {
        return $"targetFramework={TargetFramework} semver={UsesSemanticVersioning} preStableBreakingChanges={AllowsBreakingChangesBeforeStable} stableBreakingChangesRequireMajor={StableApiRequiresMajorVersion}";
    }
}

/// <summary>
/// Provides SDK compatibility policy helpers.
/// </summary>
public static class SigtranCompatibility
{
    /// <summary>Creates the current SDK compatibility policy.</summary>
    /// <returns>The current compatibility policy.</returns>
    public static SigtranCompatibilityPolicy CreateCurrentPolicy()
    {
        return new(
            "net10.0",
            usesSemanticVersioning: true,
            allowsBreakingChangesBeforeStable: true,
            stableApiRequiresMajorVersion: true);
    }
}
