namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab runner operations readiness status.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerOperationsStatusReport
{
    /// <summary>Creates a reference peer lab runner operations status report.</summary>
    /// <param name="completedCapabilityCount">The completed operations capability count.</param>
    /// <param name="foundationReady">Whether the operations foundation is ready.</param>
    /// <param name="runnerEvidenceReady">Whether retained runner evidence is ready.</param>
    /// <param name="operatorHandoffReady">Whether operator handoff is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranReferencePeerLabRunnerOperationsStatusReport(
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

    /// <summary>Whether runner operations can support a production readiness claim.</summary>
    public bool ProductionReady => FoundationReady && RunnerEvidenceReady && OperatorHandoffReady && Blockers.Count == 0;

    /// <summary>Formats a compact runner operations status summary.</summary>
    /// <returns>The runner operations status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={RunnerEvidenceReady} handoff={OperatorHandoffReady} blockers={Blockers.Count} production={ProductionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner operations status helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerOperationsStatus
{
    /// <summary>The completed runner operations unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed reference peer lab runner operations capabilities.</summary>
    /// <returns>The completed reference peer lab runner operations capabilities.</returns>
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

    /// <summary>Returns reference peer lab runner operations foundation status without real retained evidence.</summary>
    /// <returns>The reference peer lab runner operations foundation status.</returns>
    public static SigtranReferencePeerLabRunnerOperationsStatusReport GetFoundationReport()
    {
        bool foundationReady = GetCompletedCapabilities().Count == CompletedUnitCount;
        return new(
            CompletedUnitCount,
            foundationReady,
            runnerEvidenceReady: false,
            operatorHandoffReady: false,
            ["real-reference-peer-run-required", "retained-runner-evidence-package-required", "operator-handoff-required"]);
    }

    /// <summary>Returns reference peer lab runner operations status from an operator handoff.</summary>
    /// <param name="handoff">The operator handoff.</param>
    /// <returns>The reference peer lab runner operations status.</returns>
    public static SigtranReferencePeerLabRunnerOperationsStatusReport FromHandoff(SigtranReferencePeerLabRunnerOperatorHandoffReport handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        IReadOnlyList<string> blockers = handoff.ReadyForProductionPromotion
            ? []
            : GetHandoffBlockers(handoff);

        return new(
            CompletedUnitCount,
            foundationReady: true,
            runnerEvidenceReady: handoff.PackageManifest.IsPackageReady,
            operatorHandoffReady: handoff.ReadyForOperatorReview,
            blockers);
    }

    private static IReadOnlyList<string> GetHandoffBlockers(SigtranReferencePeerLabRunnerOperatorHandoffReport handoff)
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

        foreach (SigtranReferencePeerLabRunnerFailure failure in handoff.RetryEvaluation.FailureReport.BlockingFailures)
        {
            blockers.Add(failure.Code);
        }

        return blockers.ToArray();
    }
}
