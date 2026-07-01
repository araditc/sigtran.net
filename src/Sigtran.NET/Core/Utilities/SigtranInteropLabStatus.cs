namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides real interoperability lab status.
/// </summary>
public static class SigtranInteropLabStatus
{
    private static readonly string[] Capabilities =
    [
        "interop-lab-scenario-catalog",
        "interop-lab-artifact-manifests",
        "interop-lab-run-reports",
        "external-peer-template",
        "trace-comparison-reporting",
        "evidence-promotion",
        "interop-lab-ci-profile",
        "interop-lab-readiness-report",
        "production-readiness-gate-integration",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Real Interoperability Lab";

    /// <summary>The number of completed interoperability lab work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed interoperability lab capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact interoperability lab status summary.</summary>
    /// <returns>The interoperability lab status summary.</returns>
    public static string Describe()
    {
        SigtranInteropLabReadinessSnapshot readiness = SigtranInteropLabReadiness.GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={readiness.FoundationReady} productionReady={readiness.ProductionReady}";
    }
}
