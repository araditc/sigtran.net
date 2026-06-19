namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides production operations and support status.
/// </summary>
public static class SigtranOperationsStatus
{
    private static readonly string[] Capabilities =
    [
        "operations-capability-catalog",
        "runbook-catalog",
        "incident-response-targets",
        "health-check-matrix",
        "rollback-plan",
        "maintenance-policy",
        "support-handbook",
        "operations-readiness-report",
        "operations-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Operations And Support";

    /// <summary>The number of completed operations work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed operations capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the operations foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranOperationsReadiness.GetReport().FoundationReady
        && SigtranOperationsCi.CreateDefault().RequiresOperationsReadiness;

    /// <summary>Whether production operations are ready.</summary>
    public static bool ProductionOperationsReady => FoundationReady
        && SigtranOperationsReadiness.GetReport().ProductionOperationsReady;

    /// <summary>Formats a compact operations status summary.</summary>
    /// <returns>The operations status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionOperationsReady={ProductionOperationsReady}";
    }
}
