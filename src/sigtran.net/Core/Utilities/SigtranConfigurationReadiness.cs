namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes configuration and environment readiness.
/// </summary>
public sealed class SigtranConfigurationReadinessReport
{
    /// <summary>Creates a configuration readiness report.</summary>
    /// <param name="hasSchema">Whether configuration schema is available.</param>
    /// <param name="hasValidation">Whether validation helpers are available.</param>
    /// <param name="hasEnvironmentMatrix">Whether environment matrix is available.</param>
    /// <param name="hasSecretPolicy">Whether secret policy is available.</param>
    /// <param name="hasTransportConfiguration">Whether transport configuration is available.</param>
    /// <param name="hasRoutingConfiguration">Whether routing configuration is available.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    public SigtranConfigurationReadinessReport(
        bool hasSchema,
        bool hasValidation,
        bool hasEnvironmentMatrix,
        bool hasSecretPolicy,
        bool hasTransportConfiguration,
        bool hasRoutingConfiguration,
        bool commercialReady)
    {
        HasSchema = hasSchema;
        HasValidation = hasValidation;
        HasEnvironmentMatrix = hasEnvironmentMatrix;
        HasSecretPolicy = hasSecretPolicy;
        HasTransportConfiguration = hasTransportConfiguration;
        HasRoutingConfiguration = hasRoutingConfiguration;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether configuration schema is available.</summary>
    public bool HasSchema { get; }

    /// <summary>Whether validation helpers are available.</summary>
    public bool HasValidation { get; }

    /// <summary>Whether environment matrix is available.</summary>
    public bool HasEnvironmentMatrix { get; }

    /// <summary>Whether secret policy is available.</summary>
    public bool HasSecretPolicy { get; }

    /// <summary>Whether transport configuration is available.</summary>
    public bool HasTransportConfiguration { get; }

    /// <summary>Whether routing configuration is available.</summary>
    public bool HasRoutingConfiguration { get; }

    /// <summary>Whether wider commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether the configuration foundation is ready.</summary>
    public bool FoundationReady => HasSchema
        && HasValidation
        && HasEnvironmentMatrix
        && HasSecretPolicy
        && HasTransportConfiguration
        && HasRoutingConfiguration;

    /// <summary>Whether production configuration claims are ready.</summary>
    public bool ProductionConfigurationReady => FoundationReady && CommercialReady;
}

/// <summary>
/// Provides configuration readiness helpers.
/// </summary>
public static class SigtranConfigurationReadiness
{
    /// <summary>Returns the current configuration readiness report.</summary>
    /// <returns>The current configuration readiness report.</returns>
    public static SigtranConfigurationReadinessReport GetReport()
    {
        string[] requiredKeys = SigtranConfigurationSchema.GetFields()
            .Where(field => field.Required)
            .Select(field => field.Key)
            .ToArray();

        return new(
            hasSchema: SigtranConfigurationSchema.GetFields().Count > 0,
            hasValidation: SigtranConfigurationValidation.ValidateRequiredKeys("production", requiredKeys).IsValid,
            hasEnvironmentMatrix: SigtranEnvironmentMatrix.GetEntries().Any(entry => entry.Environment == SigtranRuntimeEnvironment.Production && entry.RequiresExternalSecretProvider),
            hasSecretPolicy: SigtranSecretPolicies.CreateDefault().IsProductionSafe,
            hasTransportConfiguration: SigtranTransportConfigurations.CreateNativeSctpDefault().IsSigtranReady,
            hasRoutingConfiguration: SigtranRoutingConfigurations.CreateEnterpriseDefault().IsEnterpriseReady,
            commercialReady: SigtranCommercialReadiness.GetReport().CommercialReady);
    }
}
