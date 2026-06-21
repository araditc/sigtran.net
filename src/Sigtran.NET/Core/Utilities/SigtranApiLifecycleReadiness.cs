namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes API lifecycle readiness.
/// </summary>
public sealed class SigtranApiLifecycleReadinessReport
{
    /// <summary>Creates an API lifecycle readiness report.</summary>
    /// <param name="hasSurfaceCatalog">Whether an API surface catalog is available.</param>
    /// <param name="hasStabilityContracts">Whether stability contracts are available.</param>
    /// <param name="hasVersionMatrix">Whether an API version matrix is available.</param>
    /// <param name="hasDeprecationPolicy">Whether a deprecation policy is available.</param>
    /// <param name="hasMigrationGuides">Whether migration guide entries are available.</param>
    /// <param name="hasBreakingChangeReview">Whether breaking-change review policy is available.</param>
    /// <param name="hasPublicApiBaseline">Whether a public API baseline is available.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    public SigtranApiLifecycleReadinessReport(
        bool hasSurfaceCatalog,
        bool hasStabilityContracts,
        bool hasVersionMatrix,
        bool hasDeprecationPolicy,
        bool hasMigrationGuides,
        bool hasBreakingChangeReview,
        bool hasPublicApiBaseline,
        bool commercialReady)
    {
        HasSurfaceCatalog = hasSurfaceCatalog;
        HasStabilityContracts = hasStabilityContracts;
        HasVersionMatrix = hasVersionMatrix;
        HasDeprecationPolicy = hasDeprecationPolicy;
        HasMigrationGuides = hasMigrationGuides;
        HasBreakingChangeReview = hasBreakingChangeReview;
        HasPublicApiBaseline = hasPublicApiBaseline;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether an API surface catalog is available.</summary>
    public bool HasSurfaceCatalog { get; }

    /// <summary>Whether stability contracts are available.</summary>
    public bool HasStabilityContracts { get; }

    /// <summary>Whether an API version matrix is available.</summary>
    public bool HasVersionMatrix { get; }

    /// <summary>Whether a deprecation policy is available.</summary>
    public bool HasDeprecationPolicy { get; }

    /// <summary>Whether migration guide entries are available.</summary>
    public bool HasMigrationGuides { get; }

    /// <summary>Whether breaking-change review policy is available.</summary>
    public bool HasBreakingChangeReview { get; }

    /// <summary>Whether a public API baseline is available.</summary>
    public bool HasPublicApiBaseline { get; }

    /// <summary>Whether wider commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether the API lifecycle foundation is ready.</summary>
    public bool FoundationReady => HasSurfaceCatalog
        && HasStabilityContracts
        && HasVersionMatrix
        && HasDeprecationPolicy
        && HasMigrationGuides
        && HasBreakingChangeReview
        && HasPublicApiBaseline;

    /// <summary>Whether stable API lifecycle claims are ready.</summary>
    public bool StableApiLifecycleReady => FoundationReady && CommercialReady;
}

/// <summary>
/// Provides API lifecycle readiness helpers.
/// </summary>
public static class SigtranApiLifecycleReadiness
{
    /// <summary>Returns the current API lifecycle readiness report.</summary>
    /// <returns>The current API lifecycle readiness report.</returns>
    public static SigtranApiLifecycleReadinessReport GetReport()
    {
        return new(
            hasSurfaceCatalog: SigtranApiSurfaceCatalog.GetSurfaces().Count > 0,
            hasStabilityContracts: SigtranApiStability.GetContracts().Count > 0,
            hasVersionMatrix: SigtranApiVersionMatrix.GetEntries().Count > 0,
            hasDeprecationPolicy: SigtranDeprecationPolicies.CreateStableDefault().IsStableLifecyclePolicy,
            hasMigrationGuides: SigtranMigrationGuides.GetEntries().All(entry => entry.RequiresCodeSamples),
            hasBreakingChangeReview: SigtranBreakingChangeReview.CreateDefault().IsCommercialApiGovernanceReady,
            hasPublicApiBaseline: SigtranPublicApiBaseline.CreateCurrent().CoversKnownSurfaces,
            commercialReady: SigtranCommercialReadiness.GetReport().CommercialReady);
    }
}
