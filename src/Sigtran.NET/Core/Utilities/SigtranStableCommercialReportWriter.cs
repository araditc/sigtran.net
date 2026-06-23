using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a retained stable commercial release report.
/// </summary>
public sealed class SigtranStableCommercialReportWriteResult
{
    /// <summary>Creates a retained stable commercial release report result.</summary>
    /// <param name="publishPlan">The stable publish execution plan rendered into the report.</param>
    /// <param name="reportPath">The retained report path.</param>
    /// <param name="reportSha256">The retained report SHA-256 digest.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    /// <param name="stableTagCreated">Whether the stable tag was created and verified.</param>
    /// <param name="stablePackagePublished">Whether the stable package was published.</param>
    /// <param name="publicationEvidenceRetained">Whether final publication evidence was retained.</param>
    public SigtranStableCommercialReportWriteResult(
        SigtranStablePublishExecutionPlan publishPlan,
        string reportPath,
        string reportSha256,
        DateTimeOffset writtenAtUtc,
        bool stableTagCreated,
        bool stablePackagePublished,
        bool publicationEvidenceRetained)
    {
        PublishPlan = publishPlan ?? throw new ArgumentNullException(nameof(publishPlan));
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
        ReportSha256 = string.IsNullOrWhiteSpace(reportSha256) ? throw new ArgumentException("Report SHA-256 digest is required.", nameof(reportSha256)) : reportSha256;
        WrittenAtUtc = writtenAtUtc.Offset == TimeSpan.Zero ? writtenAtUtc : writtenAtUtc.ToUniversalTime();
        StableTagCreated = stableTagCreated;
        StablePackagePublished = stablePackagePublished;
        PublicationEvidenceRetained = publicationEvidenceRetained;
    }

    /// <summary>The stable publish execution plan rendered into the report.</summary>
    public SigtranStablePublishExecutionPlan PublishPlan { get; }

    /// <summary>The retained report path.</summary>
    public string ReportPath { get; }

    /// <summary>The retained report SHA-256 digest.</summary>
    public string ReportSha256 { get; }

    /// <summary>The UTC report write time.</summary>
    public DateTimeOffset WrittenAtUtc { get; }

    /// <summary>Whether the stable tag was created and verified.</summary>
    public bool StableTagCreated { get; }

    /// <summary>Whether the stable package was published.</summary>
    public bool StablePackagePublished { get; }

    /// <summary>Whether final publication evidence was retained.</summary>
    public bool PublicationEvidenceRetained { get; }

    /// <summary>Whether the report exists on disk.</summary>
    public bool ReportExists => File.Exists(ReportPath);

    /// <summary>The retained report size in bytes.</summary>
    public long ReportSizeBytes => ReportExists ? new FileInfo(ReportPath).Length : 0;

    /// <summary>Whether the report digest is a valid SHA-256 hex value.</summary>
    public bool HasValidReportDigest => ReportSha256.Length == 64
        && ReportSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the report write time is UTC.</summary>
    public bool HasUtcWriteTime => WrittenAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the stable commercial release is complete.</summary>
    public bool StableCommercialReleaseComplete => PublishPlan.IsReady
        && StableTagCreated
        && StablePackagePublished
        && PublicationEvidenceRetained;

    /// <summary>Whether the report is ready for stable release audit trail creation.</summary>
    public bool IsReadyForAuditTrail => PublishPlan.IsReady
        && ReportExists
        && ReportSizeBytes > 0
        && HasValidReportDigest
        && HasUtcWriteTime;

    /// <summary>Returns stable commercial report blockers.</summary>
    /// <returns>The stable commercial report blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!PublishPlan.IsReady)
        {
            blockers.Add("stable-publish-execution-plan-not-ready");
        }

        if (!StableTagCreated)
        {
            blockers.Add("stable-tag-creation-evidence-required");
        }

        if (!StablePackagePublished)
        {
            blockers.Add("stable-package-publication-evidence-required");
        }

        if (!PublicationEvidenceRetained)
        {
            blockers.Add("stable-publication-evidence-retention-required");
        }

        return blockers;
    }

    /// <summary>Formats a compact stable commercial report summary.</summary>
    /// <returns>The stable commercial report summary.</returns>
    public string Describe()
    {
        return $"stableCommercialReportReady={IsReadyForAuditTrail} complete={StableCommercialReleaseComplete} report={ReportPath}";
    }
}

/// <summary>
/// Provides retained stable commercial release report helpers.
/// </summary>
public static class SigtranStableCommercialReportWriters
{
    /// <summary>Writes a retained stable commercial release report.</summary>
    /// <param name="publishPlan">The stable publish execution plan.</param>
    /// <param name="outputDirectory">The report output directory.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    /// <param name="stableTagCreated">Whether the stable tag was created and verified.</param>
    /// <param name="stablePackagePublished">Whether the stable package was published.</param>
    /// <param name="publicationEvidenceRetained">Whether final publication evidence was retained.</param>
    /// <returns>The retained stable commercial report result.</returns>
    public static SigtranStableCommercialReportWriteResult WriteReport(
        SigtranStablePublishExecutionPlan publishPlan,
        string outputDirectory,
        DateTimeOffset writtenAtUtc,
        bool stableTagCreated,
        bool stablePackagePublished,
        bool publicationEvidenceRetained)
    {
        ArgumentNullException.ThrowIfNull(publishPlan);
        string directory = string.IsNullOrWhiteSpace(outputDirectory) ? throw new ArgumentException("Output directory is required.", nameof(outputDirectory)) : outputDirectory;
        Directory.CreateDirectory(directory);

        string reportPath = Path.Combine(directory, "stable-commercial-release-report.md");
        string report = RenderMarkdown(
            publishPlan,
            writtenAtUtc,
            stableTagCreated,
            stablePackagePublished,
            publicationEvidenceRetained);
        File.WriteAllText(reportPath, report, Encoding.UTF8);
        string digest = ComputeSha256(File.ReadAllBytes(reportPath));

        return new(
            publishPlan,
            reportPath,
            digest,
            writtenAtUtc,
            stableTagCreated,
            stablePackagePublished,
            publicationEvidenceRetained);
    }

    /// <summary>Renders the stable commercial release report as Markdown.</summary>
    /// <param name="publishPlan">The stable publish execution plan.</param>
    /// <param name="writtenAtUtc">The UTC report write time.</param>
    /// <param name="stableTagCreated">Whether the stable tag was created and verified.</param>
    /// <param name="stablePackagePublished">Whether the stable package was published.</param>
    /// <param name="publicationEvidenceRetained">Whether final publication evidence was retained.</param>
    /// <returns>The rendered Markdown report.</returns>
    public static string RenderMarkdown(
        SigtranStablePublishExecutionPlan publishPlan,
        DateTimeOffset writtenAtUtc,
        bool stableTagCreated,
        bool stablePackagePublished,
        bool publicationEvidenceRetained)
    {
        ArgumentNullException.ThrowIfNull(publishPlan);
        DateTimeOffset writtenAt = writtenAtUtc.Offset == TimeSpan.Zero ? writtenAtUtc : writtenAtUtc.ToUniversalTime();
        SigtranStableReleaseTarget target = publishPlan.Authorization.TagGate.CommandPlan.Decision.Checklist.EvidenceMap.Target;
        bool stableCommercialReleaseComplete = publishPlan.IsReady
            && stableTagCreated
            && stablePackagePublished
            && publicationEvidenceRetained;

        StringBuilder builder = new();
        builder.AppendLine("# Stable Commercial Release Report");
        builder.AppendLine();
        builder.Append("- Version: `").Append(target.Version).AppendLine("`");
        builder.Append("- Target tag: `").Append(target.TargetTag).AppendLine("`");
        builder.Append("- Source commit: `").Append(target.SourceCommit).AppendLine("`");
        builder.Append("- Artifact root: `").Append(target.ArtifactRoot).AppendLine("`");
        builder.Append("- Publish plan ready: `").Append(publishPlan.IsReady).AppendLine("`");
        builder.Append("- Stable tag created: `").Append(stableTagCreated).AppendLine("`");
        builder.Append("- Stable package published: `").Append(stablePackagePublished).AppendLine("`");
        builder.Append("- Publication evidence retained: `").Append(publicationEvidenceRetained).AppendLine("`");
        builder.Append("- Stable commercial release complete: `").Append(stableCommercialReleaseComplete).AppendLine("`");
        builder.Append("- Written at UTC: `").Append(writtenAt.ToString("O", CultureInfo.InvariantCulture)).AppendLine("`");
        builder.AppendLine();
        builder.AppendLine("## Execution Commands");
        builder.AppendLine();

        foreach (SigtranStablePublishExecutionCommand command in publishPlan.Commands.OrderBy(static command => command.Order))
        {
            builder.Append("- `").Append(command.Order).Append("` `")
                .Append(command.Name)
                .Append("`: `")
                .Append(command.Kind)
                .AppendLine("`");
        }

        return builder.ToString();
    }

    private static string ComputeSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }
}
