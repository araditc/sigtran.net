namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes native SCTP lab verification readiness.
/// </summary>
public sealed class SigtranNativeSctpLabReadinessReport
{
    /// <summary>Creates a native SCTP lab readiness report.</summary>
    /// <param name="hasScenarioCatalog">Whether scenario catalog is available.</param>
    /// <param name="hasArtifactManifest">Whether artifact manifest support is available.</param>
    /// <param name="hasRunPlan">Whether run plan is available.</param>
    /// <param name="hasCommandSet">Whether command set is available.</param>
    /// <param name="hasRunReports">Whether run reports are available.</param>
    /// <param name="hasEvidenceRegistry">Whether evidence registry is available.</param>
    /// <param name="hasCompletePassingEvidence">Whether complete passing evidence exists.</param>
    public SigtranNativeSctpLabReadinessReport(
        bool hasScenarioCatalog,
        bool hasArtifactManifest,
        bool hasRunPlan,
        bool hasCommandSet,
        bool hasRunReports,
        bool hasEvidenceRegistry,
        bool hasCompletePassingEvidence)
    {
        HasScenarioCatalog = hasScenarioCatalog;
        HasArtifactManifest = hasArtifactManifest;
        HasRunPlan = hasRunPlan;
        HasCommandSet = hasCommandSet;
        HasRunReports = hasRunReports;
        HasEvidenceRegistry = hasEvidenceRegistry;
        HasCompletePassingEvidence = hasCompletePassingEvidence;
    }

    /// <summary>Whether scenario catalog is available.</summary>
    public bool HasScenarioCatalog { get; }

    /// <summary>Whether artifact manifest support is available.</summary>
    public bool HasArtifactManifest { get; }

    /// <summary>Whether run plan is available.</summary>
    public bool HasRunPlan { get; }

    /// <summary>Whether command set is available.</summary>
    public bool HasCommandSet { get; }

    /// <summary>Whether run reports are available.</summary>
    public bool HasRunReports { get; }

    /// <summary>Whether evidence registry is available.</summary>
    public bool HasEvidenceRegistry { get; }

    /// <summary>Whether complete passing evidence exists.</summary>
    public bool HasCompletePassingEvidence { get; }

    /// <summary>Whether the native SCTP lab foundation is ready.</summary>
    public bool FoundationReady => HasScenarioCatalog && HasArtifactManifest && HasRunPlan && HasCommandSet && HasRunReports && HasEvidenceRegistry;

    /// <summary>Whether native SCTP lab verification is production-ready.</summary>
    public bool ProductionReady => FoundationReady && HasCompletePassingEvidence;
}

/// <summary>
/// Provides native SCTP lab readiness helpers.
/// </summary>
public static class SigtranNativeSctpLabReadiness
{
    /// <summary>Returns the current native SCTP lab readiness report.</summary>
    /// <returns>The current native SCTP lab readiness report.</returns>
    public static SigtranNativeSctpLabReadinessReport GetReport()
    {
        return new(
            hasScenarioCatalog: SigtranNativeSctpLabScenarios.GetScenarios().Count > 0,
            hasArtifactManifest: true,
            hasRunPlan: SigtranNativeSctpLabRunPlans.CreateDefault().Scenarios.Count > 0,
            hasCommandSet: SigtranNativeSctpLabCommands.CreateDefault().Commands.Count > 0,
            hasRunReports: true,
            hasEvidenceRegistry: true,
            hasCompletePassingEvidence: SigtranNativeSctpLabEvidence.CreateCurrentRegistry().HasCompletePassingEvidence());
    }
}
