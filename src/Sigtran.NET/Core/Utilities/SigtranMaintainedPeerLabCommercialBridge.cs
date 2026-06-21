namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab commercial readiness bridge output.
/// </summary>
public sealed class SigtranMaintainedPeerLabCommercialReadinessReport
{
    /// <summary>Creates a maintained peer lab commercial readiness bridge report.</summary>
    /// <param name="evidenceBundle">The evidence bundle.</param>
    /// <param name="workflowTemplate">The workflow template.</param>
    /// <param name="evidenceReport">The evidence promotion report.</param>
    /// <param name="statusReport">The maintained peer lab status report.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranMaintainedPeerLabCommercialReadinessReport(
        SigtranMaintainedPeerLabEvidenceBundle evidenceBundle,
        SigtranMaintainedPeerLabWorkflowTemplate workflowTemplate,
        SigtranMaintainedPeerLabEvidenceReport evidenceReport,
        SigtranMaintainedPeerLabStatusReport statusReport,
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
    public SigtranMaintainedPeerLabEvidenceBundle EvidenceBundle { get; }

    /// <summary>The workflow template.</summary>
    public SigtranMaintainedPeerLabWorkflowTemplate WorkflowTemplate { get; }

    /// <summary>The evidence promotion report.</summary>
    public SigtranMaintainedPeerLabEvidenceReport EvidenceReport { get; }

    /// <summary>The maintained peer lab status report.</summary>
    public SigtranMaintainedPeerLabStatusReport StatusReport { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the bridge foundation is ready.</summary>
    public bool FoundationReady => EvidenceBundle.RunManifest.IsExecutableContract && WorkflowTemplate.IsReady;

    /// <summary>Whether retained maintained peer lab evidence is ready.</summary>
    public bool EvidenceReady => EvidenceBundle.IsHandoffReady && EvidenceReport.PromotionReady && StatusReport.CommercialReady;

    /// <summary>Whether maintained peer lab evidence can support a commercial release claim.</summary>
    public bool CommercialReady => FoundationReady && EvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact commercial readiness bridge summary.</summary>
    /// <returns>The commercial readiness bridge summary.</returns>
    public string Describe()
    {
        return $"run={EvidenceBundle.RunManifest.RunId} foundation={FoundationReady} evidence={EvidenceReady} blockers={Blockers.Count} commercial={CommercialReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab commercial readiness bridge helpers.
/// </summary>
public static class SigtranMaintainedPeerLabCommercialBridge
{
    /// <summary>Evaluates maintained peer lab commercial readiness from an evidence handoff bundle.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <returns>The maintained peer lab commercial readiness bridge report.</returns>
    public static SigtranMaintainedPeerLabCommercialReadinessReport Evaluate(SigtranMaintainedPeerLabEvidenceBundle evidenceBundle)
    {
        return Evaluate(evidenceBundle, SigtranMaintainedPeerLabWorkflows.CreateDefault());
    }

    /// <summary>Evaluates maintained peer lab commercial readiness from an evidence handoff bundle and workflow template.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <param name="workflowTemplate">The workflow template.</param>
    /// <returns>The maintained peer lab commercial readiness bridge report.</returns>
    public static SigtranMaintainedPeerLabCommercialReadinessReport Evaluate(
        SigtranMaintainedPeerLabEvidenceBundle evidenceBundle,
        SigtranMaintainedPeerLabWorkflowTemplate workflowTemplate)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        ArgumentNullException.ThrowIfNull(workflowTemplate);

        SigtranMaintainedPeerLabEvidenceReport evidenceReport = evidenceBundle.ToEvidenceReport();
        SigtranMaintainedPeerLabStatusReport statusReport = SigtranMaintainedPeerLabStatus.FromEvidence(evidenceReport);
        List<string> blockers = [];

        if (!workflowTemplate.IsReady)
        {
            blockers.Add("maintained-peer-workflow-template-required");
        }

        if (!evidenceBundle.UsesConsistentRun)
        {
            blockers.Add("maintained-peer-bundle-run-consistency-required");
        }

        if (!evidenceBundle.IsHandoffReady)
        {
            blockers.Add("maintained-peer-bundle-handoff-required");
        }

        if (!evidenceReport.PromotionReady)
        {
            blockers.Add("maintained-peer-promotion-evidence-required");
        }

        if (!statusReport.CommercialReady)
        {
            blockers.Add("maintained-peer-status-commercial-ready-required");
        }

        return new(evidenceBundle, workflowTemplate, evidenceReport, statusReport, blockers);
    }
}
