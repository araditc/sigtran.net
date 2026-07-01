namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reference external peer lab runner output artifact.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerArtifactOutput
{
    /// <summary>Creates a reference peer lab runner output artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="required">Whether the artifact is required.</param>
    /// <param name="producerCommandKind">The command kind expected to produce the artifact.</param>
    public SigtranReferencePeerLabRunnerArtifactOutput(
        SigtranReferencePeerLabArtifactKind kind,
        string path,
        bool required,
        SigtranReferencePeerLabCommandKind producerCommandKind)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Required = required;
        ProducerCommandKind = producerCommandKind;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranReferencePeerLabArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required.</summary>
    public bool Required { get; }

    /// <summary>The command kind expected to produce the artifact.</summary>
    public SigtranReferencePeerLabCommandKind ProducerCommandKind { get; }

    /// <summary>Formats a compact artifact output summary.</summary>
    /// <returns>The artifact output summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} producer={ProducerCommandKind} required={Required} path={Path}";
    }
}

/// <summary>
/// Describes the expected output materialization plan for a reference external peer lab runner.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerArtifactMaterializationPlan
{
    private readonly SigtranReferencePeerLabRunnerArtifactOutput[] _outputs;

    /// <summary>Creates a reference peer lab runner artifact materialization plan.</summary>
    /// <param name="workspace">The runner workspace.</param>
    /// <param name="outputs">The expected output artifacts.</param>
    public SigtranReferencePeerLabRunnerArtifactMaterializationPlan(
        SigtranReferencePeerLabRunnerWorkspace workspace,
        IReadOnlyList<SigtranReferencePeerLabRunnerArtifactOutput> outputs)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(outputs);

        Workspace = workspace;
        _outputs = outputs.Count == 0 ? throw new ArgumentException("At least one output artifact is required.", nameof(outputs)) : outputs.ToArray();
    }

    /// <summary>The runner workspace.</summary>
    public SigtranReferencePeerLabRunnerWorkspace Workspace { get; }

    /// <summary>The expected output artifacts.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerArtifactOutput> Outputs => _outputs.ToArray();

    /// <summary>Whether every required planned artifact has a runner output mapping.</summary>
    public bool CoversRequiredArtifacts => Workspace.RunManifest.ArtifactPlan.Items
        .Where(static item => item.Required)
        .All(item => _outputs.Any(output => output.Kind == item.Kind && output.Path == item.Path && output.Required));

    /// <summary>Whether every output path stays under the workspace artifact root.</summary>
    public bool OutputPathsStayUnderArtifactRoot => _outputs.All(output => output.Path.StartsWith(Workspace.ArtifactRoot, StringComparison.Ordinal));

    /// <summary>Whether every output is connected to an existing command producer.</summary>
    public bool HasProducerCommands => _outputs.All(output => Workspace.RunManifest.CommandPlan.Commands.Any(command => command.Kind == output.ProducerCommandKind));

    /// <summary>Whether the materialization plan is ready for runner execution.</summary>
    public bool IsMaterializationReady => Workspace.IsMaterializationReady
        && CoversRequiredArtifacts
        && OutputPathsStayUnderArtifactRoot
        && HasProducerCommands;

    /// <summary>Returns the required output artifact paths.</summary>
    /// <returns>The required output artifact paths.</returns>
    public IReadOnlyList<string> GetRequiredOutputPaths()
    {
        return _outputs
            .Where(static output => output.Required)
            .Select(static output => output.Path)
            .ToArray();
    }

    /// <summary>Formats a compact artifact materialization summary.</summary>
    /// <returns>The artifact materialization summary.</returns>
    public string Describe()
    {
        return $"run={Workspace.RunManifest.RunId} outputs={_outputs.Length} required={CoversRequiredArtifacts} producers={HasProducerCommands} ready={IsMaterializationReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner artifact materialization helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerArtifacts
{
    /// <summary>Creates the default runner artifact materialization plan from a workspace.</summary>
    /// <param name="workspace">The runner workspace.</param>
    /// <returns>The default artifact materialization plan.</returns>
    public static SigtranReferencePeerLabRunnerArtifactMaterializationPlan CreateDefault(SigtranReferencePeerLabRunnerWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new(
            workspace,
            workspace.RunManifest.ArtifactPlan.Items
                .Select(item => new SigtranReferencePeerLabRunnerArtifactOutput(
                    item.Kind,
                    item.Path,
                    item.Required,
                    FindProducer(workspace.RunManifest.CommandPlan, item.Kind)))
                .ToArray());
    }

    private static SigtranReferencePeerLabCommandKind FindProducer(
        SigtranReferencePeerLabCommandPlan commandPlan,
        SigtranReferencePeerLabArtifactKind artifactKind)
    {
        return commandPlan.Commands
            .First(command => command.ExpectedArtifactKinds.Contains(artifactKind))
            .Kind;
    }
}
