namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a breaking-change review requirement set.
/// </summary>
public sealed class SigtranBreakingChangeReviewPolicy
{
    /// <summary>Creates a breaking-change review policy.</summary>
    /// <param name="requiresApiBaselineDiff">Whether an API baseline diff is required.</param>
    /// <param name="requiresMigrationGuide">Whether a migration guide is required.</param>
    /// <param name="requiresMaintainerApproval">Whether maintainer approval is required.</param>
    /// <param name="requiresMajorVersionAfterStable">Whether stable breaking changes require a major version.</param>
    public SigtranBreakingChangeReviewPolicy(
        bool requiresApiBaselineDiff,
        bool requiresMigrationGuide,
        bool requiresMaintainerApproval,
        bool requiresMajorVersionAfterStable)
    {
        RequiresApiBaselineDiff = requiresApiBaselineDiff;
        RequiresMigrationGuide = requiresMigrationGuide;
        RequiresMaintainerApproval = requiresMaintainerApproval;
        RequiresMajorVersionAfterStable = requiresMajorVersionAfterStable;
    }

    /// <summary>Whether an API baseline diff is required.</summary>
    public bool RequiresApiBaselineDiff { get; }

    /// <summary>Whether a migration guide is required.</summary>
    public bool RequiresMigrationGuide { get; }

    /// <summary>Whether maintainer approval is required.</summary>
    public bool RequiresMaintainerApproval { get; }

    /// <summary>Whether stable breaking changes require a major version.</summary>
    public bool RequiresMajorVersionAfterStable { get; }

    /// <summary>Whether the review policy is ready for commercial API governance.</summary>
    public bool IsCommercialApiGovernanceReady => RequiresApiBaselineDiff
        && RequiresMigrationGuide
        && RequiresMaintainerApproval
        && RequiresMajorVersionAfterStable;
}

/// <summary>
/// Provides breaking-change review policy helpers.
/// </summary>
public static class SigtranBreakingChangeReview
{
    /// <summary>Creates the default breaking-change review policy.</summary>
    /// <returns>The default breaking-change review policy.</returns>
    public static SigtranBreakingChangeReviewPolicy CreateDefault()
    {
        return new(
            requiresApiBaselineDiff: true,
            requiresMigrationGuide: true,
            requiresMaintainerApproval: true,
            requiresMajorVersionAfterStable: true);
    }
}
