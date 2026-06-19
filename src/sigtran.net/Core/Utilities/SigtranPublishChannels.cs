namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a release publication channel.
/// </summary>
public enum SigtranPublishChannelKind
{
    /// <summary>Internal development feed.</summary>
    Internal,

    /// <summary>Public alpha feed.</summary>
    Alpha,

    /// <summary>Public beta feed.</summary>
    Beta,

    /// <summary>Public stable feed.</summary>
    Stable
}

/// <summary>
/// Describes one publication channel.
/// </summary>
public sealed class SigtranPublishChannel
{
    /// <summary>Creates a publication channel.</summary>
    /// <param name="kind">The channel kind.</param>
    /// <param name="feedName">The feed name.</param>
    /// <param name="requiresCommercialReadiness">Whether commercial readiness is required.</param>
    /// <param name="allowsPrereleaseVersions">Whether prerelease versions are allowed.</param>
    public SigtranPublishChannel(
        SigtranPublishChannelKind kind,
        string feedName,
        bool requiresCommercialReadiness,
        bool allowsPrereleaseVersions)
    {
        Kind = kind;
        FeedName = string.IsNullOrWhiteSpace(feedName) ? throw new ArgumentException("Feed name is required.", nameof(feedName)) : feedName;
        RequiresCommercialReadiness = requiresCommercialReadiness;
        AllowsPrereleaseVersions = allowsPrereleaseVersions;
    }

    /// <summary>The channel kind.</summary>
    public SigtranPublishChannelKind Kind { get; }

    /// <summary>The feed name.</summary>
    public string FeedName { get; }

    /// <summary>Whether commercial readiness is required.</summary>
    public bool RequiresCommercialReadiness { get; }

    /// <summary>Whether prerelease versions are allowed.</summary>
    public bool AllowsPrereleaseVersions { get; }

    /// <summary>Returns whether the channel accepts a package version.</summary>
    /// <param name="version">The package version.</param>
    /// <returns>True when the version is accepted; otherwise false.</returns>
    public bool AcceptsVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        bool isPrerelease = version.Contains('-', StringComparison.Ordinal);
        return AllowsPrereleaseVersions || !isPrerelease;
    }
}

/// <summary>
/// Provides official publication channel rules.
/// </summary>
public static class SigtranPublishChannels
{
    /// <summary>Returns the official publication channels.</summary>
    /// <returns>The official publication channels.</returns>
    public static IReadOnlyList<SigtranPublishChannel> GetChannels()
    {
        return
        [
            new SigtranPublishChannel(SigtranPublishChannelKind.Internal, "internal", requiresCommercialReadiness: false, allowsPrereleaseVersions: true),
            new SigtranPublishChannel(SigtranPublishChannelKind.Alpha, "nuget-alpha", requiresCommercialReadiness: false, allowsPrereleaseVersions: true),
            new SigtranPublishChannel(SigtranPublishChannelKind.Beta, "nuget-beta", requiresCommercialReadiness: false, allowsPrereleaseVersions: true),
            new SigtranPublishChannel(SigtranPublishChannelKind.Stable, "nuget.org", requiresCommercialReadiness: true, allowsPrereleaseVersions: false)
        ];
    }
}
