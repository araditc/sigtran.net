namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides API lifecycle readiness status.
/// </summary>
public static class SigtranApiLifecycleStatus
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
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "API Stability Deprecation And Migration Readiness";

    /// <summary>The number of completed API lifecycle work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed API lifecycle capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the API lifecycle foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranApiLifecycleReadiness.GetReport().FoundationReady
        && SigtranApiLifecycleCi.CreateDefault().RequiresApiLifecycleReadiness;

    /// <summary>Whether stable API lifecycle claims are ready.</summary>
    public static bool StableApiLifecycleReady => FoundationReady
        && SigtranApiLifecycleReadiness.GetReport().StableApiLifecycleReady;

    /// <summary>Formats a compact API lifecycle status summary.</summary>
    /// <returns>The API lifecycle status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} stableApiLifecycleReady={StableApiLifecycleReady}";
    }
}
