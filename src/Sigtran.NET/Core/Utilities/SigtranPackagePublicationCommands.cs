using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a guarded package publication command kind.
/// </summary>
public enum SigtranPackagePublicationCommandKind
{
    /// <summary>Validate that the retained publication gate allows publication.</summary>
    ValidatePublicationGate,

    /// <summary>Retain the dry-run rehearsal report.</summary>
    RetainDryRunRehearsal,

    /// <summary>Pack the NuGet package.</summary>
    PackPackage,

    /// <summary>Verify the NuGet package before upload.</summary>
    VerifyPackage,

    /// <summary>Publish the NuGet package behind guarded release inputs.</summary>
    PublishPackage
}

/// <summary>
/// Describes one guarded package publication command.
/// </summary>
public sealed class SigtranPackagePublicationCommand
{
    /// <summary>Creates a guarded package publication command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="order">The deterministic command order.</param>
    /// <param name="name">The command name.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="requiresPublicationGate">Whether the command requires a positive publication gate.</param>
    public SigtranPackagePublicationCommand(
        SigtranPackagePublicationCommandKind kind,
        int order,
        string name,
        string commandText,
        bool requiresPublicationGate)
    {
        Kind = kind;
        Order = order > 0 ? order : throw new ArgumentOutOfRangeException(nameof(order));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException("Command text is required.", nameof(commandText)) : commandText;
        RequiresPublicationGate = requiresPublicationGate;
    }

    /// <summary>The command kind.</summary>
    public SigtranPackagePublicationCommandKind Kind { get; }

    /// <summary>The deterministic command order.</summary>
    public int Order { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string CommandText { get; }

    /// <summary>Whether the command requires a positive publication gate.</summary>
    public bool RequiresPublicationGate { get; }

    /// <summary>Whether the command contract is complete.</summary>
    public bool IsReady => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(CommandText);
}

/// <summary>
/// Describes a guarded package publication command plan.
/// </summary>
public sealed class SigtranPackagePublicationCommandPlan
{
    /// <summary>Creates a guarded package publication command plan.</summary>
    /// <param name="rehearsal">The retained dry-run rehearsal.</param>
    /// <param name="commands">The ordered publication commands.</param>
    public SigtranPackagePublicationCommandPlan(
        SigtranPackagePublicationDryRunRehearsal rehearsal,
        IReadOnlyList<SigtranPackagePublicationCommand> commands)
    {
        Rehearsal = rehearsal ?? throw new ArgumentNullException(nameof(rehearsal));
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one package publication command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The retained dry-run rehearsal.</summary>
    public SigtranPackagePublicationDryRunRehearsal Rehearsal { get; }

    /// <summary>The ordered publication commands.</summary>
    public IReadOnlyList<SigtranPackagePublicationCommand> Commands { get; }

    /// <summary>Whether command order is deterministic and contiguous.</summary>
    public bool UsesDeterministicOrder => Commands
        .OrderBy(static command => command.Order)
        .Select(static command => command.Order)
        .SequenceEqual(Enumerable.Range(1, Commands.Count));

    /// <summary>Whether every required command kind is present.</summary>
    public bool CoversRequiredCommandKinds => Enum.GetValues<SigtranPackagePublicationCommandKind>()
        .All(kind => Commands.Any(command => command.Kind == kind));

    /// <summary>Whether the publish command requires a positive publication gate and environment secret.</summary>
    public bool PublishCommandIsGuarded => Commands
        .Where(static command => command.Kind == SigtranPackagePublicationCommandKind.PublishPackage)
        .All(static command => command.RequiresPublicationGate
            && command.CommandText.Contains("${NUGET_API_KEY:", StringComparison.Ordinal));

    /// <summary>Whether the command plan is ready for script materialization.</summary>
    public bool IsReady => Rehearsal.IsReadyForRetention
        && UsesDeterministicOrder
        && CoversRequiredCommandKinds
        && PublishCommandIsGuarded
        && Commands.All(static command => command.IsReady);

    /// <summary>Formats a compact package publication command plan summary.</summary>
    /// <returns>The package publication command plan summary.</returns>
    public string Describe()
    {
        return $"packagePublicationCommandsReady={IsReady} commands={Commands.Count} publishGuarded={PublishCommandIsGuarded}";
    }
}

/// <summary>
/// Describes a materialized guarded package publication command script.
/// </summary>
public sealed class SigtranPackagePublicationCommandMaterialization
{
    /// <summary>Creates a guarded package publication command materialization.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="renderedScript">The rendered script content.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    public SigtranPackagePublicationCommandMaterialization(
        SigtranPackagePublicationCommandPlan commandPlan,
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
    public SigtranPackagePublicationCommandPlan CommandPlan { get; }

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

    /// <summary>Whether the script includes the publication gate environment guard.</summary>
    public bool IncludesPublicationGateGuard => RenderedScript.Contains("SIGTRAN_PUBLICATION_GATE_ALLOWED", StringComparison.Ordinal);

    /// <summary>Whether the script includes every command text.</summary>
    public bool IncludesAllCommands => CommandPlan.Commands.All(command => RenderedScript.Contains(command.CommandText, StringComparison.Ordinal));

    /// <summary>Whether command materialization is ready for release workflow retention.</summary>
    public bool IsReady => CommandPlan.IsReady
        && ScriptExists
        && ScriptSizeBytes > 0
        && HasUtcMaterializationTime
        && IncludesPublicationGateGuard
        && IncludesAllCommands;

    /// <summary>Formats a compact command materialization summary.</summary>
    /// <returns>The command materialization summary.</returns>
    public string Describe()
    {
        return $"packagePublicationCommandMaterializationReady={IsReady} commands={CommandPlan.Commands.Count} script={ScriptPath}";
    }
}

/// <summary>
/// Provides guarded package publication command helpers.
/// </summary>
public static class SigtranPackagePublicationCommands
{
    /// <summary>Creates a guarded package publication command plan from a dry-run rehearsal.</summary>
    /// <param name="rehearsal">The retained dry-run rehearsal.</param>
    /// <returns>The guarded package publication command plan.</returns>
    public static SigtranPackagePublicationCommandPlan CreateGuardedPlan(SigtranPackagePublicationDryRunRehearsal rehearsal)
    {
        ArgumentNullException.ThrowIfNull(rehearsal);
        SigtranNuGetPublishPlan publishPlan = SigtranNuGetPublishPlans.CreatePublish();

        return new(
            rehearsal,
            [
                new(SigtranPackagePublicationCommandKind.ValidatePublicationGate, 1, "validate-publication-gate", "test \"${SIGTRAN_PUBLICATION_GATE_ALLOWED:-false}\" = \"true\"", requiresPublicationGate: false),
                new(SigtranPackagePublicationCommandKind.RetainDryRunRehearsal, 2, "retain-dry-run-rehearsal", $"test -s \"{rehearsal.ReportPath}\"", requiresPublicationGate: false),
                new(SigtranPackagePublicationCommandKind.PackPackage, 3, "pack-package", "dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release", requiresPublicationGate: true),
                new(SigtranPackagePublicationCommandKind.VerifyPackage, 4, "verify-package", "dotnet nuget verify src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg", requiresPublicationGate: true),
                new(SigtranPackagePublicationCommandKind.PublishPackage, 5, "publish-package", publishPlan.Commands[0].Replace("<secret>", "${NUGET_API_KEY:?missing NuGet API key}", StringComparison.Ordinal), requiresPublicationGate: true)
            ]);
    }

    /// <summary>Writes a guarded publication command script.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <param name="scriptPath">The retained script path.</param>
    /// <param name="materializedAtUtc">The UTC materialization time.</param>
    /// <returns>The guarded publication command materialization.</returns>
    public static SigtranPackagePublicationCommandMaterialization WriteScript(
        SigtranPackagePublicationCommandPlan commandPlan,
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

    /// <summary>Renders a guarded publication command script.</summary>
    /// <param name="commandPlan">The source command plan.</param>
    /// <returns>The rendered guarded publication command script.</returns>
    public static string RenderScript(SigtranPackagePublicationCommandPlan commandPlan)
    {
        ArgumentNullException.ThrowIfNull(commandPlan);
        StringBuilder builder = new();

        builder.AppendLine("#!/usr/bin/env bash");
        builder.AppendLine("set -euo pipefail");
        builder.AppendLine();
        builder.AppendLine("if [ \"${SIGTRAN_PUBLICATION_GATE_ALLOWED:-false}\" != \"true\" ]; then");
        builder.AppendLine("  echo \"Publication gate is not open.\" >&2");
        builder.AppendLine("  exit 1");
        builder.AppendLine("fi");

        foreach (SigtranPackagePublicationCommand command in commandPlan.Commands.OrderBy(static item => item.Order))
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
