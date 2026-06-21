namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a configured maintained external peer lab binding.
/// </summary>
public sealed class SigtranMaintainedPeerLabBinding
{
    private readonly Dictionary<string, string> _environmentVariables;
    private readonly string[] _satisfiedCriterionIds;

    /// <summary>Creates a maintained external peer lab binding.</summary>
    /// <param name="id">The stable binding id.</param>
    /// <param name="peerProfile">The peer profile selected for the binding.</param>
    /// <param name="packageId">The lab package id or package-neutral placeholder.</param>
    /// <param name="packageVersion">The lab package version or package-neutral placeholder.</param>
    /// <param name="artifactRoot">The artifact root used by the binding.</param>
    /// <param name="environmentVariables">The environment variables required by lab scripts.</param>
    /// <param name="satisfiedCriterionIds">The maintained peer selection criteria satisfied by the binding.</param>
    public SigtranMaintainedPeerLabBinding(
        string id,
        SigtranInteropPeerProfile peerProfile,
        string packageId,
        string packageVersion,
        string artifactRoot,
        IReadOnlyDictionary<string, string> environmentVariables,
        IReadOnlyList<string> satisfiedCriterionIds)
    {
        ArgumentNullException.ThrowIfNull(peerProfile);
        ArgumentNullException.ThrowIfNull(environmentVariables);
        ArgumentNullException.ThrowIfNull(satisfiedCriterionIds);

        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Binding id is required.", nameof(id)) : id;
        PeerProfile = peerProfile;
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        PackageVersion = string.IsNullOrWhiteSpace(packageVersion) ? throw new ArgumentException("Package version is required.", nameof(packageVersion)) : packageVersion;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        _environmentVariables = environmentVariables.Count == 0
            ? throw new ArgumentException("At least one environment variable is required.", nameof(environmentVariables))
            : new Dictionary<string, string>(environmentVariables, StringComparer.OrdinalIgnoreCase);
        _satisfiedCriterionIds = satisfiedCriterionIds.Count == 0
            ? throw new ArgumentException("At least one satisfied criterion id is required.", nameof(satisfiedCriterionIds))
            : satisfiedCriterionIds.ToArray();
    }

    /// <summary>The stable binding id.</summary>
    public string Id { get; }

    /// <summary>The peer profile selected for the binding.</summary>
    public SigtranInteropPeerProfile PeerProfile { get; }

    /// <summary>The lab package id or package-neutral placeholder.</summary>
    public string PackageId { get; }

    /// <summary>The lab package version or package-neutral placeholder.</summary>
    public string PackageVersion { get; }

    /// <summary>The artifact root used by the binding.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The environment variables required by lab scripts.</summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables => new Dictionary<string, string>(_environmentVariables, StringComparer.OrdinalIgnoreCase);

    /// <summary>The maintained peer selection criteria satisfied by the binding.</summary>
    public IReadOnlyList<string> SatisfiedCriterionIds => _satisfiedCriterionIds.ToArray();

    /// <summary>Evaluates the binding against a maintained peer selection policy.</summary>
    /// <param name="policy">The maintained peer selection policy.</param>
    /// <returns>The maintained peer selection report.</returns>
    public SigtranMaintainedPeerSelectionReport EvaluateSelection(SigtranMaintainedPeerSelectionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.Evaluate(PeerProfile, _satisfiedCriterionIds);
    }

    /// <summary>Formats a compact package-neutral binding summary.</summary>
    /// <returns>The binding summary.</returns>
    public string Describe()
    {
        return $"binding={Id} peer={PeerProfile.Id} packageConfigured={PackageId.Length > 0} versionConfigured={PackageVersion.Length > 0} artifacts={ArtifactRoot} criteria={_satisfiedCriterionIds.Length}";
    }
}

/// <summary>
/// Provides package-neutral maintained external peer lab bindings.
/// </summary>
public static class SigtranMaintainedPeerLabBindings
{
    /// <summary>The environment variable that identifies the external peer profile.</summary>
    public const string PeerIdEnvironmentVariable = "SIGTRAN_EXTERNAL_PEER_ID";

    /// <summary>The environment variable that identifies the external peer package.</summary>
    public const string PackageEnvironmentVariable = "SIGTRAN_EXTERNAL_PEER_PACKAGE";

    /// <summary>The environment variable that identifies the external peer package version.</summary>
    public const string PackageVersionEnvironmentVariable = "SIGTRAN_EXTERNAL_PEER_PACKAGE_VERSION";

    /// <summary>The environment variable that identifies the external peer artifact root.</summary>
    public const string ArtifactRootEnvironmentVariable = "SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT";

    /// <summary>Creates the default maintained external peer lab binding.</summary>
    /// <returns>The default maintained external peer lab binding.</returns>
    public static SigtranMaintainedPeerLabBinding CreateDefault()
    {
        SigtranInteropPeerProfile profile = SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway();
        IReadOnlyList<string> criteria = SigtranMaintainedPeerSelectionPolicy.CreateDefault()
            .GetCriteria()
            .Select(static criterion => criterion.Id)
            .ToArray();

        Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            [PeerIdEnvironmentVariable] = profile.Id,
            [PackageEnvironmentVariable] = "external-peer-package",
            [PackageVersionEnvironmentVariable] = "configured-by-lab",
            [ArtifactRootEnvironmentVariable] = "artifacts/external-peer/maintained"
        };

        return new(
            "maintained-external-peer-lab",
            profile,
            environment[PackageEnvironmentVariable],
            environment[PackageVersionEnvironmentVariable],
            environment[ArtifactRootEnvironmentVariable],
            environment,
            criteria);
    }

    /// <summary>Creates the maintained external peer lab binding catalog.</summary>
    /// <returns>The maintained external peer lab binding catalog.</returns>
    public static IReadOnlyList<SigtranMaintainedPeerLabBinding> CreateCatalog()
    {
        return [CreateDefault()];
    }
}
