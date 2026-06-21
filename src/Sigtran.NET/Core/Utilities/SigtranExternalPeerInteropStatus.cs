namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides external peer interoperability execution status.
/// </summary>
public static class SigtranExternalPeerInteropStatus
{
    private static readonly string[] Capabilities =
    [
        "external-peer-environment",
        "external-peer-configuration",
        "external-peer-trace-expectations",
        "external-peer-artifact-manifest",
        "external-peer-run-plan",
        "external-peer-command-set",
        "external-peer-run-report",
        "external-peer-evidence-registry",
        "external-peer-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "External Peer Interop Execution";

    /// <summary>The number of completed external peer execution work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed external peer execution capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the external peer execution foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranExternalPeerInteropReadiness.GetReport().FoundationReady
        && SigtranExternalPeerInteropCi.CreateDefault().RequiresExternalPeerRunner;

    /// <summary>Whether external peer interop has passing execution evidence.</summary>
    public static bool Verified => FoundationReady && SigtranExternalPeerInteropReadiness.GetReport().Verified;

    /// <summary>Formats a compact external peer execution status summary.</summary>
    /// <returns>The external peer execution status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} verified={Verified}";
    }
}
