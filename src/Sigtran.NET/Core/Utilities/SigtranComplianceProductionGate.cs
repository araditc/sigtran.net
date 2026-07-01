namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the compliance contribution to production readiness.
/// </summary>
public sealed class SigtranComplianceProductionGateResult
{
    /// <summary>Creates a compliance production gate result.</summary>
    /// <param name="complianceFoundationReady">Whether compliance foundation is ready.</param>
    /// <param name="productionReady">Whether wider production readiness is complete.</param>
    public SigtranComplianceProductionGateResult(bool complianceFoundationReady, bool productionReady)
    {
        ComplianceFoundationReady = complianceFoundationReady;
        ProductionReady = productionReady;
    }

    /// <summary>Whether compliance foundation is ready.</summary>
    public bool ComplianceFoundationReady { get; }

    /// <summary>Whether wider production readiness is complete.</summary>
    public bool ProductionReady { get; }

    /// <summary>Whether the compliance production gate is ready for production claims.</summary>
    public bool CanClaimEnterpriseCompliance => ComplianceFoundationReady && ProductionReady;

    /// <summary>Formats a compact compliance gate summary.</summary>
    /// <returns>The compliance gate summary.</returns>
    public string Describe()
    {
        return $"complianceFoundationReady={ComplianceFoundationReady} productionReady={ProductionReady} enterpriseCompliance={CanClaimEnterpriseCompliance}";
    }
}

/// <summary>
/// Provides compliance production gate helpers.
/// </summary>
public static class SigtranComplianceProductionGate
{
    /// <summary>Evaluates the current compliance production gate.</summary>
    /// <returns>The compliance production gate result.</returns>
    public static SigtranComplianceProductionGateResult Evaluate()
    {
        SigtranComplianceReadinessSnapshot report = SigtranComplianceReadiness.GetReport();
        return new(report.FoundationReady, report.ProductionReady);
    }
}
