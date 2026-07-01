namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides supply-chain release execution status.
/// </summary>
public static class SigtranSupplyChainReleaseStatus
{
    private static readonly string[] Capabilities =
    [
        "final-sbom-artifact",
        "trusted-timestamped-signing",
        "provenance-attestation",
        "public-api-diff-artifact",
        "release-artifact-upload",
        "supply-chain-release-command-plan",
        "supply-chain-release-gate",
        "workflow-execution",
        "final-sweep-validation",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "retained-release-run-artifacts-required",
        "release-evidence-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Supply Chain Release Execution";

    /// <summary>The number of completed supply-chain release execution work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed supply-chain release capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for release promotion.</summary>
    /// <returns>The default release promotion blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the execution foundation is ready.</summary>
    public static bool ExecutionFoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranSupplyChainReleaseCommands.CreateDefault("1.0.0").IsComplete
        && SigtranReleaseArtifactUploads.CreateDefault().HasPromotionArtifacts;

    /// <summary>Whether the default status can promote a production release.</summary>
    public static bool ReleaseReady => ExecutionFoundationReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact supply-chain release execution summary.</summary>
    /// <returns>The supply-chain release execution summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={ExecutionFoundationReady} releaseReady={ReleaseReady} blockers={DefaultBlockers.Length}";
    }
}
