namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes an approved commercial evidence publication handoff gate result.
/// </summary>
public sealed class SigtranCommercialEvidencePublicationHandoffGateResult
{
    /// <summary>Creates a publication handoff gate result.</summary>
    /// <param name="handoff">The publication handoff under evaluation.</param>
    /// <param name="commercialReadinessApproved">Whether commercial readiness was approved for the requested channel.</param>
    /// <param name="blockers">The publication handoff blockers.</param>
    public SigtranCommercialEvidencePublicationHandoffGateResult(
        SigtranCommercialEvidencePublicationHandoff handoff,
        bool commercialReadinessApproved,
        IReadOnlyList<string> blockers)
    {
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        CommercialReadinessApproved = commercialReadinessApproved;
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The publication handoff under evaluation.</summary>
    public SigtranCommercialEvidencePublicationHandoff Handoff { get; }

    /// <summary>Whether commercial readiness was approved for the requested channel.</summary>
    public bool CommercialReadinessApproved { get; }

    /// <summary>The publication handoff blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the publication handoff can proceed to the package publication gate.</summary>
    public bool CanProceedToPackagePublicationGate => Blockers.Count == 0;

    /// <summary>Formats a compact publication handoff gate summary.</summary>
    /// <returns>The publication handoff gate summary.</returns>
    public string Describe()
    {
        return $"commercialEvidencePublicationHandoffGateReady={CanProceedToPackagePublicationGate} blockers={Blockers.Count} channel={Handoff.Channel.FeedName}";
    }
}

/// <summary>
/// Provides approved commercial evidence publication handoff gate helpers.
/// </summary>
public static class SigtranCommercialEvidencePublicationHandoffGates
{
    /// <summary>Evaluates an approved commercial evidence publication handoff.</summary>
    /// <param name="handoff">The publication handoff under evaluation.</param>
    /// <param name="commercialReadinessApproved">Whether commercial readiness was approved for the requested channel.</param>
    /// <returns>The publication handoff gate result.</returns>
    public static SigtranCommercialEvidencePublicationHandoffGateResult Evaluate(
        SigtranCommercialEvidencePublicationHandoff handoff,
        bool commercialReadinessApproved)
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

        if (handoff.RequiresCommercialReadiness && !commercialReadinessApproved)
        {
            blockers.Add("stable-commercial-readiness-required");
        }

        return new(handoff, commercialReadinessApproved, blockers);
    }
}
