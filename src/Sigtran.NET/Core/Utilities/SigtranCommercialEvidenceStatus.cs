namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides commercial evidence dossier status.
/// </summary>
public static class SigtranCommercialEvidenceStatus
{
    private static readonly string[] Capabilities =
    [
        "commercial-evidence-requirements",
        "commercial-evidence-artifact-contract",
        "commercial-evidence-manifest",
        "commercial-evidence-bundle",
        "commercial-evidence-gate",
        "commercial-evidence-readiness",
        "commercial-evidence-ci-profile",
        "status-capability-normalization",
        "readme-alignment",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercial Evidence Dossier";

    /// <summary>The number of completed commercial evidence work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed commercial evidence capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the commercial evidence foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranCommercialEvidenceReadiness.GetReport().FoundationReady;

    /// <summary>Whether current commercial evidence can support production claims.</summary>
    public static bool CommercialEvidenceReady => FoundationReady
        && SigtranCommercialEvidenceReadiness.GetReport().CommercialEvidenceReady;

    /// <summary>Formats a compact commercial evidence status summary.</summary>
    /// <returns>The commercial evidence status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} commercialEvidenceReady={CommercialEvidenceReady}";
    }
}
