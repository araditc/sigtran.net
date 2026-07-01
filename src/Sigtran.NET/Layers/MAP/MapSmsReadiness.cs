namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Readiness report for the MAP SMS profile foundation phase.
/// </summary>
public readonly struct MapSmsReadinessSnapshot
{
    /// <summary>Creates a MAP SMS readiness report.</summary>
    /// <param name="hasOperationMetadata">Whether operation metadata is available.</param>
    /// <param name="hasAddressPrimitives">Whether address primitives are available.</param>
    /// <param name="hasForwardSmCodecs">Whether MO/MT ForwardSM codecs are available.</param>
    /// <param name="hasRoutingInfoCodec">Whether SRI-SM codec is available.</param>
    /// <param name="hasDeliveryStatusCodec">Whether delivery-status codec is available.</param>
    /// <param name="hasAlertServiceCentreCodec">Whether AlertServiceCentre codec is available.</param>
    /// <param name="hasErrorsAndExtensions">Whether error mapping and extension containers are available.</param>
    /// <param name="hasTcapClientFacade">Whether the TCAP client facade is available.</param>
    /// <param name="hasInteropVectors">Whether external MAP SMS interoperability vectors are present.</param>
    public MapSmsReadinessSnapshot(
        bool hasOperationMetadata,
        bool hasAddressPrimitives,
        bool hasForwardSmCodecs,
        bool hasRoutingInfoCodec,
        bool hasDeliveryStatusCodec,
        bool hasAlertServiceCentreCodec,
        bool hasErrorsAndExtensions,
        bool hasTcapClientFacade,
        bool hasInteropVectors)
    {
        HasOperationMetadata = hasOperationMetadata;
        HasAddressPrimitives = hasAddressPrimitives;
        HasForwardSmCodecs = hasForwardSmCodecs;
        HasRoutingInfoCodec = hasRoutingInfoCodec;
        HasDeliveryStatusCodec = hasDeliveryStatusCodec;
        HasAlertServiceCentreCodec = hasAlertServiceCentreCodec;
        HasErrorsAndExtensions = hasErrorsAndExtensions;
        HasTcapClientFacade = hasTcapClientFacade;
        HasInteropVectors = hasInteropVectors;
    }

    /// <summary>Whether operation metadata is available.</summary>
    public bool HasOperationMetadata { get; }

    /// <summary>Whether address primitives are available.</summary>
    public bool HasAddressPrimitives { get; }

    /// <summary>Whether MO/MT ForwardSM codecs are available.</summary>
    public bool HasForwardSmCodecs { get; }

    /// <summary>Whether SRI-SM codec is available.</summary>
    public bool HasRoutingInfoCodec { get; }

    /// <summary>Whether delivery-status codec is available.</summary>
    public bool HasDeliveryStatusCodec { get; }

    /// <summary>Whether AlertServiceCentre codec is available.</summary>
    public bool HasAlertServiceCentreCodec { get; }

    /// <summary>Whether error mapping and extension containers are available.</summary>
    public bool HasErrorsAndExtensions { get; }

    /// <summary>Whether the TCAP client facade is available.</summary>
    public bool HasTcapClientFacade { get; }

    /// <summary>Whether external MAP SMS interoperability vectors are present.</summary>
    public bool HasInteropVectors { get; }

    /// <summary>The completed foundation capability count.</summary>
    public int FoundationCapabilityCount =>
        Count(HasOperationMetadata)
        + Count(HasAddressPrimitives)
        + Count(HasForwardSmCodecs)
        + Count(HasRoutingInfoCodec)
        + Count(HasDeliveryStatusCodec)
        + Count(HasAlertServiceCentreCodec)
        + Count(HasErrorsAndExtensions)
        + Count(HasTcapClientFacade);

    /// <summary>Whether the MAP SMS foundation is ready.</summary>
    public bool FoundationReady => FoundationCapabilityCount == MapSmsReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether MAP SMS can claim production interoperability readiness.</summary>
    public bool IsProductionReady => FoundationReady && HasInteropVectors;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"mapSmsFoundationReady={FoundationReady} mapSmsProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{MapSmsReadiness.RequiredFoundationCapabilityCount} interopVectors={HasInteropVectors}";
    }

    private static int Count(bool value) => value ? 1 : 0;
}

/// <summary>
/// Provides readiness information for MAP SMS work.
/// </summary>
public static class MapSmsReadiness
{
    /// <summary>The release label for MAP SMS readiness.</summary>
    public const string ReleaseLabel = "MAP SMS profile foundation";

    /// <summary>The number of required foundation capabilities.</summary>
    public const int RequiredFoundationCapabilityCount = 8;

    /// <summary>Explains the remaining production gate.</summary>
    public const string ProductionGateDescription = "External MAP SMS interoperability vectors and operator-profile validation are required before production claims.";

    /// <summary>Builds the current MAP SMS readiness report.</summary>
    /// <returns>The current MAP SMS readiness report.</returns>
    public static MapSmsReadinessSnapshot GetReport()
    {
        return new(
            hasOperationMetadata: true,
            hasAddressPrimitives: true,
            hasForwardSmCodecs: true,
            hasRoutingInfoCodec: true,
            hasDeliveryStatusCodec: true,
            hasAlertServiceCentreCodec: true,
            hasErrorsAndExtensions: true,
            hasTcapClientFacade: true,
            hasInteropVectors: false);
    }
}
