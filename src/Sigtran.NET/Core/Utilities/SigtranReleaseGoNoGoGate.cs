namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the production go/no-go decision.
/// </summary>
public enum SigtranReleaseGoNoGoDecision
{
    /// <summary>The release must not proceed.</summary>
    NoGo,

    /// <summary>The release may proceed only to evidence-producing execution.</summary>
    EvidenceExecutionOnly,

    /// <summary>The release may proceed to release-candidate publication.</summary>
    Prerelease,

    /// <summary>The release may proceed to stable publication.</summary>
    Stable
}

/// <summary>
/// Groups the inputs evaluated by the production go/no-go gate.
/// </summary>
public sealed class SigtranReleaseGoNoGoInput
{
    /// <summary>Creates production go/no-go input.</summary>
    /// <param name="preflight">The release preflight report.</param>
    /// <param name="environmentProfile">The protected release environment profile.</param>
    /// <param name="handoffPlan">The evidence dossier handoff plan.</param>
    /// <param name="releaseReadiness">The production release execution readiness report.</param>
    public SigtranReleaseGoNoGoInput(
        SigtranReleasePreflightReport preflight,
        SigtranProtectedReleaseEnvironmentProfile environmentProfile,
        SigtranEvidenceDossierHandoffPlan handoffPlan,
        SigtranReleaseExecutionReadinessSnapshot releaseReadiness)
    {
        Preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
        EnvironmentProfile = environmentProfile ?? throw new ArgumentNullException(nameof(environmentProfile));
        HandoffPlan = handoffPlan ?? throw new ArgumentNullException(nameof(handoffPlan));
        ReleaseReadiness = releaseReadiness ?? throw new ArgumentNullException(nameof(releaseReadiness));
    }

    /// <summary>The release preflight report.</summary>
    public SigtranReleasePreflightReport Preflight { get; }

    /// <summary>The protected release environment profile.</summary>
    public SigtranProtectedReleaseEnvironmentProfile EnvironmentProfile { get; }

    /// <summary>The evidence dossier handoff plan.</summary>
    public SigtranEvidenceDossierHandoffPlan HandoffPlan { get; }

    /// <summary>The production release execution readiness report.</summary>
    public SigtranReleaseExecutionReadinessSnapshot ReleaseReadiness { get; }
}

/// <summary>
/// Reports the production go/no-go decision.
/// </summary>
public sealed class SigtranReleaseGoNoGoReport
{
    /// <summary>Creates a production go/no-go report.</summary>
    /// <param name="input">The evaluated go/no-go input.</param>
    /// <param name="decision">The gate decision.</param>
    /// <param name="blockers">The gate blockers.</param>
    public SigtranReleaseGoNoGoReport(
        SigtranReleaseGoNoGoInput input,
        SigtranReleaseGoNoGoDecision decision,
        IReadOnlyList<string> blockers)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Decision = decision;
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The evaluated go/no-go input.</summary>
    public SigtranReleaseGoNoGoInput Input { get; }

    /// <summary>The gate decision.</summary>
    public SigtranReleaseGoNoGoDecision Decision { get; }

    /// <summary>The gate blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether evidence-producing execution may start.</summary>
    public bool CanStartEvidenceExecution => Decision is SigtranReleaseGoNoGoDecision.EvidenceExecutionOnly
        or SigtranReleaseGoNoGoDecision.Prerelease
        or SigtranReleaseGoNoGoDecision.Stable;

    /// <summary>Whether package publication is allowed by the gate.</summary>
    public bool CanPublishPackage => Decision is SigtranReleaseGoNoGoDecision.Prerelease
        or SigtranReleaseGoNoGoDecision.Stable;

    /// <summary>Whether stable publication is allowed by the gate.</summary>
    public bool CanPublishStablePackage => Decision == SigtranReleaseGoNoGoDecision.Stable;

    /// <summary>Formats a compact go/no-go summary.</summary>
    /// <returns>The go/no-go summary.</returns>
    public string Describe()
    {
        return $"productionGoNoGo={Decision} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides production go/no-go gate helpers.
/// </summary>
public static class SigtranReleaseGoNoGoGates
{
    /// <summary>Evaluates the production go/no-go gate.</summary>
    /// <param name="input">The go/no-go input.</param>
    /// <returns>The production go/no-go report.</returns>
    public static SigtranReleaseGoNoGoReport Evaluate(SigtranReleaseGoNoGoInput input)
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
            return new(input, SigtranReleaseGoNoGoDecision.NoGo, blockers);
        }

        if (!input.ReleaseReadiness.ReleaseReady)
        {
            return new(input, SigtranReleaseGoNoGoDecision.EvidenceExecutionOnly, ["production-release-evidence-incomplete"]);
        }

        return input.Preflight.Input.Target.Channel == "stable"
            ? new(input, SigtranReleaseGoNoGoDecision.Stable, [])
            : new(input, SigtranReleaseGoNoGoDecision.Prerelease, []);
    }

    /// <summary>Creates the current go/no-go input from default readiness contracts.</summary>
    /// <param name="version">The release-candidate package version.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <param name="availableSecretNames">The available release secret names.</param>
    /// <returns>The current production go/no-go input.</returns>
    public static SigtranReleaseGoNoGoInput CreateCurrentInput(
        string version,
        string sourceCommit,
        IEnumerable<string> availableSecretNames)
    {
        SigtranReleasePreflightInput preflightInput = SigtranReleasePreflightChecks.CreatePrereleaseInput(
            version,
            sourceCommit,
            availableSecretNames);
        SigtranReleasePreflightReport preflight = SigtranReleasePreflightChecks.Evaluate(preflightInput);
        SigtranEvidenceDossierHandoffPlan handoff = SigtranEvidenceDossierHandoffs.CreateDefault(
            preflightInput.Target,
            preflightInput.RetentionMap,
            preflightInput.Checklist);

        return new(
            preflight,
            SigtranProtectedReleaseEnvironments.CreateDefault(),
            handoff,
            SigtranReleaseExecutionReadiness.CreateCurrent());
    }
}
