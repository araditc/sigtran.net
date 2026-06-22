namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a publication handoff for an approved commercial evidence run.
/// </summary>
public sealed class SigtranCommercialEvidencePublicationHandoff
{
    /// <summary>Creates an approved commercial evidence publication handoff.</summary>
    /// <param name="promotionPackage">The approved run promotion package.</param>
    /// <param name="channel">The requested publication channel.</param>
    /// <param name="requestedBy">The requester identity.</param>
    /// <param name="createdAtUtc">The UTC handoff creation time.</param>
    /// <param name="publishRequested">Whether publication was explicitly requested.</param>
    public SigtranCommercialEvidencePublicationHandoff(
        SigtranCommercialEvidenceApprovedRunPromotionPackage promotionPackage,
        SigtranPublishChannel channel,
        string requestedBy,
        DateTimeOffset createdAtUtc,
        bool publishRequested)
    {
        PromotionPackage = promotionPackage ?? throw new ArgumentNullException(nameof(promotionPackage));
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? throw new ArgumentException("Requester is required.", nameof(requestedBy)) : requestedBy;
        CreatedAtUtc = createdAtUtc.Offset == TimeSpan.Zero ? createdAtUtc : createdAtUtc.ToUniversalTime();
        PublishRequested = publishRequested;
    }

    /// <summary>The approved run promotion package.</summary>
    public SigtranCommercialEvidenceApprovedRunPromotionPackage PromotionPackage { get; }

    /// <summary>The requested publication channel.</summary>
    public SigtranPublishChannel Channel { get; }

    /// <summary>The requester identity.</summary>
    public string RequestedBy { get; }

    /// <summary>The UTC handoff creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Whether publication was explicitly requested.</summary>
    public bool PublishRequested { get; }

    /// <summary>The package version covered by the handoff.</summary>
    public string PackageVersion => PromotionPackage.ApprovalReport.Manifest.Checklist.Target.PackageVersion;

    /// <summary>Whether the handoff creation time is normalized to UTC.</summary>
    public bool HasUtcCreationTime => CreatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the selected publication channel accepts the package version.</summary>
    public bool ChannelAcceptsPackageVersion => Channel.AcceptsVersion(PackageVersion);

    /// <summary>Whether the selected publication channel requires commercial readiness.</summary>
    public bool RequiresCommercialReadiness => Channel.RequiresCommercialReadiness;

    /// <summary>Whether the handoff is ready for publication gate evaluation.</summary>
    public bool IsReadyForPublicationGate => PromotionPackage.IsReady
        && PublishRequested
        && HasUtcCreationTime
        && ChannelAcceptsPackageVersion;

    /// <summary>Formats a compact publication handoff summary.</summary>
    /// <returns>The publication handoff summary.</returns>
    public string Describe()
    {
        return $"commercialEvidencePublicationHandoffReady={IsReadyForPublicationGate} channel={Channel.FeedName} version={PackageVersion}";
    }
}

/// <summary>
/// Provides approved commercial evidence publication handoff helpers.
/// </summary>
public static class SigtranCommercialEvidencePublicationHandoffs
{
    /// <summary>Creates a publication handoff for an approved commercial evidence run.</summary>
    /// <param name="promotionPackage">The approved run promotion package.</param>
    /// <param name="channelKind">The requested publication channel kind.</param>
    /// <param name="requestedBy">The requester identity.</param>
    /// <param name="createdAtUtc">The UTC handoff creation time.</param>
    /// <param name="publishRequested">Whether publication was explicitly requested.</param>
    /// <returns>The approved commercial evidence publication handoff.</returns>
    public static SigtranCommercialEvidencePublicationHandoff Create(
        SigtranCommercialEvidenceApprovedRunPromotionPackage promotionPackage,
        SigtranPublishChannelKind channelKind,
        string requestedBy,
        DateTimeOffset createdAtUtc,
        bool publishRequested = true)
    {
        SigtranPublishChannel channel = SigtranPublishChannels.GetChannels().First(item => item.Kind == channelKind);
        return new(promotionPackage, channel, requestedBy, createdAtUtc, publishRequested);
    }
}
