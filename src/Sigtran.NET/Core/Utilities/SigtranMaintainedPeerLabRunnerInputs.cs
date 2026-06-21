namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one maintained external peer lab runner input file.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerInputFile
{
    /// <summary>Creates a maintained peer lab runner input file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="content">The input file content.</param>
    public SigtranMaintainedPeerLabRunnerInputFile(string path, string content)
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
/// Describes the deterministic input bundle for a maintained external peer lab runner.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerInputBundle
{
    /// <summary>Creates a maintained peer lab runner input bundle.</summary>
    /// <param name="workspace">The runner workspace.</param>
    /// <param name="environmentFile">The rendered environment file model.</param>
    /// <param name="commandScript">The rendered command script model.</param>
    public SigtranMaintainedPeerLabRunnerInputBundle(
        SigtranMaintainedPeerLabRunnerWorkspace workspace,
        SigtranMaintainedPeerLabEnvironmentFile environmentFile,
        SigtranMaintainedPeerLabCommandScript commandScript)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(environmentFile);
        ArgumentNullException.ThrowIfNull(commandScript);

        Workspace = workspace;
        EnvironmentFile = environmentFile;
        CommandScript = commandScript;
    }

    /// <summary>The runner workspace.</summary>
    public SigtranMaintainedPeerLabRunnerWorkspace Workspace { get; }

    /// <summary>The rendered environment file model.</summary>
    public SigtranMaintainedPeerLabEnvironmentFile EnvironmentFile { get; }

    /// <summary>The rendered command script model.</summary>
    public SigtranMaintainedPeerLabCommandScript CommandScript { get; }

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
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerInputFile> GetInputFiles()
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
/// Provides maintained external peer lab runner input helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerInputs
{
    /// <summary>Creates the default runner input bundle from a maintained peer lab run manifest.</summary>
    /// <param name="runManifest">The maintained peer lab run manifest.</param>
    /// <returns>The default runner input bundle.</returns>
    public static SigtranMaintainedPeerLabRunnerInputBundle CreateDefault(SigtranMaintainedPeerLabRunManifest runManifest)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        SigtranMaintainedPeerLabRunnerWorkspace workspace = SigtranMaintainedPeerLabRunnerWorkspaces.CreateDefault(runManifest);
        SigtranMaintainedPeerLabEnvironmentFile environmentFile = SigtranMaintainedPeerLabEnvironmentFiles.FromManifest(runManifest);
        SigtranMaintainedPeerLabCommandScript commandScript = new(runManifest, $"{workspace.ConfigDirectory}/{runManifest.RunId}-peer.env");

        return new(workspace, environmentFile, commandScript);
    }
}
