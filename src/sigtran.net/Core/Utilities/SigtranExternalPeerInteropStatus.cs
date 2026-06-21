namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability execution status.
/// </summary>
public static class SigtranExternalPeerInteropStatus
{
    private static readonly string[] Capabilities =
    [
        "openss7-environment",
        "openss7-configuration",
        "openss7-trace-expectations",
        "openss7-artifact-manifest",
        "openss7-run-plan",
        "openss7-command-set",
        "openss7-run-report",
        "openss7-evidence-registry",
        "openss7-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "OpenSS7/IPSS7 Interop Execution";

    /// <summary>The number of completed OpenSS7/IPSS7 execution work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed OpenSS7/IPSS7 execution capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the OpenSS7/IPSS7 execution foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranExternalPeerInteropReadiness.GetReport().FoundationReady
        && SigtranExternalPeerInteropCi.CreateDefault().RequiresExternalPeerRunner;

    /// <summary>Whether OpenSS7/IPSS7 interop has passing execution evidence.</summary>
    public static bool Verified => FoundationReady && SigtranExternalPeerInteropReadiness.GetReport().Verified;

    /// <summary>Formats a compact OpenSS7/IPSS7 execution status summary.</summary>
    /// <returns>The OpenSS7/IPSS7 execution status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} verified={Verified}";
    }
}
