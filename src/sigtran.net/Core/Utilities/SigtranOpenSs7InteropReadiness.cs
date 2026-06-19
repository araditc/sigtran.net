namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes OpenSS7/IPSS7 interoperability execution readiness.
/// </summary>
public sealed class SigtranOpenSs7InteropReadinessReport
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability readiness report.</summary>
    /// <param name="hasEnvironment">Whether environment requirements are available.</param>
    /// <param name="hasConfiguration">Whether configuration is available.</param>
    /// <param name="hasTraceExpectations">Whether trace expectations are available.</param>
    /// <param name="hasArtifacts">Whether artifact manifest support is available.</param>
    /// <param name="hasRunPlan">Whether run plan is available.</param>
    /// <param name="hasCommandSet">Whether command set is available.</param>
    /// <param name="hasRunReport">Whether run report support is available.</param>
    /// <param name="hasEvidenceRegistry">Whether evidence registry is available.</param>
    /// <param name="hasPassingEvidence">Whether passing evidence exists.</param>
    public SigtranOpenSs7InteropReadinessReport(
        bool hasEnvironment,
        bool hasConfiguration,
        bool hasTraceExpectations,
        bool hasArtifacts,
        bool hasRunPlan,
        bool hasCommandSet,
        bool hasRunReport,
        bool hasEvidenceRegistry,
        bool hasPassingEvidence)
    {
        HasEnvironment = hasEnvironment;
        HasConfiguration = hasConfiguration;
        HasTraceExpectations = hasTraceExpectations;
        HasArtifacts = hasArtifacts;
        HasRunPlan = hasRunPlan;
        HasCommandSet = hasCommandSet;
        HasRunReport = hasRunReport;
        HasEvidenceRegistry = hasEvidenceRegistry;
        HasPassingEvidence = hasPassingEvidence;
    }

    /// <summary>Whether environment requirements are available.</summary>
    public bool HasEnvironment { get; }

    /// <summary>Whether configuration is available.</summary>
    public bool HasConfiguration { get; }

    /// <summary>Whether trace expectations are available.</summary>
    public bool HasTraceExpectations { get; }

    /// <summary>Whether artifact manifest support is available.</summary>
    public bool HasArtifacts { get; }

    /// <summary>Whether run plan is available.</summary>
    public bool HasRunPlan { get; }

    /// <summary>Whether command set is available.</summary>
    public bool HasCommandSet { get; }

    /// <summary>Whether run report support is available.</summary>
    public bool HasRunReport { get; }

    /// <summary>Whether evidence registry is available.</summary>
    public bool HasEvidenceRegistry { get; }

    /// <summary>Whether passing evidence exists.</summary>
    public bool HasPassingEvidence { get; }

    /// <summary>Whether the OpenSS7/IPSS7 execution foundation is ready.</summary>
    public bool FoundationReady => HasEnvironment && HasConfiguration && HasTraceExpectations && HasArtifacts && HasRunPlan && HasCommandSet && HasRunReport && HasEvidenceRegistry;

    /// <summary>Whether OpenSS7/IPSS7 interop can be claimed as verified.</summary>
    public bool Verified => FoundationReady && HasPassingEvidence;
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability readiness helpers.
/// </summary>
public static class SigtranOpenSs7InteropReadiness
{
    /// <summary>Returns the current OpenSS7/IPSS7 interoperability readiness report.</summary>
    /// <returns>The current OpenSS7/IPSS7 interoperability readiness report.</returns>
    public static SigtranOpenSs7InteropReadinessReport GetReport()
    {
        return new(
            hasEnvironment: SigtranOpenSs7InteropEnvironments.CreateDefault().HasMinimumLabPrerequisites,
            hasConfiguration: SigtranOpenSs7InteropConfigurations.CreateDefaultAspToSg().IsAspToSgReady,
            hasTraceExpectations: SigtranOpenSs7InteropTraceExpectationsCatalog.CreateAspToSg().CoversAspLifecycle,
            hasArtifacts: true,
            hasRunPlan: SigtranOpenSs7InteropRunPlans.CreateDefaultAspToSg().IsExecutable,
            hasCommandSet: SigtranOpenSs7InteropCommands.CreateDefault().Commands.Count > 0,
            hasRunReport: true,
            hasEvidenceRegistry: true,
            hasPassingEvidence: SigtranOpenSs7InteropEvidence.CreateCurrentRegistry().HasPassingAspToSgEvidence);
    }
}
