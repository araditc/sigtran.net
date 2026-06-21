namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes measured latency percentiles for one performance surface.
/// </summary>
public sealed class SigtranPerformanceLatencyEvidence
{
    /// <summary>Creates measured latency percentile evidence.</summary>
    /// <param name="surface">The measured latency surface.</param>
    /// <param name="sampleCount">The latency sample count.</param>
    /// <param name="p50">The measured P50 latency.</param>
    /// <param name="p95">The measured P95 latency.</param>
    /// <param name="p99">The measured P99 latency.</param>
    /// <param name="maximum">The measured maximum latency.</param>
    public SigtranPerformanceLatencyEvidence(
        SigtranLatencySurface surface,
        long sampleCount,
        TimeSpan p50,
        TimeSpan p95,
        TimeSpan p99,
        TimeSpan maximum)
    {
        Surface = surface;
        SampleCount = sampleCount <= 0 ? throw new ArgumentOutOfRangeException(nameof(sampleCount), "Latency sample count must be positive.") : sampleCount;
        P50 = p50 <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(p50), "P50 latency must be positive.") : p50;
        P95 = p95 < p50 ? throw new ArgumentOutOfRangeException(nameof(p95), "P95 latency must be greater than or equal to P50.") : p95;
        P99 = p99 < p95 ? throw new ArgumentOutOfRangeException(nameof(p99), "P99 latency must be greater than or equal to P95.") : p99;
        Maximum = maximum < p99 ? throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum latency must be greater than or equal to P99.") : maximum;
    }

    /// <summary>The measured latency surface.</summary>
    public SigtranLatencySurface Surface { get; }

    /// <summary>The latency sample count.</summary>
    public long SampleCount { get; }

    /// <summary>The measured P50 latency.</summary>
    public TimeSpan P50 { get; }

    /// <summary>The measured P95 latency.</summary>
    public TimeSpan P95 { get; }

    /// <summary>The measured P99 latency.</summary>
    public TimeSpan P99 { get; }

    /// <summary>The measured maximum latency.</summary>
    public TimeSpan Maximum { get; }

    /// <summary>Formats a compact latency evidence summary.</summary>
    /// <returns>The latency evidence summary.</returns>
    public string Describe()
    {
        return $"surface={Surface} samples={SampleCount} p95Ms={P95.TotalMilliseconds:F3} p99Ms={P99.TotalMilliseconds:F3} maxMs={Maximum.TotalMilliseconds:F3}";
    }
}

/// <summary>
/// Describes the result of checking measured latency evidence against a budget.
/// </summary>
public sealed class SigtranPerformanceLatencyBudgetReport
{
    /// <summary>Creates a latency budget report.</summary>
    /// <param name="evidence">The measured latency evidence.</param>
    /// <param name="budget">The expected latency budget.</param>
    public SigtranPerformanceLatencyBudgetReport(
        SigtranPerformanceLatencyEvidence evidence,
        SigtranLatencyBudget budget)
    {
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        Budget = budget ?? throw new ArgumentNullException(nameof(budget));
        if (evidence.Surface != budget.Surface)
        {
            throw new ArgumentException("Latency evidence and budget surfaces must match.", nameof(budget));
        }
    }

    /// <summary>The measured latency evidence.</summary>
    public SigtranPerformanceLatencyEvidence Evidence { get; }

    /// <summary>The expected latency budget.</summary>
    public SigtranLatencyBudget Budget { get; }

    /// <summary>Whether measured P95 latency is within budget.</summary>
    public bool P95WithinBudget => Evidence.P95 <= Budget.P95Budget;

    /// <summary>Whether measured P99 latency is within budget.</summary>
    public bool P99WithinBudget => Evidence.P99 <= Budget.P99Budget;

    /// <summary>Whether the latency evidence passes P95 and P99 budgets.</summary>
    public bool Passed => P95WithinBudget && P99WithinBudget;

    /// <summary>Formats a compact latency budget report summary.</summary>
    /// <returns>The latency budget report summary.</returns>
    public string Describe()
    {
        return $"surface={Evidence.Surface} p95Ok={P95WithinBudget} p99Ok={P99WithinBudget} passed={Passed}";
    }
}

/// <summary>
/// Evaluates measured performance latency evidence against SDK latency budgets.
/// </summary>
public static class SigtranPerformanceLatencyEvidenceEvaluator
{
    /// <summary>Evaluates all measured latency evidence entries against known budgets.</summary>
    /// <param name="evidence">The measured latency evidence entries.</param>
    /// <returns>The latency budget reports.</returns>
    public static IReadOnlyList<SigtranPerformanceLatencyBudgetReport> Evaluate(IReadOnlyList<SigtranPerformanceLatencyEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        IReadOnlyList<SigtranLatencyBudget> budgets = SigtranLatencyBudgets.GetBudgets();
        return evidence
            .Select(item => new SigtranPerformanceLatencyBudgetReport(item, FindBudget(budgets, item.Surface)))
            .ToArray();
    }

    /// <summary>Returns whether measured latency evidence covers and passes every known budget.</summary>
    /// <param name="evidence">The measured latency evidence entries.</param>
    /// <returns>True when all known budgets have passing evidence; otherwise false.</returns>
    public static bool CoversAndPassesAllBudgets(IReadOnlyList<SigtranPerformanceLatencyEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        IReadOnlyList<SigtranLatencyBudget> budgets = SigtranLatencyBudgets.GetBudgets();
        IReadOnlyList<SigtranPerformanceLatencyBudgetReport> reports = Evaluate(evidence);
        return budgets.All(budget => reports.Any(report => report.Evidence.Surface == budget.Surface && report.Passed));
    }

    private static SigtranLatencyBudget FindBudget(IReadOnlyList<SigtranLatencyBudget> budgets, SigtranLatencySurface surface)
    {
        return budgets.FirstOrDefault(budget => budget.Surface == surface)
            ?? throw new ArgumentException($"No latency budget is registered for surface '{surface}'.", nameof(surface));
    }
}
