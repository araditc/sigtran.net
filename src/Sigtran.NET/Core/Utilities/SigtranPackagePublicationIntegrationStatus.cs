namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides package publication integration status reporting.
/// </summary>
public static class SigtranPackagePublicationIntegrationStatus
{
    private static readonly string[] Capabilities =
    [
        "publication-request",
        "package-artifact-binding",
        "credential-readiness",
        "publication-evidence-assembly",
        "publish-guard-bridge",
        "channel-policy-bridge",
        "publication-gate-execution",
        "dry-run-rehearsal",
        "guarded-command-materialization",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "retained-real-release-evidence-required",
        "approved-protected-publication-run-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Package Publication Gate Integration";

    /// <summary>The number of completed package publication integration work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns completed package publication integration capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns the default package publication blockers.</summary>
    /// <returns>The default package publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether package publication gate integration foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranPublicationCredentials.CreateDefaultPolicy().RequiresProductionSecrets
        && SigtranNuGetPublishPlans.CreateDryRun().IsDryRunSafe
        && SigtranNuGetPublishPlans.CreatePublish().IsPublishCapable;

    /// <summary>Whether retained real release evidence is available in the default status.</summary>
    public static bool RetainedRealReleaseEvidenceReady => false;

    /// <summary>Whether package publication is ready in the default status.</summary>
    public static bool PackagePublicationReady => FoundationReady
        && RetainedRealReleaseEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact package publication integration status summary.</summary>
    /// <returns>The package publication integration status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} retainedRealReleaseEvidenceReady={RetainedRealReleaseEvidenceReady} packagePublicationReady={PackagePublicationReady} blockers={DefaultBlockers.Length}";
    }
}
