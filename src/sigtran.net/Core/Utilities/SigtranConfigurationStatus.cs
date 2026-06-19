namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides configuration and environment readiness status.
/// </summary>
public static class SigtranConfigurationStatus
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
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Configuration Policy And Environment Readiness";

    /// <summary>The number of completed configuration readiness work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed configuration readiness capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the configuration foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranConfigurationReadiness.GetReport().FoundationReady
        && SigtranConfigurationCi.CreateDefault().RequiresConfigurationReadiness;

    /// <summary>Whether production configuration claims are ready.</summary>
    public static bool ProductionConfigurationReady => FoundationReady
        && SigtranConfigurationReadiness.GetReport().ProductionConfigurationReady;

    /// <summary>Formats a compact configuration readiness status summary.</summary>
    /// <returns>The configuration readiness status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionConfigurationReady={ProductionConfigurationReady}";
    }
}
