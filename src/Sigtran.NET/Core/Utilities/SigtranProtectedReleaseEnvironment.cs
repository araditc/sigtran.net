namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a protected release environment channel.
/// </summary>
public enum SigtranProtectedReleaseChannel
{
    /// <summary>Dry-run validation without package publication.</summary>
    DryRun,

    /// <summary>Prerelease publication for release-candidate packages.</summary>
    Prerelease,

    /// <summary>Stable publication for commercially ready packages.</summary>
    Stable
}

/// <summary>
/// Describes one protected release environment rule.
/// </summary>
public sealed class SigtranProtectedReleaseEnvironmentRule
{
    /// <summary>Creates a protected release environment rule.</summary>
    /// <param name="name">The release environment name.</param>
    /// <param name="channel">The release channel.</param>
    /// <param name="requiredReviewerCount">The required reviewer count.</param>
    /// <param name="requiresProtectedRef">Whether the environment requires a protected branch or tag.</param>
    /// <param name="allowsPublication">Whether the environment allows package publication.</param>
    public SigtranProtectedReleaseEnvironmentRule(
        string name,
        SigtranProtectedReleaseChannel channel,
        int requiredReviewerCount,
        bool requiresProtectedRef,
        bool allowsPublication)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Environment name is required.", nameof(name)) : name;
        Channel = channel;
        RequiredReviewerCount = requiredReviewerCount < 0 ? throw new ArgumentOutOfRangeException(nameof(requiredReviewerCount), "Reviewer count cannot be negative.") : requiredReviewerCount;
        RequiresProtectedRef = requiresProtectedRef;
        AllowsPublication = allowsPublication;
    }

    /// <summary>The release environment name.</summary>
    public string Name { get; }

    /// <summary>The release channel.</summary>
    public SigtranProtectedReleaseChannel Channel { get; }

    /// <summary>The required reviewer count.</summary>
    public int RequiredReviewerCount { get; }

    /// <summary>Whether the environment requires a protected branch or tag.</summary>
    public bool RequiresProtectedRef { get; }

    /// <summary>Whether the environment allows package publication.</summary>
    public bool AllowsPublication { get; }

    /// <summary>Whether the rule is strict enough for its release channel.</summary>
    public bool IsProtectedEnough => Channel switch
    {
        SigtranProtectedReleaseChannel.DryRun => !AllowsPublication,
        SigtranProtectedReleaseChannel.Prerelease => AllowsPublication && RequiresProtectedRef && RequiredReviewerCount >= 1,
        SigtranProtectedReleaseChannel.Stable => AllowsPublication && RequiresProtectedRef && RequiredReviewerCount >= 2,
        _ => false
    };
}

/// <summary>
/// Describes the protected release environment profile.
/// </summary>
public sealed class SigtranProtectedReleaseEnvironmentProfile
{
    /// <summary>Creates a protected release environment profile.</summary>
    /// <param name="rules">The protected environment rules.</param>
    public SigtranProtectedReleaseEnvironmentProfile(IReadOnlyList<SigtranProtectedReleaseEnvironmentRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules.Count == 0 ? throw new ArgumentException("At least one protected environment rule is required.", nameof(rules)) : rules.ToArray();
    }

    /// <summary>The protected environment rules.</summary>
    public IReadOnlyList<SigtranProtectedReleaseEnvironmentRule> Rules { get; }

    /// <summary>Whether every release channel has an environment rule.</summary>
    public bool CoversReleaseChannels => Enum.GetValues<SigtranProtectedReleaseChannel>()
        .All(channel => Rules.Any(rule => rule.Channel == channel));

    /// <summary>Whether stable publication requires the strongest approval rule.</summary>
    public bool ProtectsStablePublication => Rules.Any(static rule => rule.Channel == SigtranProtectedReleaseChannel.Stable
        && rule.IsProtectedEnough);

    /// <summary>Whether dry-run execution is blocked from publication.</summary>
    public bool KeepsDryRunNonPublishing => Rules.Any(static rule => rule.Channel == SigtranProtectedReleaseChannel.DryRun
        && rule.IsProtectedEnough);

    /// <summary>Whether all release environment rules are protected enough for their channels.</summary>
    public bool IsReady => CoversReleaseChannels
        && ProtectsStablePublication
        && KeepsDryRunNonPublishing
        && Rules.All(static rule => rule.IsProtectedEnough);

    /// <summary>Formats a compact protected environment profile summary.</summary>
    /// <returns>The protected environment profile summary.</returns>
    public string Describe()
    {
        return $"protectedReleaseEnvironmentsReady={IsReady} rules={Rules.Count}";
    }
}

/// <summary>
/// Provides protected release environment profile helpers.
/// </summary>
public static class SigtranProtectedReleaseEnvironments
{
    /// <summary>Creates the default protected release environment profile.</summary>
    /// <returns>The default protected release environment profile.</returns>
    public static SigtranProtectedReleaseEnvironmentProfile CreateDefault()
    {
        return new(
        [
            new("release-dry-run", SigtranProtectedReleaseChannel.DryRun, requiredReviewerCount: 0, requiresProtectedRef: false, allowsPublication: false),
            new("release-prerelease", SigtranProtectedReleaseChannel.Prerelease, requiredReviewerCount: 1, requiresProtectedRef: true, allowsPublication: true),
            new("release-stable", SigtranProtectedReleaseChannel.Stable, requiredReviewerCount: 2, requiresProtectedRef: true, allowsPublication: true)
        ]);
    }
}
