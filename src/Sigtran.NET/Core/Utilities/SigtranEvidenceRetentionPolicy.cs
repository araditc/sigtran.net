namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes evidence retention requirements.
/// </summary>
public sealed class SigtranEvidenceRetentionPolicy
{
    /// <summary>Creates an evidence retention policy.</summary>
    /// <param name="retentionPeriod">The minimum retention period.</param>
    /// <param name="requiresImmutableStorage">Whether immutable storage is required.</param>
    /// <param name="requiresTraceRedaction">Whether protocol trace redaction is required.</param>
    /// <param name="requiresProvenanceLink">Whether evidence must link to release provenance.</param>
    public SigtranEvidenceRetentionPolicy(
        TimeSpan retentionPeriod,
        bool requiresImmutableStorage,
        bool requiresTraceRedaction,
        bool requiresProvenanceLink)
    {
        RetentionPeriod = retentionPeriod <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(retentionPeriod)) : retentionPeriod;
        RequiresImmutableStorage = requiresImmutableStorage;
        RequiresTraceRedaction = requiresTraceRedaction;
        RequiresProvenanceLink = requiresProvenanceLink;
    }

    /// <summary>The minimum retention period.</summary>
    public TimeSpan RetentionPeriod { get; }

    /// <summary>Whether immutable storage is required.</summary>
    public bool RequiresImmutableStorage { get; }

    /// <summary>Whether protocol trace redaction is required.</summary>
    public bool RequiresTraceRedaction { get; }

    /// <summary>Whether evidence must link to release provenance.</summary>
    public bool RequiresProvenanceLink { get; }

    /// <summary>Whether the policy is suitable for production release evidence.</summary>
    public bool IsReleaseEvidencePolicy => RetentionPeriod >= TimeSpan.FromDays(365)
        && RequiresImmutableStorage
        && RequiresTraceRedaction
        && RequiresProvenanceLink;
}

/// <summary>
/// Provides evidence retention policy helpers.
/// </summary>
public static class SigtranEvidenceRetentionPolicies
{
    /// <summary>Creates the default production evidence retention policy.</summary>
    /// <returns>The default production evidence retention policy.</returns>
    public static SigtranEvidenceRetentionPolicy CreateProductionDefault()
    {
        return new(
            TimeSpan.FromDays(365 * 3),
            requiresImmutableStorage: true,
            requiresTraceRedaction: true,
            requiresProvenanceLink: true);
    }
}
