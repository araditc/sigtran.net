namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides productionization status.
/// </summary>
public static class SigtranProductionReadinessStatus
{
    private static readonly string[] Capabilities =
    [
        "production-readiness-gates",
        "native-sctp-support-matrix",
        "external-interop-evidence-registry",
        "release-candidate-manifest",
        "package-governance-policy",
        "security-response-policy",
        "compatibility-policy",
        "observability-profile",
        "deployment-profiles",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "ProductionReadiness and Release Hardening";

    /// <summary>The number of completed productionization work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed productionization capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact productionization status summary.</summary>
    /// <returns>The productionization status summary.</returns>
    public static string Describe()
    {
        SigtranProductionReadinessSnapshot readiness = SigtranProductionReadiness.GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} internalReleaseReady={readiness.InternalReleaseReady} productionReady={readiness.ProductionReady}";
    }
}
