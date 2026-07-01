namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the production evidence dossier intake report.
/// </summary>
public sealed class SigtranReleaseEvidenceDossierIntakeReport
{
    /// <summary>Creates a production evidence dossier intake report.</summary>
    /// <param name="completeness">The artifact completeness result.</param>
    /// <param name="reportPath">The retained report path.</param>
    public SigtranReleaseEvidenceDossierIntakeReport(
        SigtranReleaseEvidenceArtifactCompletenessResult completeness,
        string reportPath)
    {
        Completeness = completeness ?? throw new ArgumentNullException(nameof(completeness));
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
    }

    /// <summary>The artifact completeness result.</summary>
    public SigtranReleaseEvidenceArtifactCompletenessResult Completeness { get; }

    /// <summary>The retained report path.</summary>
    public string ReportPath { get; }

    /// <summary>The artifact intake target.</summary>
    public SigtranReleaseEvidenceArtifactIntakeTarget Target => Completeness.RedactionReviewManifest.DigestManifest.SourceManifest.Target;

    /// <summary>The number of registered artifact sources.</summary>
    public int SourceCount => Completeness.RedactionReviewManifest.DigestManifest.SourceManifest.Sources.Count;

    /// <summary>The number of digest entries.</summary>
    public int DigestCount => Completeness.RedactionReviewManifest.DigestManifest.Digests.Count;

    /// <summary>The number of redaction reviews.</summary>
    public int RedactionReviewCount => Completeness.RedactionReviewManifest.Reviews.Count;

    /// <summary>Whether the report path is scoped under the dossier root.</summary>
    public bool UsesDossierReportPath => ReportPath.StartsWith(Target.DossierRoot + "/", StringComparison.Ordinal);

    /// <summary>Whether the report is ready for retention.</summary>
    public bool IsReady => Completeness.IsComplete
        && UsesDossierReportPath
        && SourceCount > 0
        && DigestCount == SourceCount;

    /// <summary>Renders the report as Markdown.</summary>
    /// <returns>The rendered Markdown report.</returns>
    public string RenderMarkdown()
    {
        return string.Join(
            Environment.NewLine,
            [
                "# Production Evidence Dossier Intake Report",
                string.Empty,
                $"Run: `{Target.ExecutionRun.RunId}`",
                $"Intake: `{Target.IntakeId}`",
                $"Reviewer: `{Target.ReviewerName}`",
                $"Dossier root: `{Target.DossierRoot}`",
                $"Sources: `{SourceCount}`",
                $"Digests: `{DigestCount}`",
                $"Redaction reviews: `{RedactionReviewCount}`",
                $"Complete: `{Completeness.IsComplete}`",
                $"Blockers: `{Completeness.Blockers.Count}`"
            ]);
    }
}

/// <summary>
/// Provides production evidence dossier intake report helpers.
/// </summary>
public static class SigtranReleaseEvidenceDossierIntakeReports
{
    /// <summary>Creates the default retained dossier intake report.</summary>
    /// <param name="completeness">The artifact completeness result.</param>
    /// <returns>The dossier intake report.</returns>
    public static SigtranReleaseEvidenceDossierIntakeReport CreateDefault(
        SigtranReleaseEvidenceArtifactCompletenessResult completeness)
    {
        ArgumentNullException.ThrowIfNull(completeness);
        SigtranReleaseEvidenceArtifactIntakeTarget target = completeness.RedactionReviewManifest.DigestManifest.SourceManifest.Target;

        return new(completeness, $"{target.DossierRoot}/intake-report.md");
    }
}
