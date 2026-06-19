namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Readiness report for the MTP3 and SCCP phase.
/// </summary>
public readonly struct SccpReadinessReport
{
    /// <summary>Creates a SCCP readiness report.</summary>
    /// <param name="hasMtp3Routing">Whether MTP3 SIO and routing labels are available.</param>
    /// <param name="hasPartyAddressing">Whether SCCP party addressing is available.</param>
    /// <param name="hasConnectionlessCodecs">Whether UDT, XUDT, and LUDT codecs are available.</param>
    /// <param name="hasSegmentation">Whether segmentation parameters are available.</param>
    /// <param name="hasServiceMessages">Whether UDTS return-cause messages are available.</param>
    /// <param name="hasRoutingApis">Whether route-on-SSN and route-on-GT APIs are available.</param>
    /// <param name="hasInteropVectors">Whether external interoperability vectors are present.</param>
    public SccpReadinessReport(
        bool hasMtp3Routing,
        bool hasPartyAddressing,
        bool hasConnectionlessCodecs,
        bool hasSegmentation,
        bool hasServiceMessages,
        bool hasRoutingApis,
        bool hasInteropVectors)
    {
        HasMtp3Routing = hasMtp3Routing;
        HasPartyAddressing = hasPartyAddressing;
        HasConnectionlessCodecs = hasConnectionlessCodecs;
        HasSegmentation = hasSegmentation;
        HasServiceMessages = hasServiceMessages;
        HasRoutingApis = hasRoutingApis;
        HasInteropVectors = hasInteropVectors;
    }

    /// <summary>Whether MTP3 SIO and routing labels are available.</summary>
    public bool HasMtp3Routing { get; }

    /// <summary>Whether SCCP party addressing is available.</summary>
    public bool HasPartyAddressing { get; }

    /// <summary>Whether UDT, XUDT, and LUDT codecs are available.</summary>
    public bool HasConnectionlessCodecs { get; }

    /// <summary>Whether segmentation parameters are available.</summary>
    public bool HasSegmentation { get; }

    /// <summary>Whether UDTS return-cause messages are available.</summary>
    public bool HasServiceMessages { get; }

    /// <summary>Whether route-on-SSN and route-on-GT APIs are available.</summary>
    public bool HasRoutingApis { get; }

    /// <summary>Whether external interoperability vectors are present.</summary>
    public bool HasInteropVectors { get; }

    /// <summary>The number of completed SDK foundation capabilities.</summary>
    public int FoundationCapabilityCount =>
        Count(HasMtp3Routing)
        + Count(HasPartyAddressing)
        + Count(HasConnectionlessCodecs)
        + Count(HasSegmentation)
        + Count(HasServiceMessages)
        + Count(HasRoutingApis);

    /// <summary>Whether the SDK foundation for MTP3 and SCCP is complete.</summary>
    public bool FoundationReady => FoundationCapabilityCount == SccpReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether SCCP is ready for production interoperability claims.</summary>
    public bool IsProductionReady => FoundationReady && HasInteropVectors;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"sccpFoundationReady={FoundationReady} sccpProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{SccpReadiness.RequiredFoundationCapabilityCount} interopVectors={HasInteropVectors}";
    }

    private static int Count(bool value) => value ? 1 : 0;
}

/// <summary>
/// Provides readiness information for MTP3 and SCCP work.
/// </summary>
public static class SccpReadiness
{
    /// <summary>The release label for MTP3 and SCCP readiness.</summary>
    public const string ReleaseLabel = "MTP3 and SCCP foundation";

    /// <summary>The number of required SDK foundation capabilities.</summary>
    public const int RequiredFoundationCapabilityCount = 6;

    /// <summary>Explains the remaining production gate.</summary>
    public const string ProductionGateDescription = "External SCCP interoperability vectors and network trace validation are required before production claims.";

    /// <summary>Builds the current SCCP readiness report.</summary>
    /// <returns>The current SCCP readiness report.</returns>
    public static SccpReadinessReport GetReport()
    {
        return new(
            hasMtp3Routing: true,
            hasPartyAddressing: true,
            hasConnectionlessCodecs: true,
            hasSegmentation: true,
            hasServiceMessages: true,
            hasRoutingApis: true,
            hasInteropVectors: false);
    }
}
