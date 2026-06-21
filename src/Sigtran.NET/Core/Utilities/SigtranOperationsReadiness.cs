namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes production operations readiness.
/// </summary>
public sealed class SigtranOperationsReadinessReport
{
    /// <summary>Creates an operations readiness report.</summary>
    /// <param name="hasRunbooks">Whether runbooks are available.</param>
    /// <param name="hasIncidentTargets">Whether incident targets are available.</param>
    /// <param name="hasHealthChecks">Whether health checks are available.</param>
    /// <param name="hasRollbackPlan">Whether rollback plan is available.</param>
    /// <param name="hasMaintenancePolicy">Whether maintenance policy is available.</param>
    /// <param name="hasSupportHandbook">Whether support handbook is available.</param>
    /// <param name="commercialReady">Whether commercial readiness is complete.</param>
    public SigtranOperationsReadinessReport(
        bool hasRunbooks,
        bool hasIncidentTargets,
        bool hasHealthChecks,
        bool hasRollbackPlan,
        bool hasMaintenancePolicy,
        bool hasSupportHandbook,
        bool commercialReady)
    {
        HasRunbooks = hasRunbooks;
        HasIncidentTargets = hasIncidentTargets;
        HasHealthChecks = hasHealthChecks;
        HasRollbackPlan = hasRollbackPlan;
        HasMaintenancePolicy = hasMaintenancePolicy;
        HasSupportHandbook = hasSupportHandbook;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether runbooks are available.</summary>
    public bool HasRunbooks { get; }

    /// <summary>Whether incident targets are available.</summary>
    public bool HasIncidentTargets { get; }

    /// <summary>Whether health checks are available.</summary>
    public bool HasHealthChecks { get; }

    /// <summary>Whether rollback plan is available.</summary>
    public bool HasRollbackPlan { get; }

    /// <summary>Whether maintenance policy is available.</summary>
    public bool HasMaintenancePolicy { get; }

    /// <summary>Whether support handbook is available.</summary>
    public bool HasSupportHandbook { get; }

    /// <summary>Whether commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether operations foundation is ready.</summary>
    public bool FoundationReady => HasRunbooks && HasIncidentTargets && HasHealthChecks && HasRollbackPlan && HasMaintenancePolicy && HasSupportHandbook;

    /// <summary>Whether production operations are ready.</summary>
    public bool ProductionOperationsReady => FoundationReady && CommercialReady;
}

/// <summary>
/// Provides operations readiness helpers.
/// </summary>
public static class SigtranOperationsReadiness
{
    /// <summary>Returns the current operations readiness report.</summary>
    /// <returns>The current operations readiness report.</returns>
    public static SigtranOperationsReadinessReport GetReport()
    {
        return new(
            hasRunbooks: SigtranRunbooks.GetRunbooks().Count > 0,
            hasIncidentTargets: SigtranIncidentResponse.GetTargets().Count > 0,
            hasHealthChecks: SigtranHealthChecks.GetDefinitions().Count > 0,
            hasRollbackPlan: SigtranRollbackPlans.CreateDefaultPackageRollback().Steps.Count > 0,
            hasMaintenancePolicy: SigtranMaintenancePolicies.CreateDefault().RequiresRollbackPlan,
            hasSupportHandbook: SigtranSupportHandbook.GetRules().Count > 0,
            commercialReady: SigtranCommercialReadiness.GetReport().CommercialReady);
    }
}
