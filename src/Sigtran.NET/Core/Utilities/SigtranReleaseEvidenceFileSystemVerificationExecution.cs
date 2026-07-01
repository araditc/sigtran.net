namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed retained file verification report execution.
/// </summary>
public sealed class SigtranReleaseEvidenceFileSystemVerificationExecution
{
    /// <summary>Creates filesystem-backed retained file verification report execution.</summary>
    /// <param name="manifestExecution">The filesystem-backed retained file manifest execution.</param>
    /// <param name="report">The retained file verification report.</param>
    public SigtranReleaseEvidenceFileSystemVerificationExecution(
        SigtranReleaseEvidenceFileSystemManifestExecution manifestExecution,
        SigtranReleaseEvidenceFileVerificationReport report)
    {
        ManifestExecution = manifestExecution ?? throw new ArgumentNullException(nameof(manifestExecution));
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    /// <summary>The filesystem-backed retained file manifest execution.</summary>
    public SigtranReleaseEvidenceFileSystemManifestExecution ManifestExecution { get; }

    /// <summary>The retained file verification report.</summary>
    public SigtranReleaseEvidenceFileVerificationReport Report { get; }

    /// <summary>The number of filesystem observations evaluated by the report.</summary>
    public int ObservationCount => ManifestExecution.Observations.Count;

    /// <summary>Whether every observed retained file exists on the filesystem.</summary>
    public bool HasFilesystemEvidence => ManifestExecution.AllObservedFilesExist;

    /// <summary>Whether the report contains filesystem-derived blockers.</summary>
    public bool HasBlockers => Report.Blockers.Count > 0;

    /// <summary>Whether filesystem-backed verification report execution is ready for retained artifact writing.</summary>
    public bool IsReady => ManifestExecution.IsReady
        && Report.IsVerified;

    /// <summary>Formats a compact filesystem verification execution summary.</summary>
    /// <returns>The filesystem verification execution summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceFileSystemVerificationReady={IsReady} observations={ObservationCount} blockers={Report.Blockers.Count}";
    }
}

/// <summary>
/// Provides filesystem-backed retained file verification report execution helpers.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemVerificationReports
{
    /// <summary>Evaluates a filesystem-backed retained file manifest execution.</summary>
    /// <param name="manifestExecution">The filesystem-backed retained file manifest execution.</param>
    /// <returns>The filesystem-backed verification report execution.</returns>
    public static SigtranReleaseEvidenceFileSystemVerificationExecution Evaluate(
        SigtranReleaseEvidenceFileSystemManifestExecution manifestExecution)
    {
        ArgumentNullException.ThrowIfNull(manifestExecution);
        SigtranReleaseEvidenceFileVerificationReport report = SigtranReleaseEvidenceFileVerificationReports.Evaluate(manifestExecution.Manifest);

        return new(manifestExecution, report);
    }
}
