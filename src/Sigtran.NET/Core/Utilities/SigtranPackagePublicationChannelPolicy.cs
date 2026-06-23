namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes publication channel policy evaluation for a guarded package publication.
/// </summary>
public sealed class SigtranPackagePublicationChannelPolicyEvaluation
{
    /// <summary>Creates a package publication channel policy evaluation.</summary>
    /// <param name="publishGuardEvaluation">The package publication publish guard evaluation.</param>
    /// <param name="channelDecision">The publication channel decision.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    public SigtranPackagePublicationChannelPolicyEvaluation(
        SigtranPackagePublicationPublishGuardEvaluation publishGuardEvaluation,
        SigtranPublicationChannelDecision channelDecision,
        DateTimeOffset evaluatedAtUtc)
    {
        PublishGuardEvaluation = publishGuardEvaluation ?? throw new ArgumentNullException(nameof(publishGuardEvaluation));
        ChannelDecision = channelDecision ?? throw new ArgumentNullException(nameof(channelDecision));
        EvaluatedAtUtc = evaluatedAtUtc.Offset == TimeSpan.Zero ? evaluatedAtUtc : evaluatedAtUtc.ToUniversalTime();
    }

    /// <summary>The package publication publish guard evaluation.</summary>
    public SigtranPackagePublicationPublishGuardEvaluation PublishGuardEvaluation { get; }

    /// <summary>The publication channel decision.</summary>
    public SigtranPublicationChannelDecision ChannelDecision { get; }

    /// <summary>The UTC evaluation time.</summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>The requested publication channel.</summary>
    public SigtranPublishChannel Channel => PublishGuardEvaluation.EvidenceAssembly.CredentialReadiness.ArtifactSet.Request.Channel;

    /// <summary>The requested package version.</summary>
    public string PackageVersion => PublishGuardEvaluation.EvidenceAssembly.CredentialReadiness.ArtifactSet.Request.PackageVersion;

    /// <summary>Whether the evaluation time is normalized to UTC.</summary>
    public bool HasUtcEvaluationTime => EvaluatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether channel policy allows the requested package version and readiness state.</summary>
    public bool ChannelPolicyAllowsPublication => ChannelDecision.Allowed;

    /// <summary>Whether evaluation can move into the final package publication gate.</summary>
    public bool IsReadyForPublicationGate => PublishGuardEvaluation.IsReadyForChannelPolicy
        && ChannelPolicyAllowsPublication
        && HasUtcEvaluationTime;

    /// <summary>Formats a compact channel policy evaluation summary.</summary>
    /// <returns>The channel policy evaluation summary.</returns>
    public string Describe()
    {
        return $"packagePublicationChannelReady={IsReadyForPublicationGate} channel={Channel.FeedName} version={PackageVersion} reasons={ChannelDecision.Reasons.Count}";
    }
}

/// <summary>
/// Provides package publication channel policy helpers.
/// </summary>
public static class SigtranPackagePublicationChannelPolicies
{
    /// <summary>Evaluates channel policy for a guarded package publication.</summary>
    /// <param name="publishGuardEvaluation">The package publication publish guard evaluation.</param>
    /// <param name="commercialReadinessApproved">Whether commercial readiness is approved for the requested channel.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <returns>The package publication channel policy evaluation.</returns>
    public static SigtranPackagePublicationChannelPolicyEvaluation Evaluate(
        SigtranPackagePublicationPublishGuardEvaluation publishGuardEvaluation,
        bool commercialReadinessApproved,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(publishGuardEvaluation);
        SigtranPackagePublicationRequest request = publishGuardEvaluation.EvidenceAssembly.CredentialReadiness.ArtifactSet.Request;
        SigtranPublicationChannelDecision decision = SigtranPublicationChannelPolicy.Evaluate(
            request.Channel,
            request.PackageVersion,
            commercialReadinessApproved);

        return new(publishGuardEvaluation, decision, evaluatedAtUtc);
    }
}
