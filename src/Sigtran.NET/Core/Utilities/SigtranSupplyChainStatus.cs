namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides supply-chain automation status.
/// </summary>
public static class SigtranSupplyChainStatus
{
    private static readonly string[] Capabilities =
    [
        "supply-chain-automation-plan",
        "sbom-generation-contract",
        "package-signing-contract",
        "signature-verification-contract",
        "provenance-attestation-contract",
        "supply-chain-artifact-manifest",
        "supply-chain-gate",
        "supply-chain-readiness",
        "supply-chain-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Supply Chain Automation";

    /// <summary>The number of completed supply-chain automation work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed supply-chain capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the supply-chain automation foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranSupplyChainReadiness.GetReport().FoundationReady;

    /// <summary>Whether supply-chain promotion is currently ready.</summary>
    public static bool PromotionReady => FoundationReady
        && SigtranSupplyChainReadiness.GetReport().PromotionReady;

    /// <summary>Formats a compact supply-chain automation status summary.</summary>
    /// <returns>The supply-chain automation status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} promotionReady={PromotionReady}";
    }
}
