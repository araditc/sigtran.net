namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab automation readiness status.
/// </summary>
public sealed class SigtranMaintainedPeerLabAutomationStatusReport
{
    /// <summary>Creates a maintained peer lab automation status report.</summary>
    /// <param name="completedCapabilityCount">The completed automation capability count.</param>
    /// <param name="foundationReady">Whether the automation foundation is ready.</param>
    /// <param name="commercialEvidenceReady">Whether retained evidence is ready for commercial use.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranMaintainedPeerLabAutomationStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool commercialEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        CommercialEvidenceReady = commercialEvidenceReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed automation capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the automation foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained evidence is ready for commercial use.</summary>
    public bool CommercialEvidenceReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether maintained peer lab automation can support a commercial release claim.</summary>
    public bool CommercialReady => FoundationReady && CommercialEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact automation status summary.</summary>
    /// <returns>The automation status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={CommercialEvidenceReady} blockers={Blockers.Count} commercial={CommercialReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab automation status helpers.
/// </summary>
public static class SigtranMaintainedPeerLabAutomationStatus
{
    /// <summary>The completed automation unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed maintained peer lab automation capabilities.</summary>
    /// <returns>The completed maintained peer lab automation capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return
        [
            "run-manifest-aggregation",
            "environment-file-rendering",
            "artifact-digest-manifest",
            "command-script-rendering",
            "comparison-reporting",
            "run-reporting",
            "evidence-bundle-handoff",
            "workflow-template-rendering",
            "commercial-readiness-bridge",
            "automation-status-reporting"
        ];
    }

    /// <summary>Returns maintained peer lab automation foundation status without real retained evidence.</summary>
    /// <returns>The maintained peer lab automation foundation status.</returns>
    public static SigtranMaintainedPeerLabAutomationStatusReport GetFoundationReport()
    {
        bool foundationReady = SigtranMaintainedPeerLabRunManifests.CreateDefault().IsExecutableContract
            && SigtranMaintainedPeerLabWorkflows.CreateDefault().IsReady
            && GetCompletedCapabilities().Count == CompletedUnitCount;

        return new(
            CompletedUnitCount,
            foundationReady,
            commercialEvidenceReady: false,
            ["real-maintained-peer-execution-required", "retained-evidence-bundle-required"]);
    }

    /// <summary>Returns maintained peer lab automation status from a retained evidence bundle.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <returns>The maintained peer lab automation status.</returns>
    public static SigtranMaintainedPeerLabAutomationStatusReport FromBundle(SigtranMaintainedPeerLabEvidenceBundle evidenceBundle)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        SigtranMaintainedPeerLabCommercialReadinessReport bridge = SigtranMaintainedPeerLabCommercialBridge.Evaluate(evidenceBundle);

        return new(
            CompletedUnitCount,
            foundationReady: bridge.FoundationReady,
            commercialEvidenceReady: bridge.EvidenceReady,
            bridge.Blockers);
    }
}
