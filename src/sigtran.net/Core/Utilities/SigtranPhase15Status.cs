namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 15 API lifecycle readiness status.
/// </summary>
public static class SigtranPhase15Status
{
    private static readonly string[] Capabilities =
    [
        "api-surface-catalog",
        "api-stability-contracts",
        "api-version-matrix",
        "deprecation-policy",
        "migration-guide-catalog",
        "breaking-change-review-policy",
        "public-api-baseline",
        "api-lifecycle-readiness-report",
        "api-lifecycle-ci-profile",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 15 - API Stability Deprecation And Migration Readiness";

    /// <summary>The number of completed Phase 15 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 15 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 15 API lifecycle foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranApiLifecycleReadiness.GetReport().FoundationReady
        && SigtranApiLifecycleCi.CreateDefault().RequiresApiLifecycleReadiness;

    /// <summary>Whether stable API lifecycle claims are ready.</summary>
    public static bool StableApiLifecycleReady => FoundationReady
        && SigtranApiLifecycleReadiness.GetReport().StableApiLifecycleReady;

    /// <summary>Formats a compact Phase 15 status summary.</summary>
    /// <returns>The Phase 15 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} stableApiLifecycleReady={StableApiLifecycleReady}";
    }
}
