namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes commercial readiness for external SIGTRAN peer interoperability.
/// </summary>
public sealed class SigtranExternalPeerCommercialReadinessReport
{
    /// <summary>Creates an external peer commercial readiness report.</summary>
    /// <param name="hasSelectedMaintainedPeer">Whether a maintained peer package has been selected.</param>
    /// <param name="hasLabEnvironment">Whether the lab environment can produce commercial artifacts.</param>
    /// <param name="hasRunPlan">Whether the run plan is executable.</param>
    /// <param name="hasReviewReadyEvidence">Whether passing digest-covered evidence exists.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranExternalPeerCommercialReadinessReport(
        bool hasSelectedMaintainedPeer,
        bool hasLabEnvironment,
        bool hasRunPlan,
        bool hasReviewReadyEvidence,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        HasSelectedMaintainedPeer = hasSelectedMaintainedPeer;
        HasLabEnvironment = hasLabEnvironment;
        HasRunPlan = hasRunPlan;
        HasReviewReadyEvidence = hasReviewReadyEvidence;
        Blockers = blockers.ToArray();
    }

    /// <summary>Whether a maintained peer package has been selected.</summary>
    public bool HasSelectedMaintainedPeer { get; }

    /// <summary>Whether the lab environment can produce commercial artifacts.</summary>
    public bool HasLabEnvironment { get; }

    /// <summary>Whether the run plan is executable.</summary>
    public bool HasRunPlan { get; }

    /// <summary>Whether passing digest-covered evidence exists.</summary>
    public bool HasReviewReadyEvidence { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the external peer commercial interop foundation is ready.</summary>
    public bool FoundationReady => HasSelectedMaintainedPeer && HasLabEnvironment && HasRunPlan;

    /// <summary>Whether commercial external peer interoperability can be claimed.</summary>
    public bool CommercialInteropReady => FoundationReady && HasReviewReadyEvidence && Blockers.Count == 0;

    /// <summary>Formats a compact commercial readiness summary.</summary>
    /// <returns>The commercial readiness summary.</returns>
    public string Describe()
    {
        return $"externalPeerCommercialInteropReady={CommercialInteropReady} foundationReady={FoundationReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides commercial readiness evaluation for external peer interoperability.
/// </summary>
public static class SigtranExternalPeerCommercialReadiness
{
    /// <summary>Creates the current external peer commercial readiness report.</summary>
    /// <returns>The current external peer commercial readiness report.</returns>
    public static SigtranExternalPeerCommercialReadinessReport CreateCurrent()
    {
        return Create(
            SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway(),
            [],
            SigtranExternalPeerInteropEvidence.CreateCurrentRegistry());
    }

    /// <summary>Creates an external peer commercial readiness report from selected criteria and evidence.</summary>
    /// <param name="profile">The external peer profile.</param>
    /// <param name="satisfiedSelectionCriteria">The satisfied maintained peer selection criterion ids.</param>
    /// <param name="evidenceRegistry">The evidence registry.</param>
    /// <returns>The external peer commercial readiness report.</returns>
    public static SigtranExternalPeerCommercialReadinessReport Create(
        SigtranInteropPeerProfile profile,
        IReadOnlyList<string> satisfiedSelectionCriteria,
        SigtranExternalPeerInteropEvidenceRegistry evidenceRegistry)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(satisfiedSelectionCriteria);
        ArgumentNullException.ThrowIfNull(evidenceRegistry);

        SigtranMaintainedPeerSelectionPolicy policy = SigtranMaintainedPeerSelectionPolicy.CreateDefault();
        SigtranMaintainedPeerSelectionReport selection = policy.Evaluate(profile, satisfiedSelectionCriteria);
        SigtranExternalPeerInteropRunPlan plan = SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg();

        List<string> blockers = [];
        if (!selection.Selected)
        {
            blockers.Add("maintained-peer-selection-required");
        }

        if (!plan.Environment.CanProduceCommercialArtifacts)
        {
            blockers.Add("external-peer-lab-environment-required");
        }

        if (!plan.IsExecutable)
        {
            blockers.Add("external-peer-run-plan-required");
        }

        if (!evidenceRegistry.HasCommercialReviewReadyEvidence)
        {
            blockers.Add("external-peer-review-ready-evidence-required");
        }

        return new(
            selection.Selected,
            plan.Environment.CanProduceCommercialArtifacts,
            plan.IsExecutable,
            evidenceRegistry.HasCommercialReviewReadyEvidence,
            blockers);
    }
}
