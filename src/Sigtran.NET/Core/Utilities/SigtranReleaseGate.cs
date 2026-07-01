namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the result of evaluating a release publication gate.
/// </summary>
public sealed class SigtranReleaseGateResult
{
    /// <summary>Creates a release gate result.</summary>
    /// <param name="canPublish">Whether publication is allowed.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranReleaseGateResult(bool canPublish, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPublish = canPublish;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether publication is allowed.</summary>
    public bool CanPublish { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact gate summary.</summary>
    /// <returns>The gate summary.</returns>
    public string Describe()
    {
        return $"canPublish={CanPublish} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates release publication gates.
/// </summary>
public static class SigtranReleaseGate
{
    /// <summary>Evaluates whether a release can be published to a channel.</summary>
    /// <param name="channel">The publication channel.</param>
    /// <param name="artifactManifest">The artifact manifest.</param>
    /// <param name="releaseNotes">The release notes.</param>
    /// <param name="provenance">The release provenance.</param>
    /// <param name="readiness">The production readiness report.</param>
    /// <returns>The release gate result.</returns>
    public static SigtranReleaseGateResult Evaluate(
        SigtranPublishChannel channel,
        SigtranReleaseArtifactManifest artifactManifest,
        SigtranReleaseNotes releaseNotes,
        SigtranReleaseProvenance provenance,
        SigtranProductionReadinessSnapshot readiness)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(artifactManifest);
        ArgumentNullException.ThrowIfNull(releaseNotes);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(readiness);

        List<string> reasons = [];
        if (!channel.AcceptsVersion(releaseNotes.Version))
        {
            reasons.Add("channel-version-rejected");
        }

        if (!artifactManifest.HasRequiredPackageArtifacts())
        {
            reasons.Add("missing-package-artifacts");
        }

        if (!artifactManifest.AllArtifactsHaveDigests())
        {
            reasons.Add("missing-artifact-digests");
        }

        if (!releaseNotes.IsPublishable)
        {
            reasons.Add("release-notes-not-publishable");
        }

        if (!provenance.HasRequiredReferences)
        {
            reasons.Add("provenance-incomplete");
        }

        if (channel.RequiresProductionReadiness && !readiness.ProductionReady)
        {
            reasons.Add("production-readiness-required");
        }

        return new SigtranReleaseGateResult(reasons.Count == 0, reasons);
    }
}
