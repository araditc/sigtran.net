namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides production evidence file verification status reporting.
/// </summary>
public static class SigtranReleaseEvidenceFileVerificationStatus
{
    private static readonly string[] Capabilities =
    [
        "retained-file-evidence",
        "retained-file-manifest",
        "file-verification-report",
        "retention-ledger",
        "integrity-seal",
        "publication-attachments",
        "verified-promotion-gate",
        "file-verification-command-plan",
        "status-reporting",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-retained-file-evidence-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Evidence File Verification";

    /// <summary>The number of completed production evidence file verification work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed file verification capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for production publication.</summary>
    /// <returns>The default production publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether file verification foundation contracts are ready.</summary>
    public static bool FileVerificationFoundationReady => Capabilities.Length == CompletedUnitCount
        && CreateDefaultPromotionGate().CanPromoteEvidence
        && SigtranReleaseEvidenceFileVerificationCommands.CreateDefault("artifacts/release-evidence").IsReady;

    /// <summary>Whether real retained file evidence is available in the default status.</summary>
    public static bool RealRetainedFileEvidenceReady => false;

    /// <summary>Whether retained file verification currently allows production publication.</summary>
    public static bool ProductionPublicationReady => FileVerificationFoundationReady
        && RealRetainedFileEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact production evidence file verification summary.</summary>
    /// <returns>The production evidence file verification summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FileVerificationFoundationReady} realRetainedFileEvidenceReady={RealRetainedFileEvidenceReady} productionPublicationReady={ProductionPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranReleaseEvidenceVerifiedPromotionGateResult CreateDefaultPromotionGate()
    {
        SigtranReleaseEvidenceExecutionRun run = SigtranReleaseEvidenceExecutionRuns.CreatePrereleaseRun(
            "1.0.0-rc.1",
            "abcdef123456",
            "run-20260622-001",
            "release-automation",
            DateTimeOffset.UtcNow);
        SigtranReleaseEvidenceDossierIntakeBridgeResult bridge = SigtranReleaseEvidenceDossierIntakeBridge.BuildDefault(
            run,
            "intake-20260622-001",
            "release-review",
            $"{run.RunArtifactRoot}/incoming/intake-20260622-001",
            new string('a', 64),
            new string('b', 64),
            DateTimeOffset.UtcNow);
        SigtranReleaseEvidenceRetainedFileManifest files = SigtranReleaseEvidenceRetainedFiles.CreateVerifiedManifest(
            bridge.Handoff,
            sizeBytes: 4096,
            DateTimeOffset.UtcNow);
        SigtranReleaseEvidenceFileVerificationReport report = SigtranReleaseEvidenceFileVerificationReports.Evaluate(files);
        SigtranReleaseEvidenceRetentionLedger ledger = SigtranReleaseEvidenceRetentionLedgers.CreateDefault(
            report,
            "release-review",
            DateTimeOffset.UtcNow);
        SigtranReleaseEvidenceIntegritySeal seal = SigtranReleaseEvidenceIntegritySeals.CreateDefault(
            ledger,
            DateTimeOffset.UtcNow);
        SigtranReleaseEvidencePublicationAttachmentManifest attachments = SigtranReleaseEvidencePublicationAttachments.CreateDefault(seal);

        return SigtranReleaseEvidenceVerifiedPromotionGates.Evaluate(
            attachments,
            "release-review",
            DateTimeOffset.UtcNow,
            evidenceApproved: true);
    }
}
