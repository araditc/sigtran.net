namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a package artifact that can participate in publication evaluation.
/// </summary>
public sealed class SigtranPackagePublicationArtifact
{
    /// <summary>Creates a package publication artifact reference.</summary>
    /// <param name="kind">The package artifact kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="sizeBytes">The retained artifact size in bytes.</param>
    /// <param name="required">Whether the artifact is required for publication.</param>
    public SigtranPackagePublicationArtifact(
        SigtranPackageArtifactKind kind,
        string path,
        string sha256,
        long sizeBytes,
        bool required)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        SizeBytes = sizeBytes >= 0 ? sizeBytes : throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        Required = required;
    }

    /// <summary>The package artifact kind.</summary>
    public SigtranPackageArtifactKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>The retained artifact size in bytes.</summary>
    public long SizeBytes { get; }

    /// <summary>Whether the artifact is required for publication.</summary>
    public bool Required { get; }

    /// <summary>Whether the artifact digest is a valid SHA-256 hex value.</summary>
    public bool HasValidDigest => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);

    /// <summary>Whether the artifact has a non-empty retained size.</summary>
    public bool HasContent => SizeBytes > 0;

    /// <summary>Whether the artifact can participate in publication evaluation.</summary>
    public bool IsReady => !Required || (HasValidDigest && HasContent);
}

/// <summary>
/// Binds package artifacts to a package publication request.
/// </summary>
public sealed class SigtranPackagePublicationArtifactSet
{
    /// <summary>Creates a package publication artifact set.</summary>
    /// <param name="request">The package publication request.</param>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="artifacts">The retained package artifacts.</param>
    public SigtranPackagePublicationArtifactSet(
        SigtranPackagePublicationRequest request,
        string packageId,
        IReadOnlyList<SigtranPackagePublicationArtifact> artifacts)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        ArgumentNullException.ThrowIfNull(artifacts);
        Artifacts = artifacts.Count == 0 ? throw new ArgumentException("At least one package artifact is required.", nameof(artifacts)) : artifacts.ToArray();
    }

    /// <summary>The package publication request.</summary>
    public SigtranPackagePublicationRequest Request { get; }

    /// <summary>The package identifier.</summary>
    public string PackageId { get; }

    /// <summary>The retained package artifacts.</summary>
    public IReadOnlyList<SigtranPackagePublicationArtifact> Artifacts { get; }

    /// <summary>Whether the artifact set includes a NuGet package.</summary>
    public bool IncludesPackage => Artifacts.Any(static artifact => artifact.Kind == SigtranPackageArtifactKind.Package);

    /// <summary>Whether the artifact set includes a symbols package.</summary>
    public bool IncludesSymbolPackage => Artifacts.Any(static artifact => artifact.Kind == SigtranPackageArtifactKind.SymbolPackage);

    /// <summary>Whether each artifact kind appears only once.</summary>
    public bool UsesUniqueArtifactKinds => Artifacts
        .Select(static artifact => artifact.Kind)
        .Distinct()
        .Count() == Artifacts.Count;

    /// <summary>Whether required artifacts have valid digests and retained content.</summary>
    public bool RequiredArtifactsAreReady => Artifacts
        .Where(static artifact => artifact.Required)
        .All(static artifact => artifact.IsReady);

    /// <summary>Whether artifact paths match the requested package version.</summary>
    public bool PathsMatchRequestedVersion => Artifacts.All(artifact =>
        artifact.Path.Contains($"{PackageId}.{Request.PackageVersion}", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the artifacts are ready for credential evaluation.</summary>
    public bool IsReadyForCredentialEvaluation => Request.IsReadyForArtifactBinding
        && IncludesPackage
        && IncludesSymbolPackage
        && UsesUniqueArtifactKinds
        && RequiredArtifactsAreReady
        && PathsMatchRequestedVersion;

    /// <summary>Creates a package integrity manifest from the bound publication artifacts.</summary>
    /// <returns>The package integrity manifest.</returns>
    public SigtranPackageIntegrityManifest ToIntegrityManifest()
    {
        SigtranPackageIntegrityManifest manifest = new();
        foreach (SigtranPackagePublicationArtifact artifact in Artifacts)
        {
            manifest.Add(new SigtranPackageIntegrityEntry(artifact.Kind, artifact.Path, artifact.Sha256));
        }

        return manifest;
    }

    /// <summary>Formats a compact package publication artifact set summary.</summary>
    /// <returns>The package publication artifact set summary.</returns>
    public string Describe()
    {
        return $"packagePublicationArtifactsReady={IsReadyForCredentialEvaluation} packageId={PackageId} artifacts={Artifacts.Count}";
    }
}

/// <summary>
/// Provides package publication artifact set helpers.
/// </summary>
public static class SigtranPackagePublicationArtifacts
{
    /// <summary>Creates a package publication artifact set.</summary>
    /// <param name="request">The package publication request.</param>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="artifacts">The retained package artifacts.</param>
    /// <returns>The package publication artifact set.</returns>
    public static SigtranPackagePublicationArtifactSet Create(
        SigtranPackagePublicationRequest request,
        string packageId,
        IReadOnlyList<SigtranPackagePublicationArtifact> artifacts)
    {
        return new(request, packageId, artifacts);
    }
}
