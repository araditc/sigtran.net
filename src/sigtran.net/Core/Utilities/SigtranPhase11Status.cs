namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 11 developer experience and adoption status.
/// </summary>
public static class SigtranPhase11Status
{
    private static readonly string[] Capabilities =
    [
        "developer-experience-capability-catalog",
        "m3ua-quickstart",
        "sample-templates",
        "configuration-profiles",
        "troubleshooting-index",
        "api-reference-index",
        "adoption-gates",
        "documentation-readiness-report",
        "developer-experience-ci-profile",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 11 - Developer Experience And Adoption";

    /// <summary>The number of completed Phase 11 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 11 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 11 foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranAdoptionGates.GetReport().DeveloperAdoptionReady
        && SigtranDocumentationReadiness.GetReport().DeveloperDocsReady;

    /// <summary>Whether enterprise production adoption is ready.</summary>
    public static bool EnterpriseProductionReady => FoundationReady
        && SigtranAdoptionGates.GetReport().EnterpriseProductionReady;

    /// <summary>Formats a compact Phase 11 status summary.</summary>
    /// <returns>The Phase 11 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} enterpriseProductionReady={EnterpriseProductionReady}";
    }
}
