using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a materialized filesystem-backed production evidence command script.
/// </summary>
public sealed class SigtranReleaseEvidenceFileSystemCommandMaterialization
{
    /// <summary>Creates a materialized filesystem-backed command script result.</summary>
    /// <param name="commandPlan">The source file verification command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="renderedScript">The rendered script content.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    public SigtranReleaseEvidenceFileSystemCommandMaterialization(
        SigtranReleaseEvidenceFileVerificationCommandPlan commandPlan,
        string scriptPath,
        string renderedScript,
        DateTimeOffset materializedAtUtc)
    {
        CommandPlan = commandPlan ?? throw new ArgumentNullException(nameof(commandPlan));
        ScriptPath = string.IsNullOrWhiteSpace(scriptPath) ? throw new ArgumentException("Script path is required.", nameof(scriptPath)) : scriptPath;
        RenderedScript = string.IsNullOrWhiteSpace(renderedScript) ? throw new ArgumentException("Rendered script is required.", nameof(renderedScript)) : renderedScript;
        MaterializedAtUtc = materializedAtUtc.Offset == TimeSpan.Zero ? materializedAtUtc : materializedAtUtc.ToUniversalTime();
    }

    /// <summary>The source file verification command plan.</summary>
    public SigtranReleaseEvidenceFileVerificationCommandPlan CommandPlan { get; }

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

    /// <summary>Whether the script materialization time is normalized to UTC.</summary>
    public bool HasUtcMaterializationTime => MaterializedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the rendered script includes the retained artifact root.</summary>
    public bool IncludesArtifactRoot => RenderedScript.Contains(CommandPlan.ArtifactRoot, StringComparison.Ordinal);

    /// <summary>Whether the rendered script includes every command from the command plan.</summary>
    public bool IncludesAllCommands => CommandPlan.Commands.All(command => RenderedScript.Contains(command.CommandText, StringComparison.Ordinal));

    /// <summary>Whether the rendered script includes the explicit promotion gate command.</summary>
    public bool IncludesPromotionGate => RenderedScript.Contains("evaluate-promotion-gate", StringComparison.Ordinal);

    /// <summary>Whether the command materialization is ready for retained execution evidence review.</summary>
    public bool IsReady => CommandPlan.IsReady
        && ScriptExists
        && ScriptSizeBytes > 0
        && HasUtcMaterializationTime
        && IncludesArtifactRoot
        && IncludesAllCommands
        && IncludesPromotionGate;

    /// <summary>Formats a compact command materialization summary.</summary>
    /// <returns>The command materialization summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceFileSystemCommandMaterializationReady={IsReady} commands={CommandPlan.Commands.Count} script={ScriptPath}";
    }
}

/// <summary>
/// Provides filesystem-backed production evidence command materialization helpers.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemCommandMaterializer
{
    /// <summary>Writes a shell command script from a file verification command plan.</summary>
    /// <param name="commandPlan">The source file verification command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    /// <returns>The materialized filesystem-backed command script result.</returns>
    public static SigtranReleaseEvidenceFileSystemCommandMaterialization WriteScript(
        SigtranReleaseEvidenceFileVerificationCommandPlan commandPlan,
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

    /// <summary>Renders a shell command script from a file verification command plan.</summary>
    /// <param name="commandPlan">The source file verification command plan.</param>
    /// <returns>The rendered command script.</returns>
    public static string RenderScript(SigtranReleaseEvidenceFileVerificationCommandPlan commandPlan)
    {
        ArgumentNullException.ThrowIfNull(commandPlan);
        StringBuilder builder = new();

        builder.AppendLine("#!/usr/bin/env bash");
        builder.AppendLine("set -euo pipefail");
        builder.AppendLine();
        builder.Append("# Sigtran.NET production evidence filesystem execution artifact root: ")
            .AppendLine(commandPlan.ArtifactRoot);

        foreach (SigtranReleaseEvidenceFileVerificationCommand command in commandPlan.Commands.OrderBy(static item => item.Order))
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
