using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one artifact in an approved commercial evidence promotion package.
/// </summary>
public sealed class SigtranCommercialEvidenceApprovedRunPromotionArtifact
{
    /// <summary>Creates an approved run promotion artifact reference.</summary>
    /// <param name="id">The artifact identifier.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="required">Whether the artifact is required.</param>
    public SigtranCommercialEvidenceApprovedRunPromotionArtifact(
        string id,
        string retainedPath,
        string sha256,
        bool required)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Artifact id is required.", nameof(id)) : id;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        Required = required;
    }

    /// <summary>The artifact identifier.</summary>
    public string Id { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the artifact is required.</summary>
    public bool Required { get; }

    /// <summary>Whether the artifact digest is a valid SHA-256 hex value.</summary>
    public bool HasValidDigest => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);

    /// <summary>Whether the artifact can participate in promotion.</summary>
    public bool IsReady => !Required || HasValidDigest;
}

/// <summary>
/// Describes an approved commercial evidence run promotion package.
/// </summary>
public sealed class SigtranCommercialEvidenceApprovedRunPromotionPackage
{
    /// <summary>Creates an approved commercial evidence run promotion package.</summary>
    /// <param name="packageId">The stable package identifier.</param>
    /// <param name="approvalReport">The retained approval report.</param>
    /// <param name="artifacts">The promotion package artifacts.</param>
    /// <param name="createdAtUtc">The UTC package creation time.</param>
    public SigtranCommercialEvidenceApprovedRunPromotionPackage(
        string packageId,
        SigtranCommercialEvidenceRunApprovalReportWriteResult approvalReport,
        IReadOnlyList<SigtranCommercialEvidenceApprovedRunPromotionArtifact> artifacts,
        DateTimeOffset createdAtUtc)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        ApprovalReport = approvalReport ?? throw new ArgumentNullException(nameof(approvalReport));
        ArgumentNullException.ThrowIfNull(artifacts);
        Artifacts = artifacts.Count == 0 ? throw new ArgumentException("At least one promotion artifact is required.", nameof(artifacts)) : artifacts.ToArray();
        CreatedAtUtc = createdAtUtc.Offset == TimeSpan.Zero ? createdAtUtc : createdAtUtc.ToUniversalTime();
    }

    /// <summary>The stable package identifier.</summary>
    public string PackageId { get; }

    /// <summary>The retained approval report.</summary>
    public SigtranCommercialEvidenceRunApprovalReportWriteResult ApprovalReport { get; }

    /// <summary>The promotion package artifacts.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceApprovedRunPromotionArtifact> Artifacts { get; }

    /// <summary>The UTC package creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Whether the package identifier is stable enough for publication handoff references.</summary>
    public bool HasStablePackageId => PackageId.Length >= 8
        && !PackageId.Any(char.IsWhiteSpace);

    /// <summary>Whether the package creation time is normalized to UTC.</summary>
    public bool HasUtcCreationTime => CreatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether all required artifacts have digest coverage.</summary>
    public bool RequiredArtifactsDigestCovered => Artifacts
        .Where(static artifact => artifact.Required)
        .All(static artifact => artifact.HasValidDigest);

    /// <summary>Whether artifact identifiers are unique.</summary>
    public bool UsesUniqueArtifactIds => Artifacts
        .Select(static artifact => artifact.Id)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Artifacts.Count;

    /// <summary>Whether the package includes the retained approval report.</summary>
    public bool IncludesApprovalReport => Artifacts.Any(static artifact => string.Equals(artifact.Id, "approval-report", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the package includes integrity seal evidence.</summary>
    public bool IncludesIntegritySeal => Artifacts.Any(static artifact => string.Equals(artifact.Id, "integrity-seal", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the package includes publication attachment evidence.</summary>
    public bool IncludesPublicationAttachments => Artifacts.Any(static artifact => string.Equals(artifact.Id, "publication-attachments", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the package includes promotion gate evidence.</summary>
    public bool IncludesPromotionGate => Artifacts.Any(static artifact => string.Equals(artifact.Id, "promotion-gate", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the promotion package is ready for publication handoff.</summary>
    public bool IsReady => ApprovalReport.IsReady
        && HasStablePackageId
        && HasUtcCreationTime
        && UsesUniqueArtifactIds
        && RequiredArtifactsDigestCovered
        && IncludesApprovalReport
        && IncludesIntegritySeal
        && IncludesPublicationAttachments
        && IncludesPromotionGate;

    /// <summary>Formats a compact approved run promotion package summary.</summary>
    /// <returns>The approved run promotion package summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceApprovedRunPromotionPackageReady={IsReady} packageId={PackageId} artifacts={Artifacts.Count}";
    }
}

/// <summary>
/// Provides approved commercial evidence run promotion package helpers.
/// </summary>
public static class SigtranCommercialEvidenceApprovedRunPromotionPackages
{
    /// <summary>Creates an approved run promotion package from a retained approval report.</summary>
    /// <param name="approvalReport">The retained approval report.</param>
    /// <param name="createdAtUtc">The UTC package creation time.</param>
    /// <returns>The approved run promotion package.</returns>
    public static SigtranCommercialEvidenceApprovedRunPromotionPackage CreateDefault(
        SigtranCommercialEvidenceRunApprovalReportWriteResult approvalReport,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(approvalReport);
        SigtranCommercialEvidenceApprovedRunTarget target = approvalReport.Manifest.Checklist.Target;
        SigtranCommercialEvidenceFileSystemPromotionExecution promotion = target.PromotionExecution;
        string packageId = $"{target.RunId}-promotion-package";
        string artifactRoot = target.ArtifactRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return new(
            packageId,
            approvalReport,
            [
                new("approval-report", approvalReport.ReportPath, approvalReport.ReportSha256, required: true),
                new("integrity-seal", Path.Combine(artifactRoot, "integrity", promotion.AttachmentExecution.SealExecution.Seal.SealId + ".json"), promotion.AttachmentExecution.SealExecution.Seal.AggregateSha256, required: true),
                new("publication-attachments", Path.Combine(artifactRoot, "publication", "attachments.json"), ComputePublicationAttachmentDigest(promotion.AttachmentExecution.AttachmentManifest), required: true),
                new("promotion-gate", Path.Combine(artifactRoot, "promotion", "gate.json"), ComputePromotionGateDigest(promotion), required: true)
            ],
            createdAtUtc);
    }

    private static string ComputePublicationAttachmentDigest(SigtranCommercialEvidencePublicationAttachmentManifest manifest)
    {
        StringBuilder builder = new();
        builder.Append(manifest.Seal.SealId).Append('|').Append(manifest.Seal.AggregateSha256).Append('\n');

        foreach (SigtranCommercialEvidencePublicationAttachment attachment in manifest.Attachments
            .OrderBy(static item => item.RetainedPath, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(attachment.Kind)
                .Append('|')
                .Append(attachment.RetainedPath)
                .Append('|')
                .Append(attachment.Sha256)
                .Append('|')
                .Append(attachment.RedactionApproved)
                .Append('\n');
        }

        return ComputeSha256(builder.ToString());
    }

    private static string ComputePromotionGateDigest(SigtranCommercialEvidenceFileSystemPromotionExecution promotion)
    {
        StringBuilder builder = new();
        builder.Append(promotion.PromotionGate.ReleaseReviewer).Append('|')
            .Append(promotion.PromotionGate.EvaluatedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
            .Append(promotion.PromotionGate.EvidenceApproved).Append('|')
            .Append(promotion.PromotionGate.Blockers.Count).Append('|')
            .Append(promotion.AttachmentExecution.AttachmentManifest.Seal.AggregateSha256);

        return ComputeSha256(builder.ToString());
    }

    private static string ComputeSha256(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
