namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides commercial evidence file verification status reporting.
/// </summary>
public static class SigtranCommercialEvidenceFileVerificationStatus
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
        "status-reporting"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-retained-file-evidence-required",
        "status-final-validation-pending"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercial Evidence File Verification";

    /// <summary>The number of completed commercial evidence file verification work units.</summary>
    public const int CompletedUnitCount = 9;

    /// <summary>Returns the completed file verification capability names.</summary>
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

    /// <summary>Whether file verification foundation contracts are ready.</summary>
    public static bool FileVerificationFoundationReady => Capabilities.Length == CompletedUnitCount
        && CreateDefaultPromotionGate().CanPromoteEvidence
        && SigtranCommercialEvidenceFileVerificationCommands.CreateDefault("artifacts/commercial-evidence").IsReady;

    /// <summary>Whether real retained file evidence is available in the default status.</summary>
    public static bool RealRetainedFileEvidenceReady => false;

    /// <summary>Whether retained file verification currently allows commercial publication.</summary>
    public static bool CommercialPublicationReady => FileVerificationFoundationReady
        && RealRetainedFileEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact commercial evidence file verification summary.</summary>
    /// <returns>The commercial evidence file verification summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FileVerificationFoundationReady} realRetainedFileEvidenceReady={RealRetainedFileEvidenceReady} commercialPublicationReady={CommercialPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranCommercialEvidenceVerifiedPromotionGateResult CreateDefaultPromotionGate()
    {
        SigtranCommercialEvidenceExecutionRun run = SigtranCommercialEvidenceExecutionRuns.CreateReleaseCandidateRun(
            "1.0.0-rc.1",
            "abcdef123456",
            "run-20260622-001",
            "release-automation",
            DateTimeOffset.UtcNow);
        SigtranCommercialEvidenceDossierIntakeBridgeResult bridge = SigtranCommercialEvidenceDossierIntakeBridge.BuildDefault(
            run,
            "intake-20260622-001",
            "release-review",
            $"{run.RunArtifactRoot}/incoming/intake-20260622-001",
            new string('a', 64),
            new string('b', 64),
            DateTimeOffset.UtcNow);
        SigtranCommercialEvidenceRetainedFileManifest files = SigtranCommercialEvidenceRetainedFiles.CreateVerifiedManifest(
            bridge.Handoff,
            sizeBytes: 4096,
            DateTimeOffset.UtcNow);
        SigtranCommercialEvidenceFileVerificationReport report = SigtranCommercialEvidenceFileVerificationReports.Evaluate(files);
        SigtranCommercialEvidenceRetentionLedger ledger = SigtranCommercialEvidenceRetentionLedgers.CreateDefault(
            report,
            "release-review",
            DateTimeOffset.UtcNow);
        SigtranCommercialEvidenceIntegritySeal seal = SigtranCommercialEvidenceIntegritySeals.CreateDefault(
            ledger,
            DateTimeOffset.UtcNow);
        SigtranCommercialEvidencePublicationAttachmentManifest attachments = SigtranCommercialEvidencePublicationAttachments.CreateDefault(seal);

        return SigtranCommercialEvidenceVerifiedPromotionGates.Evaluate(
            attachments,
            "release-review",
            DateTimeOffset.UtcNow,
            evidenceApproved: true);
    }
}
