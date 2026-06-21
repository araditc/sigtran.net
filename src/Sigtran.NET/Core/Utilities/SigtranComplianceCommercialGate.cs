namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the compliance contribution to commercial readiness.
/// </summary>
public sealed class SigtranComplianceCommercialGateResult
{
    /// <summary>Creates a compliance commercial gate result.</summary>
    /// <param name="complianceFoundationReady">Whether compliance foundation is ready.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    public SigtranComplianceCommercialGateResult(bool complianceFoundationReady, bool commercialReady)
    {
        ComplianceFoundationReady = complianceFoundationReady;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether compliance foundation is ready.</summary>
    public bool ComplianceFoundationReady { get; }

    /// <summary>Whether wider commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether the compliance commercial gate is ready for production claims.</summary>
    public bool CanClaimEnterpriseCompliance => ComplianceFoundationReady && CommercialReady;

    /// <summary>Formats a compact compliance gate summary.</summary>
    /// <returns>The compliance gate summary.</returns>
    public string Describe()
    {
        return $"complianceFoundationReady={ComplianceFoundationReady} commercialReady={CommercialReady} enterpriseCompliance={CanClaimEnterpriseCompliance}";
    }
}

/// <summary>
/// Provides compliance commercial gate helpers.
/// </summary>
public static class SigtranComplianceCommercialGate
{
    /// <summary>Evaluates the current compliance commercial gate.</summary>
    /// <returns>The compliance commercial gate result.</returns>
    public static SigtranComplianceCommercialGateResult Evaluate()
    {
        SigtranComplianceReadinessReport report = SigtranComplianceReadiness.GetReport();
        return new(report.FoundationReady, report.CommercialReady);
    }
}
