namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a package integrity entry.
/// </summary>
public sealed class SigtranPackageIntegrityEntry
{
    /// <summary>Creates a package integrity entry.</summary>
    /// <param name="kind">The package artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The SHA-256 digest value.</param>
    public SigtranPackageIntegrityEntry(SigtranPackageArtifactKind kind, string path, string sha256)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
    }

    /// <summary>The package artifact kind.</summary>
    public SigtranPackageArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The SHA-256 digest value.</summary>
    public string Sha256 { get; }
}

/// <summary>
/// Tracks package integrity entries required for publication.
/// </summary>
public sealed class SigtranPackageIntegrityManifest
{
    private readonly List<SigtranPackageIntegrityEntry> _entries = [];

    /// <summary>The recorded integrity entries.</summary>
    public IReadOnlyList<SigtranPackageIntegrityEntry> Entries => _entries.ToArray();

    /// <summary>Adds a package integrity entry.</summary>
    /// <param name="entry">The integrity entry.</param>
    public void Add(SigtranPackageIntegrityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    /// <summary>Whether package and symbol package entries both have digests.</summary>
    public bool IsComplete => HasDigest(SigtranPackageArtifactKind.Package)
        && HasDigest(SigtranPackageArtifactKind.SymbolPackage);

    /// <summary>Creates a complete sample integrity manifest for release evidence tests.</summary>
    /// <returns>The complete sample integrity manifest.</returns>
    public static SigtranPackageIntegrityManifest CreateCompleteSample()
    {
        SigtranPackageIntegrityManifest manifest = new();
        foreach (SigtranPackageArtifactPath artifact in SigtranPackageLayouts.CreateDefault().GetArtifactPaths())
        {
            manifest.Add(new SigtranPackageIntegrityEntry(artifact.Kind, artifact.Path, artifact.Kind == SigtranPackageArtifactKind.Package ? "SHA256-NUPKG" : "SHA256-SNUPKG"));
        }

        return manifest;
    }

    private bool HasDigest(SigtranPackageArtifactKind kind)
    {
        return _entries.Any(entry => entry.Kind == kind && !string.IsNullOrWhiteSpace(entry.Sha256));
    }
}
