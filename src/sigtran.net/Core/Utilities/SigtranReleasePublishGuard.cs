namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes release publish context.
/// </summary>
public sealed class SigtranReleasePublishContext
{
    /// <summary>Creates a release publish context.</summary>
    /// <param name="isManualDispatch">Whether the workflow was manually dispatched.</param>
    /// <param name="publishRequested">Whether publishing was requested.</param>
    /// <param name="isVersionTag">Whether the run is associated with a version tag.</param>
    /// <param name="hasNuGetApiKey">Whether the NuGet API key is available.</param>
    public SigtranReleasePublishContext(
        bool isManualDispatch,
        bool publishRequested,
        bool isVersionTag,
        bool hasNuGetApiKey)
    {
        IsManualDispatch = isManualDispatch;
        PublishRequested = publishRequested;
        IsVersionTag = isVersionTag;
        HasNuGetApiKey = hasNuGetApiKey;
    }

    /// <summary>Whether the workflow was manually dispatched.</summary>
    public bool IsManualDispatch { get; }

    /// <summary>Whether publishing was requested.</summary>
    public bool PublishRequested { get; }

    /// <summary>Whether the run is associated with a version tag.</summary>
    public bool IsVersionTag { get; }

    /// <summary>Whether the NuGet API key is available.</summary>
    public bool HasNuGetApiKey { get; }
}

/// <summary>
/// Describes release publish guard evaluation.
/// </summary>
public sealed class SigtranReleasePublishGuardResult
{
    /// <summary>Creates a release publish guard result.</summary>
    /// <param name="canPublish">Whether publishing is allowed.</param>
    /// <param name="reasons">The guard reasons.</param>
    public SigtranReleasePublishGuardResult(bool canPublish, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPublish = canPublish;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether publishing is allowed.</summary>
    public bool CanPublish { get; }

    /// <summary>The guard reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact publish guard summary.</summary>
    /// <returns>The publish guard summary.</returns>
    public string Describe()
    {
        return $"canPublish={CanPublish} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates release publish guard rules.
/// </summary>
public static class SigtranReleasePublishGuard
{
    /// <summary>Evaluates whether release publishing is allowed.</summary>
    /// <param name="context">The publish context.</param>
    /// <returns>The publish guard result.</returns>
    public static SigtranReleasePublishGuardResult Evaluate(SigtranReleasePublishContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<string> reasons = [];
        if (!context.PublishRequested)
        {
            reasons.Add("publish-not-requested");
        }

        if (!context.IsManualDispatch)
        {
            reasons.Add("manual-dispatch-required");
        }

        if (!context.IsVersionTag)
        {
            reasons.Add("version-tag-required");
        }

        if (!context.HasNuGetApiKey)
        {
            reasons.Add("nuget-api-key-required");
        }

        return new SigtranReleasePublishGuardResult(reasons.Count == 0, reasons);
    }
}
