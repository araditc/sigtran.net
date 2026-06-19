namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 13 compliance and audit readiness status.
/// </summary>
public static class SigtranPhase13Status
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
        "commercial-compliance-gate",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 13 - Compliance And Audit Readiness";

    /// <summary>The number of completed Phase 13 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 13 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 13 compliance foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranComplianceReadiness.GetReport().FoundationReady
        && SigtranComplianceCi.CreateDefault().RequiresComplianceReadiness;

    /// <summary>Whether enterprise compliance can be claimed for production use.</summary>
    public static bool EnterpriseComplianceReady => FoundationReady
        && SigtranComplianceReadiness.GetReport().EnterpriseComplianceReady;

    /// <summary>Formats a compact Phase 13 status summary.</summary>
    /// <returns>The Phase 13 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} enterpriseComplianceReady={EnterpriseComplianceReady}";
    }
}
