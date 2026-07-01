using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies an approved production evidence run command kind.
/// </summary>
public enum SigtranReleaseEvidenceApprovalCommandKind
{
    /// <summary>Create the approved run target.</summary>
    CreateRunTarget,

    /// <summary>Build the approval checklist.</summary>
    BuildApprovalChecklist,

    /// <summary>Record reviewer approvals.</summary>
    RecordReviewerApprovals,

    /// <summary>Write the approval report.</summary>
    WriteApprovalReport,

    /// <summary>Create the promotion package.</summary>
    CreatePromotionPackage,

    /// <summary>Create the publication handoff.</summary>
    CreatePublicationHandoff,

    /// <summary>Evaluate the publication handoff gate.</summary>
    EvaluateHandoffGate,

    /// <summary>Write the approval audit trail.</summary>
    WriteApprovalAuditTrail
}

/// <summary>
/// Describes one approved production evidence run command.
/// </summary>
public sealed class SigtranReleaseEvidenceApprovalCommand
{
    /// <summary>Creates an approved production evidence run command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="order">The deterministic command order.</param>
    /// <param name="name">The command name.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="producesArtifact">Whether the command produces a retained artifact.</param>
    public SigtranReleaseEvidenceApprovalCommand(
        SigtranReleaseEvidenceApprovalCommandKind kind,
        int order,
        string name,
        string commandText,
        bool producesArtifact)
    {
        Kind = kind;
        Order = order > 0 ? order : throw new ArgumentOutOfRangeException(nameof(order));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException("Command text is required.", nameof(commandText)) : commandText;
        ProducesArtifact = producesArtifact;
    }

    /// <summary>The command kind.</summary>
    public SigtranReleaseEvidenceApprovalCommandKind Kind { get; }

    /// <summary>The deterministic command order.</summary>
    public int Order { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string CommandText { get; }

    /// <summary>Whether the command produces a retained artifact.</summary>
    public bool ProducesArtifact { get; }

    /// <summary>Whether the command contract is complete.</summary>
    public bool IsReady => Order > 0
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(CommandText);
}

/// <summary>
/// Describes an approved production evidence run command plan.
/// </summary>
public sealed class SigtranReleaseEvidenceApprovalCommandPlan
{
    /// <summary>Creates an approved production evidence run command plan.</summary>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="runId">The production evidence run id.</param>
    /// <param name="commands">The ordered commands.</param>
    public SigtranReleaseEvidenceApprovalCommandPlan(
        string artifactRoot,
        string runId,
        IReadOnlyList<SigtranReleaseEvidenceApprovalCommand> commands)
    {
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one approval command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The production evidence run id.</summary>
    public string RunId { get; }

    /// <summary>The ordered commands.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceApprovalCommand> Commands { get; }

    /// <summary>Whether command order is deterministic and contiguous.</summary>
    public bool UsesDeterministicOrder => Commands
        .OrderBy(static command => command.Order)
        .Select(static command => command.Order)
        .SequenceEqual(Enumerable.Range(1, Commands.Count));

    /// <summary>Whether every required command kind is present.</summary>
    public bool CoversRequiredCommandKinds => Enum.GetValues<SigtranReleaseEvidenceApprovalCommandKind>()
        .All(kind => Commands.Any(command => command.Kind == kind));

    /// <summary>Whether artifact-producing commands are present.</summary>
    public bool ProducesRequiredArtifacts => Commands.Any(static command => command.Kind == SigtranReleaseEvidenceApprovalCommandKind.WriteApprovalReport && command.ProducesArtifact)
        && Commands.Any(static command => command.Kind == SigtranReleaseEvidenceApprovalCommandKind.CreatePromotionPackage && command.ProducesArtifact)
        && Commands.Any(static command => command.Kind == SigtranReleaseEvidenceApprovalCommandKind.WriteApprovalAuditTrail && command.ProducesArtifact);

    /// <summary>Whether the command plan is ready for script materialization.</summary>
    public bool IsReady => UsesDeterministicOrder
        && CoversRequiredCommandKinds
        && ProducesRequiredArtifacts
        && Commands.All(static command => command.IsReady);

    /// <summary>Formats a compact approval command plan summary.</summary>
    /// <returns>The approval command plan summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceApprovalCommandsReady={IsReady} commands={Commands.Count} runId={RunId}";
    }
}

/// <summary>
/// Describes a materialized approved production evidence run command script.
/// </summary>
public sealed class SigtranReleaseEvidenceApprovalCommandMaterialization
{
    /// <summary>Creates a materialized approval command script result.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="renderedScript">The rendered script content.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    public SigtranReleaseEvidenceApprovalCommandMaterialization(
        SigtranReleaseEvidenceApprovalCommandPlan commandPlan,
        string scriptPath,
        string renderedScript,
        DateTimeOffset materializedAtUtc)
    {
        CommandPlan = commandPlan ?? throw new ArgumentNullException(nameof(commandPlan));
        ScriptPath = string.IsNullOrWhiteSpace(scriptPath) ? throw new ArgumentException("Script path is required.", nameof(scriptPath)) : scriptPath;
        RenderedScript = string.IsNullOrWhiteSpace(renderedScript) ? throw new ArgumentException("Rendered script is required.", nameof(renderedScript)) : renderedScript;
        MaterializedAtUtc = materializedAtUtc.Offset == TimeSpan.Zero ? materializedAtUtc : materializedAtUtc.ToUniversalTime();
    }

    /// <summary>The source command plan.</summary>
    public SigtranReleaseEvidenceApprovalCommandPlan CommandPlan { get; }

    /// <summary>The retained script path.</summary>
    public string ScriptPath { get; }

    /// <summary>The rendered script content.</summary>
    public string RenderedScript { get; }

    /// <summary>The UTC materialization time.</summary>
    public DateTimeOffset MaterializedAtUtc { get; }

    /// <summary>Whether the script exists on disk.</summary>
    public bool ScriptExists => File.Exists(ScriptPath);

    /// <summary>The retained script size in bytes.</summary>
    public long ScriptSizeBytes => ScriptExists ? new FileInfo(ScriptPath).Length : 0;

    /// <summary>Whether the materialization time is normalized to UTC.</summary>
    public bool HasUtcMaterializationTime => MaterializedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the rendered script includes the run id.</summary>
    public bool IncludesRunId => RenderedScript.Contains(CommandPlan.RunId, StringComparison.Ordinal);

    /// <summary>Whether the rendered script includes every command text.</summary>
    public bool IncludesAllCommands => CommandPlan.Commands.All(command => RenderedScript.Contains(command.CommandText, StringComparison.Ordinal));

    /// <summary>Whether the command materialization is ready.</summary>
    public bool IsReady => CommandPlan.IsReady
        && ScriptExists
        && ScriptSizeBytes > 0
        && HasUtcMaterializationTime
        && IncludesRunId
        && IncludesAllCommands;

    /// <summary>Formats a compact approval command materialization summary.</summary>
    /// <returns>The approval command materialization summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceApprovalCommandMaterializationReady={IsReady} commands={CommandPlan.Commands.Count} script={ScriptPath}";
    }
}

/// <summary>
/// Provides approved production evidence run command helpers.
/// </summary>
public static class SigtranReleaseEvidenceApprovalCommands
{
    /// <summary>Creates the default approved production evidence run command plan.</summary>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="runId">The production evidence run id.</param>
    /// <returns>The default approval command plan.</returns>
    public static SigtranReleaseEvidenceApprovalCommandPlan CreateDefaultPlan(
        string artifactRoot,
        string runId)
    {
        string root = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        string id = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;

        return new(
            root,
            id,
            [
                new(SigtranReleaseEvidenceApprovalCommandKind.CreateRunTarget, 1, "create-run-target", $"sigtran evidence approval create-run-target --artifact-root {root} --run-id {id}", producesArtifact: false),
                new(SigtranReleaseEvidenceApprovalCommandKind.BuildApprovalChecklist, 2, "build-approval-checklist", $"sigtran evidence approval build-checklist --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.RecordReviewerApprovals, 3, "record-reviewer-approvals", $"sigtran evidence approval record-reviewers --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.WriteApprovalReport, 4, "write-approval-report", $"sigtran evidence approval write-report --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.CreatePromotionPackage, 5, "create-promotion-package", $"sigtran evidence approval create-promotion-package --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.CreatePublicationHandoff, 6, "create-publication-handoff", $"sigtran evidence approval create-publication-handoff --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.EvaluateHandoffGate, 7, "evaluate-handoff-gate", $"sigtran evidence approval evaluate-handoff-gate --artifact-root {root} --run-id {id}", producesArtifact: true),
                new(SigtranReleaseEvidenceApprovalCommandKind.WriteApprovalAuditTrail, 8, "write-approval-audit-trail", $"sigtran evidence approval write-audit-trail --artifact-root {root} --run-id {id}", producesArtifact: true)
            ]);
    }

    /// <summary>Writes a shell command script from an approval command plan.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    /// <returns>The materialized command script result.</returns>
    public static SigtranReleaseEvidenceApprovalCommandMaterialization WriteScript(
        SigtranReleaseEvidenceApprovalCommandPlan commandPlan,
        string scriptPath,
        DateTimeOffset materializedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(commandPlan);
        string retainedScriptPath = string.IsNullOrWhiteSpace(scriptPath) ? throw new ArgumentException("Script path is required.", nameof(scriptPath)) : scriptPath;
        string? directory = Path.GetDirectoryName(retainedScriptPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string renderedScript = RenderScript(commandPlan);
        File.WriteAllText(retainedScriptPath, renderedScript, Encoding.UTF8);
        return new(commandPlan, retainedScriptPath, renderedScript, materializedAtUtc);
    }

    /// <summary>Renders a shell command script from an approval command plan.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <returns>The rendered command script.</returns>
    public static string RenderScript(SigtranReleaseEvidenceApprovalCommandPlan commandPlan)
    {
        ArgumentNullException.ThrowIfNull(commandPlan);
        StringBuilder builder = new();

        builder.AppendLine("#!/usr/bin/env bash");
        builder.AppendLine("set -euo pipefail");
        builder.AppendLine();
        builder.Append("# Sigtran.NET approved production evidence run: ").AppendLine(commandPlan.RunId);

        foreach (SigtranReleaseEvidenceApprovalCommand command in commandPlan.Commands.OrderBy(static item => item.Order))
        {
            builder.AppendLine();
            builder.Append("# ")
                .Append(command.Order)
                .Append(". ")
                .AppendLine(command.Name);
            builder.AppendLine(command.CommandText);
        }

        return builder.ToString();
    }
}
