namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the concrete release workflow file contract.
/// </summary>
public sealed class SigtranReleaseWorkflowFileContract
{
    /// <summary>Creates a release workflow file contract.</summary>
    /// <param name="path">The workflow file path.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="requiredJobName">The required job name.</param>
    /// <param name="requiredStageNames">The required stage names.</param>
    public SigtranReleaseWorkflowFileContract(
        string path,
        string workflowName,
        string requiredJobName,
        IReadOnlyList<string> requiredStageNames)
    {
        ArgumentNullException.ThrowIfNull(requiredStageNames);
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Workflow path is required.", nameof(path)) : path;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        RequiredJobName = string.IsNullOrWhiteSpace(requiredJobName) ? throw new ArgumentException("Required job name is required.", nameof(requiredJobName)) : requiredJobName;
        RequiredStageNames = requiredStageNames.Count == 0 ? throw new ArgumentException("At least one stage is required.", nameof(requiredStageNames)) : requiredStageNames.ToArray();
    }

    /// <summary>The workflow file path.</summary>
    public string Path { get; }

    /// <summary>The workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>The required job name.</summary>
    public string RequiredJobName { get; }

    /// <summary>The required stage names.</summary>
    public IReadOnlyList<string> RequiredStageNames { get; }

    /// <summary>Whether the contract has enough information to validate a workflow file.</summary>
    public bool IsValidationReady => Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
        && RequiredStageNames.Count > 0
        && RequiredStageNames.Contains("Supply Chain")
        && RequiredStageNames.Contains("Commercial Evidence")
        && RequiredStageNames.Contains("Publish");
}

/// <summary>
/// Provides release workflow file contracts.
/// </summary>
public static class SigtranReleaseWorkflowFiles
{
    /// <summary>Creates the default release workflow file contract.</summary>
    /// <returns>The default release workflow file contract.</returns>
    public static SigtranReleaseWorkflowFileContract CreateDefault()
    {
        return new(
            ".github/workflows/release.yml",
            "release",
            "release",
            [
                "Checkout",
                "Setup .NET",
                "Restore",
                "Build",
                "Test",
                "Pack",
                "Supply Chain",
                "Commercial Evidence",
                "Publish"
            ]);
    }
}
