namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes release promotion gate evaluation.
/// </summary>
public sealed class SigtranReleasePromotionGateResult
{
    /// <summary>Creates a release promotion gate result.</summary>
    /// <param name="canPromote">Whether the release can be promoted.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranReleasePromotionGateResult(bool canPromote, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPromote = canPromote;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether the release can be promoted.</summary>
    public bool CanPromote { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact promotion gate summary.</summary>
    /// <returns>The promotion gate summary.</returns>
    public string Describe()
    {
        return $"canPromote={CanPromote} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates release promotion gates.
/// </summary>
public static class SigtranReleasePromotionGate
{
    /// <summary>Evaluates whether a release can be promoted.</summary>
    /// <param name="publishGuard">The publish guard result.</param>
    /// <param name="workflowOrchestrationReady">Whether workflow orchestration is ready.</param>
    /// <param name="supplyChainPromotionReady">Whether supply-chain promotion is ready.</param>
    /// <param name="commercialEvidenceReady">Whether commercial evidence is ready.</param>
    /// <returns>The promotion gate result.</returns>
    public static SigtranReleasePromotionGateResult Evaluate(
        SigtranReleasePublishGuardResult publishGuard,
        bool workflowOrchestrationReady,
        bool supplyChainPromotionReady,
        bool commercialEvidenceReady)
    {
        ArgumentNullException.ThrowIfNull(publishGuard);

        List<string> reasons = [];
        if (!publishGuard.CanPublish)
        {
            reasons.AddRange(publishGuard.Reasons);
        }

        if (!workflowOrchestrationReady)
        {
            reasons.Add("workflow-orchestration-required");
        }

        if (!supplyChainPromotionReady)
        {
            reasons.Add("supply-chain-promotion-required");
        }

        if (!commercialEvidenceReady)
        {
            reasons.Add("commercial-evidence-required");
        }

        return new SigtranReleasePromotionGateResult(reasons.Count == 0, reasons);
    }
}
