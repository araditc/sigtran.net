namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes API deprecation policy.
/// </summary>
public sealed class SigtranDeprecationPolicy
{
    /// <summary>Creates a deprecation policy.</summary>
    /// <param name="minimumNoticePeriod">The minimum notice period.</param>
    /// <param name="requiresObsoleteAttribute">Whether obsolete APIs require an attribute.</param>
    /// <param name="requiresMigrationGuide">Whether a migration guide is required.</param>
    /// <param name="requiresReleaseNotes">Whether release notes are required.</param>
    public SigtranDeprecationPolicy(
        TimeSpan minimumNoticePeriod,
        bool requiresObsoleteAttribute,
        bool requiresMigrationGuide,
        bool requiresReleaseNotes)
    {
        MinimumNoticePeriod = minimumNoticePeriod <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(minimumNoticePeriod)) : minimumNoticePeriod;
        RequiresObsoleteAttribute = requiresObsoleteAttribute;
        RequiresMigrationGuide = requiresMigrationGuide;
        RequiresReleaseNotes = requiresReleaseNotes;
    }

    /// <summary>The minimum notice period.</summary>
    public TimeSpan MinimumNoticePeriod { get; }

    /// <summary>Whether obsolete APIs require an attribute.</summary>
    public bool RequiresObsoleteAttribute { get; }

    /// <summary>Whether a migration guide is required.</summary>
    public bool RequiresMigrationGuide { get; }

    /// <summary>Whether release notes are required.</summary>
    public bool RequiresReleaseNotes { get; }

    /// <summary>Whether the policy is suitable for stable API lifecycle management.</summary>
    public bool IsStableLifecyclePolicy => MinimumNoticePeriod >= TimeSpan.FromDays(90)
        && RequiresObsoleteAttribute
        && RequiresMigrationGuide
        && RequiresReleaseNotes;
}

/// <summary>
/// Provides API deprecation policy helpers.
/// </summary>
public static class SigtranDeprecationPolicies
{
    /// <summary>Creates the default stable API deprecation policy.</summary>
    /// <returns>The default stable API deprecation policy.</returns>
    public static SigtranDeprecationPolicy CreateStableDefault()
    {
        return new(
            TimeSpan.FromDays(180),
            requiresObsoleteAttribute: true,
            requiresMigrationGuide: true,
            requiresReleaseNotes: true);
    }
}
