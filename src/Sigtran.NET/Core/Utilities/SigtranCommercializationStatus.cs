namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides commercialization status.
/// </summary>
public static class SigtranCommercializationStatus
{
    private static readonly string[] Capabilities =
    [
        "commercial-readiness-gates",
        "native-sctp-support-matrix",
        "external-interop-evidence-registry",
        "release-candidate-manifest",
        "package-governance-policy",
        "security-response-policy",
        "compatibility-policy",
        "observability-profile",
        "deployment-profiles",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercialization and Release Hardening";

    /// <summary>The number of completed commercialization work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed commercialization capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact commercialization status summary.</summary>
    /// <returns>The commercialization status summary.</returns>
    public static string Describe()
    {
        SigtranCommercialReadinessReport readiness = SigtranCommercialReadiness.GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} internalReleaseReady={readiness.InternalReleaseReady} commercialReady={readiness.CommercialReady}";
    }
}
