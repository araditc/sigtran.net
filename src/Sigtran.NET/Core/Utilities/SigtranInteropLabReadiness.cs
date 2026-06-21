namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the interoperability lab readiness state.
/// </summary>
public sealed class SigtranInteropLabReadinessReport
{
    /// <summary>Creates an interoperability lab readiness report.</summary>
    /// <param name="hasScenarioCatalog">Whether scenario catalog is available.</param>
    /// <param name="hasArtifactManifests">Whether artifact manifests are available.</param>
    /// <param name="hasRunReports">Whether run reports are available.</param>
    /// <param name="hasPeerProfiles">Whether peer profiles are available.</param>
    /// <param name="hasTraceComparison">Whether trace comparison is available.</param>
    /// <param name="hasEvidencePromotion">Whether evidence promotion is available.</param>
    /// <param name="hasCiProfile">Whether CI profile is available.</param>
    /// <param name="hasPassingExternalEvidence">Whether passing external evidence exists.</param>
    public SigtranInteropLabReadinessReport(
        bool hasScenarioCatalog,
        bool hasArtifactManifests,
        bool hasRunReports,
        bool hasPeerProfiles,
        bool hasTraceComparison,
        bool hasEvidencePromotion,
        bool hasCiProfile,
        bool hasPassingExternalEvidence)
    {
        HasScenarioCatalog = hasScenarioCatalog;
        HasArtifactManifests = hasArtifactManifests;
        HasRunReports = hasRunReports;
        HasPeerProfiles = hasPeerProfiles;
        HasTraceComparison = hasTraceComparison;
        HasEvidencePromotion = hasEvidencePromotion;
        HasCiProfile = hasCiProfile;
        HasPassingExternalEvidence = hasPassingExternalEvidence;
    }

    /// <summary>Whether scenario catalog is available.</summary>
    public bool HasScenarioCatalog { get; }

    /// <summary>Whether artifact manifests are available.</summary>
    public bool HasArtifactManifests { get; }

    /// <summary>Whether run reports are available.</summary>
    public bool HasRunReports { get; }

    /// <summary>Whether peer profiles are available.</summary>
    public bool HasPeerProfiles { get; }

    /// <summary>Whether trace comparison is available.</summary>
    public bool HasTraceComparison { get; }

    /// <summary>Whether evidence promotion is available.</summary>
    public bool HasEvidencePromotion { get; }

    /// <summary>Whether CI profile is available.</summary>
    public bool HasCiProfile { get; }

    /// <summary>Whether passing external evidence exists.</summary>
    public bool HasPassingExternalEvidence { get; }

    /// <summary>Whether the lab foundation is ready.</summary>
    public bool FoundationReady => HasScenarioCatalog
        && HasArtifactManifests
        && HasRunReports
        && HasPeerProfiles
        && HasTraceComparison
        && HasEvidencePromotion
        && HasCiProfile;

    /// <summary>Whether the lab can unlock production readiness.</summary>
    public bool ProductionReady => FoundationReady && HasPassingExternalEvidence;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"foundationReady={FoundationReady} productionReady={ProductionReady} externalEvidence={HasPassingExternalEvidence}";
    }
}

/// <summary>
/// Provides the current interoperability lab readiness report.
/// </summary>
public static class SigtranInteropLabReadiness
{
    /// <summary>Returns the current interoperability lab readiness report.</summary>
    /// <returns>The current interoperability lab readiness report.</returns>
    public static SigtranInteropLabReadinessReport GetReport()
    {
        return new(
            hasScenarioCatalog: SigtranInteropLabScenarios.GetScenarios().Count > 0,
            hasArtifactManifests: true,
            hasRunReports: true,
            hasPeerProfiles: SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate().ExpectedMessages.Count > 0,
            hasTraceComparison: true,
            hasEvidencePromotion: true,
            hasCiProfile: SigtranInteropLabCiProfiles.CreateDefault().Commands.Count > 0,
            hasPassingExternalEvidence: SigtranInteropEvidence.CreateCurrentRegistry().HasPassingEvidence());
    }
}
