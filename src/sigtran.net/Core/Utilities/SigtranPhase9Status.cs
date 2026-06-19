namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 9 real interoperability lab status.
/// </summary>
public static class SigtranPhase9Status
{
    private static readonly string[] Capabilities =
    [
        "interop-lab-scenario-catalog",
        "interop-lab-artifact-manifests",
        "interop-lab-run-reports",
        "openss7-ipss7-peer-template",
        "trace-comparison-reporting",
        "evidence-promotion",
        "interop-lab-ci-profile",
        "interop-lab-readiness-report",
        "commercial-readiness-gate-integration",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 9 - Real Interoperability Lab";

    /// <summary>The number of completed Phase 9 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 9 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact Phase 9 status summary.</summary>
    /// <returns>The Phase 9 status summary.</returns>
    public static string Describe()
    {
        SigtranInteropLabReadinessReport readiness = SigtranInteropLabReadiness.GetReport();
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={readiness.FoundationReady} productionReady={readiness.ProductionReady}";
    }
}
