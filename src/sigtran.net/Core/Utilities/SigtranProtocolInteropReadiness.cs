namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes SCCP, TCAP, and MAP SMS interoperability vector readiness.
/// </summary>
public sealed class SigtranProtocolInteropReadinessReport
{
    /// <summary>Creates a protocol interop readiness report.</summary>
    /// <param name="hasVectorCatalog">Whether vector catalog is available.</param>
    /// <param name="hasReferences">Whether external references are available.</param>
    /// <param name="hasArtifactManifest">Whether artifact manifest support is available.</param>
    /// <param name="hasComparisonRules">Whether comparison rules are available.</param>
    /// <param name="hasRunPlan">Whether run plan is available.</param>
    /// <param name="hasCommandSet">Whether command set is available.</param>
    /// <param name="hasRunReports">Whether run report support is available.</param>
    /// <param name="hasEvidenceRegistry">Whether evidence registry is available.</param>
    /// <param name="hasCompletePassingEvidence">Whether complete passing evidence exists.</param>
    public SigtranProtocolInteropReadinessReport(
        bool hasVectorCatalog,
        bool hasReferences,
        bool hasArtifactManifest,
        bool hasComparisonRules,
        bool hasRunPlan,
        bool hasCommandSet,
        bool hasRunReports,
        bool hasEvidenceRegistry,
        bool hasCompletePassingEvidence)
    {
        HasVectorCatalog = hasVectorCatalog;
        HasReferences = hasReferences;
        HasArtifactManifest = hasArtifactManifest;
        HasComparisonRules = hasComparisonRules;
        HasRunPlan = hasRunPlan;
        HasCommandSet = hasCommandSet;
        HasRunReports = hasRunReports;
        HasEvidenceRegistry = hasEvidenceRegistry;
        HasCompletePassingEvidence = hasCompletePassingEvidence;
    }

    /// <summary>Whether vector catalog is available.</summary>
    public bool HasVectorCatalog { get; }

    /// <summary>Whether external references are available.</summary>
    public bool HasReferences { get; }

    /// <summary>Whether artifact manifest support is available.</summary>
    public bool HasArtifactManifest { get; }

    /// <summary>Whether comparison rules are available.</summary>
    public bool HasComparisonRules { get; }

    /// <summary>Whether run plan is available.</summary>
    public bool HasRunPlan { get; }

    /// <summary>Whether command set is available.</summary>
    public bool HasCommandSet { get; }

    /// <summary>Whether run report support is available.</summary>
    public bool HasRunReports { get; }

    /// <summary>Whether evidence registry is available.</summary>
    public bool HasEvidenceRegistry { get; }

    /// <summary>Whether complete passing evidence exists.</summary>
    public bool HasCompletePassingEvidence { get; }

    /// <summary>Whether the protocol interop vector foundation is ready.</summary>
    public bool FoundationReady => HasVectorCatalog && HasReferences && HasArtifactManifest && HasComparisonRules && HasRunPlan && HasCommandSet && HasRunReports && HasEvidenceRegistry;

    /// <summary>Whether SCCP, TCAP, and MAP SMS interop vectors are verified.</summary>
    public bool Verified => FoundationReady && HasCompletePassingEvidence;
}

/// <summary>
/// Provides protocol interoperability readiness helpers.
/// </summary>
public static class SigtranProtocolInteropReadiness
{
    /// <summary>Returns the current protocol interoperability readiness report.</summary>
    /// <returns>The current protocol interoperability readiness report.</returns>
    public static SigtranProtocolInteropReadinessReport GetReport()
    {
        return new(
            hasVectorCatalog: SigtranProtocolInteropVectorCatalog.GetVectors().Count > 0,
            hasReferences: SigtranProtocolInteropReferences.GetReferences().Count > 0,
            hasArtifactManifest: true,
            hasComparisonRules: SigtranProtocolInteropComparisonRules.CreateDefault().IsCommercialValidationReady,
            hasRunPlan: SigtranProtocolInteropRunPlans.CreateDefault().IsExecutable,
            hasCommandSet: SigtranProtocolInteropCommands.CreateDefault().Commands.Count > 0,
            hasRunReports: true,
            hasEvidenceRegistry: true,
            hasCompletePassingEvidence: SigtranProtocolInteropEvidence.CreateCurrentRegistry().HasCompletePassingEvidence);
    }
}
