namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides interoperability and tooling status.
/// </summary>
public static class SigtranInteroperabilityToolingStatus
{
    private static readonly string[] Capabilities =
    [
        "trace-formatting",
        "conformance-vector-registry",
        "built-in-golden-vectors",
        "simulator-scripts",
        "map-sms-flow-builder",
        "local-tcp-sample-scenario",
        "sample-catalog",
        "ci-verification-profile",
        "interoperability-readiness-report",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string StatusLabel = "Interoperability and Tooling";

    /// <summary>The number of completed interoperability and tooling work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed interoperability and tooling capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact interoperability and tooling status summary.</summary>
    /// <returns>The interoperability and tooling status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={SigtranInteroperabilityReadiness.GetReport().FoundationReady}";
    }
}
