using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab runner evidence package item kinds.
/// </summary>
public enum SigtranReferencePeerLabRunnerEvidencePackageItemKind
{
    /// <summary>Retained lab artifact item.</summary>
    RetainedArtifact,

    /// <summary>Digest manifest item.</summary>
    DigestManifest,

    /// <summary>Provenance report item.</summary>
    ProvenanceReport,

    /// <summary>Failure report item.</summary>
    FailureReport
}

/// <summary>
/// Describes one reference external peer lab runner evidence package item.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerEvidencePackageItem
{
    /// <summary>Creates a reference peer lab runner evidence package item.</summary>
    /// <param name="kind">The package item kind.</param>
    /// <param name="name">The item name.</param>
    /// <param name="path">The item path.</param>
    /// <param name="required">Whether the item is required.</param>
    /// <param name="verified">Whether the item is verified.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranReferencePeerLabRunnerEvidencePackageItem(
        SigtranReferencePeerLabRunnerEvidencePackageItemKind kind,
        string name,
        string path,
        bool required,
        bool verified,
        string? sha256 = null)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Item name is required.", nameof(name)) : name;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Item path is required.", nameof(path)) : path;
        Required = required;
        Verified = verified;
        Sha256 = sha256;
    }

    /// <summary>The package item kind.</summary>
    public SigtranReferencePeerLabRunnerEvidencePackageItemKind Kind { get; }

    /// <summary>The item name.</summary>
    public string Name { get; }

    /// <summary>The item path.</summary>
    public string Path { get; }

    /// <summary>Whether the item is required.</summary>
    public bool Required { get; }

    /// <summary>Whether the item is verified.</summary>
    public bool Verified { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the item has a SHA-256 digest value.</summary>
    public bool HasDigest => !string.IsNullOrWhiteSpace(Sha256);

    /// <summary>Formats a compact package item summary.</summary>
    /// <returns>The package item summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} name={Name} required={Required} verified={Verified} digest={HasDigest} path={Path}";
    }
}

/// <summary>
/// Describes a reference external peer lab runner evidence package manifest.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerEvidencePackageManifest
{
    private readonly SigtranReferencePeerLabRunnerEvidencePackageItem[] _items;

    /// <summary>Creates a reference peer lab runner evidence package manifest.</summary>
    /// <param name="artifactVerification">The artifact verification report.</param>
    /// <param name="provenance">The provenance report.</param>
    /// <param name="comparisonHandoff">The comparison handoff.</param>
    /// <param name="failureReport">The failure report.</param>
    /// <param name="items">The package items.</param>
    public SigtranReferencePeerLabRunnerEvidencePackageManifest(
        SigtranReferencePeerLabRunnerArtifactVerificationReport artifactVerification,
        SigtranReferencePeerLabRunnerProvenanceReport provenance,
        SigtranReferencePeerLabRunnerComparisonHandoff comparisonHandoff,
        SigtranReferencePeerLabRunnerFailureReport failureReport,
        IReadOnlyList<SigtranReferencePeerLabRunnerEvidencePackageItem> items)
    {
        ArgumentNullException.ThrowIfNull(artifactVerification);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(comparisonHandoff);
        ArgumentNullException.ThrowIfNull(failureReport);
        ArgumentNullException.ThrowIfNull(items);

        ArtifactVerification = artifactVerification;
        Provenance = provenance;
        ComparisonHandoff = comparisonHandoff;
        FailureReport = failureReport;
        _items = items.Count == 0 ? throw new ArgumentException("At least one evidence package item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The artifact verification report.</summary>
    public SigtranReferencePeerLabRunnerArtifactVerificationReport ArtifactVerification { get; }

    /// <summary>The provenance report.</summary>
    public SigtranReferencePeerLabRunnerProvenanceReport Provenance { get; }

    /// <summary>The comparison handoff.</summary>
    public SigtranReferencePeerLabRunnerComparisonHandoff ComparisonHandoff { get; }

    /// <summary>The failure report.</summary>
    public SigtranReferencePeerLabRunnerFailureReport FailureReport { get; }

    /// <summary>The package items.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerEvidencePackageItem> Items => _items.ToArray();

    /// <summary>The lab run id.</summary>
    public string RunId => Provenance.RunId;

    /// <summary>Whether all retained artifacts are verified.</summary>
    public bool HasVerifiedArtifacts => ArtifactVerification.Verified;

    /// <summary>Whether the package has review-ready provenance.</summary>
    public bool HasReviewProvenance => Provenance.IsReviewReady;

    /// <summary>Whether the comparison handoff is ready.</summary>
    public bool HasComparisonHandoff => ComparisonHandoff.IsHandoffReady;

    /// <summary>Whether the failure report has no blocking failures.</summary>
    public bool HasNoBlockingFailures => FailureReport.Passed;

    /// <summary>Whether every required package item is verified.</summary>
    public bool HasVerifiedRequiredItems => _items.Where(static item => item.Required).All(static item => item.Verified);

    /// <summary>Whether the evidence package is ready for operator handoff.</summary>
    public bool IsPackageReady => HasVerifiedArtifacts
        && HasReviewProvenance
        && HasComparisonHandoff
        && HasNoBlockingFailures
        && HasVerifiedRequiredItems;

    /// <summary>Renders the evidence package manifest as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Evidence Package");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Package ready: `{IsPackageReady}`");
        builder.AppendLine($"Items: `{_items.Length}`");
        builder.AppendLine();
        builder.AppendLine("| Kind | Name | Required | Verified | Digest | Path |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (SigtranReferencePeerLabRunnerEvidencePackageItem item in _items)
        {
            builder.AppendLine($"| {item.Kind} | {item.Name} | {item.Required} | {item.Verified} | {item.HasDigest} | `{item.Path}` |");
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact evidence package manifest summary.</summary>
    /// <returns>The evidence package manifest summary.</returns>
    public string Describe()
    {
        return $"run={RunId} items={_items.Length} artifacts={HasVerifiedArtifacts} provenance={HasReviewProvenance} comparison={HasComparisonHandoff} failures={HasNoBlockingFailures} ready={IsPackageReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner evidence package helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerEvidencePackages
{
    /// <summary>Creates an evidence package manifest from verified runner outputs.</summary>
    /// <param name="artifactVerification">The artifact verification report.</param>
    /// <param name="provenance">The provenance report.</param>
    /// <param name="comparisonHandoff">The comparison handoff.</param>
    /// <param name="failureReport">The failure report.</param>
    /// <returns>The evidence package manifest.</returns>
    public static SigtranReferencePeerLabRunnerEvidencePackageManifest Create(
        SigtranReferencePeerLabRunnerArtifactVerificationReport artifactVerification,
        SigtranReferencePeerLabRunnerProvenanceReport provenance,
        SigtranReferencePeerLabRunnerComparisonHandoff comparisonHandoff,
        SigtranReferencePeerLabRunnerFailureReport failureReport)
    {
        ArgumentNullException.ThrowIfNull(artifactVerification);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(comparisonHandoff);
        ArgumentNullException.ThrowIfNull(failureReport);

        List<SigtranReferencePeerLabRunnerEvidencePackageItem> items = [];
        foreach (SigtranReferencePeerLabRunnerArtifactVerificationItem item in artifactVerification.Items)
        {
            items.Add(new(
                SigtranReferencePeerLabRunnerEvidencePackageItemKind.RetainedArtifact,
                item.Artifact.Kind.ToString(),
                item.Artifact.Path,
                item.Artifact.Required,
                item.Verified,
                item.Digest?.Sha256));
        }

        string reportRoot = artifactVerification.DigestReport.DigestManifest.ArtifactPlan.ArtifactRoot.TrimEnd('/', '\\');
        items.Add(new(
            SigtranReferencePeerLabRunnerEvidencePackageItemKind.DigestManifest,
            "digest-manifest",
            $"{reportRoot}/reports/{artifactVerification.Collection.RunId}-digests.json",
            required: true,
            artifactVerification.DigestReport.HasDigestCoverage));
        items.Add(new(
            SigtranReferencePeerLabRunnerEvidencePackageItemKind.ProvenanceReport,
            "provenance",
            $"{reportRoot}/reports/{artifactVerification.Collection.RunId}-provenance.md",
            required: true,
            provenance.IsReviewReady));
        items.Add(new(
            SigtranReferencePeerLabRunnerEvidencePackageItemKind.FailureReport,
            "failure-report",
            $"{reportRoot}/reports/{artifactVerification.Collection.RunId}-failures.md",
            required: true,
            failureReport.Passed));

        return new(artifactVerification, provenance, comparisonHandoff, failureReport, items);
    }
}
