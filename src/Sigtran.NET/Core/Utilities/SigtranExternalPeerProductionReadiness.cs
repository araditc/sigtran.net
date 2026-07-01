namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes production readiness for external SIGTRAN peer interoperability.
/// </summary>
public sealed class SigtranExternalPeerProductionReadinessSnapshot
{
    /// <summary>Creates an external peer production readiness report.</summary>
    /// <param name="hasSelectedReferencePeer">Whether a reference peer package has been selected.</param>
    /// <param name="hasLabEnvironment">Whether the lab environment can produce production artifacts.</param>
    /// <param name="hasRunPlan">Whether the run plan is executable.</param>
    /// <param name="hasReviewReadyEvidence">Whether passing digest-covered evidence exists.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranExternalPeerProductionReadinessSnapshot(
        bool hasSelectedReferencePeer,
        bool hasLabEnvironment,
        bool hasRunPlan,
        bool hasReviewReadyEvidence,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);
        HasSelectedReferencePeer = hasSelectedReferencePeer;
        HasLabEnvironment = hasLabEnvironment;
        HasRunPlan = hasRunPlan;
        HasReviewReadyEvidence = hasReviewReadyEvidence;
        Blockers = blockers.ToArray();
    }

    /// <summary>Whether a reference peer package has been selected.</summary>
    public bool HasSelectedReferencePeer { get; }

    /// <summary>Whether the lab environment can produce production artifacts.</summary>
    public bool HasLabEnvironment { get; }

    /// <summary>Whether the run plan is executable.</summary>
    public bool HasRunPlan { get; }

    /// <summary>Whether passing digest-covered evidence exists.</summary>
    public bool HasReviewReadyEvidence { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the external peer production interop foundation is ready.</summary>
    public bool FoundationReady => HasSelectedReferencePeer && HasLabEnvironment && HasRunPlan;

    /// <summary>Whether production external peer interoperability can be claimed.</summary>
    public bool ProductionInteropReady => FoundationReady && HasReviewReadyEvidence && Blockers.Count == 0;

    /// <summary>Formats a compact production readiness summary.</summary>
    /// <returns>The production readiness summary.</returns>
    public string Describe()
    {
        return $"externalPeerProductionInteropReady={ProductionInteropReady} foundationReady={FoundationReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides production readiness evaluation for external peer interoperability.
/// </summary>
public static class SigtranExternalPeerProductionReadiness
{
    /// <summary>Creates the current external peer production readiness report.</summary>
    /// <returns>The current external peer production readiness report.</returns>
    public static SigtranExternalPeerProductionReadinessSnapshot CreateCurrent()
    {
        return Create(
            SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway(),
            [],
            SigtranExternalPeerInteropEvidence.CreateCurrentRegistry());
    }

    /// <summary>Creates an external peer production readiness report from selected criteria and evidence.</summary>
    /// <param name="profile">The external peer profile.</param>
    /// <param name="satisfiedSelectionCriteria">The satisfied reference peer selection criterion ids.</param>
    /// <param name="evidenceRegistry">The evidence registry.</param>
    /// <returns>The external peer production readiness report.</returns>
    public static SigtranExternalPeerProductionReadinessSnapshot Create(
        SigtranInteropPeerProfile profile,
        IReadOnlyList<string> satisfiedSelectionCriteria,
        SigtranExternalPeerInteropEvidenceRegistry evidenceRegistry)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(satisfiedSelectionCriteria);
        ArgumentNullException.ThrowIfNull(evidenceRegistry);

        SigtranReferencePeerSelectionPolicy policy = SigtranReferencePeerSelectionPolicy.CreateDefault();
        SigtranReferencePeerSelectionReport selection = policy.Evaluate(profile, satisfiedSelectionCriteria);
        SigtranExternalPeerInteropRunPlan plan = SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg();

        List<string> blockers = [];
        if (!selection.Selected)
        {
            blockers.Add("reference-peer-selection-required");
        }

        if (!plan.Environment.CanProduceProductionArtifacts)
        {
            blockers.Add("external-peer-lab-environment-required");
        }

        if (!plan.IsExecutable)
        {
            blockers.Add("external-peer-run-plan-required");
        }

        if (!evidenceRegistry.HasProductionReviewReadyEvidence)
        {
            blockers.Add("external-peer-review-ready-evidence-required");
        }

        return new(
            selection.Selected,
            plan.Environment.CanProduceProductionArtifacts,
            plan.IsExecutable,
            evidenceRegistry.HasProductionReviewReadyEvidence,
            blockers);
    }
}
