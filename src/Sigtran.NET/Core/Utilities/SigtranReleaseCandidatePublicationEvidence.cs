namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies release candidate publication evidence artifact kinds.
/// </summary>
public enum SigtranReleaseCandidatePublicationEvidenceKind
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
    ReadinessReport,

    /// <summary>Release decision artifact.</summary>
    DecisionRecord,

    /// <summary>Digest manifest artifact.</summary>
    DigestManifest
}

/// <summary>
/// Describes one retained release candidate publication evidence artifact.
/// </summary>
public sealed class SigtranReleaseCandidatePublicationEvidenceItem
{
    /// <summary>Creates a release candidate publication evidence item.</summary>
    /// <param name="kind">The evidence kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="sha256">The artifact SHA-256 digest.</param>
    /// <param name="requiredForPrerelease">Whether the artifact is required for prerelease publication.</param>
    public SigtranReleaseCandidatePublicationEvidenceItem(
        SigtranReleaseCandidatePublicationEvidenceKind kind,
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
    public SigtranReleaseCandidatePublicationEvidenceKind Kind { get; }

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
public sealed class SigtranReleaseCandidatePublicationEvidenceManifest
{
    /// <summary>Creates a release candidate publication evidence manifest.</summary>
    /// <param name="version">The release candidate version.</param>
    /// <param name="decision">The release decision.</param>
    /// <param name="items">The retained evidence artifacts.</param>
    public SigtranReleaseCandidatePublicationEvidenceManifest(
        string version,
        SigtranReleaseDecision decision,
        IReadOnlyList<SigtranReleaseCandidatePublicationEvidenceItem> items)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(items);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Decision = decision;
        Items = items.Count == 0 ? throw new ArgumentException("At least one evidence item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The release candidate version.</summary>
    public string Version { get; }

    /// <summary>The release decision.</summary>
    public SigtranReleaseDecision Decision { get; }

    /// <summary>The retained evidence artifacts.</summary>
    public IReadOnlyList<SigtranReleaseCandidatePublicationEvidenceItem> Items { get; }

    /// <summary>Whether all required prerelease artifacts are present.</summary>
    public bool HasRequiredArtifacts => Has(SigtranReleaseCandidatePublicationEvidenceKind.Package)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.SymbolPackage)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.DryRunEvidence)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.ReleaseNotes)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.MigrationNotes)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.ReadinessReport)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.DecisionRecord)
        && Has(SigtranReleaseCandidatePublicationEvidenceKind.DigestManifest);

    /// <summary>Whether all required prerelease artifacts have digest coverage.</summary>
    public bool HasDigestCoverage => Items
        .Where(static item => item.RequiredForPrerelease)
        .All(static item => item.HasDigest);

    /// <summary>Whether the retained evidence allows RC publication.</summary>
    public bool CanPublishReleaseCandidate => Decision.Kind == SigtranReleaseDecisionKind.ReleaseCandidate
        && HasRequiredArtifacts
        && HasDigestCoverage;

    /// <summary>Whether the retained evidence allows stable publication.</summary>
    public bool CanPublishStable => Decision.Kind == SigtranReleaseDecisionKind.Stable
        && HasRequiredArtifacts
        && HasDigestCoverage;

    private bool Has(SigtranReleaseCandidatePublicationEvidenceKind kind)
    {
        return Items.Any(item => item.Kind == kind && item.RequiredForPrerelease);
    }
}

/// <summary>
/// Provides release candidate publication evidence manifests.
/// </summary>
public static class SigtranReleaseCandidatePublicationEvidence
{
    /// <summary>Creates the default RC publication evidence manifest from retained artifact digests.</summary>
    /// <param name="version">The release candidate version.</param>
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
    public static SigtranReleaseCandidatePublicationEvidenceManifest CreateReleaseCandidate(
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
        SigtranFinalCommercialReadinessReport readiness = SigtranFinalCommercialReadinessReports.CreateReleaseCandidate(
            version,
            releaseNotesSha256,
            migrationNotesSha256,
            hasNuGetApiKey);
        SigtranReleaseDecision decision = SigtranReleaseDecisions.Decide(readiness);
        SigtranReleaseNotesArtifact releaseNotes = SigtranReleaseNotesArtifacts.CreateReleaseCandidate(version, releaseNotesSha256);
        SigtranMigrationNotesArtifact migrationNotes = SigtranMigrationNotesArtifacts.CreateReleaseCandidate("1.0.0-alpha.1", version, migrationNotesSha256);

        return new(
            version,
            decision,
            [
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.Package, $"src/Sigtran.NET/bin/Release/Sigtran.NET.{version}.nupkg", packageSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.SymbolPackage, $"src/Sigtran.NET/bin/Release/Sigtran.NET.{version}.snupkg", symbolPackageSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.DryRunEvidence, $"artifacts/release-dry-run/{version}/release-dry-run.md", dryRunSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.ReleaseNotes, releaseNotes.Path, releaseNotesSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.MigrationNotes, migrationNotes.Path, migrationNotesSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.ReadinessReport, $"artifacts/commercial-readiness/Sigtran.NET.{version}.commercial-readiness.md", readinessReportSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.DecisionRecord, $"artifacts/release-decision/Sigtran.NET.{version}.decision.json", decisionRecordSha256, requiredForPrerelease: true),
                new SigtranReleaseCandidatePublicationEvidenceItem(SigtranReleaseCandidatePublicationEvidenceKind.DigestManifest, $"artifacts/release-digests/Sigtran.NET.{version}.sha256", digestManifestSha256, requiredForPrerelease: true)
            ]);
    }
}
