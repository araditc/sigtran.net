using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a retained production evidence run approval report.
/// </summary>
public sealed class SigtranReleaseEvidenceRunApprovalReportWriteResult
{
    /// <summary>Creates a retained production evidence run approval report result.</summary>
    /// <param name="manifest">The approval manifest rendered into the report.</param>
    /// <param name="reportPath">The retained report path.</param>
    /// <param name="reportSha256">The retained report SHA-256 digest.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    public SigtranReleaseEvidenceRunApprovalReportWriteResult(
        SigtranReleaseEvidenceRunApprovalManifest manifest,
        string reportPath,
        string reportSha256,
        DateTimeOffset writtenAtUtc)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
        ReportSha256 = string.IsNullOrWhiteSpace(reportSha256) ? throw new ArgumentException("Report SHA-256 digest is required.", nameof(reportSha256)) : reportSha256;
        WrittenAtUtc = writtenAtUtc.Offset == TimeSpan.Zero ? writtenAtUtc : writtenAtUtc.ToUniversalTime();
    }

    /// <summary>The approval manifest rendered into the report.</summary>
    public SigtranReleaseEvidenceRunApprovalManifest Manifest { get; }

    /// <summary>The retained report path.</summary>
    public string ReportPath { get; }

    /// <summary>The retained report SHA-256 digest.</summary>
    public string ReportSha256 { get; }

    /// <summary>The UTC report write time.</summary>
    public DateTimeOffset WrittenAtUtc { get; }

    /// <summary>Whether the report exists on disk.</summary>
    public bool ReportExists => File.Exists(ReportPath);

    /// <summary>The retained report size in bytes.</summary>
    public long ReportSizeBytes => ReportExists ? new FileInfo(ReportPath).Length : 0;

    /// <summary>Whether the report digest is a valid SHA-256 hex value.</summary>
    public bool HasValidReportDigest => ReportSha256.Length == 64
        && ReportSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the report write time is normalized to UTC.</summary>
    public bool HasUtcWriteTime => WrittenAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the retained report is ready for promotion package creation.</summary>
    public bool IsReady => Manifest.IsReady
        && ReportExists
        && ReportSizeBytes > 0
        && HasValidReportDigest
        && HasUtcWriteTime;

    /// <summary>Formats a compact approval report write summary.</summary>
    /// <returns>The approval report write summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceRunApprovalReportReady={IsReady} report={ReportPath}";
    }
}

/// <summary>
/// Provides retained production evidence run approval report helpers.
/// </summary>
public static class SigtranReleaseEvidenceRunApprovalReportWriters
{
    /// <summary>Writes a retained Markdown approval report.</summary>
    /// <param name="manifest">The approval manifest to render.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    /// <returns>The retained approval report write result.</returns>
    public static SigtranReleaseEvidenceRunApprovalReportWriteResult WriteReport(
        SigtranReleaseEvidenceRunApprovalManifest manifest,
        string outputDirectory,
        DateTimeOffset writtenAtUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        string directory = string.IsNullOrWhiteSpace(outputDirectory) ? throw new ArgumentException("Output directory is required.", nameof(outputDirectory)) : outputDirectory;
        Directory.CreateDirectory(directory);

        string reportPath = Path.Combine(directory, "release-evidence-run-approval-report.md");
        string report = RenderMarkdown(manifest, writtenAtUtc);
        File.WriteAllText(reportPath, report, Encoding.UTF8);
        string digest = ComputeSha256(File.ReadAllBytes(reportPath));

        return new(manifest, reportPath, digest, writtenAtUtc);
    }

    /// <summary>Renders a Markdown approval report.</summary>
    /// <param name="manifest">The approval manifest to render.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    /// <returns>The rendered Markdown report.</returns>
    public static string RenderMarkdown(
        SigtranReleaseEvidenceRunApprovalManifest manifest,
        DateTimeOffset writtenAtUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        DateTimeOffset writtenAt = writtenAtUtc.Offset == TimeSpan.Zero ? writtenAtUtc : writtenAtUtc.ToUniversalTime();
        SigtranReleaseEvidenceApprovedRunTarget target = manifest.Checklist.Target;
        StringBuilder builder = new();

        builder.AppendLine("# Production Evidence Run Approval Report");
        builder.AppendLine();
        builder.Append("- Run id: `").Append(target.RunId).AppendLine("`");
        builder.Append("- Package version: `").Append(target.PackageVersion).AppendLine("`");
        builder.Append("- Source commit: `").Append(target.SourceCommit).AppendLine("`");
        builder.Append("- Artifact root: `").Append(target.ArtifactRoot).AppendLine("`");
        builder.Append("- Checklist digest: `").Append(manifest.ChecklistSha256).AppendLine("`");
        builder.Append("- Manifest ready: `").Append(manifest.IsReady).AppendLine("`");
        builder.Append("- Written at UTC: `").Append(writtenAt.ToString("O", CultureInfo.InvariantCulture)).AppendLine("`");
        builder.AppendLine();
        builder.AppendLine("## Reviewer Approvals");
        builder.AppendLine();

        foreach (SigtranReleaseEvidenceReviewerApproval approval in manifest.Approvals)
        {
            builder.Append("- `").Append(approval.Role).Append("`: `")
                .Append(approval.Approved)
                .Append("` by `")
                .Append(approval.ReviewerName)
                .AppendLine("`");
        }

        return builder.ToString();
    }

    private static string ComputeSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }
}
