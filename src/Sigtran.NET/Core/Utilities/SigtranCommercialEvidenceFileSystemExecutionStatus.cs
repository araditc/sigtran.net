namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides filesystem-backed commercial evidence execution status reporting.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemExecutionStatus
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
        "command-materialization"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-approved-commercial-run-required",
        "status-final-validation-pending"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercial Evidence Filesystem Execution";

    /// <summary>The number of completed filesystem execution work units.</summary>
    public const int CompletedUnitCount = 9;

    /// <summary>Returns the completed filesystem execution capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for filesystem-backed commercial publication.</summary>
    /// <returns>The default commercial publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether filesystem execution foundation contracts are ready.</summary>
    public static bool ExecutionFoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranCommercialEvidenceFileVerificationCommands.CreateDefault("artifacts/commercial-evidence").IsReady;

    /// <summary>Whether a real approved commercial run has been retained and approved.</summary>
    public static bool RealApprovedCommercialRunReady => false;

    /// <summary>Whether filesystem-backed evidence currently allows commercial publication.</summary>
    public static bool CommercialPublicationReady => ExecutionFoundationReady
        && RealApprovedCommercialRunReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact filesystem execution status summary.</summary>
    /// <returns>The filesystem execution status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={ExecutionFoundationReady} realApprovedCommercialRunReady={RealApprovedCommercialRunReady} commercialPublicationReady={CommercialPublicationReady} blockers={DefaultBlockers.Length}";
    }
}
