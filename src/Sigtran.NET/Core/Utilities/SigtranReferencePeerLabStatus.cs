namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab readiness status.
/// </summary>
public sealed class SigtranReferencePeerLabStatusReport
{
    /// <summary>Creates a reference external peer lab status report.</summary>
    /// <param name="completedCapabilityCount">The completed foundation capability count.</param>
    /// <param name="foundationReady">Whether the reference peer lab foundation is ready.</param>
    /// <param name="releaseEvidenceReady">Whether retained evidence is ready for production promotion.</param>
    /// <param name="blockers">The current blockers.</param>
    public SigtranReferencePeerLabStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool releaseEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        ReleaseEvidenceReady = releaseEvidenceReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed foundation capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the reference peer lab foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained evidence is ready for production promotion.</summary>
    public bool ReleaseEvidenceReady { get; }

    /// <summary>The current blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the reference peer lab can support a production release claim.</summary>
    public bool ProductionReady => FoundationReady && ReleaseEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact reference peer lab status summary.</summary>
    /// <returns>The reference peer lab status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={ReleaseEvidenceReady} blockers={Blockers.Count} production={ProductionReady}";
    }
}

/// <summary>
/// Provides reference external peer lab status helpers.
/// </summary>
public static class SigtranReferencePeerLabStatus
{
    /// <summary>The completed foundation unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed reference peer lab foundation capabilities.</summary>
    /// <returns>The completed reference peer lab foundation capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return
        [
            "canonical-sdk-identity",
            "package-neutral-binding-catalog",
            "host-prerequisite-model",
            "validated-configuration-contract",
            "retained-artifact-plan",
            "ordered-command-plan",
            "traffic-vector-catalog",
            "evidence-promotion-gate",
            "manual-self-hosted-ci-profile",
            "foundation-status-report"
        ];
    }

    /// <summary>Returns the reference peer lab foundation status report without real retained evidence.</summary>
    /// <returns>The reference peer lab foundation status report.</returns>
    public static SigtranReferencePeerLabStatusReport GetFoundationReport()
    {
        bool foundationReady = SigtranReferencePeerLabBindings.CreateCatalog().Count > 0
            && SigtranReferencePeerLabPrerequisites.GetDefault().Count > 0
            && SigtranReferencePeerLabConfigurations.CreateDefault().Validate().IsValid
            && SigtranReferencePeerLabArtifactPlans.CreateDefault(SigtranReferencePeerLabConfigurations.CreateDefault()).CoversRequiredArtifacts
            && SigtranReferencePeerLabCi.CreateDefault().ManualDispatchOnly;

        return new(
            CompletedUnitCount,
            foundationReady,
            releaseEvidenceReady: false,
            ["real-reference-peer-run-required", "digest-covered-retained-artifacts-required"]);
    }

    /// <summary>Creates a reference peer lab status report from retained evidence.</summary>
    /// <param name="evidenceReport">The retained evidence report.</param>
    /// <returns>The reference peer lab status report.</returns>
    public static SigtranReferencePeerLabStatusReport FromEvidence(SigtranReferencePeerLabEvidenceReport evidenceReport)
    {
        ArgumentNullException.ThrowIfNull(evidenceReport);

        string[] blockers = evidenceReport.PromotionReady
            ? []
            : ["reference-peer-evidence-not-promotion-ready"];

        return new(
            CompletedUnitCount,
            foundationReady: true,
            releaseEvidenceReady: evidenceReport.PromotionReady,
            blockers);
    }
}
