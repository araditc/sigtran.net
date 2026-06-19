namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 16 configuration and environment readiness status.
/// </summary>
public static class SigtranPhase16Status
{
    private static readonly string[] Capabilities =
    [
        "configuration-schema",
        "configuration-validation",
        "environment-matrix",
        "secret-policy",
        "transport-configuration",
        "routing-configuration",
        "configuration-readiness-report",
        "configuration-ci-profile",
        "commercial-configuration-gate",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 16 - Configuration Policy And Environment Readiness";

    /// <summary>The number of completed Phase 16 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 16 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 16 configuration foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranConfigurationReadiness.GetReport().FoundationReady
        && SigtranConfigurationCi.CreateDefault().RequiresConfigurationReadiness;

    /// <summary>Whether production configuration claims are ready.</summary>
    public static bool ProductionConfigurationReady => FoundationReady
        && SigtranConfigurationReadiness.GetReport().ProductionConfigurationReady;

    /// <summary>Formats a compact Phase 16 status summary.</summary>
    /// <returns>The Phase 16 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionConfigurationReady={ProductionConfigurationReady}";
    }
}
