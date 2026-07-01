namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides release automation and supply-chain status.
/// </summary>
public static class SigtranReleaseAutomationStatus
{
    private static readonly string[] Capabilities =
    [
        "release-automation-plan",
        "release-artifact-manifest",
        "sbom-plan",
        "package-signing-plan",
        "release-provenance",
        "release-notes-validation",
        "publish-channels",
        "release-gate-evaluator",
        "release-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Release Automation And Supply Chain";

    /// <summary>The number of completed release automation work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed release automation capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the release automation foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseAutomation.CreateDefaultPlan().Steps.Count > 0
        && SigtranReleaseCiProfiles.CreateDefault().IsRunnable;

    /// <summary>Whether the SDK can publish a production stable release.</summary>
    public static bool ProductionStableReleaseReady => FoundationReady
        && SigtranProductionReadiness.GetReport().ProductionReady;

    /// <summary>Formats a compact release automation status summary.</summary>
    /// <returns>The release automation status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} productionStableReady={ProductionStableReleaseReady}";
    }
}
