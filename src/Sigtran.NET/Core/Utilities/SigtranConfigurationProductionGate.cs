namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the configuration contribution to production readiness.
/// </summary>
public sealed class SigtranConfigurationProductionGateResult
{
    /// <summary>Creates a configuration production gate result.</summary>
    /// <param name="configurationFoundationReady">Whether configuration foundation is ready.</param>
    /// <param name="productionSecretsSafe">Whether production secrets policy is safe.</param>
    /// <param name="productionReady">Whether wider production readiness is complete.</param>
    public SigtranConfigurationProductionGateResult(
        bool configurationFoundationReady,
        bool productionSecretsSafe,
        bool productionReady)
    {
        ConfigurationFoundationReady = configurationFoundationReady;
        ProductionSecretsSafe = productionSecretsSafe;
        ProductionReady = productionReady;
    }

    /// <summary>Whether configuration foundation is ready.</summary>
    public bool ConfigurationFoundationReady { get; }

    /// <summary>Whether production secrets policy is safe.</summary>
    public bool ProductionSecretsSafe { get; }

    /// <summary>Whether wider production readiness is complete.</summary>
    public bool ProductionReady { get; }

    /// <summary>Whether production configuration can be claimed.</summary>
    public bool CanClaimProductionConfiguration => ConfigurationFoundationReady && ProductionSecretsSafe && ProductionReady;

    /// <summary>Formats a compact configuration gate summary.</summary>
    /// <returns>The configuration gate summary.</returns>
    public string Describe()
    {
        return $"configurationFoundationReady={ConfigurationFoundationReady} productionSecretsSafe={ProductionSecretsSafe} productionReady={ProductionReady} productionConfiguration={CanClaimProductionConfiguration}";
    }
}

/// <summary>
/// Provides configuration production gate helpers.
/// </summary>
public static class SigtranConfigurationProductionGate
{
    /// <summary>Evaluates the current configuration production gate.</summary>
    /// <returns>The configuration production gate result.</returns>
    public static SigtranConfigurationProductionGateResult Evaluate()
    {
        SigtranConfigurationReadinessSnapshot report = SigtranConfigurationReadiness.GetReport();
        return new(
            report.FoundationReady,
            SigtranSecretPolicies.CreateDefault().IsProductionSafe,
            report.ProductionReady);
    }
}
