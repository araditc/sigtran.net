namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 14 performance and capacity readiness status.
/// </summary>
public static class SigtranPhase14Status
{
    private static readonly string[] Capabilities =
    [
        "performance-capability-catalog",
        "benchmark-scenario-catalog",
        "capacity-profile",
        "throughput-targets",
        "latency-budget",
        "load-test-plan",
        "resource-budget",
        "performance-readiness-report",
        "performance-ci-profile",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 14 - Performance Capacity And Benchmark Readiness";

    /// <summary>The number of completed Phase 14 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 14 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 14 performance foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranPerformanceReadiness.GetReport().FoundationReady
        && SigtranPerformanceCi.CreateDefault().RequiresPerformanceReadiness;

    /// <summary>Whether production performance claims are ready.</summary>
    public static bool ProductionPerformanceReady => FoundationReady
        && SigtranPerformanceReadiness.GetReport().ProductionPerformanceReady;

    /// <summary>Formats a compact Phase 14 status summary.</summary>
    /// <returns>The Phase 14 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionPerformanceReady={ProductionPerformanceReady}";
    }
}
