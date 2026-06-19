namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides performance and capacity readiness status.
/// </summary>
public static class SigtranPerformanceStatus
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
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Performance Capacity And Benchmark Readiness";

    /// <summary>The number of completed performance work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed performance capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the performance foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranPerformanceReadiness.GetReport().FoundationReady
        && SigtranPerformanceCi.CreateDefault().RequiresPerformanceReadiness;

    /// <summary>Whether production performance claims are ready.</summary>
    public static bool ProductionPerformanceReady => FoundationReady
        && SigtranPerformanceReadiness.GetReport().ProductionPerformanceReady;

    /// <summary>Formats a compact performance status summary.</summary>
    /// <returns>The performance status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionPerformanceReady={ProductionPerformanceReady}";
    }
}
