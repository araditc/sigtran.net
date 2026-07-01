namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed production evidence retention ledger execution.
/// </summary>
public sealed class SigtranReleaseEvidenceFileSystemRetentionLedgerExecution
{
    /// <summary>Creates filesystem-backed retention ledger execution.</summary>
    /// <param name="artifactWriteResult">The retained filesystem artifact write result.</param>
    /// <param name="ledger">The retention ledger created from filesystem verification.</param>
    public SigtranReleaseEvidenceFileSystemRetentionLedgerExecution(
        SigtranReleaseEvidenceFileSystemArtifactWriteResult artifactWriteResult,
        SigtranReleaseEvidenceRetentionLedger ledger)
    {
        ArtifactWriteResult = artifactWriteResult ?? throw new ArgumentNullException(nameof(artifactWriteResult));
        Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    /// <summary>The retained filesystem artifact write result.</summary>
    public SigtranReleaseEvidenceFileSystemArtifactWriteResult ArtifactWriteResult { get; }

    /// <summary>The retention ledger created from filesystem verification.</summary>
    public SigtranReleaseEvidenceRetentionLedger Ledger { get; }

    /// <summary>Whether verification artifacts were written and retained.</summary>
    public bool ArtifactsReady => ArtifactWriteResult.IsReady;

    /// <summary>Whether the retention ledger covers the verified filesystem files.</summary>
    public bool CoversVerifiedFilesystemFiles => Ledger.CoversVerifiedFiles;

    /// <summary>Whether the filesystem-backed retention ledger execution is ready for integrity sealing.</summary>
    public bool IsReady => ArtifactsReady
        && Ledger.IsReady
        && CoversVerifiedFilesystemFiles;

    /// <summary>Formats a compact filesystem retention ledger execution summary.</summary>
    /// <returns>The filesystem retention ledger execution summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceFileSystemRetentionLedgerReady={IsReady} entries={Ledger.Entries.Count}";
    }
}

/// <summary>
/// Provides filesystem-backed production evidence retention ledger execution helpers.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemRetentionLedgers
{
    /// <summary>Creates retention ledger execution from retained filesystem verification artifacts.</summary>
    /// <param name="artifactWriteResult">The retained filesystem artifact write result.</param>
    /// <param name="reviewer">The reviewer that accepted the retained files.</param>
    /// <param name="retainedAtUtc">The UTC retention start time.</param>
    /// <param name="retentionDays">The retention duration in days.</param>
    /// <returns>The filesystem-backed retention ledger execution.</returns>
    public static SigtranReleaseEvidenceFileSystemRetentionLedgerExecution Create(
        SigtranReleaseEvidenceFileSystemArtifactWriteResult artifactWriteResult,
        string reviewer,
        DateTimeOffset retainedAtUtc,
        int retentionDays = 365)
    {
        ArgumentNullException.ThrowIfNull(artifactWriteResult);
        SigtranReleaseEvidenceRetentionLedger ledger = SigtranReleaseEvidenceRetentionLedgers.CreateDefault(
            artifactWriteResult.VerificationExecution.Report,
            reviewer,
            retainedAtUtc,
            retentionDays);

        return new(artifactWriteResult, ledger);
    }
}
