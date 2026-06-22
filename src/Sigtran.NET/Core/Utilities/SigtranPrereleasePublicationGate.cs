namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a prerelease publication request.
/// </summary>
public sealed class SigtranPrereleasePublicationRequest
{
    /// <summary>Creates a prerelease publication request.</summary>
    /// <param name="version">The package version.</param>
    /// <param name="publishRequested">Whether publication was explicitly requested.</param>
    /// <param name="hasNuGetApiKey">Whether the NuGet API key is available.</param>
    /// <param name="dryRunPassed">Whether the dry-run release passed.</param>
    /// <param name="supplyChainReleaseReady">Whether supply-chain release execution foundation is ready.</param>
    public SigtranPrereleasePublicationRequest(
        string version,
        bool publishRequested,
        bool hasNuGetApiKey,
        bool dryRunPassed,
        bool supplyChainReleaseReady)
    {
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        PublishRequested = publishRequested;
        HasNuGetApiKey = hasNuGetApiKey;
        DryRunPassed = dryRunPassed;
        SupplyChainReleaseReady = supplyChainReleaseReady;
    }

    /// <summary>The package version.</summary>
    public string Version { get; }

    /// <summary>Whether publication was explicitly requested.</summary>
    public bool PublishRequested { get; }

    /// <summary>Whether the NuGet API key is available.</summary>
    public bool HasNuGetApiKey { get; }

    /// <summary>Whether the dry-run release passed.</summary>
    public bool DryRunPassed { get; }

    /// <summary>Whether supply-chain release execution foundation is ready.</summary>
    public bool SupplyChainReleaseReady { get; }
}

/// <summary>
/// Describes prerelease publication gate evaluation.
/// </summary>
public sealed class SigtranPrereleasePublicationGateResult
{
    /// <summary>Creates a prerelease publication gate result.</summary>
    /// <param name="canPublishPrerelease">Whether prerelease publication is allowed.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranPrereleasePublicationGateResult(bool canPublishPrerelease, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPublishPrerelease = canPublishPrerelease;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether prerelease publication is allowed.</summary>
    public bool CanPublishPrerelease { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact prerelease gate summary.</summary>
    /// <returns>The prerelease gate summary.</returns>
    public string Describe()
    {
        return $"canPublishPrerelease={CanPublishPrerelease} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates gated NuGet prerelease publication.
/// </summary>
public static class SigtranPrereleasePublicationGate
{
    /// <summary>Evaluates whether a prerelease package can be published.</summary>
    /// <param name="request">The prerelease publication request.</param>
    /// <returns>The prerelease publication gate result.</returns>
    public static SigtranPrereleasePublicationGateResult Evaluate(SigtranPrereleasePublicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        SigtranReleaseVersionPolicy versionPolicy = SigtranReleaseVersionPolicies.CreateDefault();

        List<string> reasons = [];
        if (!versionPolicy.IsValidPackageVersion(request.Version)
            || versionPolicy.GetLane(request.Version) != SigtranReleaseVersionLane.Prerelease)
        {
            reasons.Add("prerelease-version-required");
        }

        if (!request.PublishRequested)
        {
            reasons.Add("publish-not-requested");
        }

        if (!request.HasNuGetApiKey)
        {
            reasons.Add("nuget-api-key-required");
        }

        if (!request.DryRunPassed)
        {
            reasons.Add("dry-run-release-required");
        }

        if (!request.SupplyChainReleaseReady)
        {
            reasons.Add("supply-chain-release-foundation-required");
        }

        return new SigtranPrereleasePublicationGateResult(reasons.Count == 0, reasons);
    }
}
