namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab runner readiness status.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerStatusReport
{
    /// <summary>Creates a maintained peer lab runner status report.</summary>
    /// <param name="completedCapabilityCount">The completed runner capability count.</param>
    /// <param name="foundationReady">Whether the runner foundation is ready.</param>
    /// <param name="runnerEvidenceReady">Whether retained runner evidence is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranMaintainedPeerLabRunnerStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool runnerEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        RunnerEvidenceReady = runnerEvidenceReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed runner capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the runner foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained runner evidence is ready.</summary>
    public bool RunnerEvidenceReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether runner evidence can support a commercial readiness claim.</summary>
    public bool CommercialReady => FoundationReady && RunnerEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact runner status summary.</summary>
    /// <returns>The runner status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={RunnerEvidenceReady} blockers={Blockers.Count} commercial={CommercialReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner status helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerStatus
{
    /// <summary>The completed runner unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed maintained peer lab runner capabilities.</summary>
    /// <returns>The completed maintained peer lab runner capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return
        [
            "runner-workspace-materialization",
            "runner-input-bundle",
            "artifact-output-materialization",
            "runner-preflight",
            "runner-command-manifest",
            "runner-evidence-collection",
            "runner-digest-generation",
            "runner-comparison-handoff",
            "runner-workflow-readiness",
            "runner-status-reporting"
        ];
    }

    /// <summary>Returns maintained peer lab runner foundation status without real retained evidence.</summary>
    /// <returns>The maintained peer lab runner foundation status.</returns>
    public static SigtranMaintainedPeerLabRunnerStatusReport GetFoundationReport()
    {
        SigtranMaintainedPeerLabRunManifest runManifest = SigtranMaintainedPeerLabRunManifests.CreateDefault();
        SigtranMaintainedPeerLabRunnerInputBundle inputs = SigtranMaintainedPeerLabRunnerInputs.CreateDefault(runManifest);
        SigtranMaintainedPeerLabRunnerArtifactMaterializationPlan artifacts = SigtranMaintainedPeerLabRunnerArtifacts.CreateDefault(inputs.Workspace);
        IReadOnlyList<string> allPrerequisites = SigtranMaintainedPeerLabPrerequisites.GetDefault()
            .Select(static prerequisite => prerequisite.Id)
            .ToArray();
        SigtranMaintainedPeerLabRunnerPreflightReport preflight = SigtranMaintainedPeerLabRunnerPreflight.Evaluate(inputs, artifacts, allPrerequisites);
        SigtranMaintainedPeerLabRunnerCommandManifest commands = SigtranMaintainedPeerLabRunnerCommandManifests.Create(inputs, artifacts, preflight);
        SigtranMaintainedPeerLabRunnerWorkflowReadinessReport workflow = SigtranMaintainedPeerLabRunnerWorkflowReadiness.Evaluate(commands);

        bool foundationReady = workflow.Ready && GetCompletedCapabilities().Count == CompletedUnitCount;
        return new(
            CompletedUnitCount,
            foundationReady,
            runnerEvidenceReady: false,
            ["real-maintained-peer-run-required", "retained-runner-evidence-required"]);
    }

    /// <summary>Returns maintained peer lab runner status from a comparison handoff.</summary>
    /// <param name="comparisonHandoff">The runner comparison handoff.</param>
    /// <returns>The maintained peer lab runner status.</returns>
    public static SigtranMaintainedPeerLabRunnerStatusReport FromHandoff(SigtranMaintainedPeerLabRunnerComparisonHandoff comparisonHandoff)
    {
        ArgumentNullException.ThrowIfNull(comparisonHandoff);
        SigtranMaintainedPeerLabCommercialReadinessReport commercial = SigtranMaintainedPeerLabCommercialBridge.Evaluate(comparisonHandoff.ToEvidenceBundle());

        return new(
            CompletedUnitCount,
            foundationReady: true,
            runnerEvidenceReady: comparisonHandoff.IsHandoffReady && commercial.CommercialReady,
            commercial.Blockers);
    }
}
