namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides developer experience and adoption status.
/// </summary>
public static class SigtranDeveloperExperienceStatus
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
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Developer Experience And Adoption";

    /// <summary>The number of completed developer experience work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed developer experience capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the developer experience foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranAdoptionGates.GetReport().DeveloperAdoptionReady
        && SigtranDocumentationReadiness.GetReport().DeveloperDocsReady;

    /// <summary>Whether enterprise production adoption is ready.</summary>
    public static bool EnterpriseProductionReady => FoundationReady
        && SigtranAdoptionGates.GetReport().EnterpriseProductionReady;

    /// <summary>Formats a compact developer experience status summary.</summary>
    /// <returns>The developer experience status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} enterpriseProductionReady={EnterpriseProductionReady}";
    }
}
