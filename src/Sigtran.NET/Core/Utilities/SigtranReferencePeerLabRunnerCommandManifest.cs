using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reference external peer lab runner command manifest entry.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerCommandEntry
{
    private readonly string[] _expectedOutputPaths;

    /// <summary>Creates a reference peer lab runner command entry.</summary>
    /// <param name="sequence">The one-based execution sequence.</param>
    /// <param name="command">The command contract.</param>
    /// <param name="expectedOutputPaths">The expected output paths.</param>
    public SigtranReferencePeerLabRunnerCommandEntry(
        int sequence,
        SigtranReferencePeerLabCommand command,
        IReadOnlyList<string> expectedOutputPaths)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedOutputPaths);
        Sequence = sequence <= 0 ? throw new ArgumentOutOfRangeException(nameof(sequence)) : sequence;
        Command = command;
        _expectedOutputPaths = expectedOutputPaths.ToArray();
    }

    /// <summary>The one-based execution sequence.</summary>
    public int Sequence { get; }

    /// <summary>The command contract.</summary>
    public SigtranReferencePeerLabCommand Command { get; }

    /// <summary>The expected output paths.</summary>
    public IReadOnlyList<string> ExpectedOutputPaths => _expectedOutputPaths.ToArray();

    /// <summary>Whether this entry has output mappings for every expected artifact kind.</summary>
    public bool CoversExpectedArtifacts => Command.ExpectedArtifactKinds.Count == _expectedOutputPaths.Length;

    /// <summary>Formats a compact command entry summary.</summary>
    /// <returns>The command entry summary.</returns>
    public string Describe()
    {
        return $"sequence={Sequence} kind={Command.Kind} outputs={_expectedOutputPaths.Length}";
    }
}

/// <summary>
/// Describes an executable command manifest for a reference external peer lab runner.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerCommandManifest
{
    private readonly SigtranReferencePeerLabRunnerCommandEntry[] _commands;

    /// <summary>Creates a reference peer lab runner command manifest.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="artifactPlan">The runner artifact materialization plan.</param>
    /// <param name="preflightReport">The runner preflight report.</param>
    /// <param name="commands">The ordered command entries.</param>
    public SigtranReferencePeerLabRunnerCommandManifest(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        SigtranReferencePeerLabRunnerArtifactMaterializationPlan artifactPlan,
        SigtranReferencePeerLabRunnerPreflightReport preflightReport,
        IReadOnlyList<SigtranReferencePeerLabRunnerCommandEntry> commands)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(preflightReport);
        ArgumentNullException.ThrowIfNull(commands);

        InputBundle = inputBundle;
        ArtifactPlan = artifactPlan;
        PreflightReport = preflightReport;
        _commands = commands.Count == 0 ? throw new ArgumentException("At least one runner command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The runner input bundle.</summary>
    public SigtranReferencePeerLabRunnerInputBundle InputBundle { get; }

    /// <summary>The runner artifact materialization plan.</summary>
    public SigtranReferencePeerLabRunnerArtifactMaterializationPlan ArtifactPlan { get; }

    /// <summary>The runner preflight report.</summary>
    public SigtranReferencePeerLabRunnerPreflightReport PreflightReport { get; }

    /// <summary>The ordered command entries.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerCommandEntry> Commands => _commands.ToArray();

    /// <summary>Whether command entries follow a contiguous one-based sequence.</summary>
    public bool HasContiguousSequence => _commands.Select(static command => command.Sequence).Order().SequenceEqual(Enumerable.Range(1, _commands.Length));

    /// <summary>Whether every command entry has expected artifact mappings.</summary>
    public bool CoversExpectedArtifacts => _commands.All(static command => command.CoversExpectedArtifacts);

    /// <summary>Whether the command manifest is ready for runner execution.</summary>
    public bool IsExecutionReady => InputBundle.IsMaterializationReady
        && ArtifactPlan.IsMaterializationReady
        && PreflightReport.Ready
        && HasContiguousSequence
        && CoversExpectedArtifacts;

    /// <summary>Renders a Markdown command manifest.</summary>
    /// <returns>The Markdown command manifest.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Command Manifest");
        builder.AppendLine();
        builder.AppendLine($"Run: `{InputBundle.Workspace.RunManifest.RunId}`");
        builder.AppendLine($"Ready: `{IsExecutionReady}`");
        builder.AppendLine();
        builder.AppendLine("## Commands");

        foreach (SigtranReferencePeerLabRunnerCommandEntry entry in _commands.OrderBy(static command => command.Sequence))
        {
            builder.Append("- ");
            builder.Append(entry.Sequence);
            builder.Append(". ");
            builder.Append(entry.Command.Name);
            builder.Append(" -> ");
            builder.AppendLine(string.Join(", ", entry.ExpectedOutputPaths));
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact command manifest summary.</summary>
    /// <returns>The command manifest summary.</returns>
    public string Describe()
    {
        return $"run={InputBundle.Workspace.RunManifest.RunId} commands={_commands.Length} sequence={HasContiguousSequence} artifacts={CoversExpectedArtifacts} ready={IsExecutionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner command manifest helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerCommandManifests
{
    /// <summary>Creates a command manifest from runner input, artifact, and preflight contracts.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="artifactPlan">The runner artifact materialization plan.</param>
    /// <param name="preflightReport">The runner preflight report.</param>
    /// <returns>The runner command manifest.</returns>
    public static SigtranReferencePeerLabRunnerCommandManifest Create(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        SigtranReferencePeerLabRunnerArtifactMaterializationPlan artifactPlan,
        SigtranReferencePeerLabRunnerPreflightReport preflightReport)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(preflightReport);

        int sequence = 1;
        List<SigtranReferencePeerLabRunnerCommandEntry> commands = [];
        foreach (SigtranReferencePeerLabCommand command in inputBundle.Workspace.RunManifest.CommandPlan.Commands)
        {
            IReadOnlyList<string> outputs = artifactPlan.Outputs
                .Where(output => command.ExpectedArtifactKinds.Contains(output.Kind))
                .Select(static output => output.Path)
                .ToArray();
            commands.Add(new(sequence, command, outputs));
            sequence++;
        }

        return new(inputBundle, artifactPlan, preflightReport, commands);
    }
}
