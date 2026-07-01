namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab production readiness bridge output.
/// </summary>
public sealed class SigtranReferencePeerLabProductionReadinessSnapshot
{
    /// <summary>Creates a reference peer lab production readiness bridge report.</summary>
    /// <param name="evidenceBundle">The evidence bundle.</param>
    /// <param name="workflowTemplate">The workflow template.</param>
    /// <param name="evidenceReport">The evidence promotion report.</param>
    /// <param name="statusReport">The reference peer lab status report.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranReferencePeerLabProductionReadinessSnapshot(
        SigtranReferencePeerLabEvidenceBundle evidenceBundle,
        SigtranReferencePeerLabWorkflowTemplate workflowTemplate,
        SigtranReferencePeerLabEvidenceReport evidenceReport,
        SigtranReferencePeerLabStatusReport statusReport,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        ArgumentNullException.ThrowIfNull(workflowTemplate);
        ArgumentNullException.ThrowIfNull(evidenceReport);
        ArgumentNullException.ThrowIfNull(statusReport);
        ArgumentNullException.ThrowIfNull(blockers);

        EvidenceBundle = evidenceBundle;
        WorkflowTemplate = workflowTemplate;
        EvidenceReport = evidenceReport;
        StatusReport = statusReport;
        Blockers = blockers.ToArray();
    }

    /// <summary>The evidence bundle.</summary>
    public SigtranReferencePeerLabEvidenceBundle EvidenceBundle { get; }

    /// <summary>The workflow template.</summary>
    public SigtranReferencePeerLabWorkflowTemplate WorkflowTemplate { get; }

    /// <summary>The evidence promotion report.</summary>
    public SigtranReferencePeerLabEvidenceReport EvidenceReport { get; }

    /// <summary>The reference peer lab status report.</summary>
    public SigtranReferencePeerLabStatusReport StatusReport { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the bridge foundation is ready.</summary>
    public bool FoundationReady => EvidenceBundle.RunManifest.IsExecutableContract && WorkflowTemplate.IsReady;

    /// <summary>Whether retained reference peer lab evidence is ready.</summary>
    public bool EvidenceReady => EvidenceBundle.IsHandoffReady && EvidenceReport.PromotionReady && StatusReport.ProductionReady;

    /// <summary>Whether reference peer lab evidence can support a production release claim.</summary>
    public bool ProductionReady => FoundationReady && EvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact production readiness bridge summary.</summary>
    /// <returns>The production readiness bridge summary.</returns>
    public string Describe()
    {
        return $"run={EvidenceBundle.RunManifest.RunId} foundation={FoundationReady} evidence={EvidenceReady} blockers={Blockers.Count} production={ProductionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab production readiness bridge helpers.
/// </summary>
public static class SigtranReferencePeerLabProductionBridge
{
    /// <summary>Evaluates reference peer lab production readiness from an evidence handoff bundle.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <returns>The reference peer lab production readiness bridge report.</returns>
    public static SigtranReferencePeerLabProductionReadinessSnapshot Evaluate(SigtranReferencePeerLabEvidenceBundle evidenceBundle)
    {
        return Evaluate(evidenceBundle, SigtranReferencePeerLabWorkflows.CreateDefault());
    }

    /// <summary>Evaluates reference peer lab production readiness from an evidence handoff bundle and workflow template.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <param name="workflowTemplate">The workflow template.</param>
    /// <returns>The reference peer lab production readiness bridge report.</returns>
    public static SigtranReferencePeerLabProductionReadinessSnapshot Evaluate(
        SigtranReferencePeerLabEvidenceBundle evidenceBundle,
        SigtranReferencePeerLabWorkflowTemplate workflowTemplate)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        ArgumentNullException.ThrowIfNull(workflowTemplate);

        SigtranReferencePeerLabEvidenceReport evidenceReport = evidenceBundle.ToEvidenceReport();
        SigtranReferencePeerLabStatusReport statusReport = SigtranReferencePeerLabStatus.FromEvidence(evidenceReport);
        List<string> blockers = [];

        if (!workflowTemplate.IsReady)
        {
            blockers.Add("reference-peer-workflow-template-required");
        }

        if (!evidenceBundle.UsesConsistentRun)
        {
            blockers.Add("reference-peer-bundle-run-consistency-required");
        }

        if (!evidenceBundle.IsHandoffReady)
        {
            blockers.Add("reference-peer-bundle-handoff-required");
        }

        if (!evidenceReport.PromotionReady)
        {
            blockers.Add("reference-peer-promotion-evidence-required");
        }

        if (!statusReport.ProductionReady)
        {
            blockers.Add("reference-peer-status-production-ready-required");
        }

        return new(evidenceBundle, workflowTemplate, evidenceReport, statusReport, blockers);
    }
}
