namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides release workflow orchestration status.
/// </summary>
public static class SigtranReleaseWorkflowStatus
{
    private static readonly string[] Capabilities =
    [
        "release-workflow-contract",
        "concrete-workflow-file",
        "workflow-yaml-validation",
        "publish-guard",
        "artifact-retention",
        "least-privilege-permissions",
        "concurrency-policy",
        "environment-contract",
        "promotion-gate",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Release Workflow Orchestration";

    /// <summary>The number of completed release workflow contract work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed release workflow capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the release workflow contract foundation is ready.</summary>
    public static bool ContractReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseWorkflowReadiness.GetReport().ContractReady;

    /// <summary>Whether release workflow orchestration is fully ready.</summary>
    public static bool OrchestrationReady => ContractReady
        && SigtranReleaseWorkflowReadiness.GetReport().OrchestrationReady;

    /// <summary>Formats a compact release workflow status summary.</summary>
    /// <returns>The release workflow status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} contractReady={ContractReady} orchestrationReady={OrchestrationReady}";
    }
}
