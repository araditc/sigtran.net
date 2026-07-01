namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes performance and capacity readiness.
/// </summary>
public sealed class SigtranPerformanceReadinessSnapshot
{
    /// <summary>Creates a performance readiness report.</summary>
    /// <param name="hasCapabilityCatalog">Whether the performance capability catalog is available.</param>
    /// <param name="hasBenchmarkScenarios">Whether benchmark scenarios are available.</param>
    /// <param name="hasCapacityProfile">Whether a capacity profile is available.</param>
    /// <param name="hasThroughputTargets">Whether throughput targets are available.</param>
    /// <param name="hasLatencyBudgets">Whether latency budgets are available.</param>
    /// <param name="hasLoadTestPlan">Whether a load-test plan is available.</param>
    /// <param name="hasResourceBudget">Whether a resource budget is available.</param>
    /// <param name="hasBenchmarkEvidence">Whether real benchmark evidence has been captured.</param>
    /// <param name="productionReady">Whether wider production readiness is complete.</param>
    public SigtranPerformanceReadinessSnapshot(
        bool hasCapabilityCatalog,
        bool hasBenchmarkScenarios,
        bool hasCapacityProfile,
        bool hasThroughputTargets,
        bool hasLatencyBudgets,
        bool hasLoadTestPlan,
        bool hasResourceBudget,
        bool hasBenchmarkEvidence,
        bool productionReady)
    {
        HasCapabilityCatalog = hasCapabilityCatalog;
        HasBenchmarkScenarios = hasBenchmarkScenarios;
        HasCapacityProfile = hasCapacityProfile;
        HasThroughputTargets = hasThroughputTargets;
        HasLatencyBudgets = hasLatencyBudgets;
        HasLoadTestPlan = hasLoadTestPlan;
        HasResourceBudget = hasResourceBudget;
        HasBenchmarkEvidence = hasBenchmarkEvidence;
        ProductionReady = productionReady;
    }

    /// <summary>Whether the performance capability catalog is available.</summary>
    public bool HasCapabilityCatalog { get; }

    /// <summary>Whether benchmark scenarios are available.</summary>
    public bool HasBenchmarkScenarios { get; }

    /// <summary>Whether a capacity profile is available.</summary>
    public bool HasCapacityProfile { get; }

    /// <summary>Whether throughput targets are available.</summary>
    public bool HasThroughputTargets { get; }

    /// <summary>Whether latency budgets are available.</summary>
    public bool HasLatencyBudgets { get; }

    /// <summary>Whether a load-test plan is available.</summary>
    public bool HasLoadTestPlan { get; }

    /// <summary>Whether a resource budget is available.</summary>
    public bool HasResourceBudget { get; }

    /// <summary>Whether real benchmark evidence has been captured.</summary>
    public bool HasBenchmarkEvidence { get; }

    /// <summary>Whether wider production readiness is complete.</summary>
    public bool ProductionReady { get; }

    /// <summary>Whether the performance foundation is ready.</summary>
    public bool FoundationReady => HasCapabilityCatalog
        && HasBenchmarkScenarios
        && HasCapacityProfile
        && HasThroughputTargets
        && HasLatencyBudgets
        && HasLoadTestPlan
        && HasResourceBudget;

    /// <summary>Whether production performance claims are ready.</summary>
    public bool ProductionPerformanceReady => FoundationReady && HasBenchmarkEvidence && ProductionReady;
}

/// <summary>
/// Provides performance readiness helpers.
/// </summary>
public static class SigtranPerformanceReadiness
{
    /// <summary>Returns the current performance readiness report.</summary>
    /// <returns>The current performance readiness report.</returns>
    public static SigtranPerformanceReadinessSnapshot GetReport()
    {
        return new(
            hasCapabilityCatalog: SigtranPerformance.GetCapabilities().Count > 0,
            hasBenchmarkScenarios: SigtranBenchmarkScenarios.GetScenarios().Count > 0,
            hasCapacityProfile: SigtranCapacityProfiles.CreateEnterpriseDefault().IsEnterpriseSized,
            hasThroughputTargets: SigtranThroughputTargets.GetTargets().All(target => target.RequiresBenchmarkEvidence),
            hasLatencyBudgets: SigtranLatencyBudgets.GetBudgets().Count > 0,
            hasLoadTestPlan: SigtranLoadTestPlans.CreateProductionDefault().RequiresNativeSctp,
            hasResourceBudget: SigtranResourceBudgets.CreateProductionDefault().IsProductionBenchmarkBudget,
            hasBenchmarkEvidence: false,
            productionReady: SigtranProductionReadiness.GetReport().ProductionReady);
    }
}
