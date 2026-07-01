namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab runner workflow readiness.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerWorkflowReadinessSnapshot
{
    /// <summary>Creates a reference peer lab runner workflow readiness report.</summary>
    /// <param name="workflowTemplate">The workflow template.</param>
    /// <param name="commandManifest">The runner command manifest.</param>
    public SigtranReferencePeerLabRunnerWorkflowReadinessSnapshot(
        SigtranReferencePeerLabWorkflowTemplate workflowTemplate,
        SigtranReferencePeerLabRunnerCommandManifest commandManifest)
    {
        ArgumentNullException.ThrowIfNull(workflowTemplate);
        ArgumentNullException.ThrowIfNull(commandManifest);
        WorkflowTemplate = workflowTemplate;
        CommandManifest = commandManifest;
    }

    /// <summary>The workflow template.</summary>
    public SigtranReferencePeerLabWorkflowTemplate WorkflowTemplate { get; }

    /// <summary>The runner command manifest.</summary>
    public SigtranReferencePeerLabRunnerCommandManifest CommandManifest { get; }

    /// <summary>Whether workflow policy is safe for reference peer lab execution.</summary>
    public bool WorkflowPolicyReady => WorkflowTemplate.IsReady
        && WorkflowTemplate.ManualDispatchOnly
        && WorkflowTemplate.RequiresSelfHostedLinux
        && !WorkflowTemplate.SafeForDefaultCi;

    /// <summary>Whether runner materialization is ready for workflow execution.</summary>
    public bool RunnerMaterializationReady => CommandManifest.IsExecutionReady;

    /// <summary>Whether workflow upload patterns cover the artifact plan directories.</summary>
    public bool ArtifactUploadReady => WorkflowTemplate.ArtifactPatterns.Count >= CommandManifest.ArtifactPlan.Workspace.RunManifest.ArtifactPlan.Items.Count
        && WorkflowTemplate.ArtifactPatterns.Any(static pattern => pattern.Contains("/pcap/", StringComparison.Ordinal))
        && WorkflowTemplate.ArtifactPatterns.Any(static pattern => pattern.Contains("/logs/", StringComparison.Ordinal))
        && WorkflowTemplate.ArtifactPatterns.Any(static pattern => pattern.Contains("/reports/", StringComparison.Ordinal));

    /// <summary>Whether the runner workflow is ready to execute on a reference peer lab runner.</summary>
    public bool Ready => WorkflowPolicyReady && RunnerMaterializationReady && ArtifactUploadReady;

    /// <summary>Formats a compact runner workflow readiness summary.</summary>
    /// <returns>The runner workflow readiness summary.</returns>
    public string Describe()
    {
        return $"workflow={WorkflowTemplate.Name} policy={WorkflowPolicyReady} runner={RunnerMaterializationReady} artifacts={ArtifactUploadReady} ready={Ready}";
    }
}

/// <summary>
/// Provides reference external peer lab runner workflow readiness helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerWorkflowReadiness
{
    /// <summary>Evaluates workflow readiness for a runner command manifest.</summary>
    /// <param name="commandManifest">The runner command manifest.</param>
    /// <returns>The runner workflow readiness report.</returns>
    public static SigtranReferencePeerLabRunnerWorkflowReadinessSnapshot Evaluate(SigtranReferencePeerLabRunnerCommandManifest commandManifest)
    {
        ArgumentNullException.ThrowIfNull(commandManifest);
        return new(SigtranReferencePeerLabWorkflows.CreateDefault(), commandManifest);
    }
}
