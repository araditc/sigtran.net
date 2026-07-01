namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides compliance and audit readiness status.
/// </summary>
public static class SigtranComplianceStatus
{
    private static readonly string[] Capabilities =
    [
        "compliance-capability-catalog",
        "audit-event-catalog",
        "evidence-retention-policy",
        "license-compliance-policy",
        "data-handling-classification",
        "export-control-policy",
        "compliance-readiness-report",
        "compliance-ci-profile",
        "production-compliance-gate",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Compliance And Audit Readiness";

    /// <summary>The number of completed compliance work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed compliance capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the compliance foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranComplianceReadiness.GetReport().FoundationReady
        && SigtranComplianceCi.CreateDefault().RequiresComplianceReadiness;

    /// <summary>Whether enterprise compliance can be claimed for production use.</summary>
    public static bool EnterpriseComplianceReady => FoundationReady
        && SigtranComplianceReadiness.GetReport().EnterpriseComplianceReady;

    /// <summary>Formats a compact compliance status summary.</summary>
    /// <returns>The compliance status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} enterpriseComplianceReady={EnterpriseComplianceReady}";
    }
}
