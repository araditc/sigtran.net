namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides commercial evidence execution orchestration status reporting.
/// </summary>
public static class SigtranCommercialEvidenceExecutionStatus
{
    private static readonly string[] Capabilities =
    [
        "execution-run-identity",
        "execution-stage-catalog",
        "operator-command-plan",
        "execution-environment-contract",
        "artifact-collection-manifest",
        "digest-redaction-verification",
        "blocker-classification",
        "retry-resume-policy",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-execution-artifacts-required",
        "status-final-validation-pending"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Commercial Evidence Execution Orchestration";

    /// <summary>The number of completed commercial evidence execution orchestration work units.</summary>
    public const int CompletedUnitCount = 9;

    /// <summary>Returns the completed execution orchestration capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for commercial publication.</summary>
    /// <returns>The default commercial publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the default execution orchestration contracts are ready.</summary>
    public static bool ExecutionOrchestrationReady
    {
        get
        {
            SigtranCommercialEvidenceExecutionRun run = CreateDefaultRun();
            SigtranCommercialEvidenceExecutionStageCatalog catalog = SigtranCommercialEvidenceExecutionStages.CreateDefault(run);
            SigtranCommercialEvidenceExecutionCommandPlan commandPlan = SigtranCommercialEvidenceExecutionCommands.CreateDefault(catalog);
            SigtranCommercialEvidenceExecutionEnvironmentContract environment = SigtranCommercialEvidenceExecutionEnvironments.CreateDefault(run);
            SigtranCommercialEvidenceExecutionArtifactManifest artifacts = SigtranCommercialEvidenceExecutionArtifacts.CreateDefault(catalog);
            SigtranCommercialEvidenceExecutionVerificationPlan verification = SigtranCommercialEvidenceExecutionVerifications.CreateDefault(artifacts);
            SigtranCommercialEvidenceExecutionBlockerClassifier blockerClassifier = SigtranCommercialEvidenceExecutionBlockers.CreateDefault();
            SigtranCommercialEvidenceExecutionRetryPolicy retryPolicy = SigtranCommercialEvidenceExecutionRetryPolicies.CreateDefault(catalog);

            return Capabilities.Length == CompletedUnitCount
                && run.IsReady
                && catalog.IsReady
                && commandPlan.IsReady
                && environment.IsReady
                && artifacts.IsReady
                && verification.IsReady
                && blockerClassifier.IsReady
                && retryPolicy.IsReady;
        }
    }

    /// <summary>Whether retained real execution evidence is available in the default status.</summary>
    public static bool RetainedExecutionEvidenceReady => false;

    /// <summary>Whether the current default status can publish a commercial package.</summary>
    public static bool CommercialPublicationReady => ExecutionOrchestrationReady
        && RetainedExecutionEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact commercial evidence execution orchestration summary.</summary>
    /// <returns>The commercial evidence execution orchestration summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} orchestrationReady={ExecutionOrchestrationReady} retainedEvidenceReady={RetainedExecutionEvidenceReady} commercialPublicationReady={CommercialPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranCommercialEvidenceExecutionRun CreateDefaultRun()
    {
        return SigtranCommercialEvidenceExecutionRuns.CreateReleaseCandidateRun(
            "1.0.0-rc.1",
            "abcdef123456",
            "run-20260622-001",
            "release-automation",
            DateTimeOffset.UtcNow);
    }
}
