namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides production evidence dossier status.
/// </summary>
public static class SigtranReleaseEvidenceStatus
{
    private static readonly string[] Capabilities =
    [
        "release-evidence-requirements",
        "release-evidence-artifact-contract",
        "release-evidence-manifest",
        "release-evidence-bundle",
        "release-evidence-gate",
        "release-evidence-readiness",
        "release-evidence-ci-profile",
        "status-capability-normalization",
        "readme-alignment",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Evidence Dossier";

    /// <summary>The number of completed production evidence work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed production evidence capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the production evidence foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseEvidenceReadiness.GetReport().FoundationReady;

    /// <summary>Whether current production evidence can support production claims.</summary>
    public static bool ReleaseEvidenceReady => FoundationReady
        && SigtranReleaseEvidenceReadiness.GetReport().ReleaseEvidenceReady;

    /// <summary>Formats a compact production evidence status summary.</summary>
    /// <returns>The production evidence status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} releaseEvidenceReady={ReleaseEvidenceReady}";
    }
}
