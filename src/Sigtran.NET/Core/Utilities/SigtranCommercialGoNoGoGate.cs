namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the commercial go/no-go decision.
/// </summary>
public enum SigtranCommercialGoNoGoDecision
{
    /// <summary>The release must not proceed.</summary>
    NoGo,

    /// <summary>The release may proceed only to evidence-producing execution.</summary>
    EvidenceExecutionOnly,

    /// <summary>The release may proceed to release-candidate publication.</summary>
    ReleaseCandidate,

    /// <summary>The release may proceed to stable publication.</summary>
    Stable
}

/// <summary>
/// Groups the inputs evaluated by the commercial go/no-go gate.
/// </summary>
public sealed class SigtranCommercialGoNoGoInput
{
    /// <summary>Creates commercial go/no-go input.</summary>
    /// <param name="preflight">The release preflight report.</param>
    /// <param name="environmentProfile">The protected release environment profile.</param>
    /// <param name="handoffPlan">The evidence dossier handoff plan.</param>
    /// <param name="releaseReadiness">The commercial release execution readiness report.</param>
    public SigtranCommercialGoNoGoInput(
        SigtranCommercialReleasePreflightReport preflight,
        SigtranProtectedReleaseEnvironmentProfile environmentProfile,
        SigtranEvidenceDossierHandoffPlan handoffPlan,
        SigtranCommercialReleaseExecutionReadinessReport releaseReadiness)
    {
        Preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
        EnvironmentProfile = environmentProfile ?? throw new ArgumentNullException(nameof(environmentProfile));
        HandoffPlan = handoffPlan ?? throw new ArgumentNullException(nameof(handoffPlan));
        ReleaseReadiness = releaseReadiness ?? throw new ArgumentNullException(nameof(releaseReadiness));
    }

    /// <summary>The release preflight report.</summary>
    public SigtranCommercialReleasePreflightReport Preflight { get; }

    /// <summary>The protected release environment profile.</summary>
    public SigtranProtectedReleaseEnvironmentProfile EnvironmentProfile { get; }

    /// <summary>The evidence dossier handoff plan.</summary>
    public SigtranEvidenceDossierHandoffPlan HandoffPlan { get; }

    /// <summary>The commercial release execution readiness report.</summary>
    public SigtranCommercialReleaseExecutionReadinessReport ReleaseReadiness { get; }
}

/// <summary>
/// Reports the commercial go/no-go decision.
/// </summary>
public sealed class SigtranCommercialGoNoGoReport
{
    /// <summary>Creates a commercial go/no-go report.</summary>
    /// <param name="input">The evaluated go/no-go input.</param>
    /// <param name="decision">The gate decision.</param>
    /// <param name="blockers">The gate blockers.</param>
    public SigtranCommercialGoNoGoReport(
        SigtranCommercialGoNoGoInput input,
        SigtranCommercialGoNoGoDecision decision,
        IReadOnlyList<string> blockers)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Decision = decision;
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The evaluated go/no-go input.</summary>
    public SigtranCommercialGoNoGoInput Input { get; }

    /// <summary>The gate decision.</summary>
    public SigtranCommercialGoNoGoDecision Decision { get; }

    /// <summary>The gate blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether evidence-producing execution may start.</summary>
    public bool CanStartEvidenceExecution => Decision is SigtranCommercialGoNoGoDecision.EvidenceExecutionOnly
        or SigtranCommercialGoNoGoDecision.ReleaseCandidate
        or SigtranCommercialGoNoGoDecision.Stable;

    /// <summary>Whether package publication is allowed by the gate.</summary>
    public bool CanPublishPackage => Decision is SigtranCommercialGoNoGoDecision.ReleaseCandidate
        or SigtranCommercialGoNoGoDecision.Stable;

    /// <summary>Whether stable publication is allowed by the gate.</summary>
    public bool CanPublishStablePackage => Decision == SigtranCommercialGoNoGoDecision.Stable;

    /// <summary>Formats a compact go/no-go summary.</summary>
    /// <returns>The go/no-go summary.</returns>
    public string Describe()
    {
        return $"commercialGoNoGo={Decision} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides commercial go/no-go gate helpers.
/// </summary>
public static class SigtranCommercialGoNoGoGates
{
    /// <summary>Evaluates the commercial go/no-go gate.</summary>
    /// <param name="input">The go/no-go input.</param>
    /// <returns>The commercial go/no-go report.</returns>
    public static SigtranCommercialGoNoGoReport Evaluate(SigtranCommercialGoNoGoInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        List<string> blockers = [];

        if (!input.Preflight.IsReady)
        {
            blockers.Add("release-preflight-not-ready");
        }

        if (!input.EnvironmentProfile.IsReady)
        {
            blockers.Add("protected-release-environment-not-ready");
        }

        if (!input.HandoffPlan.IsReady)
        {
            blockers.Add("evidence-dossier-handoff-not-ready");
        }

        if (blockers.Count > 0)
        {
            return new(input, SigtranCommercialGoNoGoDecision.NoGo, blockers);
        }

        if (!input.ReleaseReadiness.CommercialReleaseReady)
        {
            return new(input, SigtranCommercialGoNoGoDecision.EvidenceExecutionOnly, ["commercial-release-evidence-incomplete"]);
        }

        return input.Preflight.Input.Target.Channel == "stable"
            ? new(input, SigtranCommercialGoNoGoDecision.Stable, [])
            : new(input, SigtranCommercialGoNoGoDecision.ReleaseCandidate, []);
    }

    /// <summary>Creates the current go/no-go input from default readiness contracts.</summary>
    /// <param name="version">The release-candidate package version.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <param name="availableSecretNames">The available release secret names.</param>
    /// <returns>The current commercial go/no-go input.</returns>
    public static SigtranCommercialGoNoGoInput CreateCurrentInput(
        string version,
        string sourceCommit,
        IEnumerable<string> availableSecretNames)
    {
        SigtranCommercialReleasePreflightInput preflightInput = SigtranCommercialReleasePreflightChecks.CreateReleaseCandidateInput(
            version,
            sourceCommit,
            availableSecretNames);
        SigtranCommercialReleasePreflightReport preflight = SigtranCommercialReleasePreflightChecks.Evaluate(preflightInput);
        SigtranEvidenceDossierHandoffPlan handoff = SigtranEvidenceDossierHandoffs.CreateDefault(
            preflightInput.Target,
            preflightInput.RetentionMap,
            preflightInput.Checklist);

        return new(
            preflight,
            SigtranProtectedReleaseEnvironments.CreateDefault(),
            handoff,
            SigtranCommercialReleaseExecutionReadiness.CreateCurrent());
    }
}
