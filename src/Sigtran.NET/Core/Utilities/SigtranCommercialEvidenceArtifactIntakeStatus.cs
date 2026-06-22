namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides commercial evidence artifact intake status reporting.
/// </summary>
public static class SigtranCommercialEvidenceArtifactIntakeStatus
{
    private static readonly string[] Capabilities =
    [
        "artifact-intake-target",
        "artifact-source-manifest",
        "artifact-digest-manifest",
        "redaction-review-manifest",
        "artifact-completeness-evaluation",
        "dossier-intake-report",
        "promotion-handoff",
        "execution-dossier-bridge",
        "final-validation",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-artifact-file-evidence-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercial Evidence Artifact Intake";

    /// <summary>The number of completed commercial evidence artifact intake work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed artifact intake capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for commercial publication.</summary>
    /// <returns>The default commercial publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the artifact intake foundation contracts are ready.</summary>
    public static bool ArtifactIntakeFoundationReady => Capabilities.Length == CompletedUnitCount
        && CreateDefaultBridgeResult().IsReady;

    /// <summary>Whether real artifact file evidence is available in the default status.</summary>
    public static bool RealArtifactEvidenceReady => false;

    /// <summary>Whether artifact intake currently allows commercial publication.</summary>
    public static bool CommercialPublicationReady => ArtifactIntakeFoundationReady
        && RealArtifactEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact commercial evidence artifact intake summary.</summary>
    /// <returns>The commercial evidence artifact intake summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={ArtifactIntakeFoundationReady} realArtifactEvidenceReady={RealArtifactEvidenceReady} commercialPublicationReady={CommercialPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranCommercialEvidenceDossierIntakeBridgeResult CreateDefaultBridgeResult()
    {
        SigtranCommercialEvidenceExecutionRun run = SigtranCommercialEvidenceExecutionRuns.CreateReleaseCandidateRun(
            "1.0.0-rc.1",
            "abcdef123456",
            "run-20260622-001",
            "release-automation",
            DateTimeOffset.UtcNow);

        return SigtranCommercialEvidenceDossierIntakeBridge.BuildDefault(
            run,
            "intake-20260622-001",
            "release-review",
            $"{run.RunArtifactRoot}/incoming/intake-20260622-001",
            new string('a', 64),
            new string('b', 64),
            DateTimeOffset.UtcNow);
    }
}
