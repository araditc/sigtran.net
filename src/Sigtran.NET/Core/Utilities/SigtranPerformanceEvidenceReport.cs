using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Aggregates benchmark workload, retained artifacts, latency, resource, and resilience evidence into one publishable report.
/// </summary>
public sealed class SigtranPerformanceEvidenceReport
{
    private readonly SigtranPerformanceLatencyBudgetReport[] _latencyReports;

    /// <summary>Creates a performance evidence report.</summary>
    /// <param name="runPlan">The peer-traffic benchmark run plan.</param>
    /// <param name="latencyReports">The latency budget reports.</param>
    /// <param name="resourceReport">The resource budget report.</param>
    /// <param name="resilienceEvidence">The resilience evidence.</param>
    /// <param name="generatedUtc">The report generation timestamp.</param>
    public SigtranPerformanceEvidenceReport(
        SigtranPerformanceEvidenceRunPlan runPlan,
        IReadOnlyList<SigtranPerformanceLatencyBudgetReport> latencyReports,
        SigtranPerformanceResourceBudgetReport resourceReport,
        SigtranPerformanceResilienceEvidence resilienceEvidence,
        DateTimeOffset generatedUtc)
    {
        RunPlan = runPlan ?? throw new ArgumentNullException(nameof(runPlan));
        _latencyReports = (latencyReports ?? throw new ArgumentNullException(nameof(latencyReports))).ToArray();
        ResourceReport = resourceReport ?? throw new ArgumentNullException(nameof(resourceReport));
        ResilienceEvidence = resilienceEvidence ?? throw new ArgumentNullException(nameof(resilienceEvidence));
        GeneratedUtc = generatedUtc;
    }

    /// <summary>The peer-traffic benchmark run plan.</summary>
    public SigtranPerformanceEvidenceRunPlan RunPlan { get; }

    /// <summary>The latency budget reports.</summary>
    public IReadOnlyList<SigtranPerformanceLatencyBudgetReport> LatencyReports => _latencyReports.ToArray();

    /// <summary>The resource budget report.</summary>
    public SigtranPerformanceResourceBudgetReport ResourceReport { get; }

    /// <summary>The resilience evidence.</summary>
    public SigtranPerformanceResilienceEvidence ResilienceEvidence { get; }

    /// <summary>The report generation timestamp.</summary>
    public DateTimeOffset GeneratedUtc { get; }

    /// <summary>Whether every known latency budget has passing evidence.</summary>
    public bool LatencyEvidencePassed => _latencyReports.Length == SigtranLatencyBudgets.GetBudgets().Count
        && _latencyReports.All(static report => report.Passed);

    /// <summary>Whether the report can be published as release-grade performance evidence.</summary>
    public bool Publishable => RunPlan.SupportsCommercialEvidence
        && LatencyEvidencePassed
        && ResourceReport.Passed
        && ResilienceEvidence.SupportsCommercialEvidence;

    /// <summary>Formats a compact performance evidence report summary.</summary>
    /// <returns>The performance evidence report summary.</returns>
    public string Describe()
    {
        return $"run={RunPlan.RunId} publishable={Publishable} latency={LatencyEvidencePassed} resource={ResourceReport.Passed} resilience={ResilienceEvidence.SupportsCommercialEvidence}";
    }

    /// <summary>Renders the performance evidence report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# SIGTRAN.NET Performance Evidence Report");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunPlan.RunId}`");
        builder.AppendLine($"Generated UTC: `{GeneratedUtc:O}`");
        builder.AppendLine($"Publishable: `{Publishable}`");
        builder.AppendLine();
        builder.AppendLine("## Gates");
        builder.AppendLine();
        builder.AppendLine("| Area | Passed |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Workload and artifacts | `{RunPlan.SupportsCommercialEvidence}` |");
        builder.AppendLine($"| Latency P95/P99 | `{LatencyEvidencePassed}` |");
        builder.AppendLine($"| CPU memory allocation | `{ResourceReport.Passed}` |");
        builder.AppendLine($"| Resilience failover | `{ResilienceEvidence.SupportsCommercialEvidence}` |");
        builder.AppendLine();
        builder.AppendLine("## Latency");
        builder.AppendLine();
        builder.AppendLine("| Surface | P95 OK | P99 OK | Passed |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (SigtranPerformanceLatencyBudgetReport report in _latencyReports)
        {
            builder.AppendLine($"| {report.Evidence.Surface} | `{report.P95WithinBudget}` | `{report.P99WithinBudget}` | `{report.Passed}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Resources");
        builder.AppendLine();
        builder.AppendLine($"CPU OK: `{ResourceReport.CpuWithinBudget}`");
        builder.AppendLine($"Working set OK: `{ResourceReport.WorkingSetWithinBudget}`");
        builder.AppendLine($"Allocation OK: `{ResourceReport.AllocationWithinBudget}`");
        builder.AppendLine();
        builder.AppendLine("## Resilience");
        builder.AppendLine();
        builder.AppendLine($"Recovery within budget: `{ResilienceEvidence.RecoveryWithinBudget}`");
        builder.AppendLine($"Lost messages: `{ResilienceEvidence.LostMessages}`");
        return builder.ToString();
    }
}

/// <summary>
/// Creates performance evidence reports from measured evidence.
/// </summary>
public static class SigtranPerformanceEvidenceReports
{
    /// <summary>Creates a publishable performance evidence report from measured evidence.</summary>
    /// <param name="runPlan">The peer-traffic benchmark run plan.</param>
    /// <param name="latencyEvidence">The measured latency evidence.</param>
    /// <param name="resourceEvidence">The measured resource evidence.</param>
    /// <param name="resilienceEvidence">The resilience evidence.</param>
    /// <param name="generatedUtc">The report generation timestamp.</param>
    /// <returns>The performance evidence report.</returns>
    public static SigtranPerformanceEvidenceReport Create(
        SigtranPerformanceEvidenceRunPlan runPlan,
        IReadOnlyList<SigtranPerformanceLatencyEvidence> latencyEvidence,
        SigtranPerformanceResourceEvidence resourceEvidence,
        SigtranPerformanceResilienceEvidence resilienceEvidence,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(runPlan);
        ArgumentNullException.ThrowIfNull(latencyEvidence);
        ArgumentNullException.ThrowIfNull(resourceEvidence);
        ArgumentNullException.ThrowIfNull(resilienceEvidence);

        return new(
            runPlan,
            SigtranPerformanceLatencyEvidenceEvaluator.Evaluate(latencyEvidence),
            SigtranPerformanceResourceEvidenceEvaluator.EvaluateCommercial(resourceEvidence),
            resilienceEvidence,
            generatedUtc);
    }
}
