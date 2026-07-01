using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reference external peer lab runner command outcome.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerCommandOutcome
{
    /// <summary>Creates a reference peer lab runner command outcome.</summary>
    /// <param name="command">The command manifest entry.</param>
    /// <param name="startedUtc">The optional start timestamp.</param>
    /// <param name="completedUtc">The optional completion timestamp.</param>
    /// <param name="hasError">Whether the command has an error event.</param>
    public SigtranReferencePeerLabRunnerCommandOutcome(
        SigtranReferencePeerLabRunnerCommandEntry command,
        DateTimeOffset? startedUtc,
        DateTimeOffset? completedUtc,
        bool hasError)
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
        StartedUtc = startedUtc;
        CompletedUtc = completedUtc;
        HasError = hasError;
    }

    /// <summary>The command manifest entry.</summary>
    public SigtranReferencePeerLabRunnerCommandEntry Command { get; }

    /// <summary>The optional start timestamp.</summary>
    public DateTimeOffset? StartedUtc { get; }

    /// <summary>The optional completion timestamp.</summary>
    public DateTimeOffset? CompletedUtc { get; }

    /// <summary>Whether the command has an error event.</summary>
    public bool HasError { get; }

    /// <summary>The command duration when timestamps are complete.</summary>
    public TimeSpan? Duration => StartedUtc is null || CompletedUtc is null ? null : CompletedUtc.Value - StartedUtc.Value;

    /// <summary>Whether the command passed.</summary>
    public bool Passed => StartedUtc is not null && CompletedUtc is not null && !HasError;

    /// <summary>Formats a compact command outcome summary.</summary>
    /// <returns>The command outcome summary.</returns>
    public string Describe()
    {
        return $"sequence={Command.Sequence} kind={Command.Command.Kind} started={StartedUtc is not null} completed={CompletedUtc is not null} error={HasError} passed={Passed}";
    }
}

/// <summary>
/// Describes reference external peer lab runner command outcome aggregation.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerCommandOutcomeReport
{
    private readonly SigtranReferencePeerLabRunnerCommandOutcome[] _outcomes;

    /// <summary>Creates a reference peer lab runner command outcome report.</summary>
    /// <param name="commandManifest">The command manifest.</param>
    /// <param name="executionLog">The execution log.</param>
    /// <param name="outcomes">The command outcomes.</param>
    public SigtranReferencePeerLabRunnerCommandOutcomeReport(
        SigtranReferencePeerLabRunnerCommandManifest commandManifest,
        SigtranReferencePeerLabRunnerExecutionLog executionLog,
        IReadOnlyList<SigtranReferencePeerLabRunnerCommandOutcome> outcomes)
    {
        ArgumentNullException.ThrowIfNull(commandManifest);
        ArgumentNullException.ThrowIfNull(executionLog);
        ArgumentNullException.ThrowIfNull(outcomes);

        CommandManifest = commandManifest;
        ExecutionLog = executionLog;
        _outcomes = outcomes.Count == 0 ? throw new ArgumentException("At least one command outcome is required.", nameof(outcomes)) : outcomes.ToArray();
    }

    /// <summary>The command manifest.</summary>
    public SigtranReferencePeerLabRunnerCommandManifest CommandManifest { get; }

    /// <summary>The execution log.</summary>
    public SigtranReferencePeerLabRunnerExecutionLog ExecutionLog { get; }

    /// <summary>The command outcomes.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerCommandOutcome> Outcomes => _outcomes.ToArray();

    /// <summary>Whether every command passed and the log lifecycle is complete.</summary>
    public bool Passed => ExecutionLog.Complete && _outcomes.All(static outcome => outcome.Passed);

    /// <summary>The failed command kinds.</summary>
    public IReadOnlyList<SigtranReferencePeerLabCommandKind> FailedCommandKinds => _outcomes
        .Where(static outcome => !outcome.Passed)
        .Select(static outcome => outcome.Command.Command.Kind)
        .ToArray();

    /// <summary>Renders a Markdown command outcome report.</summary>
    /// <returns>The Markdown command outcome report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Command Outcomes");
        builder.AppendLine();
        builder.AppendLine($"Run: `{CommandManifest.InputBundle.Workspace.RunManifest.RunId}`");
        builder.AppendLine($"Passed: `{Passed}`");
        builder.AppendLine();
        builder.AppendLine("## Commands");

        foreach (SigtranReferencePeerLabRunnerCommandOutcome outcome in _outcomes.OrderBy(static outcome => outcome.Command.Sequence))
        {
            builder.Append("- ");
            builder.Append(outcome.Command.Sequence);
            builder.Append(". ");
            builder.Append(outcome.Command.Command.Name);
            builder.Append(": ");
            builder.AppendLine(outcome.Passed ? "passed" : "failed");
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact command outcome report summary.</summary>
    /// <returns>The command outcome report summary.</returns>
    public string Describe()
    {
        return $"run={CommandManifest.InputBundle.Workspace.RunManifest.RunId} outcomes={_outcomes.Length} failed={FailedCommandKinds.Count} passed={Passed}";
    }
}

/// <summary>
/// Provides reference external peer lab runner command outcome helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerCommandOutcomes
{
    /// <summary>Aggregates command outcomes from a command manifest and execution log.</summary>
    /// <param name="commandManifest">The command manifest.</param>
    /// <param name="executionLog">The execution log.</param>
    /// <returns>The command outcome report.</returns>
    public static SigtranReferencePeerLabRunnerCommandOutcomeReport FromLog(
        SigtranReferencePeerLabRunnerCommandManifest commandManifest,
        SigtranReferencePeerLabRunnerExecutionLog executionLog)
    {
        ArgumentNullException.ThrowIfNull(commandManifest);
        ArgumentNullException.ThrowIfNull(executionLog);

        List<SigtranReferencePeerLabRunnerCommandOutcome> outcomes = [];
        foreach (SigtranReferencePeerLabRunnerCommandEntry command in commandManifest.Commands)
        {
            DateTimeOffset? started = executionLog.Entries
                .Where(entry => entry.CommandKind == command.Command.Kind && entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.CommandStarted)
                .Select(static entry => (DateTimeOffset?)entry.TimestampUtc)
                .FirstOrDefault();
            DateTimeOffset? completed = executionLog.Entries
                .Where(entry => entry.CommandKind == command.Command.Kind && entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.CommandCompleted)
                .Select(static entry => (DateTimeOffset?)entry.TimestampUtc)
                .FirstOrDefault();
            bool hasError = executionLog.Entries.Any(entry => entry.CommandKind == command.Command.Kind && entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.Error);
            outcomes.Add(new(command, started, completed, hasError));
        }

        return new(commandManifest, executionLog, outcomes);
    }
}
