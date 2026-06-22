namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the release decision outcome.
/// </summary>
public enum SigtranReleaseDecisionKind
{
    /// <summary>Publication is blocked.</summary>
    Blocked,

    /// <summary>Release candidate publication is allowed.</summary>
    ReleaseCandidate,

    /// <summary>Stable commercial publication is allowed.</summary>
    Stable
}

/// <summary>
/// Describes the RC versus stable publication decision.
/// </summary>
public sealed class SigtranReleaseDecision
{
    /// <summary>Creates a release decision.</summary>
    /// <param name="version">The evaluated version.</param>
    /// <param name="kind">The decision outcome.</param>
    /// <param name="recommendedChannel">The recommended publication channel.</param>
    /// <param name="reasons">The decision reasons.</param>
    public SigtranReleaseDecision(
        string version,
        SigtranReleaseDecisionKind kind,
        string recommendedChannel,
        IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Kind = kind;
        RecommendedChannel = string.IsNullOrWhiteSpace(recommendedChannel) ? throw new ArgumentException("Recommended channel is required.", nameof(recommendedChannel)) : recommendedChannel;
        Reasons = reasons.Count == 0 ? throw new ArgumentException("At least one decision reason is required.", nameof(reasons)) : reasons.ToArray();
    }

    /// <summary>The evaluated version.</summary>
    public string Version { get; }

    /// <summary>The decision outcome.</summary>
    public SigtranReleaseDecisionKind Kind { get; }

    /// <summary>The recommended publication channel.</summary>
    public string RecommendedChannel { get; }

    /// <summary>The decision reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Whether any NuGet publication is allowed.</summary>
    public bool AllowsPublication => Kind != SigtranReleaseDecisionKind.Blocked;

    /// <summary>Whether stable commercial publication is allowed.</summary>
    public bool AllowsStablePublication => Kind == SigtranReleaseDecisionKind.Stable;

    /// <summary>Formats a compact decision summary.</summary>
    /// <returns>The decision summary.</returns>
    public string Describe()
    {
        return $"version={Version} decision={Kind} channel={RecommendedChannel} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates release decisions from retained readiness reports.
/// </summary>
public static class SigtranReleaseDecisions
{
    /// <summary>Decides whether to publish an RC, publish stable, or block publication.</summary>
    /// <param name="report">The final commercial readiness report.</param>
    /// <returns>The release decision.</returns>
    public static SigtranReleaseDecision Decide(SigtranFinalCommercialReadinessReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.StableReleaseReady)
        {
            return new(report.Version, SigtranReleaseDecisionKind.Stable, "nuget.org", ["stable-commercial-release-ready"]);
        }

        if (report.ReleaseCandidateReady)
        {
            string[] reasons = report.CommercialBlockers.Count == 0
                ? ["stable-commercial-readiness-required"]
                : ["stable-commercial-blockers-retained", .. report.CommercialBlockers];
            return new(report.Version, SigtranReleaseDecisionKind.ReleaseCandidate, "nuget-prerelease", reasons);
        }

        return new(
            report.Version,
            SigtranReleaseDecisionKind.Blocked,
            "none",
            GetBlockedReasons(report));
    }

    private static IReadOnlyList<string> GetBlockedReasons(SigtranFinalCommercialReadinessReport report)
    {
        List<string> reasons = [];

        if (!report.DryRunReady)
        {
            reasons.Add("dry-run-release-required");
        }

        if (!report.PrereleasePublicationReady)
        {
            reasons.Add("prerelease-publication-gate-required");
        }

        if (!report.ReleaseNotesReady)
        {
            reasons.Add("release-notes-required");
        }

        if (!report.MigrationNotesReady)
        {
            reasons.Add("migration-notes-required");
        }

        if (!report.SupplyChainReleaseReady)
        {
            reasons.Add("supply-chain-release-foundation-required");
        }

        reasons.AddRange(report.CommercialBlockers.Select(static blocker => $"commercial-blocker:{blocker}"));
        return reasons.Count == 0 ? ["publication-gate-required"] : reasons;
    }
}
