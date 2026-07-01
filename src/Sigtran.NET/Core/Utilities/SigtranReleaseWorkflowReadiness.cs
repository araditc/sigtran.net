namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes release workflow orchestration readiness.
/// </summary>
public sealed class SigtranReleaseWorkflowReadinessSnapshot
{
    /// <summary>Creates a release workflow readiness report.</summary>
    /// <param name="hasWorkflowContract">Whether the workflow contract is available.</param>
    /// <param name="hasSupplyChainIntegration">Whether supply-chain integration is required.</param>
    /// <param name="hasReleaseEvidenceIntegration">Whether production evidence integration is required.</param>
    /// <param name="hasPublishContract">Whether package publishing is represented.</param>
    /// <param name="hasWorkflowFile">Whether a concrete workflow file is present.</param>
    public SigtranReleaseWorkflowReadinessSnapshot(
        bool hasWorkflowContract,
        bool hasSupplyChainIntegration,
        bool hasReleaseEvidenceIntegration,
        bool hasPublishContract,
        bool hasWorkflowFile)
    {
        HasWorkflowContract = hasWorkflowContract;
        HasSupplyChainIntegration = hasSupplyChainIntegration;
        HasReleaseEvidenceIntegration = hasReleaseEvidenceIntegration;
        HasPublishContract = hasPublishContract;
        HasWorkflowFile = hasWorkflowFile;
    }

    /// <summary>Whether the workflow contract is available.</summary>
    public bool HasWorkflowContract { get; }

    /// <summary>Whether supply-chain integration is required.</summary>
    public bool HasSupplyChainIntegration { get; }

    /// <summary>Whether production evidence integration is required.</summary>
    public bool HasReleaseEvidenceIntegration { get; }

    /// <summary>Whether package publishing is represented.</summary>
    public bool HasPublishContract { get; }

    /// <summary>Whether a concrete workflow file is present.</summary>
    public bool HasWorkflowFile { get; }

    /// <summary>Whether the release workflow contract foundation is ready.</summary>
    public bool ContractReady => HasWorkflowContract
        && HasSupplyChainIntegration
        && HasReleaseEvidenceIntegration
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
    public static SigtranReleaseWorkflowReadinessSnapshot GetReport()
    {
        SigtranReleaseWorkflowPlan plan = SigtranReleaseWorkflows.CreateReleasePlan();
        return new(
            hasWorkflowContract: plan.IsRenderable,
            hasSupplyChainIntegration: plan.RequiresSupplyChain && SigtranSupplyChainStatus.FoundationReady,
            hasReleaseEvidenceIntegration: plan.RequiresReleaseEvidence && SigtranReleaseEvidenceStatus.FoundationReady,
            hasPublishContract: plan.HasPublishStage,
            hasWorkflowFile: SigtranReleaseWorkflowFiles.CreateDefault().IsValidationReady);
    }
}
