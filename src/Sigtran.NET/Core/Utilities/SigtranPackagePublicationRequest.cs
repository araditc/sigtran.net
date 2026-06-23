namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a package publication request derived from an approved commercial evidence handoff.
/// </summary>
public sealed class SigtranPackagePublicationRequest
{
    /// <summary>Creates a package publication request.</summary>
    /// <param name="handoffGate">The approved commercial evidence handoff gate result.</param>
    /// <param name="requestedAtUtc">The UTC publication request time.</param>
    public SigtranPackagePublicationRequest(
        SigtranCommercialEvidencePublicationHandoffGateResult handoffGate,
        DateTimeOffset requestedAtUtc)
    {
        HandoffGate = handoffGate ?? throw new ArgumentNullException(nameof(handoffGate));
        RequestedAtUtc = requestedAtUtc.Offset == TimeSpan.Zero ? requestedAtUtc : requestedAtUtc.ToUniversalTime();
    }

    /// <summary>The approved commercial evidence handoff gate result.</summary>
    public SigtranCommercialEvidencePublicationHandoffGateResult HandoffGate { get; }

    /// <summary>The UTC publication request time.</summary>
    public DateTimeOffset RequestedAtUtc { get; }

    /// <summary>The requested package version.</summary>
    public string PackageVersion => HandoffGate.Handoff.PackageVersion;

    /// <summary>The requested publication channel.</summary>
    public SigtranPublishChannel Channel => HandoffGate.Handoff.Channel;

    /// <summary>The requester identity from the handoff.</summary>
    public string RequestedBy => HandoffGate.Handoff.RequestedBy;

    /// <summary>The commercial evidence run identifier.</summary>
    public string RunId => HandoffGate.Handoff.PromotionPackage.ApprovalReport.Manifest.Checklist.Target.RunId;

    /// <summary>The approved promotion package identifier.</summary>
    public string PromotionPackageId => HandoffGate.Handoff.PromotionPackage.PackageId;

    /// <summary>Whether the request time is normalized to UTC.</summary>
    public bool HasUtcRequestTime => RequestedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the upstream handoff gate allows package publication evaluation.</summary>
    public bool HandoffGateAllowsPackageEvaluation => HandoffGate.CanProceedToPackagePublicationGate;

    /// <summary>Whether the request is ready for package artifact binding.</summary>
    public bool IsReadyForArtifactBinding => HasUtcRequestTime
        && HandoffGateAllowsPackageEvaluation;

    /// <summary>Formats a compact package publication request summary.</summary>
    /// <returns>The package publication request summary.</returns>
    public string Describe()
    {
        return $"packagePublicationRequestReady={IsReadyForArtifactBinding} version={PackageVersion} channel={Channel.FeedName} runId={RunId}";
    }
}

/// <summary>
/// Provides package publication request helpers.
/// </summary>
public static class SigtranPackagePublicationRequests
{
    /// <summary>Creates a package publication request from an approved commercial evidence handoff gate.</summary>
    /// <param name="handoffGate">The approved commercial evidence handoff gate result.</param>
    /// <param name="requestedAtUtc">The UTC publication request time.</param>
    /// <returns>The package publication request.</returns>
    public static SigtranPackagePublicationRequest Create(
        SigtranCommercialEvidencePublicationHandoffGateResult handoffGate,
        DateTimeOffset requestedAtUtc)
    {
        return new(handoffGate, requestedAtUtc);
    }
}
