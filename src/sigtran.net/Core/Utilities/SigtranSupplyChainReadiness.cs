namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes supply-chain automation readiness.
/// </summary>
public sealed class SigtranSupplyChainReadinessReport
{
    /// <summary>Creates a supply-chain readiness report.</summary>
    /// <param name="hasAutomationPlan">Whether the automation plan is available.</param>
    /// <param name="hasArtifactManifest">Whether artifact manifest support is available.</param>
    /// <param name="hasGateEvaluator">Whether gate evaluation is available.</param>
    /// <param name="hasCiProfile">Whether CI metadata is available.</param>
    /// <param name="hasCurrentPromotionEvidence">Whether current promotion evidence is available.</param>
    public SigtranSupplyChainReadinessReport(
        bool hasAutomationPlan,
        bool hasArtifactManifest,
        bool hasGateEvaluator,
        bool hasCiProfile,
        bool hasCurrentPromotionEvidence)
    {
        HasAutomationPlan = hasAutomationPlan;
        HasArtifactManifest = hasArtifactManifest;
        HasGateEvaluator = hasGateEvaluator;
        HasCiProfile = hasCiProfile;
        HasCurrentPromotionEvidence = hasCurrentPromotionEvidence;
    }

    /// <summary>Whether the automation plan is available.</summary>
    public bool HasAutomationPlan { get; }

    /// <summary>Whether artifact manifest support is available.</summary>
    public bool HasArtifactManifest { get; }

    /// <summary>Whether gate evaluation is available.</summary>
    public bool HasGateEvaluator { get; }

    /// <summary>Whether CI metadata is available.</summary>
    public bool HasCiProfile { get; }

    /// <summary>Whether current promotion evidence is available.</summary>
    public bool HasCurrentPromotionEvidence { get; }

    /// <summary>Whether the supply-chain automation foundation is ready.</summary>
    public bool FoundationReady => HasAutomationPlan && HasArtifactManifest && HasGateEvaluator && HasCiProfile;

    /// <summary>Whether a commercial release can currently be promoted.</summary>
    public bool PromotionReady => FoundationReady && HasCurrentPromotionEvidence;
}

/// <summary>
/// Provides supply-chain readiness helpers.
/// </summary>
public static class SigtranSupplyChainReadiness
{
    /// <summary>Returns the current supply-chain readiness report.</summary>
    /// <returns>The current supply-chain readiness report.</returns>
    public static SigtranSupplyChainReadinessReport GetReport()
    {
        return new(
            hasAutomationPlan: SigtranSupplyChainAutomation.CreateDefaultPlan().IsExecutable,
            hasArtifactManifest: true,
            hasGateEvaluator: true,
            hasCiProfile: SigtranSupplyChainCi.CreateDefault().RequiresSigningSecrets,
            hasCurrentPromotionEvidence: false);
    }
}
