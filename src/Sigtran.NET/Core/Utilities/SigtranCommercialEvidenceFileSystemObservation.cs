using System.Security.Cryptography;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained commercial evidence file observation from the local filesystem.
/// </summary>
public sealed class SigtranCommercialEvidenceFileSystemObservation
{
    /// <summary>Creates a retained commercial evidence filesystem observation.</summary>
    /// <param name="fileSystemPath">The local filesystem path that was observed.</param>
    /// <param name="retainedFile">The retained file verification model created from the observation.</param>
    public SigtranCommercialEvidenceFileSystemObservation(
        string fileSystemPath,
        SigtranCommercialEvidenceRetainedFile retainedFile)
    {
        FileSystemPath = string.IsNullOrWhiteSpace(fileSystemPath) ? throw new ArgumentException("Filesystem path is required.", nameof(fileSystemPath)) : fileSystemPath;
        RetainedFile = retainedFile ?? throw new ArgumentNullException(nameof(retainedFile));
    }

    /// <summary>The local filesystem path that was observed.</summary>
    public string FileSystemPath { get; }

    /// <summary>The retained file verification model created from the observation.</summary>
    public SigtranCommercialEvidenceRetainedFile RetainedFile { get; }

    /// <summary>Whether the observed filesystem path matches the retained evidence path.</summary>
    public bool FileSystemPathMatchesRetainedPath => string.Equals(FileSystemPath, RetainedFile.RetainedPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the observed filesystem file exists.</summary>
    public bool Exists => RetainedFile.Exists;

    /// <summary>Whether the observed digest matches the promotion handoff digest.</summary>
    public bool DigestMatchesHandoff => RetainedFile.DigestMatches;

    /// <summary>Whether the filesystem observation verifies the retained evidence file.</summary>
    public bool IsVerified => RetainedFile.IsVerified;

    /// <summary>Formats a compact filesystem observation summary.</summary>
    /// <returns>The filesystem observation summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceFileSystemObservation={FileSystemPath} verified={IsVerified} exists={Exists}";
    }
}

/// <summary>
/// Observes retained commercial evidence files from the local filesystem.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemObserver
{
    /// <summary>The deterministic digest marker used when a file is missing.</summary>
    public const string MissingFileSha256 = "0000000000000000000000000000000000000000000000000000000000000000";

    /// <summary>Observes a retained evidence file and computes its digest when it exists.</summary>
    /// <param name="item">The promotion handoff item that declares the expected retained file.</param>
    /// <param name="fileSystemPath">An optional local filesystem path override.</param>
    /// <param name="observedAtUtc">An optional UTC observation time.</param>
    /// <returns>The filesystem observation.</returns>
    public static SigtranCommercialEvidenceFileSystemObservation Observe(
        SigtranCommercialEvidencePromotionHandoffItem item,
        string? fileSystemPath = null,
        DateTimeOffset? observedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        string path = string.IsNullOrWhiteSpace(fileSystemPath) ? item.RetainedPath : fileSystemPath;
        DateTimeOffset observedAt = observedAtUtc ?? DateTimeOffset.UtcNow;
        observedAt = observedAt.Offset == TimeSpan.Zero ? observedAt : observedAt.ToUniversalTime();

        bool exists = File.Exists(path);
        long sizeBytes = exists ? new FileInfo(path).Length : 0;
        string actualSha256 = exists ? ComputeSha256(path) : MissingFileSha256;

        SigtranCommercialEvidenceRetainedFile retainedFile = new(
            item.Kind,
            item.RetainedPath,
            item.Sha256,
            actualSha256,
            sizeBytes,
            observedAt,
            exists);

        return new(path, retainedFile);
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
