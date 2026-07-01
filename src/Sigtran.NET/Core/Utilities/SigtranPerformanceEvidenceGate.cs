namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the result of evaluating performance evidence for production claims.
/// </summary>
public sealed class SigtranPerformanceEvidenceGateResult
{
    private readonly string[] _reasons;

    /// <summary>Creates a performance evidence gate result.</summary>
    /// <param name="foundationReady">Whether the performance foundation is ready.</param>
    /// <param name="reportPublishable">Whether the performance evidence report is publishable.</param>
    /// <param name="productionReady">Whether wider production readiness is complete.</param>
    /// <param name="reasons">The gate reason identifiers.</param>
    public SigtranPerformanceEvidenceGateResult(
        bool foundationReady,
        bool reportPublishable,
        bool productionReady,
        IReadOnlyList<string> reasons)
    {
        FoundationReady = foundationReady;
        ReportPublishable = reportPublishable;
        ProductionReady = productionReady;
        _reasons = (reasons ?? throw new ArgumentNullException(nameof(reasons))).ToArray();
    }

    /// <summary>Whether the performance foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether the performance evidence report is publishable.</summary>
    public bool ReportPublishable { get; }

    /// <summary>Whether wider production readiness is complete.</summary>
    public bool ProductionReady { get; }

    /// <summary>The gate reason identifiers.</summary>
    public IReadOnlyList<string> Reasons => _reasons.ToArray();

    /// <summary>Whether production performance claims are allowed.</summary>
    public bool CanClaimProductionPerformance => FoundationReady
        && ReportPublishable
        && ProductionReady
        && _reasons.Length == 0;

    /// <summary>Formats a compact gate result summary.</summary>
    /// <returns>The gate result summary.</returns>
    public string Describe()
    {
        return $"foundationReady={FoundationReady} reportPublishable={ReportPublishable} productionReady={ProductionReady} productionPerformance={CanClaimProductionPerformance} reasons={_reasons.Length}";
    }
}

/// <summary>
/// Evaluates retained performance evidence against production claim gates.
/// </summary>
public static class SigtranPerformanceEvidenceGate
{
    /// <summary>Evaluates whether retained performance evidence can support production performance claims.</summary>
    /// <param name="report">The performance evidence report.</param>
    /// <param name="productionReady">Whether wider production readiness is complete.</param>
    /// <returns>The performance evidence gate result.</returns>
    public static SigtranPerformanceEvidenceGateResult Evaluate(
        SigtranPerformanceEvidenceReport? report,
        bool productionReady)
    {
        bool foundationReady = SigtranPerformanceReadiness.GetReport().FoundationReady;
        bool reportPublishable = report?.Publishable == true;
        List<string> reasons = [];

        if (!foundationReady)
        {
            reasons.Add("performance-foundation-required");
        }

        if (!reportPublishable)
        {
            reasons.Add("publishable-performance-report-required");
        }

        if (!productionReady)
        {
            reasons.Add("production-readiness-required");
        }

        return new(foundationReady, reportPublishable, productionReady, reasons);
    }

    /// <summary>Evaluates whether retained performance evidence can support production performance claims using current production readiness.</summary>
    /// <param name="report">The performance evidence report.</param>
    /// <returns>The performance evidence gate result.</returns>
    public static SigtranPerformanceEvidenceGateResult EvaluateCurrent(SigtranPerformanceEvidenceReport? report)
    {
        return Evaluate(report, SigtranProductionReadiness.GetReport().ProductionReady);
    }
}
