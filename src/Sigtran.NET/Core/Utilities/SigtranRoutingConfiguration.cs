namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes M3UA routing configuration requirements.
/// </summary>
public sealed class SigtranRoutingConfiguration
{
    /// <summary>Creates routing configuration requirements.</summary>
    /// <param name="requiresRoutingContext">Whether Routing Context is required.</param>
    /// <param name="requiresNetworkAppearancePolicy">Whether Network Appearance policy is required.</param>
    /// <param name="requiresRouteTableValidation">Whether route table validation is required.</param>
    /// <param name="requiresAmbiguityRejection">Whether ambiguous routes are rejected.</param>
    public SigtranRoutingConfiguration(
        bool requiresRoutingContext,
        bool requiresNetworkAppearancePolicy,
        bool requiresRouteTableValidation,
        bool requiresAmbiguityRejection)
    {
        RequiresRoutingContext = requiresRoutingContext;
        RequiresNetworkAppearancePolicy = requiresNetworkAppearancePolicy;
        RequiresRouteTableValidation = requiresRouteTableValidation;
        RequiresAmbiguityRejection = requiresAmbiguityRejection;
    }

    /// <summary>Whether Routing Context is required.</summary>
    public bool RequiresRoutingContext { get; }

    /// <summary>Whether Network Appearance policy is required.</summary>
    public bool RequiresNetworkAppearancePolicy { get; }

    /// <summary>Whether route table validation is required.</summary>
    public bool RequiresRouteTableValidation { get; }

    /// <summary>Whether ambiguous routes are rejected.</summary>
    public bool RequiresAmbiguityRejection { get; }

    /// <summary>Whether the routing configuration is ready for enterprise validation.</summary>
    public bool IsEnterpriseReady => RequiresRoutingContext
        && RequiresNetworkAppearancePolicy
        && RequiresRouteTableValidation
        && RequiresAmbiguityRejection;
}

/// <summary>
/// Provides routing configuration helpers.
/// </summary>
public static class SigtranRoutingConfigurations
{
    /// <summary>Creates the default enterprise routing configuration.</summary>
    /// <returns>The default enterprise routing configuration.</returns>
    public static SigtranRoutingConfiguration CreateEnterpriseDefault()
    {
        return new(
            requiresRoutingContext: true,
            requiresNetworkAppearancePolicy: true,
            requiresRouteTableValidation: true,
            requiresAmbiguityRejection: true);
    }
}
