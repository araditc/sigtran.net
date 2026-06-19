namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes release workflow orchestration readiness.
/// </summary>
public sealed class SigtranReleaseWorkflowReadinessReport
{
    /// <summary>Creates a release workflow readiness report.</summary>
    /// <param name="hasWorkflowContract">Whether the workflow contract is available.</param>
    /// <param name="hasSupplyChainIntegration">Whether supply-chain integration is required.</param>
    /// <param name="hasCommercialEvidenceIntegration">Whether commercial evidence integration is required.</param>
    /// <param name="hasPublishContract">Whether package publishing is represented.</param>
    /// <param name="hasWorkflowFile">Whether a concrete workflow file is present.</param>
    public SigtranReleaseWorkflowReadinessReport(
        bool hasWorkflowContract,
        bool hasSupplyChainIntegration,
        bool hasCommercialEvidenceIntegration,
        bool hasPublishContract,
        bool hasWorkflowFile)
    {
        HasWorkflowContract = hasWorkflowContract;
        HasSupplyChainIntegration = hasSupplyChainIntegration;
        HasCommercialEvidenceIntegration = hasCommercialEvidenceIntegration;
        HasPublishContract = hasPublishContract;
        HasWorkflowFile = hasWorkflowFile;
    }

    /// <summary>Whether the workflow contract is available.</summary>
    public bool HasWorkflowContract { get; }

    /// <summary>Whether supply-chain integration is required.</summary>
    public bool HasSupplyChainIntegration { get; }

    /// <summary>Whether commercial evidence integration is required.</summary>
    public bool HasCommercialEvidenceIntegration { get; }

    /// <summary>Whether package publishing is represented.</summary>
    public bool HasPublishContract { get; }

    /// <summary>Whether a concrete workflow file is present.</summary>
    public bool HasWorkflowFile { get; }

    /// <summary>Whether the release workflow contract foundation is ready.</summary>
    public bool ContractReady => HasWorkflowContract
        && HasSupplyChainIntegration
        && HasCommercialEvidenceIntegration
        && HasPublishContract;

    /// <summary>Whether release workflow orchestration is fully ready.</summary>
    public bool OrchestrationReady => ContractReady && HasWorkflowFile;
}

/// <summary>
/// Provides release workflow readiness helpers.
/// </summary>
public static class SigtranReleaseWorkflowReadiness
{
    /// <summary>Returns the current release workflow readiness report.</summary>
    /// <returns>The release workflow readiness report.</returns>
    public static SigtranReleaseWorkflowReadinessReport GetReport()
    {
        SigtranReleaseWorkflowPlan plan = SigtranReleaseWorkflows.CreateCommercialReleasePlan();
        return new(
            hasWorkflowContract: plan.IsRenderable,
            hasSupplyChainIntegration: plan.RequiresSupplyChain && SigtranSupplyChainStatus.FoundationReady,
            hasCommercialEvidenceIntegration: plan.RequiresCommercialEvidence && SigtranCommercialEvidenceStatus.FoundationReady,
            hasPublishContract: plan.HasPublishStage,
            hasWorkflowFile: false);
    }
}
