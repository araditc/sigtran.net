using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes verification state for one maintained external peer lab runner artifact.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerArtifactVerificationItem
{
    /// <summary>Creates a maintained peer lab runner artifact verification item.</summary>
    /// <param name="artifact">The collected artifact.</param>
    /// <param name="digest">The matching digest entry, when one was recorded.</param>
    public SigtranMaintainedPeerLabRunnerArtifactVerificationItem(
        SigtranMaintainedPeerLabRunnerCollectedArtifact artifact,
        SigtranMaintainedPeerLabArtifactDigest? digest)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        Artifact = artifact;
        Digest = digest;
    }

    /// <summary>The collected artifact.</summary>
    public SigtranMaintainedPeerLabRunnerCollectedArtifact Artifact { get; }

    /// <summary>The matching digest entry, when one was recorded.</summary>
    public SigtranMaintainedPeerLabArtifactDigest? Digest { get; }

    /// <summary>Whether the artifact is retained.</summary>
    public bool Retained => Artifact.Retained;

    /// <summary>Whether a digest entry exists for the artifact.</summary>
    public bool DigestPresent => Digest is not null;

    /// <summary>Whether the matching digest entry contains a valid SHA-256 value.</summary>
    public bool DigestValid => Digest?.HasValidSha256 == true;

    /// <summary>Whether this artifact satisfies the runner verification contract.</summary>
    public bool Verified => Artifact.Required
        ? Retained && DigestPresent && DigestValid
        : !Retained || (DigestPresent && DigestValid);

    /// <summary>Formats a compact artifact verification summary.</summary>
    /// <returns>The artifact verification summary.</returns>
    public string Describe()
    {
        return $"kind={Artifact.Kind} retained={Retained} digest={DigestPresent} valid={DigestValid} verified={Verified} path={Artifact.Path}";
    }
}

/// <summary>
/// Describes verification output for maintained external peer lab runner artifacts.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerArtifactVerificationReport
{
    private readonly SigtranMaintainedPeerLabRunnerArtifactVerificationItem[] _items;

    /// <summary>Creates a maintained peer lab runner artifact verification report.</summary>
    /// <param name="collection">The evidence collection.</param>
    /// <param name="digestReport">The digest report.</param>
    /// <param name="items">The per-artifact verification items.</param>
    public SigtranMaintainedPeerLabRunnerArtifactVerificationReport(
        SigtranMaintainedPeerLabRunnerEvidenceCollection collection,
        SigtranMaintainedPeerLabRunnerDigestReport digestReport,
        IReadOnlyList<SigtranMaintainedPeerLabRunnerArtifactVerificationItem> items)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(digestReport);
        ArgumentNullException.ThrowIfNull(items);

        Collection = collection;
        DigestReport = digestReport;
        _items = items.Count == 0 ? throw new ArgumentException("At least one artifact verification item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The evidence collection.</summary>
    public SigtranMaintainedPeerLabRunnerEvidenceCollection Collection { get; }

    /// <summary>The digest report.</summary>
    public SigtranMaintainedPeerLabRunnerDigestReport DigestReport { get; }

    /// <summary>The per-artifact verification items.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerArtifactVerificationItem> Items => _items.ToArray();

    /// <summary>The missing required artifact paths.</summary>
    public IReadOnlyList<string> MissingArtifactPaths => _items
        .Where(static item => item.Artifact.Required && !item.Retained)
        .Select(static item => item.Artifact.Path)
        .ToArray();

    /// <summary>The retained artifact paths missing digest entries.</summary>
    public IReadOnlyList<string> MissingDigestPaths => _items
        .Where(static item => item.Retained && !item.DigestPresent)
        .Select(static item => item.Artifact.Path)
        .ToArray();

    /// <summary>The retained artifact paths with invalid digest values.</summary>
    public IReadOnlyList<string> InvalidDigestPaths => _items
        .Where(static item => item.Retained && item.DigestPresent && !item.DigestValid)
        .Select(static item => item.Artifact.Path)
        .ToArray();

    /// <summary>Whether every required retained artifact is digest verified.</summary>
    public bool Verified => _items.All(static item => item.Verified) && DigestReport.HasDigestCoverage;

    /// <summary>Renders the artifact verification report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Runner Artifact Verification");
        builder.AppendLine();
        builder.AppendLine($"Run: `{Collection.RunId}`");
        builder.AppendLine($"Verified: `{Verified}`");
        builder.AppendLine($"Missing artifacts: `{MissingArtifactPaths.Count}`");
        builder.AppendLine($"Missing digests: `{MissingDigestPaths.Count}`");
        builder.AppendLine($"Invalid digests: `{InvalidDigestPaths.Count}`");
        builder.AppendLine();
        builder.AppendLine("| Kind | Retained | Digest | Valid | Verified | Path |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (SigtranMaintainedPeerLabRunnerArtifactVerificationItem item in _items)
        {
            builder.AppendLine($"| {item.Artifact.Kind} | {item.Retained} | {item.DigestPresent} | {item.DigestValid} | {item.Verified} | `{item.Artifact.Path}` |");
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact artifact verification summary.</summary>
    /// <returns>The artifact verification summary.</returns>
    public string Describe()
    {
        return $"run={Collection.RunId} items={_items.Length} missingArtifacts={MissingArtifactPaths.Count} missingDigests={MissingDigestPaths.Count} invalidDigests={InvalidDigestPaths.Count} verified={Verified}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner artifact verification helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerArtifactVerification
{
    /// <summary>Verifies collected artifacts against the retained digest manifest.</summary>
    /// <param name="collection">The evidence collection.</param>
    /// <param name="digestReport">The digest report.</param>
    /// <returns>The artifact verification report.</returns>
    public static SigtranMaintainedPeerLabRunnerArtifactVerificationReport Verify(
        SigtranMaintainedPeerLabRunnerEvidenceCollection collection,
        SigtranMaintainedPeerLabRunnerDigestReport digestReport)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(digestReport);

        if (!string.Equals(collection.RunId, digestReport.Collection.RunId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Digest report must belong to the same run as the evidence collection.", nameof(digestReport));
        }

        IReadOnlyList<SigtranMaintainedPeerLabArtifactDigest> digests = digestReport.DigestManifest.Digests;
        SigtranMaintainedPeerLabRunnerArtifactVerificationItem[] items = collection.Artifacts
            .Select(artifact => new SigtranMaintainedPeerLabRunnerArtifactVerificationItem(
                artifact,
                digests.FirstOrDefault(digest => digest.Kind == artifact.Kind && string.Equals(digest.Path, artifact.Path, StringComparison.Ordinal))))
            .ToArray();

        return new(collection, digestReport, items);
    }
}
