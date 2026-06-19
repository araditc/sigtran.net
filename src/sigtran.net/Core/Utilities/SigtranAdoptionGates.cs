namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes SDK adoption gates for enterprise users.
/// </summary>
public sealed class SigtranAdoptionGateReport
{
    /// <summary>Creates an adoption gate report.</summary>
    /// <param name="hasQuickstart">Whether quickstart material is available.</param>
    /// <param name="hasSamples">Whether samples are available.</param>
    /// <param name="hasConfigurationProfiles">Whether configuration profiles are available.</param>
    /// <param name="hasTroubleshooting">Whether troubleshooting guidance is available.</param>
    /// <param name="hasApiReferenceIndex">Whether API reference index is available.</param>
    /// <param name="commercialReady">Whether the SDK is commercially ready.</param>
    public SigtranAdoptionGateReport(
        bool hasQuickstart,
        bool hasSamples,
        bool hasConfigurationProfiles,
        bool hasTroubleshooting,
        bool hasApiReferenceIndex,
        bool commercialReady)
    {
        HasQuickstart = hasQuickstart;
        HasSamples = hasSamples;
        HasConfigurationProfiles = hasConfigurationProfiles;
        HasTroubleshooting = hasTroubleshooting;
        HasApiReferenceIndex = hasApiReferenceIndex;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether quickstart material is available.</summary>
    public bool HasQuickstart { get; }

    /// <summary>Whether samples are available.</summary>
    public bool HasSamples { get; }

    /// <summary>Whether configuration profiles are available.</summary>
    public bool HasConfigurationProfiles { get; }

    /// <summary>Whether troubleshooting guidance is available.</summary>
    public bool HasTroubleshooting { get; }

    /// <summary>Whether API reference index is available.</summary>
    public bool HasApiReferenceIndex { get; }

    /// <summary>Whether the SDK is commercially ready.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether developer adoption foundations are ready.</summary>
    public bool DeveloperAdoptionReady => HasQuickstart && HasSamples && HasConfigurationProfiles && HasTroubleshooting && HasApiReferenceIndex;

    /// <summary>Whether enterprise production adoption is ready.</summary>
    public bool EnterpriseProductionReady => DeveloperAdoptionReady && CommercialReady;
}

/// <summary>
/// Provides adoption gate helpers.
/// </summary>
public static class SigtranAdoptionGates
{
    /// <summary>Returns the current adoption gate report.</summary>
    /// <returns>The current adoption gate report.</returns>
    public static SigtranAdoptionGateReport GetReport()
    {
        return new(
            hasQuickstart: SigtranQuickstarts.CreateM3uaAspToSg().Steps.Count > 0,
            hasSamples: SigtranSampleTemplates.GetTemplates().Count > 0,
            hasConfigurationProfiles: SigtranConfigurationProfiles.GetProfiles().Count > 0,
            hasTroubleshooting: SigtranTroubleshooting.GetEntries().Count > 0,
            hasApiReferenceIndex: SigtranApiReferenceIndex.GetEntries().Count > 0,
            commercialReady: SigtranCommercialReadiness.GetReport().CommercialReady);
    }
}
