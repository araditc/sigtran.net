namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides filesystem-backed production evidence execution status reporting.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemExecutionStatus
{
    private static readonly string[] Capabilities =
    [
        "filesystem-observation",
        "filesystem-manifest-execution",
        "filesystem-verification-report",
        "filesystem-artifact-writing",
        "filesystem-retention-ledger",
        "filesystem-integrity-seal",
        "filesystem-publication-attachments",
        "filesystem-promotion-gate",
        "command-materialization",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-approved-production-run-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Evidence Filesystem Execution";

    /// <summary>The number of completed filesystem execution work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed filesystem execution capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for filesystem-backed production publication.</summary>
    /// <returns>The default production publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether filesystem execution foundation contracts are ready.</summary>
    public static bool ExecutionFoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseEvidenceFileVerificationCommands.CreateDefault("artifacts/release-evidence").IsReady;

    /// <summary>Whether a real approved production run has been retained and approved.</summary>
    public static bool RealApprovedProductionRunReady => false;

    /// <summary>Whether filesystem-backed evidence currently allows production publication.</summary>
    public static bool ProductionPublicationReady => ExecutionFoundationReady
        && RealApprovedProductionRunReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact filesystem execution status summary.</summary>
    /// <returns>The filesystem execution status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={ExecutionFoundationReady} realApprovedProductionRunReady={RealApprovedProductionRunReady} productionPublicationReady={ProductionPublicationReady} blockers={DefaultBlockers.Length}";
    }
}
