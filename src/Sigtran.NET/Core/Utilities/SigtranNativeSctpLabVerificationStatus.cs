namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides native SCTP lab verification status.
/// </summary>
public static class SigtranNativeSctpLabVerificationStatus
{
    private static readonly string[] Capabilities =
    [
        "native-sctp-lab-scenarios",
        "native-sctp-artifact-manifest",
        "native-sctp-run-plan",
        "native-sctp-command-set",
        "native-sctp-run-report",
        "native-sctp-evidence-registry",
        "native-sctp-lab-readiness",
        "native-sctp-lab-ci-profile",
        "native-sctp-production-gate",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Native SCTP Lab Verification";

    /// <summary>The number of completed native SCTP lab verification work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed native SCTP lab verification capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the native SCTP lab foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranNativeSctpLabReadiness.GetReport().FoundationReady
        && SigtranNativeSctpLabCi.CreateDefault().RequiresLinuxRunner;

    /// <summary>Whether native SCTP lab verification has production evidence.</summary>
    public static bool NativeSctpProductionVerified => FoundationReady
        && SigtranNativeSctpLabReadiness.GetReport().ProductionReady;

    /// <summary>Formats a compact native SCTP lab verification status summary.</summary>
    /// <returns>The native SCTP lab verification status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} nativeSctpProductionVerified={NativeSctpProductionVerified}";
    }
}
