using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a retained dry-run rehearsal for package publication.
/// </summary>
public sealed class SigtranPackagePublicationDryRunRehearsal
{
    /// <summary>Creates a package publication dry-run rehearsal.</summary>
    /// <param name="gateExecution">The package publication gate execution.</param>
    /// <param name="plan">The dry-run NuGet publication plan.</param>
    /// <param name="reportPath">The retained rehearsal report path.</param>
    /// <param name="renderedReport">The rendered rehearsal report.</param>
    /// <param name="rehearsedAtUtc">The UTC rehearsal time.</param>
    public SigtranPackagePublicationDryRunRehearsal(
        SigtranPackagePublicationGateExecution gateExecution,
        SigtranNuGetPublishPlan plan,
        string reportPath,
        string renderedReport,
        DateTimeOffset rehearsedAtUtc)
    {
        GateExecution = gateExecution ?? throw new ArgumentNullException(nameof(gateExecution));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
        RenderedReport = string.IsNullOrWhiteSpace(renderedReport) ? throw new ArgumentException("Rendered report is required.", nameof(renderedReport)) : renderedReport;
        RehearsedAtUtc = rehearsedAtUtc.Offset == TimeSpan.Zero ? rehearsedAtUtc : rehearsedAtUtc.ToUniversalTime();
    }

    /// <summary>The package publication gate execution.</summary>
    public SigtranPackagePublicationGateExecution GateExecution { get; }

    /// <summary>The dry-run NuGet publication plan.</summary>
    public SigtranNuGetPublishPlan Plan { get; }

    /// <summary>The retained rehearsal report path.</summary>
    public string ReportPath { get; }

    /// <summary>The rendered rehearsal report.</summary>
    public string RenderedReport { get; }

    /// <summary>The UTC rehearsal time.</summary>
    public DateTimeOffset RehearsedAtUtc { get; }

    /// <summary>Whether the report exists on disk.</summary>
    public bool ReportExists => File.Exists(ReportPath);

    /// <summary>The retained report size in bytes.</summary>
    public long ReportSizeBytes => ReportExists ? new FileInfo(ReportPath).Length : 0;

    /// <summary>Whether the rehearsal time is normalized to UTC.</summary>
    public bool HasUtcRehearsalTime => RehearsedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the dry-run plan is safe and contains no upload command.</summary>
    public bool HasSafeDryRunPlan => Plan.IsDryRunSafe;

    /// <summary>Whether the report includes every dry-run command.</summary>
    public bool ReportIncludesAllCommands => Plan.Commands.All(command => RenderedReport.Contains(command, StringComparison.Ordinal));

    /// <summary>Whether the dry-run rehearsal artifact is ready for retention.</summary>
    public bool IsReadyForRetention => HasSafeDryRunPlan
        && ReportExists
        && ReportSizeBytes > 0
        && HasUtcRehearsalTime
        && ReportIncludesAllCommands;

    /// <summary>Whether package publication can proceed after dry-run retention.</summary>
    public bool CanProceedAfterDryRun => GateExecution.IsPublicationAllowed
        && IsReadyForRetention;

    /// <summary>Formats a compact dry-run rehearsal summary.</summary>
    /// <returns>The dry-run rehearsal summary.</returns>
    public string Describe()
    {
        return $"packagePublicationDryRunReady={IsReadyForRetention} canProceed={CanProceedAfterDryRun} commands={Plan.Commands.Count}";
    }
}

/// <summary>
/// Provides package publication dry-run rehearsal helpers.
/// </summary>
public static class SigtranPackagePublicationDryRunRehearsals
{
    /// <summary>Writes a retained dry-run rehearsal report.</summary>
    /// <param name="gateExecution">The package publication gate execution.</param>
    /// <param name="reportPath">The retained rehearsal report path.</param>
    /// <param name="rehearsedAtUtc">The UTC rehearsal time.</param>
    /// <returns>The package publication dry-run rehearsal.</returns>
    public static SigtranPackagePublicationDryRunRehearsal WriteReport(
        SigtranPackagePublicationGateExecution gateExecution,
        string reportPath,
        DateTimeOffset rehearsedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(gateExecution);
        string retainedReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Report path is required.", nameof(reportPath)) : reportPath;
        string? directory = Path.GetDirectoryName(retainedReportPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SigtranNuGetPublishPlan plan = SigtranNuGetPublishPlans.CreateDryRun();
        string renderedReport = RenderReport(gateExecution, plan, rehearsedAtUtc);
        File.WriteAllText(retainedReportPath, renderedReport, Encoding.UTF8);

        return new(gateExecution, plan, retainedReportPath, renderedReport, rehearsedAtUtc);
    }

    /// <summary>Renders a dry-run rehearsal report.</summary>
    /// <param name="gateExecution">The package publication gate execution.</param>
    /// <param name="plan">The dry-run NuGet publication plan.</param>
    /// <param name="rehearsedAtUtc">The UTC rehearsal time.</param>
    /// <returns>The rendered dry-run rehearsal report.</returns>
    public static string RenderReport(
        SigtranPackagePublicationGateExecution gateExecution,
        SigtranNuGetPublishPlan plan,
        DateTimeOffset rehearsedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(gateExecution);
        ArgumentNullException.ThrowIfNull(plan);
        DateTimeOffset utcTime = rehearsedAtUtc.Offset == TimeSpan.Zero ? rehearsedAtUtc : rehearsedAtUtc.ToUniversalTime();
        StringBuilder builder = new();
        SigtranPackagePublicationRequest request = gateExecution.ChannelPolicyEvaluation.PublishGuardEvaluation.EvidenceAssembly.CredentialReadiness.ArtifactSet.Request;

        builder.AppendLine("# Package Publication Dry-Run Rehearsal");
        builder.AppendLine();
        builder.Append("| Field | Value |").AppendLine();
        builder.Append("| --- | --- |").AppendLine();
        builder.Append("| Package Version | ").Append(request.PackageVersion).AppendLine(" |");
        builder.Append("| Channel | ").Append(request.Channel.FeedName).AppendLine(" |");
        builder.Append("| Run Id | ").Append(request.RunId).AppendLine(" |");
        builder.Append("| Rehearsed At UTC | ").Append(utcTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).AppendLine(" |");
        builder.Append("| Gate Allows Publication | ").Append(gateExecution.IsPublicationAllowed).AppendLine(" |");
        builder.Append("| Dry-Run Safe | ").Append(plan.IsDryRunSafe).AppendLine(" |");
        builder.AppendLine();
        builder.AppendLine("## Commands");
        builder.AppendLine();

        foreach (string command in plan.Commands)
        {
            builder.Append("- `").Append(command).AppendLine("`");
        }

        return builder.ToString();
    }
}
