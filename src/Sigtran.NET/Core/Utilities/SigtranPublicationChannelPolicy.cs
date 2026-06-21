namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes publication channel policy evaluation.
/// </summary>
public sealed class SigtranPublicationChannelDecision
{
    /// <summary>Creates a publication channel decision.</summary>
    /// <param name="allowed">Whether the channel is allowed.</param>
    /// <param name="reasons">The decision reasons.</param>
    public SigtranPublicationChannelDecision(bool allowed, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        Allowed = allowed;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether the channel is allowed.</summary>
    public bool Allowed { get; }

    /// <summary>The decision reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }
}

/// <summary>
/// Evaluates publication channel rules for NuGet package publication.
/// </summary>
public static class SigtranPublicationChannelPolicy
{
    /// <summary>Evaluates whether a package version can use a publication channel.</summary>
    /// <param name="channel">The publication channel.</param>
    /// <param name="version">The package version.</param>
    /// <param name="commercialReadiness">Whether commercial readiness is complete.</param>
    /// <returns>The channel decision.</returns>
    public static SigtranPublicationChannelDecision Evaluate(SigtranPublishChannel channel, string version, bool commercialReadiness)
    {
        ArgumentNullException.ThrowIfNull(channel);

        List<string> reasons = [];
        SigtranReleaseVersionPolicy versionPolicy = SigtranReleaseVersionPolicies.CreateDefault();
        if (!versionPolicy.IsValidPackageVersion(version))
        {
            reasons.Add("valid-version-required");
        }
        else if (!channel.AcceptsVersion(version))
        {
            reasons.Add("channel-version-mismatch");
        }

        if (channel.RequiresCommercialReadiness && !commercialReadiness)
        {
            reasons.Add("commercial-readiness-required");
        }

        return new SigtranPublicationChannelDecision(reasons.Count == 0, reasons);
    }
}
