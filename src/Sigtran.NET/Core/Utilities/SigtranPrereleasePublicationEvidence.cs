namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies prerelease publication evidence artifact kinds.
/// </summary>
public enum SigtranPrereleasePublicationEvidenceKind
{
    /// <summary>NuGet package artifact.</summary>
    Package,

    /// <summary>NuGet symbol package artifact.</summary>
    SymbolPackage,

    /// <summary>Dry-run release evidence artifact.</summary>
    DryRunEvidence,

    /// <summary>Release notes artifact.</summary>
    ReleaseNotes,

    /// <summary>Migration notes artifact.</summary>
    MigrationNotes,

    /// <summary>Final readiness report artifact.</summary>
    ReadinessSnapshot,

    /// <summary>Release decision artifact.</summary>
    DecisionRecord,

    /// <summary>Digest manifest artifact.</summary>
    DigestManifest
}

/// <summary>
/// Describes one retained prerelease publication evidence artifact.
/// </summary>
public sealed class SigtranPrereleasePublicationEvidenceItem
{
    /// <summary>Creates a prerelease publication evidence item.</summary>
    /// <param name="kind">The evidence kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="sha256">The artifact SHA-256 digest.</param>
    /// <param name="requiredForPrerelease">Whether the artifact is required for prerelease publication.</param>
    public SigtranPrereleasePublicationEvidenceItem(
        SigtranPrereleasePublicationEvidenceKind kind,
        string path,
        string sha256,
        bool requiredForPrerelease)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("Artifact digest is required.", nameof(sha256)) : sha256;
        RequiredForPrerelease = requiredForPrerelease;
    }

    /// <summary>The evidence kind.</summary>
    public SigtranPrereleasePublicationEvidenceKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>The artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the artifact is required for prerelease publication.</summary>
    public bool RequiredForPrerelease { get; }

    /// <summary>Whether the item has SHA-256 digest coverage.</summary>
    public bool HasDigest => Sha256.Length == 64;
}

/// <summary>
/// Describes retained publication evidence for an RC package.
/// </summary>
public sealed class SigtranPrereleasePublicationEvidenceManifest
{
    /// <summary>Creates a prerelease publication evidence manifest.</summary>
    /// <param name="version">The prerelease version.</param>
    /// <param name="decision">The release decision.</param>
    /// <param name="items">The retained evidence artifacts.</param>
    public SigtranPrereleasePublicationEvidenceManifest(
        string version,
        SigtranReleaseDecision decision,
        IReadOnlyList<SigtranPrereleasePublicationEvidenceItem> items)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(items);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Decision = decision;
        Items = items.Count == 0 ? throw new ArgumentException("At least one evidence item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The prerelease version.</summary>
    public string Version { get; }

    /// <summary>The release decision.</summary>
    public SigtranReleaseDecision Decision { get; }

    /// <summary>The retained evidence artifacts.</summary>
    public IReadOnlyList<SigtranPrereleasePublicationEvidenceItem> Items { get; }

    /// <summary>Whether all required prerelease artifacts are present.</summary>
    public bool HasRequiredArtifacts => Has(SigtranPrereleasePublicationEvidenceKind.Package)
        && Has(SigtranPrereleasePublicationEvidenceKind.SymbolPackage)
        && Has(SigtranPrereleasePublicationEvidenceKind.DryRunEvidence)
        && Has(SigtranPrereleasePublicationEvidenceKind.ReleaseNotes)
        && Has(SigtranPrereleasePublicationEvidenceKind.MigrationNotes)
        && Has(SigtranPrereleasePublicationEvidenceKind.ReadinessSnapshot)
        && Has(SigtranPrereleasePublicationEvidenceKind.DecisionRecord)
        && Has(SigtranPrereleasePublicationEvidenceKind.DigestManifest);

    /// <summary>Whether all required prerelease artifacts have digest coverage.</summary>
    public bool HasDigestCoverage => Items
        .Where(static item => item.RequiredForPrerelease)
        .All(static item => item.HasDigest);

    /// <summary>Whether the retained evidence allows RC publication.</summary>
    public bool CanPublishPrerelease => Decision.Kind == SigtranReleaseDecisionKind.Prerelease
        && HasRequiredArtifacts
        && HasDigestCoverage;

    /// <summary>Whether the retained evidence allows stable publication.</summary>
    public bool CanPublishStable => Decision.Kind == SigtranReleaseDecisionKind.Stable
        && HasRequiredArtifacts
        && HasDigestCoverage;

    private bool Has(SigtranPrereleasePublicationEvidenceKind kind)
    {
        return Items.Any(item => item.Kind == kind && item.RequiredForPrerelease);
    }
}

/// <summary>
/// Provides prerelease publication evidence manifests.
/// </summary>
public static class SigtranPrereleasePublicationEvidence
{
    /// <summary>Creates the default RC publication evidence manifest from retained artifact digests.</summary>
    /// <param name="version">The prerelease version.</param>
    /// <param name="packageSha256">The package digest.</param>
    /// <param name="symbolPackageSha256">The symbol package digest.</param>
    /// <param name="dryRunSha256">The dry-run evidence digest.</param>
    /// <param name="releaseNotesSha256">The release notes digest.</param>
    /// <param name="migrationNotesSha256">The migration notes digest.</param>
    /// <param name="readinessReportSha256">The readiness report digest.</param>
    /// <param name="decisionRecordSha256">The decision record digest.</param>
    /// <param name="digestManifestSha256">The digest manifest digest.</param>
    /// <param name="hasNuGetApiKey">Whether the prerelease publication secret is available.</param>
    /// <returns>The default RC publication evidence manifest.</returns>
    public static SigtranPrereleasePublicationEvidenceManifest CreatePrerelease(
        string version,
        string packageSha256,
        string symbolPackageSha256,
        string dryRunSha256,
        string releaseNotesSha256,
        string migrationNotesSha256,
        string readinessReportSha256,
        string decisionRecordSha256,
        string digestManifestSha256,
        bool hasNuGetApiKey)
    {
        SigtranFinalProductionReadinessSnapshot readiness = SigtranFinalProductionReadinessSnapshots.CreatePrerelease(
            version,
            releaseNotesSha256,
            migrationNotesSha256,
            hasNuGetApiKey);
        SigtranReleaseDecision decision = SigtranReleaseDecisions.Decide(readiness);
        SigtranReleaseNotesArtifact releaseNotes = SigtranReleaseNotesArtifacts.CreatePrerelease(version, releaseNotesSha256);
        SigtranMigrationNotesArtifact migrationNotes = SigtranMigrationNotesArtifacts.CreatePrerelease("1.0.0-alpha.1", version, migrationNotesSha256);

        return new(
            version,
            decision,
            [
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.Package, $"src/Sigtran.NET/bin/Release/Sigtran.NET.{version}.nupkg", packageSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.SymbolPackage, $"src/Sigtran.NET/bin/Release/Sigtran.NET.{version}.snupkg", symbolPackageSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.DryRunEvidence, $"artifacts/release-dry-run/{version}/release-dry-run.md", dryRunSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.ReleaseNotes, releaseNotes.Path, releaseNotesSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.MigrationNotes, migrationNotes.Path, migrationNotesSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.ReadinessSnapshot, $"artifacts/production-readiness/Sigtran.NET.{version}.production-readiness.md", readinessReportSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.DecisionRecord, $"artifacts/release-decision/Sigtran.NET.{version}.decision.json", decisionRecordSha256, requiredForPrerelease: true),
                new SigtranPrereleasePublicationEvidenceItem(SigtranPrereleasePublicationEvidenceKind.DigestManifest, $"artifacts/release-digests/Sigtran.NET.{version}.sha256", digestManifestSha256, requiredForPrerelease: true)
            ]);
    }
}
