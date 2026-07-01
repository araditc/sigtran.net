using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab step status values.
/// </summary>
public enum SigtranReferencePeerLabStepStatus
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
/// Describes one reference external peer lab step result.
/// </summary>
public sealed class SigtranReferencePeerLabStepResult
{
    /// <summary>Creates a reference peer lab step result.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="status">The step status.</param>
    /// <param name="startedUtc">The start timestamp.</param>
    /// <param name="completedUtc">The optional completion timestamp.</param>
    /// <param name="logPath">The optional retained log path.</param>
    public SigtranReferencePeerLabStepResult(
        SigtranReferencePeerLabCommandKind kind,
        SigtranReferencePeerLabStepStatus status,
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
    public SigtranReferencePeerLabCommandKind Kind { get; }

    /// <summary>The step status.</summary>
    public SigtranReferencePeerLabStepStatus Status { get; }

    /// <summary>The start timestamp.</summary>
    public DateTimeOffset StartedUtc { get; }

    /// <summary>The optional completion timestamp.</summary>
    public DateTimeOffset? CompletedUtc { get; }

    /// <summary>The optional retained log path.</summary>
    public string? LogPath { get; }

    /// <summary>Whether the step passed.</summary>
    public bool Passed => Status == SigtranReferencePeerLabStepStatus.Passed;

    /// <summary>The step duration when completion is known.</summary>
    public TimeSpan? Duration => CompletedUtc is null ? null : CompletedUtc.Value - StartedUtc;
}

/// <summary>
/// Describes a reference external peer lab run report.
/// </summary>
public sealed class SigtranReferencePeerLabRunReport
{
    private readonly SigtranReferencePeerLabStepResult[] _steps;

    /// <summary>Creates a reference peer lab run report.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="steps">The step results.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    public SigtranReferencePeerLabRunReport(
        SigtranReferencePeerLabRunManifest runManifest,
        IReadOnlyList<SigtranReferencePeerLabStepResult> steps,
        SigtranReferencePeerLabComparisonReport comparisonReport)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(comparisonReport);

        RunManifest = runManifest;
        _steps = steps.Count == 0 ? throw new ArgumentException("At least one step result is required.", nameof(steps)) : steps.ToArray();
        ComparisonReport = comparisonReport;
    }

    /// <summary>The run manifest.</summary>
    public SigtranReferencePeerLabRunManifest RunManifest { get; }

    /// <summary>The step results.</summary>
    public IReadOnlyList<SigtranReferencePeerLabStepResult> Steps => _steps.ToArray();

    /// <summary>The comparison report.</summary>
    public SigtranReferencePeerLabComparisonReport ComparisonReport { get; }

    /// <summary>Whether every command step passed and comparison passed.</summary>
    public bool Passed => RunManifest.CommandPlan.Commands.All(command => _steps.Any(step => step.Kind == command.Kind && step.Passed))
        && ComparisonReport.Passed;

    /// <summary>The run report artifact path.</summary>
    public string ReportArtifactPath => RunManifest.ArtifactPlan.Items
        .First(item => item.Kind == SigtranReferencePeerLabArtifactKind.RunReport)
        .Path;

    /// <summary>Renders a Markdown run report.</summary>
    /// <returns>The Markdown run report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Run Report");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunManifest.RunId}`");
        builder.AppendLine($"Passed: `{Passed}`");
        builder.AppendLine($"Comparison: `{ComparisonReport.Passed}`");
        builder.AppendLine();
        builder.AppendLine("## Steps");

        foreach (SigtranReferencePeerLabStepResult step in _steps)
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
/// Provides reference external peer lab run report helpers.
/// </summary>
public static class SigtranReferencePeerLabRunReports
{
    /// <summary>Creates a passing run report from a manifest and comparison report.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="startedUtc">The first step start timestamp.</param>
    /// <returns>The passing run report.</returns>
    public static SigtranReferencePeerLabRunReport CreatePassing(
        SigtranReferencePeerLabRunManifest runManifest,
        SigtranReferencePeerLabComparisonReport comparisonReport,
        DateTimeOffset startedUtc)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(comparisonReport);

        List<SigtranReferencePeerLabStepResult> steps = [];
        int index = 0;
        foreach (SigtranReferencePeerLabCommand command in runManifest.CommandPlan.Commands)
        {
            DateTimeOffset start = startedUtc.AddSeconds(index);
            steps.Add(new(command.Kind, SigtranReferencePeerLabStepStatus.Passed, start, start.AddSeconds(1)));
            index++;
        }

        return new(runManifest, steps, comparisonReport);
    }
}
