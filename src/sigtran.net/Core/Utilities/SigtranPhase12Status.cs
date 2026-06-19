namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 12 production operations and support status.
/// </summary>
public static class SigtranPhase12Status
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
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 12 - Production Operations And Support";

    /// <summary>The number of completed Phase 12 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 12 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 12 operations foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranOperationsReadiness.GetReport().FoundationReady
        && SigtranOperationsCi.CreateDefault().RequiresOperationsReadiness;

    /// <summary>Whether production operations are ready.</summary>
    public static bool ProductionOperationsReady => FoundationReady
        && SigtranOperationsReadiness.GetReport().ProductionOperationsReady;

    /// <summary>Formats a compact Phase 12 status summary.</summary>
    /// <returns>The Phase 12 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionOperationsReady={ProductionOperationsReady}";
    }
}
