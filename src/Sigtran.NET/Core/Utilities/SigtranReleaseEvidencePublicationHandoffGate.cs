namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes an approved production evidence publication handoff gate result.
/// </summary>
public sealed class SigtranReleaseEvidencePublicationHandoffGateResult
{
    /// <summary>Creates a publication handoff gate result.</summary>
    /// <param name="handoff">The publication handoff under evaluation.</param>
    /// <param name="productionReadinessApproved">Whether production readiness was approved for the requested channel.</param>
    /// <param name="blockers">The publication handoff blockers.</param>
    public SigtranReleaseEvidencePublicationHandoffGateResult(
        SigtranReleaseEvidencePublicationHandoff handoff,
        bool productionReadinessApproved,
        IReadOnlyList<string> blockers)
    {
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        ProductionReadinessApproved = productionReadinessApproved;
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The publication handoff under evaluation.</summary>
    public SigtranReleaseEvidencePublicationHandoff Handoff { get; }

    /// <summary>Whether production readiness was approved for the requested channel.</summary>
    public bool ProductionReadinessApproved { get; }

    /// <summary>The publication handoff blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the publication handoff can proceed to the package publication gate.</summary>
    public bool CanProceedToPackagePublicationGate => Blockers.Count == 0;

    /// <summary>Formats a compact publication handoff gate summary.</summary>
    /// <returns>The publication handoff gate summary.</returns>
    public string Describe()
    {
        return $"productionEvidencePublicationHandoffGateReady={CanProceedToPackagePublicationGate} blockers={Blockers.Count} channel={Handoff.Channel.FeedName}";
    }
}

/// <summary>
/// Provides approved production evidence publication handoff gate helpers.
/// </summary>
public static class SigtranReleaseEvidencePublicationHandoffGates
{
    /// <summary>Evaluates an approved production evidence publication handoff.</summary>
    /// <param name="handoff">The publication handoff under evaluation.</param>
    /// <param name="productionReadinessApproved">Whether production readiness was approved for the requested channel.</param>
    /// <returns>The publication handoff gate result.</returns>
    public static SigtranReleaseEvidencePublicationHandoffGateResult Evaluate(
        SigtranReleaseEvidencePublicationHandoff handoff,
        bool productionReadinessApproved)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        List<string> blockers = [];

        if (!handoff.PromotionPackage.IsReady)
        {
            blockers.Add("promotion-package-not-ready");
        }

        if (!handoff.PublishRequested)
        {
            blockers.Add("publish-not-requested");
        }

        if (!handoff.HasUtcCreationTime)
        {
            blockers.Add("handoff-time-not-utc");
        }

        if (!handoff.ChannelAcceptsPackageVersion)
        {
            blockers.Add("channel-version-rejected");
        }

        if (handoff.RequiresProductionReadiness && !productionReadinessApproved)
        {
            blockers.Add("stable-release-readiness-required");
        }

        return new(handoff, productionReadinessApproved, blockers);
    }
}
