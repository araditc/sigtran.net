namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes retained filesystem verification artifacts written to disk.
/// </summary>
public sealed class SigtranCommercialEvidenceFileSystemArtifactWriteResult
{
    /// <summary>Creates a retained filesystem verification artifact write result.</summary>
    /// <param name="verificationExecution">The filesystem verification execution.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="reportPath">The written verification report path.</param>
    /// <param name="observationManifestPath">The written observation manifest path.</param>
    /// <param name="writtenAtUtc">The UTC write time.</param>
    public SigtranCommercialEvidenceFileSystemArtifactWriteResult(
        SigtranCommercialEvidenceFileSystemVerificationExecution verificationExecution,
        string outputDirectory,
        string reportPath,
        string observationManifestPath,
        DateTimeOffset writtenAtUtc)
    {
        VerificationExecution = verificationExecution ?? throw new ArgumentNullException(nameof(verificationExecution));
        OutputDirectory = string.IsNullOrWhiteSpace(outputDirectory) ? throw new ArgumentException("Output directory is required.", nameof(outputDirectory)) : outputDirectory;
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
        ObservationManifestPath = string.IsNullOrWhiteSpace(observationManifestPath) ? throw new ArgumentException("Observation manifest path is required.", nameof(observationManifestPath)) : observationManifestPath;
        WrittenAtUtc = writtenAtUtc.Offset == TimeSpan.Zero ? writtenAtUtc : writtenAtUtc.ToUniversalTime();
    }

    /// <summary>The filesystem verification execution.</summary>
    public SigtranCommercialEvidenceFileSystemVerificationExecution VerificationExecution { get; }

    /// <summary>The output directory.</summary>
    public string OutputDirectory { get; }

    /// <summary>The written verification report path.</summary>
    public string ReportPath { get; }

    /// <summary>The written observation manifest path.</summary>
    public string ObservationManifestPath { get; }

    /// <summary>The UTC write time.</summary>
    public DateTimeOffset WrittenAtUtc { get; }

    /// <summary>Whether all expected artifacts exist.</summary>
    public bool AllArtifactsExist => File.Exists(ReportPath)
        && File.Exists(ObservationManifestPath);

    /// <summary>Whether all expected artifacts have non-empty content.</summary>
    public bool AllArtifactsHaveContent => AllArtifactsExist
        && new FileInfo(ReportPath).Length > 0
        && new FileInfo(ObservationManifestPath).Length > 0;

    /// <summary>Whether the write time is normalized to UTC.</summary>
    public bool UsesUtcWriteTime => WrittenAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether retained filesystem verification artifacts are ready for ledger execution.</summary>
    public bool IsReady => VerificationExecution.IsReady
        && AllArtifactsExist
        && AllArtifactsHaveContent
        && UsesUtcWriteTime;

    /// <summary>Formats a compact artifact write summary.</summary>
    /// <returns>The artifact write summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceFileSystemArtifactsReady={IsReady} report={ReportPath} observations={ObservationManifestPath}";
    }
}

/// <summary>
/// Writes retained filesystem verification artifacts to disk.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemArtifactWriters
{
    /// <summary>Writes the filesystem verification report and observation manifest.</summary>
    /// <param name="verificationExecution">The filesystem verification execution.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="writtenAtUtc">An optional UTC write time.</param>
    /// <returns>The retained filesystem verification artifact write result.</returns>
    public static SigtranCommercialEvidenceFileSystemArtifactWriteResult WriteVerificationArtifacts(
        SigtranCommercialEvidenceFileSystemVerificationExecution verificationExecution,
        string outputDirectory,
        DateTimeOffset? writtenAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(verificationExecution);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);
        string reportPath = Path.Combine(outputDirectory, "filesystem-verification-report.md");
        string observationManifestPath = Path.Combine(outputDirectory, "filesystem-observations.tsv");
        File.WriteAllText(reportPath, RenderReport(verificationExecution));
        File.WriteAllText(observationManifestPath, RenderObservations(verificationExecution.ManifestExecution.Observations));

        return new(
            verificationExecution,
            outputDirectory,
            reportPath,
            observationManifestPath,
            writtenAtUtc ?? DateTimeOffset.UtcNow);
    }

    private static string RenderReport(SigtranCommercialEvidenceFileSystemVerificationExecution execution)
    {
        return string.Join(
            Environment.NewLine,
            [
                "# Commercial Evidence Filesystem Verification Report",
                string.Empty,
                $"Intake: `{execution.ManifestExecution.Handoff.Report.Target.IntakeId}`",
                $"Observations: `{execution.ObservationCount}`",
                $"Filesystem evidence: `{execution.HasFilesystemEvidence}`",
                $"Verified: `{execution.Report.IsVerified}`",
                $"Missing files: `{execution.Report.MissingFileCount}`",
                $"Digest mismatches: `{execution.Report.DigestMismatchCount}`",
                $"Blockers: `{string.Join(",", execution.Report.Blockers)}`"
            ]);
    }

    private static string RenderObservations(IReadOnlyList<SigtranCommercialEvidenceFileSystemObservation> observations)
    {
        string[] lines = observations
            .Select(static observation => string.Join(
                '\t',
                observation.RetainedFile.Kind,
                observation.RetainedFile.RetainedPath,
                observation.FileSystemPath,
                observation.Exists,
                observation.RetainedFile.SizeBytes,
                observation.RetainedFile.ActualSha256,
                observation.RetainedFile.DigestMatches))
            .Prepend("kind\tretainedPath\tfileSystemPath\texists\tsizeBytes\tactualSha256\tdigestMatches")
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }
}
