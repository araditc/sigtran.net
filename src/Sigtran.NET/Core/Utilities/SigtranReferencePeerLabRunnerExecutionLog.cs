using System.Text;
using System.Text.Json;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab runner execution log event kinds.
/// </summary>
public enum SigtranReferencePeerLabRunnerLogEventKind
{
    /// <summary>The runner started.</summary>
    Started,

    /// <summary>Informational runner event.</summary>
    Information,

    /// <summary>A command started.</summary>
    CommandStarted,

    /// <summary>A command completed.</summary>
    CommandCompleted,

    /// <summary>An artifact event was recorded.</summary>
    Artifact,

    /// <summary>A warning event was recorded.</summary>
    Warning,

    /// <summary>An error event was recorded.</summary>
    Error,

    /// <summary>The runner completed.</summary>
    Completed
}

/// <summary>
/// Describes one reference external peer lab runner execution log entry.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerExecutionLogEntry
{
    /// <summary>Creates a reference peer lab runner execution log entry.</summary>
    /// <param name="timestampUtc">The UTC timestamp.</param>
    /// <param name="kind">The event kind.</param>
    /// <param name="message">The event message.</param>
    /// <param name="commandKind">The optional command kind.</param>
    /// <param name="artifactPath">The optional artifact path.</param>
    public SigtranReferencePeerLabRunnerExecutionLogEntry(
        DateTimeOffset timestampUtc,
        SigtranReferencePeerLabRunnerLogEventKind kind,
        string message,
        SigtranReferencePeerLabCommandKind? commandKind = null,
        string? artifactPath = null)
    {
        TimestampUtc = timestampUtc;
        Kind = kind;
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Log message is required.", nameof(message)) : message;
        CommandKind = commandKind;
        ArtifactPath = string.IsNullOrWhiteSpace(artifactPath) ? null : artifactPath;
    }

    /// <summary>The UTC timestamp.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>The event kind.</summary>
    public SigtranReferencePeerLabRunnerLogEventKind Kind { get; }

    /// <summary>The event message.</summary>
    public string Message { get; }

    /// <summary>The optional command kind.</summary>
    public SigtranReferencePeerLabCommandKind? CommandKind { get; }

    /// <summary>The optional artifact path.</summary>
    public string? ArtifactPath { get; }

    /// <summary>Renders the log entry as one JSON line.</summary>
    /// <returns>The JSON line.</returns>
    public string RenderJsonLine()
    {
        return JsonSerializer.Serialize(new
        {
            timestampUtc = TimestampUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            kind = Kind.ToString(),
            message = Message,
            commandKind = CommandKind?.ToString(),
            artifactPath = ArtifactPath
        });
    }

    /// <summary>Formats a compact log entry summary.</summary>
    /// <returns>The log entry summary.</returns>
    public string Describe()
    {
        return $"timestamp={TimestampUtc:O} kind={Kind} command={CommandKind?.ToString() ?? "none"}";
    }
}

/// <summary>
/// Describes reference external peer lab runner execution logs.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerExecutionLog
{
    private readonly SigtranReferencePeerLabRunnerExecutionLogEntry[] _entries;

    /// <summary>Creates a reference peer lab runner execution log.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="entries">The log entries.</param>
    public SigtranReferencePeerLabRunnerExecutionLog(
        string runId,
        IReadOnlyList<SigtranReferencePeerLabRunnerExecutionLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        _entries = entries.Count == 0 ? throw new ArgumentException("At least one log entry is required.", nameof(entries)) : entries.ToArray();
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The log entries.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerExecutionLogEntry> Entries => _entries.ToArray();

    /// <summary>Whether the log contains an error event.</summary>
    public bool HasErrors => _entries.Any(static entry => entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.Error);

    /// <summary>Whether the log has runner start and completion events.</summary>
    public bool HasLifecycle => _entries.Any(static entry => entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.Started)
        && _entries.Any(static entry => entry.Kind == SigtranReferencePeerLabRunnerLogEventKind.Completed);

    /// <summary>Whether the log is complete and error-free.</summary>
    public bool Complete => HasLifecycle && !HasErrors;

    /// <summary>Renders the execution log as JSON lines.</summary>
    /// <returns>The JSON lines.</returns>
    public string RenderJsonLines()
    {
        StringBuilder builder = new();
        foreach (SigtranReferencePeerLabRunnerExecutionLogEntry entry in _entries)
        {
            builder.AppendLine(entry.RenderJsonLine());
        }

        return builder.ToString();
    }

    /// <summary>Renders the execution log as Markdown.</summary>
    /// <returns>The Markdown log.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Execution Log");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Complete: `{Complete}`");
        builder.AppendLine();
        builder.AppendLine("## Entries");
        foreach (SigtranReferencePeerLabRunnerExecutionLogEntry entry in _entries)
        {
            builder.Append("- ");
            builder.Append(entry.TimestampUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(" ");
            builder.Append(entry.Kind);
            builder.Append(": ");
            builder.AppendLine(entry.Message);
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact execution log summary.</summary>
    /// <returns>The execution log summary.</returns>
    public string Describe()
    {
        return $"run={RunId} entries={_entries.Length} lifecycle={HasLifecycle} errors={HasErrors} complete={Complete}";
    }
}

/// <summary>
/// Provides reference external peer lab runner execution log helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerExecutionLogs
{
    /// <summary>Creates a passing execution log from a command manifest.</summary>
    /// <param name="commandManifest">The runner command manifest.</param>
    /// <param name="startedUtc">The runner start timestamp.</param>
    /// <returns>The execution log.</returns>
    public static SigtranReferencePeerLabRunnerExecutionLog CreatePassing(
        SigtranReferencePeerLabRunnerCommandManifest commandManifest,
        DateTimeOffset startedUtc)
    {
        ArgumentNullException.ThrowIfNull(commandManifest);

        List<SigtranReferencePeerLabRunnerExecutionLogEntry> entries =
        [
            new(startedUtc, SigtranReferencePeerLabRunnerLogEventKind.Started, "Runner started.")
        ];

        int index = 1;
        foreach (SigtranReferencePeerLabRunnerCommandEntry command in commandManifest.Commands)
        {
            DateTimeOffset commandStart = startedUtc.AddSeconds(index++);
            entries.Add(new(commandStart, SigtranReferencePeerLabRunnerLogEventKind.CommandStarted, command.Command.Name, command.Command.Kind));
            entries.Add(new(commandStart.AddMilliseconds(500), SigtranReferencePeerLabRunnerLogEventKind.CommandCompleted, command.Command.Name, command.Command.Kind));
        }

        entries.Add(new(startedUtc.AddSeconds(index), SigtranReferencePeerLabRunnerLogEventKind.Completed, "Runner completed."));
        return new(commandManifest.InputBundle.Workspace.RunManifest.RunId, entries);
    }
}
