using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained external peer lab step status values.
/// </summary>
public enum SigtranMaintainedPeerLabStepStatus
{
    /// <summary>The step has not run.</summary>
    Pending,

    /// <summary>The step passed.</summary>
    Passed,

    /// <summary>The step failed.</summary>
    Failed,

    /// <summary>The step was skipped.</summary>
    Skipped
}

/// <summary>
/// Describes one maintained external peer lab step result.
/// </summary>
public sealed class SigtranMaintainedPeerLabStepResult
{
    /// <summary>Creates a maintained peer lab step result.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="status">The step status.</param>
    /// <param name="startedUtc">The start timestamp.</param>
    /// <param name="completedUtc">The optional completion timestamp.</param>
    /// <param name="logPath">The optional retained log path.</param>
    public SigtranMaintainedPeerLabStepResult(
        SigtranMaintainedPeerLabCommandKind kind,
        SigtranMaintainedPeerLabStepStatus status,
        DateTimeOffset startedUtc,
        DateTimeOffset? completedUtc = null,
        string? logPath = null)
    {
        Kind = kind;
        Status = status;
        StartedUtc = startedUtc;
        CompletedUtc = completedUtc;
        LogPath = string.IsNullOrWhiteSpace(logPath) ? null : logPath;
    }

    /// <summary>The command kind.</summary>
    public SigtranMaintainedPeerLabCommandKind Kind { get; }

    /// <summary>The step status.</summary>
    public SigtranMaintainedPeerLabStepStatus Status { get; }

    /// <summary>The start timestamp.</summary>
    public DateTimeOffset StartedUtc { get; }

    /// <summary>The optional completion timestamp.</summary>
    public DateTimeOffset? CompletedUtc { get; }

    /// <summary>The optional retained log path.</summary>
    public string? LogPath { get; }

    /// <summary>Whether the step passed.</summary>
    public bool Passed => Status == SigtranMaintainedPeerLabStepStatus.Passed;

    /// <summary>The step duration when completion is known.</summary>
    public TimeSpan? Duration => CompletedUtc is null ? null : CompletedUtc.Value - StartedUtc;
}

/// <summary>
/// Describes a maintained external peer lab run report.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunReport
{
    private readonly SigtranMaintainedPeerLabStepResult[] _steps;

    /// <summary>Creates a maintained peer lab run report.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="steps">The step results.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    public SigtranMaintainedPeerLabRunReport(
        SigtranMaintainedPeerLabRunManifest runManifest,
        IReadOnlyList<SigtranMaintainedPeerLabStepResult> steps,
        SigtranMaintainedPeerLabComparisonReport comparisonReport)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(comparisonReport);

        RunManifest = runManifest;
        _steps = steps.Count == 0 ? throw new ArgumentException("At least one step result is required.", nameof(steps)) : steps.ToArray();
        ComparisonReport = comparisonReport;
    }

    /// <summary>The run manifest.</summary>
    public SigtranMaintainedPeerLabRunManifest RunManifest { get; }

    /// <summary>The step results.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabStepResult> Steps => _steps.ToArray();

    /// <summary>The comparison report.</summary>
    public SigtranMaintainedPeerLabComparisonReport ComparisonReport { get; }

    /// <summary>Whether every command step passed and comparison passed.</summary>
    public bool Passed => RunManifest.CommandPlan.Commands.All(command => _steps.Any(step => step.Kind == command.Kind && step.Passed))
        && ComparisonReport.Passed;

    /// <summary>The run report artifact path.</summary>
    public string ReportArtifactPath => RunManifest.ArtifactPlan.Items
        .First(item => item.Kind == SigtranMaintainedPeerLabArtifactKind.RunReport)
        .Path;

    /// <summary>Renders a Markdown run report.</summary>
    /// <returns>The Markdown run report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Run Report");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunManifest.RunId}`");
        builder.AppendLine($"Passed: `{Passed}`");
        builder.AppendLine($"Comparison: `{ComparisonReport.Passed}`");
        builder.AppendLine();
        builder.AppendLine("## Steps");

        foreach (SigtranMaintainedPeerLabStepResult step in _steps)
        {
            builder.Append("- ");
            builder.Append(step.Kind);
            builder.Append(": ");
            builder.Append(step.Status);
            if (step.Duration is not null)
            {
                builder.Append(" duration=");
                builder.Append(step.Duration.Value.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                builder.Append('s');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact run report summary.</summary>
    /// <returns>The run report summary.</returns>
    public string Describe()
    {
        return $"run={RunManifest.RunId} steps={_steps.Length} comparison={ComparisonReport.Passed} passed={Passed}";
    }
}

/// <summary>
/// Provides maintained external peer lab run report helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunReports
{
    /// <summary>Creates a passing run report from a manifest and comparison report.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="startedUtc">The first step start timestamp.</param>
    /// <returns>The passing run report.</returns>
    public static SigtranMaintainedPeerLabRunReport CreatePassing(
        SigtranMaintainedPeerLabRunManifest runManifest,
        SigtranMaintainedPeerLabComparisonReport comparisonReport,
        DateTimeOffset startedUtc)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(comparisonReport);

        List<SigtranMaintainedPeerLabStepResult> steps = [];
        int index = 0;
        foreach (SigtranMaintainedPeerLabCommand command in runManifest.CommandPlan.Commands)
        {
            DateTimeOffset start = startedUtc.AddSeconds(index);
            steps.Add(new(command.Kind, SigtranMaintainedPeerLabStepStatus.Passed, start, start.AddSeconds(1)));
            index++;
        }

        return new(runManifest, steps, comparisonReport);
    }
}
