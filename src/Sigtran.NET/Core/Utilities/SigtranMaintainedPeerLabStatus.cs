namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab readiness status.
/// </summary>
public sealed class SigtranMaintainedPeerLabStatusReport
{
    /// <summary>Creates a maintained external peer lab status report.</summary>
    /// <param name="completedCapabilityCount">The completed foundation capability count.</param>
    /// <param name="foundationReady">Whether the maintained peer lab foundation is ready.</param>
    /// <param name="commercialEvidenceReady">Whether retained evidence is ready for commercial promotion.</param>
    /// <param name="blockers">The current blockers.</param>
    public SigtranMaintainedPeerLabStatusReport(
        int completedCapabilityCount,
        bool foundationReady,
        bool commercialEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        CompletedCapabilityCount = completedCapabilityCount < 0 ? throw new ArgumentOutOfRangeException(nameof(completedCapabilityCount)) : completedCapabilityCount;
        FoundationReady = foundationReady;
        CommercialEvidenceReady = commercialEvidenceReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed foundation capability count.</summary>
    public int CompletedCapabilityCount { get; }

    /// <summary>Whether the maintained peer lab foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether retained evidence is ready for commercial promotion.</summary>
    public bool CommercialEvidenceReady { get; }

    /// <summary>The current blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the maintained peer lab can support a commercial release claim.</summary>
    public bool CommercialReady => FoundationReady && CommercialEvidenceReady && Blockers.Count == 0;

    /// <summary>Formats a compact maintained peer lab status summary.</summary>
    /// <returns>The maintained peer lab status summary.</returns>
    public string Describe()
    {
        return $"capabilities={CompletedCapabilityCount} foundation={FoundationReady} evidence={CommercialEvidenceReady} blockers={Blockers.Count} commercial={CommercialReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab status helpers.
/// </summary>
public static class SigtranMaintainedPeerLabStatus
{
    /// <summary>The completed foundation unit count.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed maintained peer lab foundation capabilities.</summary>
    /// <returns>The completed maintained peer lab foundation capabilities.</returns>
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

    /// <summary>Returns the maintained peer lab foundation status report without real retained evidence.</summary>
    /// <returns>The maintained peer lab foundation status report.</returns>
    public static SigtranMaintainedPeerLabStatusReport GetFoundationReport()
    {
        bool foundationReady = SigtranMaintainedPeerLabBindings.CreateCatalog().Count > 0
            && SigtranMaintainedPeerLabPrerequisites.GetDefault().Count > 0
            && SigtranMaintainedPeerLabConfigurations.CreateDefault().Validate().IsValid
            && SigtranMaintainedPeerLabArtifactPlans.CreateDefault(SigtranMaintainedPeerLabConfigurations.CreateDefault()).CoversRequiredArtifacts
            && SigtranMaintainedPeerLabCi.CreateDefault().ManualDispatchOnly;

        return new(
            CompletedUnitCount,
            foundationReady,
            commercialEvidenceReady: false,
            ["real-maintained-peer-run-required", "digest-covered-retained-artifacts-required"]);
    }

    /// <summary>Creates a maintained peer lab status report from retained evidence.</summary>
    /// <param name="evidenceReport">The retained evidence report.</param>
    /// <returns>The maintained peer lab status report.</returns>
    public static SigtranMaintainedPeerLabStatusReport FromEvidence(SigtranMaintainedPeerLabEvidenceReport evidenceReport)
    {
        ArgumentNullException.ThrowIfNull(evidenceReport);

        string[] blockers = evidenceReport.PromotionReady
            ? []
            : ["maintained-peer-evidence-not-promotion-ready"];

        return new(
            CompletedUnitCount,
            foundationReady: true,
            commercialEvidenceReady: evidenceReport.PromotionReady,
            blockers);
    }
}
