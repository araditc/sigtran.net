namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes package publication gate evaluation.
/// </summary>
public sealed class SigtranPublicationGateResult
{
    /// <summary>Creates a publication gate result.</summary>
    /// <param name="canPublish">Whether package publication is allowed.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranPublicationGateResult(bool canPublish, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPublish = canPublish;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether package publication is allowed.</summary>
    public bool CanPublish { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact publication gate summary.</summary>
    /// <returns>The publication gate summary.</returns>
    public string Describe()
    {
        return $"canPublish={CanPublish} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates package publication readiness.
/// </summary>
public static class SigtranPublicationGate
{
    /// <summary>Evaluates whether package publication is allowed.</summary>
    /// <param name="publishGuard">The release publish guard result.</param>
    /// <param name="channelDecision">The publication channel decision.</param>
    /// <param name="credentialPolicy">The credential policy.</param>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <param name="evidence">The publication evidence manifest.</param>
    /// <param name="metadataReady">Whether NuGet metadata is ready.</param>
    /// <param name="layoutReady">Whether package layout is ready.</param>
    /// <returns>The publication gate result.</returns>
    public static SigtranPublicationGateResult Evaluate(
        SigtranReleasePublishGuardResult publishGuard,
        SigtranPublicationChannelDecision channelDecision,
        SigtranPublicationCredentialPolicy credentialPolicy,
        IReadOnlySet<string> availableSecretNames,
        SigtranPublicationEvidenceManifest evidence,
        bool metadataReady,
        bool layoutReady)
    {
        ArgumentNullException.ThrowIfNull(publishGuard);
        ArgumentNullException.ThrowIfNull(channelDecision);
        ArgumentNullException.ThrowIfNull(credentialPolicy);
        ArgumentNullException.ThrowIfNull(availableSecretNames);
        ArgumentNullException.ThrowIfNull(evidence);

        List<string> reasons = [];
        if (!publishGuard.CanPublish)
        {
            reasons.AddRange(publishGuard.Reasons);
        }

        if (!channelDecision.Allowed)
        {
            reasons.AddRange(channelDecision.Reasons);
        }

        reasons.AddRange(credentialPolicy.GetMissingSecrets(availableSecretNames).Select(static secret => $"missing-secret:{secret}"));

        if (!evidence.IsComplete)
        {
            reasons.Add("publication-evidence-required");
        }

        if (!metadataReady)
        {
            reasons.Add("nuget-metadata-required");
        }

        if (!layoutReady)
        {
            reasons.Add("package-layout-required");
        }

        return new SigtranPublicationGateResult(reasons.Count == 0, reasons);
    }
}
