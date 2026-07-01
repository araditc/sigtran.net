namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the filesystem workspace used by a reference external peer lab runner.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerWorkspace
{
    /// <summary>Creates a reference peer lab runner workspace.</summary>
    /// <param name="runManifest">The reference peer lab run manifest.</param>
    /// <param name="workspaceRoot">The runner workspace root.</param>
    /// <param name="scriptRoot">The runner script root.</param>
    public SigtranReferencePeerLabRunnerWorkspace(
        SigtranReferencePeerLabRunManifest runManifest,
        string workspaceRoot,
        string scriptRoot)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        RunManifest = runManifest;
        WorkspaceRoot = NormalizeRequired(workspaceRoot, nameof(workspaceRoot));
        ScriptRoot = NormalizeRequired(scriptRoot, nameof(scriptRoot));
    }

    /// <summary>The reference peer lab run manifest.</summary>
    public SigtranReferencePeerLabRunManifest RunManifest { get; }

    /// <summary>The runner workspace root.</summary>
    public string WorkspaceRoot { get; }

    /// <summary>The runner script root.</summary>
    public string ScriptRoot { get; }

    /// <summary>The artifact root used by the run manifest.</summary>
    public string ArtifactRoot => RunManifest.ArtifactPlan.ArtifactRoot;

    /// <summary>The config directory used by the run manifest.</summary>
    public string ConfigDirectory => $"{ArtifactRoot}/config";

    /// <summary>The log directory used by the run manifest.</summary>
    public string LogDirectory => $"{ArtifactRoot}/logs";

    /// <summary>The packet capture directory used by the run manifest.</summary>
    public string PacketCaptureDirectory => $"{ArtifactRoot}/pcap";

    /// <summary>The SDK trace directory used by the run manifest.</summary>
    public string TraceDirectory => $"{ArtifactRoot}/trace";

    /// <summary>The comparison report directory used by the run manifest.</summary>
    public string ComparisonDirectory => $"{ArtifactRoot}/comparison";

    /// <summary>The run report directory used by the run manifest.</summary>
    public string ReportDirectory => $"{ArtifactRoot}/reports";

    /// <summary>Whether the workspace has enough deterministic paths for runner materialization.</summary>
    public bool IsMaterializationReady => RunManifest.IsExecutableContract
        && !WorkspaceRoot.Contains('\\', StringComparison.Ordinal)
        && !ScriptRoot.Contains('\\', StringComparison.Ordinal)
        && ArtifactRoot.StartsWith(WorkspaceRoot, StringComparison.Ordinal)
        && RunManifest.ArtifactPlan.CoversRequiredArtifacts
        && RunManifest.ArtifactPlan.Items.All(item => item.Path.StartsWith(ArtifactRoot, StringComparison.Ordinal));

    /// <summary>Returns the directories that must exist before a reference peer lab run starts.</summary>
    /// <returns>The required runner directories.</returns>
    public IReadOnlyList<string> GetRequiredDirectories()
    {
        return
        [
            WorkspaceRoot,
            ScriptRoot,
            ArtifactRoot,
            ConfigDirectory,
            LogDirectory,
            PacketCaptureDirectory,
            TraceDirectory,
            ComparisonDirectory,
            ReportDirectory
        ];
    }

    /// <summary>Formats a compact runner workspace summary.</summary>
    /// <returns>The runner workspace summary.</returns>
    public string Describe()
    {
        return $"run={RunManifest.RunId} workspace={WorkspaceRoot} scripts={ScriptRoot} artifactRoot={ArtifactRoot} directories={GetRequiredDirectories().Count} ready={IsMaterializationReady}";
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Path is required.", parameterName);
        }

        return value.Trim().Replace('\\', '/').TrimEnd('/');
    }
}

/// <summary>
/// Provides reference external peer lab runner workspace helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerWorkspaces
{
    /// <summary>Creates the default runner workspace from a reference peer lab run manifest.</summary>
    /// <param name="runManifest">The reference peer lab run manifest.</param>
    /// <returns>The default reference peer lab runner workspace.</returns>
    public static SigtranReferencePeerLabRunnerWorkspace CreateDefault(SigtranReferencePeerLabRunManifest runManifest)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        string workspaceRoot = runManifest.ArtifactPlan.ArtifactRoot
            .Replace('\\', '/')
            .TrimEnd('/');
        int artifactIndex = workspaceRoot.IndexOf("/artifacts/", StringComparison.Ordinal);
        if (artifactIndex > 0)
        {
            workspaceRoot = workspaceRoot[..artifactIndex];
        }

        string scriptRoot = $"{workspaceRoot}/scripts/reference-peer-lab";
        return new(runManifest, workspaceRoot, scriptRoot);
    }
}
