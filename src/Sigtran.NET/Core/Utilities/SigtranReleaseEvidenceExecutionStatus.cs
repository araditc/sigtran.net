namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides production evidence execution orchestration status reporting.
/// </summary>
public static class SigtranReleaseEvidenceExecutionStatus
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
        "final-validation",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-execution-artifacts-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Evidence Execution Orchestration";

    /// <summary>The number of completed production evidence execution orchestration work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed execution orchestration capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default blockers for production publication.</summary>
    /// <returns>The default production publication blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the default execution orchestration contracts are ready.</summary>
    public static bool ExecutionOrchestrationReady
    {
        get
        {
            SigtranReleaseEvidenceExecutionRun run = CreateDefaultRun();
            SigtranReleaseEvidenceExecutionStageCatalog catalog = SigtranReleaseEvidenceExecutionStages.CreateDefault(run);
            SigtranReleaseEvidenceExecutionCommandPlan commandPlan = SigtranReleaseEvidenceExecutionCommands.CreateDefault(catalog);
            SigtranReleaseEvidenceExecutionEnvironmentContract environment = SigtranReleaseEvidenceExecutionEnvironments.CreateDefault(run);
            SigtranReleaseEvidenceExecutionArtifactManifest artifacts = SigtranReleaseEvidenceExecutionArtifacts.CreateDefault(catalog);
            SigtranReleaseEvidenceExecutionVerificationPlan verification = SigtranReleaseEvidenceExecutionVerifications.CreateDefault(artifacts);
            SigtranReleaseEvidenceExecutionBlockerClassifier blockerClassifier = SigtranReleaseEvidenceExecutionBlockers.CreateDefault();
            SigtranReleaseEvidenceExecutionRetryPolicy retryPolicy = SigtranReleaseEvidenceExecutionRetryPolicies.CreateDefault(catalog);

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

    /// <summary>Whether the current default status can publish a production package.</summary>
    public static bool ProductionPublicationReady => ExecutionOrchestrationReady
        && RetainedExecutionEvidenceReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact production evidence execution orchestration summary.</summary>
    /// <returns>The production evidence execution orchestration summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} orchestrationReady={ExecutionOrchestrationReady} retainedEvidenceReady={RetainedExecutionEvidenceReady} productionPublicationReady={ProductionPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranReleaseEvidenceExecutionRun CreateDefaultRun()
    {
        return SigtranReleaseEvidenceExecutionRuns.CreatePrereleaseRun(
            "1.0.0-rc.1",
            "abcdef123456",
            "run-20260622-001",
            "release-automation",
            DateTimeOffset.UtcNow);
    }
}
