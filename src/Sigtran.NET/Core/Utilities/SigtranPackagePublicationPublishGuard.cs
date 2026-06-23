namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes release publish guard evaluation for assembled package publication evidence.
/// </summary>
public sealed class SigtranPackagePublicationPublishGuardEvaluation
{
    /// <summary>Creates a package publication publish guard evaluation.</summary>
    /// <param name="evidenceAssembly">The assembled package publication evidence.</param>
    /// <param name="publishContext">The release publish context.</param>
    /// <param name="publishGuardResult">The release publish guard result.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    public SigtranPackagePublicationPublishGuardEvaluation(
        SigtranPackagePublicationEvidenceAssembly evidenceAssembly,
        SigtranReleasePublishContext publishContext,
        SigtranReleasePublishGuardResult publishGuardResult,
        DateTimeOffset evaluatedAtUtc)
    {
        EvidenceAssembly = evidenceAssembly ?? throw new ArgumentNullException(nameof(evidenceAssembly));
        PublishContext = publishContext ?? throw new ArgumentNullException(nameof(publishContext));
        PublishGuardResult = publishGuardResult ?? throw new ArgumentNullException(nameof(publishGuardResult));
        EvaluatedAtUtc = evaluatedAtUtc.Offset == TimeSpan.Zero ? evaluatedAtUtc : evaluatedAtUtc.ToUniversalTime();
    }

    /// <summary>The assembled package publication evidence.</summary>
    public SigtranPackagePublicationEvidenceAssembly EvidenceAssembly { get; }

    /// <summary>The release publish context.</summary>
    public SigtranReleasePublishContext PublishContext { get; }

    /// <summary>The release publish guard result.</summary>
    public SigtranReleasePublishGuardResult PublishGuardResult { get; }

    /// <summary>The UTC evaluation time.</summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>Whether the evaluation time is normalized to UTC.</summary>
    public bool HasUtcEvaluationTime => EvaluatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the release publish guard allows publication.</summary>
    public bool PublishGuardAllowsPublication => PublishGuardResult.CanPublish;

    /// <summary>Whether evaluation can move into channel policy evaluation.</summary>
    public bool IsReadyForChannelPolicy => EvidenceAssembly.IsReadyForPublishGuard
        && PublishGuardAllowsPublication
        && HasUtcEvaluationTime;

    /// <summary>Formats a compact publish guard evaluation summary.</summary>
    /// <returns>The publish guard evaluation summary.</returns>
    public string Describe()
    {
        return $"packagePublicationPublishGuardReady={IsReadyForChannelPolicy} canPublish={PublishGuardResult.CanPublish} reasons={PublishGuardResult.Reasons.Count}";
    }
}

/// <summary>
/// Provides package publication publish guard helpers.
/// </summary>
public static class SigtranPackagePublicationPublishGuards
{
    /// <summary>Evaluates the release publish guard for assembled package publication evidence.</summary>
    /// <param name="evidenceAssembly">The assembled package publication evidence.</param>
    /// <param name="isManualDispatch">Whether the release workflow was manually dispatched.</param>
    /// <param name="isVersionTag">Whether the release workflow is attached to a version tag.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <returns>The package publication publish guard evaluation.</returns>
    public static SigtranPackagePublicationPublishGuardEvaluation Evaluate(
        SigtranPackagePublicationEvidenceAssembly evidenceAssembly,
        bool isManualDispatch,
        bool isVersionTag,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidenceAssembly);
        SigtranReleasePublishContext context = new(
            isManualDispatch,
            publishRequested: evidenceAssembly.CredentialReadiness.ArtifactSet.Request.HandoffGate.Handoff.PublishRequested,
            isVersionTag,
            hasNuGetApiKey: evidenceAssembly.CredentialReadiness.HasNuGetApiKey);
        SigtranReleasePublishGuardResult guard = SigtranReleasePublishGuard.Evaluate(context);

        return new(evidenceAssembly, context, guard, evaluatedAtUtc);
    }
}
