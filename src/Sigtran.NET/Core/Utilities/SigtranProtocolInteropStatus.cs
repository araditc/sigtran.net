namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides SCCP, TCAP, and MAP SMS interoperability vector status.
/// </summary>
public static class SigtranProtocolInteropStatus
{
    private static readonly string[] Capabilities =
    [
        "protocol-vector-catalog",
        "protocol-reference-catalog",
        "protocol-artifact-manifest",
        "protocol-comparison-rules",
        "protocol-run-plan",
        "protocol-command-set",
        "protocol-run-report",
        "protocol-evidence-registry",
        "protocol-ci-profile",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "SCCP TCAP MAP Interop Vectors";

    /// <summary>The number of completed protocol interop vector work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed protocol interop vector capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the protocol interop vector foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranProtocolInteropReadiness.GetReport().FoundationReady
        && SigtranProtocolInteropCi.CreateDefault().RequiresExternalVectors;

    /// <summary>Whether SCCP, TCAP, and MAP SMS vectors have complete passing evidence.</summary>
    public static bool Verified => FoundationReady && SigtranProtocolInteropReadiness.GetReport().Verified;

    /// <summary>Formats a compact protocol interop vector status summary.</summary>
    /// <returns>The protocol interop vector status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} verified={Verified}";
    }
}
