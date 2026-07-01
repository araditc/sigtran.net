namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab automation readiness status.
/// </summary>
public sealed class SigtranReferencePeerLabAutomationStatusReport
{
    /// <summary>Creates a reference peer lab automation status report.</summary>
    /// <param name="completedCapabilityCount">The completed automation capability count.</param>
    /// <param name="foundationReady">Whether the automation foundation is ready.</param>
    /// <param name="releaseEvidenceReady">Whether retained evidence is ready for production use.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranReferencePeerLabAutomationStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool releaseEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        ReleaseEvidenceReady = releaseEvidenceReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed automation capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the automation foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained evidence is ready for production use.</summary>
    public bool ReleaseEvidenceReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether reference peer lab automation can support a production release claim.</summary>
    public bool ProductionReady => FoundationReady && ReleaseEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact automation status summary.</summary>
    /// <returns>The automation status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={ReleaseEvidenceReady} blockers={Blockers.Count} production={ProductionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab automation status helpers.
/// </summary>
public static class SigtranReferencePeerLabAutomationStatus
{
    /// <summary>The completed automation unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed reference peer lab automation capabilities.</summary>
    /// <returns>The completed reference peer lab automation capabilities.</returns>
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
            "production-readiness-bridge",
            "automation-status-reporting"
        ];
    }

    /// <summary>Returns reference peer lab automation foundation status without real retained evidence.</summary>
    /// <returns>The reference peer lab automation foundation status.</returns>
    public static SigtranReferencePeerLabAutomationStatusReport GetFoundationReport()
    {
        bool foundationReady = SigtranReferencePeerLabRunManifests.CreateDefault().IsExecutableContract
            && SigtranReferencePeerLabWorkflows.CreateDefault().IsReady
            && GetCompletedCapabilities().Count == CompletedUnitCount;

        return new(
            CompletedUnitCount,
            foundationReady,
            releaseEvidenceReady: false,
            ["real-reference-peer-execution-required", "retained-evidence-bundle-required"]);
    }

    /// <summary>Returns reference peer lab automation status from a retained evidence bundle.</summary>
    /// <param name="evidenceBundle">The evidence handoff bundle.</param>
    /// <returns>The reference peer lab automation status.</returns>
    public static SigtranReferencePeerLabAutomationStatusReport FromBundle(SigtranReferencePeerLabEvidenceBundle evidenceBundle)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        SigtranReferencePeerLabProductionReadinessSnapshot bridge = SigtranReferencePeerLabProductionBridge.Evaluate(evidenceBundle);

        return new(
            CompletedUnitCount,
            foundationReady: bridge.FoundationReady,
            releaseEvidenceReady: bridge.EvidenceReady,
            bridge.Blockers);
    }
}
