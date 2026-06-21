namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Summarizes performance and resilience evidence execution status.
/// </summary>
public sealed class SigtranPerformanceEvidenceStatusReport
{
    private readonly string[] _capabilities;
    private readonly string[] _blockers;

    /// <summary>Creates a performance evidence status report.</summary>
    /// <param name="label">The status label.</param>
    /// <param name="completedUnitCount">The completed unit count.</param>
    /// <param name="capabilities">The completed capability names.</param>
    /// <param name="foundationReady">Whether the evidence foundation is ready.</param>
    /// <param name="reportPublishable">Whether a publishable performance report is available.</param>
    /// <param name="productionPerformanceReady">Whether production performance claims are allowed.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranPerformanceEvidenceStatusReport(
        string label,
        int completedUnitCount,
        IReadOnlyList<string> capabilities,
        bool foundationReady,
        bool reportPublishable,
        bool productionPerformanceReady,
        IReadOnlyList<string> blockers)
    {
        Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Performance evidence status label is required.", nameof(label)) : label;
        CompletedUnitCount = completedUnitCount >= 0 ? completedUnitCount : throw new ArgumentOutOfRangeException(nameof(completedUnitCount), "Completed unit count must not be negative.");
        _capabilities = (capabilities ?? throw new ArgumentNullException(nameof(capabilities))).ToArray();
        FoundationReady = foundationReady;
        ReportPublishable = reportPublishable;
        ProductionPerformanceReady = productionPerformanceReady;
        _blockers = (blockers ?? throw new ArgumentNullException(nameof(blockers))).ToArray();
    }

    /// <summary>The status label.</summary>
    public string Label { get; }

    /// <summary>The completed unit count.</summary>
    public int CompletedUnitCount { get; }

    /// <summary>The completed capability names.</summary>
    public IReadOnlyList<string> Capabilities => _capabilities.ToArray();

    /// <summary>Whether the evidence foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether a publishable performance report is available.</summary>
    public bool ReportPublishable { get; }

    /// <summary>Whether production performance claims are allowed.</summary>
    public bool ProductionPerformanceReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers => _blockers.ToArray();

    /// <summary>Formats a compact performance evidence status summary.</summary>
    /// <returns>The performance evidence status summary.</returns>
    public string Describe()
    {
        return $"label={Label} completedUnits={CompletedUnitCount} capabilities={_capabilities.Length} foundationReady={FoundationReady} reportPublishable={ReportPublishable} productionPerformanceReady={ProductionPerformanceReady} blockers={_blockers.Length}";
    }
}

/// <summary>
/// Provides current performance and resilience evidence execution status.
/// </summary>
public static class SigtranPerformanceEvidenceStatus
{
    /// <summary>The status label.</summary>
    public const string StatusLabel = "Performance and resilience evidence";

    /// <summary>The number of completed performance evidence units.</summary>
    public const int CompletedUnitCount = 9;

    private static readonly string[] CurrentCapabilities =
    [
        "peer-traffic-workload-evidence",
        "retained-artifact-run-plan",
        "latency-percentile-evidence",
        "resource-evidence",
        "resilience-failover-evidence",
        "publishable-report",
        "production-evidence-gate",
        "runner-ci-handoff",
        "status-and-summary-documentation"
    ];

    /// <summary>The completed capability names.</summary>
    public static IReadOnlyList<string> Capabilities => CurrentCapabilities.ToArray();

    /// <summary>Whether the evidence foundation is ready.</summary>
    public static bool FoundationReady => CurrentCapabilities.Length == CompletedUnitCount
        && SigtranPerformanceReadiness.GetReport().FoundationReady
        && SigtranPerformanceEvidenceRunnerPlans.CreateCiHandoff(SigtranPerformanceEvidenceRunnerPlans.CreateDefault()).IsReady;

    /// <summary>Returns the current performance evidence status report.</summary>
    /// <param name="report">The optional retained performance evidence report.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    /// <returns>The current performance evidence status report.</returns>
    public static SigtranPerformanceEvidenceStatusReport GetStatus(
        SigtranPerformanceEvidenceReport? report = null,
        bool commercialReady = false)
    {
        SigtranPerformanceEvidenceGateResult gate = SigtranPerformanceEvidenceGate.Evaluate(report, commercialReady);
        return new(
            StatusLabel,
            CompletedUnitCount,
            CurrentCapabilities,
            FoundationReady,
            gate.ReportPublishable,
            gate.CanClaimProductionPerformance,
            gate.Reasons);
    }

    /// <summary>Formats a compact performance evidence status summary.</summary>
    /// <returns>The performance evidence status summary.</returns>
    public static string Describe()
    {
        return GetStatus().Describe();
    }
}
