namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab runner operations readiness status.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerOperationsStatusReport
{
    /// <summary>Creates a maintained peer lab runner operations status report.</summary>
    /// <param name="completedCapabilityCount">The completed operations capability count.</param>
    /// <param name="foundationReady">Whether the operations foundation is ready.</param>
    /// <param name="runnerEvidenceReady">Whether retained runner evidence is ready.</param>
    /// <param name="operatorHandoffReady">Whether operator handoff is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranMaintainedPeerLabRunnerOperationsStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool runnerEvidenceReady,
        bool operatorHandoffReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        RunnerEvidenceReady = runnerEvidenceReady;
        OperatorHandoffReady = operatorHandoffReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed operations capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the operations foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained runner evidence is ready.</summary>
    public bool RunnerEvidenceReady { get; }

    /// <summary>Whether operator handoff is ready.</summary>
    public bool OperatorHandoffReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether runner operations can support a commercial readiness claim.</summary>
    public bool CommercialReady => FoundationReady && RunnerEvidenceReady && OperatorHandoffReady && Blockers.Count == 0;

    /// <summary>Formats a compact runner operations status summary.</summary>
    /// <returns>The runner operations status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={RunnerEvidenceReady} handoff={OperatorHandoffReady} blockers={Blockers.Count} commercial={CommercialReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner operations status helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerOperationsStatus
{
    /// <summary>The completed runner operations unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed maintained peer lab runner operations capabilities.</summary>
    /// <returns>The completed maintained peer lab runner operations capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return
        [
            "runner-file-materialization",
            "runner-execution-log",
            "runner-command-outcomes",
            "runner-artifact-verification",
            "runner-provenance",
            "runner-failure-classification",
            "runner-retry-policy",
            "runner-evidence-package-manifest",
            "runner-operator-handoff",
            "runner-operations-status-reporting"
        ];
    }

    /// <summary>Returns maintained peer lab runner operations foundation status without real retained evidence.</summary>
    /// <returns>The maintained peer lab runner operations foundation status.</returns>
    public static SigtranMaintainedPeerLabRunnerOperationsStatusReport GetFoundationReport()
    {
        bool foundationReady = GetCompletedCapabilities().Count == CompletedUnitCount;
        return new(
            CompletedUnitCount,
            foundationReady,
            runnerEvidenceReady: false,
            operatorHandoffReady: false,
            ["real-maintained-peer-run-required", "retained-runner-evidence-package-required", "operator-handoff-required"]);
    }

    /// <summary>Returns maintained peer lab runner operations status from an operator handoff.</summary>
    /// <param name="handoff">The operator handoff.</param>
    /// <returns>The maintained peer lab runner operations status.</returns>
    public static SigtranMaintainedPeerLabRunnerOperationsStatusReport FromHandoff(SigtranMaintainedPeerLabRunnerOperatorHandoffReport handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        IReadOnlyList<string> blockers = handoff.ReadyForCommercialPromotion
            ? []
            : GetHandoffBlockers(handoff);

        return new(
            CompletedUnitCount,
            foundationReady: true,
            runnerEvidenceReady: handoff.PackageManifest.IsPackageReady,
            operatorHandoffReady: handoff.ReadyForOperatorReview,
            blockers);
    }

    private static IReadOnlyList<string> GetHandoffBlockers(SigtranMaintainedPeerLabRunnerOperatorHandoffReport handoff)
    {
        List<string> blockers = [];
        if (!handoff.ReadyForOperatorReview)
        {
            blockers.Add("operator-handoff-not-review-ready");
        }

        if (!handoff.PackageManifest.IsPackageReady)
        {
            blockers.Add("evidence-package-not-ready");
        }

        foreach (SigtranMaintainedPeerLabRunnerFailure failure in handoff.RetryEvaluation.FailureReport.BlockingFailures)
        {
            blockers.Add(failure.Code);
        }

        return blockers.ToArray();
    }
}
