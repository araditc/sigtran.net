namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the configuration contribution to commercial readiness.
/// </summary>
public sealed class SigtranConfigurationCommercialGateResult
{
    /// <summary>Creates a configuration commercial gate result.</summary>
    /// <param name="configurationFoundationReady">Whether configuration foundation is ready.</param>
    /// <param name="productionSecretsSafe">Whether production secrets policy is safe.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    public SigtranConfigurationCommercialGateResult(
        bool configurationFoundationReady,
        bool productionSecretsSafe,
        bool commercialReady)
    {
        ConfigurationFoundationReady = configurationFoundationReady;
        ProductionSecretsSafe = productionSecretsSafe;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether configuration foundation is ready.</summary>
    public bool ConfigurationFoundationReady { get; }

    /// <summary>Whether production secrets policy is safe.</summary>
    public bool ProductionSecretsSafe { get; }

    /// <summary>Whether wider commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether production configuration can be claimed.</summary>
    public bool CanClaimProductionConfiguration => ConfigurationFoundationReady && ProductionSecretsSafe && CommercialReady;

    /// <summary>Formats a compact configuration gate summary.</summary>
    /// <returns>The configuration gate summary.</returns>
    public string Describe()
    {
        return $"configurationFoundationReady={ConfigurationFoundationReady} productionSecretsSafe={ProductionSecretsSafe} commercialReady={CommercialReady} productionConfiguration={CanClaimProductionConfiguration}";
    }
}

/// <summary>
/// Provides configuration commercial gate helpers.
/// </summary>
public static class SigtranConfigurationCommercialGate
{
    /// <summary>Evaluates the current configuration commercial gate.</summary>
    /// <returns>The configuration commercial gate result.</returns>
    public static SigtranConfigurationCommercialGateResult Evaluate()
    {
        SigtranConfigurationReadinessReport report = SigtranConfigurationReadiness.GetReport();
        return new(
            report.FoundationReady,
            SigtranSecretPolicies.CreateDefault().IsProductionSafe,
            report.CommercialReady);
    }
}
