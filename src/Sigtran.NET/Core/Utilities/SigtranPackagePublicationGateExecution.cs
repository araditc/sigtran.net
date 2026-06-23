namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes final package publication gate execution.
/// </summary>
public sealed class SigtranPackagePublicationGateExecution
{
    /// <summary>Creates a package publication gate execution result.</summary>
    /// <param name="channelPolicyEvaluation">The package publication channel policy evaluation.</param>
    /// <param name="gateResult">The package publication gate result.</param>
    /// <param name="metadataReady">Whether NuGet metadata is ready.</param>
    /// <param name="layoutReady">Whether package layout is ready.</param>
    /// <param name="executedAtUtc">The UTC execution time.</param>
    public SigtranPackagePublicationGateExecution(
        SigtranPackagePublicationChannelPolicyEvaluation channelPolicyEvaluation,
        SigtranPublicationGateResult gateResult,
        bool metadataReady,
        bool layoutReady,
        DateTimeOffset executedAtUtc)
    {
        ChannelPolicyEvaluation = channelPolicyEvaluation ?? throw new ArgumentNullException(nameof(channelPolicyEvaluation));
        GateResult = gateResult ?? throw new ArgumentNullException(nameof(gateResult));
        MetadataReady = metadataReady;
        LayoutReady = layoutReady;
        ExecutedAtUtc = executedAtUtc.Offset == TimeSpan.Zero ? executedAtUtc : executedAtUtc.ToUniversalTime();
    }

    /// <summary>The package publication channel policy evaluation.</summary>
    public SigtranPackagePublicationChannelPolicyEvaluation ChannelPolicyEvaluation { get; }

    /// <summary>The package publication gate result.</summary>
    public SigtranPublicationGateResult GateResult { get; }

    /// <summary>Whether NuGet metadata is ready.</summary>
    public bool MetadataReady { get; }

    /// <summary>Whether package layout is ready.</summary>
    public bool LayoutReady { get; }

    /// <summary>The UTC execution time.</summary>
    public DateTimeOffset ExecutedAtUtc { get; }

    /// <summary>Whether the execution time is normalized to UTC.</summary>
    public bool HasUtcExecutionTime => ExecutedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the final package publication gate allows publication.</summary>
    public bool GateAllowsPublication => GateResult.CanPublish;

    /// <summary>Whether package publication is allowed by every integrated gate.</summary>
    public bool IsPublicationAllowed => ChannelPolicyEvaluation.IsReadyForPublicationGate
        && GateAllowsPublication
        && HasUtcExecutionTime;

    /// <summary>Formats a compact package publication gate execution summary.</summary>
    /// <returns>The package publication gate execution summary.</returns>
    public string Describe()
    {
        return $"packagePublicationGateAllowed={IsPublicationAllowed} gateCanPublish={GateResult.CanPublish} reasons={GateResult.Reasons.Count}";
    }
}

/// <summary>
/// Provides package publication gate execution helpers.
/// </summary>
public static class SigtranPackagePublicationGateExecutions
{
    /// <summary>Executes the final package publication gate from channel policy evaluation.</summary>
    /// <param name="channelPolicyEvaluation">The package publication channel policy evaluation.</param>
    /// <param name="metadataReady">Whether NuGet metadata is ready.</param>
    /// <param name="layoutReady">Whether package layout is ready.</param>
    /// <param name="executedAtUtc">The UTC execution time.</param>
    /// <returns>The package publication gate execution.</returns>
    public static SigtranPackagePublicationGateExecution Execute(
        SigtranPackagePublicationChannelPolicyEvaluation channelPolicyEvaluation,
        bool metadataReady,
        bool layoutReady,
        DateTimeOffset executedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(channelPolicyEvaluation);
        SigtranPackagePublicationCredentialReadiness credentials = channelPolicyEvaluation.PublishGuardEvaluation.EvidenceAssembly.CredentialReadiness;
        SigtranPublicationGateResult result = SigtranPublicationGate.Evaluate(
            channelPolicyEvaluation.PublishGuardEvaluation.PublishGuardResult,
            channelPolicyEvaluation.ChannelDecision,
            credentials.CredentialPolicy,
            credentials.AvailableSecretNames,
            channelPolicyEvaluation.PublishGuardEvaluation.EvidenceAssembly.EvidenceManifest,
            metadataReady,
            layoutReady);

        return new(channelPolicyEvaluation, result, metadataReady, layoutReady, executedAtUtc);
    }
}
