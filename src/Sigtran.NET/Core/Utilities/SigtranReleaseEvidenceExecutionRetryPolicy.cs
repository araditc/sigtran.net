namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retry rule for a production evidence execution blocker kind.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionRetryRule
{
    /// <summary>Creates a production evidence execution retry rule.</summary>
    /// <param name="blockerKind">The blocker kind.</param>
    /// <param name="retryable">Whether the blocker kind may be retried.</param>
    /// <param name="maximumAttempts">The maximum attempt count.</param>
    /// <param name="defaultResumeStageId">The default stage identifier used for resume.</param>
    public SigtranReleaseEvidenceExecutionRetryRule(
        SigtranReleaseEvidenceExecutionBlockerKind blockerKind,
        bool retryable,
        int maximumAttempts,
        string defaultResumeStageId)
    {
        BlockerKind = blockerKind;
        Retryable = retryable;
        MaximumAttempts = maximumAttempts <= 0 ? throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Maximum attempts must be positive.") : maximumAttempts;
        DefaultResumeStageId = string.IsNullOrWhiteSpace(defaultResumeStageId) ? throw new ArgumentException("Default resume stage id is required.", nameof(defaultResumeStageId)) : defaultResumeStageId;
    }

    /// <summary>The blocker kind.</summary>
    public SigtranReleaseEvidenceExecutionBlockerKind BlockerKind { get; }

    /// <summary>Whether the blocker kind may be retried.</summary>
    public bool Retryable { get; }

    /// <summary>The maximum attempt count.</summary>
    public int MaximumAttempts { get; }

    /// <summary>The default stage identifier used for resume.</summary>
    public string DefaultResumeStageId { get; }
}

/// <summary>
/// Describes a retry and resume decision.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionRetryDecision
{
    /// <summary>Creates a production evidence execution retry decision.</summary>
    /// <param name="blocker">The classified blocker.</param>
    /// <param name="canRetry">Whether the run can retry.</param>
    /// <param name="resumeStageId">The stage identifier to resume from.</param>
    /// <param name="requiresManualCorrection">Whether manual correction is required.</param>
    public SigtranReleaseEvidenceExecutionRetryDecision(
        SigtranReleaseEvidenceExecutionBlocker blocker,
        bool canRetry,
        string resumeStageId,
        bool requiresManualCorrection)
    {
        Blocker = blocker ?? throw new ArgumentNullException(nameof(blocker));
        CanRetry = canRetry;
        ResumeStageId = string.IsNullOrWhiteSpace(resumeStageId) ? throw new ArgumentException("Resume stage id is required.", nameof(resumeStageId)) : resumeStageId;
        RequiresManualCorrection = requiresManualCorrection;
    }

    /// <summary>The classified blocker.</summary>
    public SigtranReleaseEvidenceExecutionBlocker Blocker { get; }

    /// <summary>Whether the run can retry.</summary>
    public bool CanRetry { get; }

    /// <summary>The stage identifier to resume from.</summary>
    public string ResumeStageId { get; }

    /// <summary>Whether manual correction is required.</summary>
    public bool RequiresManualCorrection { get; }
}

/// <summary>
/// Describes the production evidence execution retry and resume policy.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionRetryPolicy
{
    /// <summary>Creates a production evidence execution retry policy.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <param name="rules">The retry rules.</param>
    public SigtranReleaseEvidenceExecutionRetryPolicy(
        SigtranReleaseEvidenceExecutionStageCatalog catalog,
        IReadOnlyList<SigtranReleaseEvidenceExecutionRetryRule> rules)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules.Count == 0 ? throw new ArgumentException("At least one retry rule is required.", nameof(rules)) : rules.ToArray();
    }

    /// <summary>The execution stage catalog.</summary>
    public SigtranReleaseEvidenceExecutionStageCatalog Catalog { get; }

    /// <summary>The retry rules.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceExecutionRetryRule> Rules { get; }

    /// <summary>Whether retry rules cover known blocker kinds.</summary>
    public bool CoversKnownBlockers => SigtranReleaseEvidenceExecutionBlockers.CreateDefault().KnownBlockers
        .Select(static blocker => blocker.Kind)
        .Distinct()
        .All(kind => Rules.Any(rule => rule.BlockerKind == kind));

    /// <summary>Whether every resume stage exists in the execution stage catalog.</summary>
    public bool UsesKnownResumeStages => Rules.All(rule => Catalog.Stages.Any(stage => stage.Id == rule.DefaultResumeStageId));

    /// <summary>Whether non-retryable host, approval, and unknown blockers require manual correction.</summary>
    public bool ProtectsNonRetryableFailures => Rules
        .Where(static rule => rule.BlockerKind is SigtranReleaseEvidenceExecutionBlockerKind.NativeSctp
            or SigtranReleaseEvidenceExecutionBlockerKind.ProtectedApproval
            or SigtranReleaseEvidenceExecutionBlockerKind.Unknown)
        .All(static rule => !rule.Retryable);

    /// <summary>Whether the retry policy is ready for execution resume decisions.</summary>
    public bool IsReady => Catalog.IsReady
        && CoversKnownBlockers
        && UsesKnownResumeStages
        && ProtectsNonRetryableFailures;

    /// <summary>Creates a retry decision for a classified blocker.</summary>
    /// <param name="blocker">The classified blocker.</param>
    /// <param name="attemptCount">The current attempt count.</param>
    /// <returns>The retry and resume decision.</returns>
    public SigtranReleaseEvidenceExecutionRetryDecision Decide(
        SigtranReleaseEvidenceExecutionBlocker blocker,
        int attemptCount)
    {
        ArgumentNullException.ThrowIfNull(blocker);
        if (attemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptCount), "Attempt count cannot be negative.");
        }

        SigtranReleaseEvidenceExecutionRetryRule rule = Rules.FirstOrDefault(item => item.BlockerKind == blocker.Kind)
            ?? Rules.First(item => item.BlockerKind == SigtranReleaseEvidenceExecutionBlockerKind.Unknown);
        bool canRetry = blocker.Retryable && rule.Retryable && attemptCount < rule.MaximumAttempts;

        return new(blocker, canRetry, rule.DefaultResumeStageId, requiresManualCorrection: !canRetry);
    }

    /// <summary>Formats a compact retry policy summary.</summary>
    /// <returns>The retry policy summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceRetryPolicyReady={IsReady} rules={Rules.Count}";
    }
}

/// <summary>
/// Provides production evidence execution retry policy helpers.
/// </summary>
public static class SigtranReleaseEvidenceExecutionRetryPolicies
{
    /// <summary>Creates the default retry and resume policy.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <returns>The default retry and resume policy.</returns>
    public static SigtranReleaseEvidenceExecutionRetryPolicy CreateDefault(SigtranReleaseEvidenceExecutionStageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new(
            catalog,
            [
                new(SigtranReleaseEvidenceExecutionBlockerKind.ReadinessPreflight, false, 1, "readiness-preflight"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.Environment, true, 3, "readiness-preflight"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.CommandFailure, true, 2, "readiness-preflight"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.NativeSctp, false, 1, "native-sctp-lab"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.ExternalPeer, true, 3, "external-peer-interop"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.ArtifactRetention, true, 2, "dossier-assembly"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.DigestVerification, true, 2, "dossier-assembly"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.RedactionReview, true, 2, "dossier-assembly"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.ProtectedApproval, false, 1, "dossier-assembly"),
                new(SigtranReleaseEvidenceExecutionBlockerKind.Unknown, false, 1, "readiness-preflight")
            ]);
    }
}
