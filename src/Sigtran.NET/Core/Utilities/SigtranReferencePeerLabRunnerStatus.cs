namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab runner readiness status.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerStatusReport
{
    /// <summary>Creates a reference peer lab runner status report.</summary>
    /// <param name="completedCapabilityCount">The completed runner capability count.</param>
    /// <param name="foundationReady">Whether the runner foundation is ready.</param>
    /// <param name="runnerEvidenceReady">Whether retained runner evidence is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranReferencePeerLabRunnerStatusReport(
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

    /// <summary>Whether runner evidence can support a production readiness claim.</summary>
    public bool ProductionReady => FoundationReady && RunnerEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact runner status summary.</summary>
    /// <returns>The runner status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={RunnerEvidenceReady} blockers={Blockers.Count} production={ProductionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner status helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerStatus
{
    /// <summary>The completed runner unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed reference peer lab runner capabilities.</summary>
    /// <returns>The completed reference peer lab runner capabilities.</returns>
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

    /// <summary>Returns reference peer lab runner foundation status without real retained evidence.</summary>
    /// <returns>The reference peer lab runner foundation status.</returns>
    public static SigtranReferencePeerLabRunnerStatusReport GetFoundationReport()
    {
        SigtranReferencePeerLabRunManifest runManifest = SigtranReferencePeerLabRunManifests.CreateDefault();
        SigtranReferencePeerLabRunnerInputBundle inputs = SigtranReferencePeerLabRunnerInputs.CreateDefault(runManifest);
        SigtranReferencePeerLabRunnerArtifactMaterializationPlan artifacts = SigtranReferencePeerLabRunnerArtifacts.CreateDefault(inputs.Workspace);
        IReadOnlyList<string> allPrerequisites = SigtranReferencePeerLabPrerequisites.GetDefault()
            .Select(static prerequisite => prerequisite.Id)
            .ToArray();
        SigtranReferencePeerLabRunnerPreflightReport preflight = SigtranReferencePeerLabRunnerPreflight.Evaluate(inputs, artifacts, allPrerequisites);
        SigtranReferencePeerLabRunnerCommandManifest commands = SigtranReferencePeerLabRunnerCommandManifests.Create(inputs, artifacts, preflight);
        SigtranReferencePeerLabRunnerWorkflowReadinessSnapshot workflow = SigtranReferencePeerLabRunnerWorkflowReadiness.Evaluate(commands);

        bool foundationReady = workflow.Ready && GetCompletedCapabilities().Count == CompletedUnitCount;
        return new(
            CompletedUnitCount,
            foundationReady,
            runnerEvidenceReady: false,
            ["real-reference-peer-run-required", "retained-runner-evidence-required"]);
    }

    /// <summary>Returns reference peer lab runner status from a comparison handoff.</summary>
    /// <param name="comparisonHandoff">The runner comparison handoff.</param>
    /// <returns>The reference peer lab runner status.</returns>
    public static SigtranReferencePeerLabRunnerStatusReport FromHandoff(SigtranReferencePeerLabRunnerComparisonHandoff comparisonHandoff)
    {
        ArgumentNullException.ThrowIfNull(comparisonHandoff);
        SigtranReferencePeerLabProductionReadinessSnapshot production = SigtranReferencePeerLabProductionBridge.Evaluate(comparisonHandoff.ToEvidenceBundle());

        return new(
            CompletedUnitCount,
            foundationReady: true,
            runnerEvidenceReady: comparisonHandoff.IsHandoffReady && production.ProductionReady,
            production.Blockers);
    }
}
