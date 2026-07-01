namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reference external peer lab runner input file.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerInputFile
{
    /// <summary>Creates a reference peer lab runner input file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="content">The input file content.</param>
    public SigtranReferencePeerLabRunnerInputFile(string path, string content)
    {
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Input path is required.", nameof(path)) : path;
        Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentException("Input content is required.", nameof(content)) : content;
    }

    /// <summary>The input file path.</summary>
    public string Path { get; }

    /// <summary>The input file content.</summary>
    public string Content { get; }

    /// <summary>Formats a compact input file summary.</summary>
    /// <returns>The input file summary.</returns>
    public string Describe()
    {
        return $"path={Path} bytes={Content.Length}";
    }
}

/// <summary>
/// Describes the deterministic input bundle for a reference external peer lab runner.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerInputBundle
{
    /// <summary>Creates a reference peer lab runner input bundle.</summary>
    /// <param name="workspace">The runner workspace.</param>
    /// <param name="environmentFile">The rendered environment file model.</param>
    /// <param name="commandScript">The rendered command script model.</param>
    public SigtranReferencePeerLabRunnerInputBundle(
        SigtranReferencePeerLabRunnerWorkspace workspace,
        SigtranReferencePeerLabEnvironmentFile environmentFile,
        SigtranReferencePeerLabCommandScript commandScript)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(environmentFile);
        ArgumentNullException.ThrowIfNull(commandScript);

        Workspace = workspace;
        EnvironmentFile = environmentFile;
        CommandScript = commandScript;
    }

    /// <summary>The runner workspace.</summary>
    public SigtranReferencePeerLabRunnerWorkspace Workspace { get; }

    /// <summary>The rendered environment file model.</summary>
    public SigtranReferencePeerLabEnvironmentFile EnvironmentFile { get; }

    /// <summary>The rendered command script model.</summary>
    public SigtranReferencePeerLabCommandScript CommandScript { get; }

    /// <summary>The environment file path to materialize.</summary>
    public string EnvironmentFilePath => $"{Workspace.ConfigDirectory}/{Workspace.RunManifest.RunId}-peer.env";

    /// <summary>The command script path to materialize.</summary>
    public string CommandScriptPath => $"{Workspace.ScriptRoot}/{Workspace.RunManifest.RunId}.sh";

    /// <summary>Whether the input bundle is deterministic and ready to materialize.</summary>
    public bool IsMaterializationReady => Workspace.IsMaterializationReady
        && EnvironmentFile.Variables.TryGetValue("SIGTRAN_LAB_RUN_ID", out string? runId)
        && runId == Workspace.RunManifest.RunId
        && CommandScript.EnvironmentFilePath == EnvironmentFilePath
        && CommandScript.CoversCommandPlan;

    /// <summary>Returns the input files that a runner should materialize before execution.</summary>
    /// <returns>The deterministic runner input files.</returns>
    public IReadOnlyList<SigtranReferencePeerLabRunnerInputFile> GetInputFiles()
    {
        return
        [
            new(EnvironmentFilePath, EnvironmentFile.Render()),
            new(CommandScriptPath, CommandScript.Render())
        ];
    }

    /// <summary>Formats a compact input bundle summary.</summary>
    /// <returns>The input bundle summary.</returns>
    public string Describe()
    {
        return $"run={Workspace.RunManifest.RunId} inputs={GetInputFiles().Count} env={EnvironmentFilePath} script={CommandScriptPath} ready={IsMaterializationReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner input helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerInputs
{
    /// <summary>Creates the default runner input bundle from a reference peer lab run manifest.</summary>
    /// <param name="runManifest">The reference peer lab run manifest.</param>
    /// <returns>The default runner input bundle.</returns>
    public static SigtranReferencePeerLabRunnerInputBundle CreateDefault(SigtranReferencePeerLabRunManifest runManifest)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        SigtranReferencePeerLabRunnerWorkspace workspace = SigtranReferencePeerLabRunnerWorkspaces.CreateDefault(runManifest);
        SigtranReferencePeerLabEnvironmentFile environmentFile = SigtranReferencePeerLabEnvironmentFiles.FromManifest(runManifest);
        SigtranReferencePeerLabCommandScript commandScript = new(runManifest, $"{workspace.ConfigDirectory}/{runManifest.RunId}-peer.env");

        return new(workspace, environmentFile, commandScript);
    }
}
