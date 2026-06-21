namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes generated public API baseline evidence.
/// </summary>
public sealed class SigtranPublicApiBaselineEvidence
{
    /// <summary>Creates generated public API baseline evidence.</summary>
    /// <param name="baselinePath">The public API baseline artifact path.</param>
    /// <param name="baselineSha256">The public API baseline SHA-256 digest.</param>
    /// <param name="memberCount">The number of documented public members in the baseline.</param>
    public SigtranPublicApiBaselineEvidence(string baselinePath, string baselineSha256, int memberCount)
    {
        BaselinePath = string.IsNullOrWhiteSpace(baselinePath) ? throw new ArgumentException("Baseline path is required.", nameof(baselinePath)) : baselinePath;
        BaselineSha256 = string.IsNullOrWhiteSpace(baselineSha256) ? throw new ArgumentException("Baseline digest is required.", nameof(baselineSha256)) : baselineSha256;
        MemberCount = memberCount;
    }

    /// <summary>The public API baseline artifact path.</summary>
    public string BaselinePath { get; }

    /// <summary>The public API baseline SHA-256 digest.</summary>
    public string BaselineSha256 { get; }

    /// <summary>The number of documented public members in the baseline.</summary>
    public int MemberCount { get; }

    /// <summary>Whether the baseline is ready for public API diff review.</summary>
    public bool IsReviewReady => BaselinePath.EndsWith("-public-api.txt", StringComparison.OrdinalIgnoreCase)
        && BaselineSha256.Length == 64
        && MemberCount > 0;
}

/// <summary>
/// Provides public API baseline evidence helpers.
/// </summary>
public static class SigtranPublicApiBaselineEvidenceFactory
{
    /// <summary>Creates public API baseline evidence from retained artifact metadata.</summary>
    /// <param name="baselineSha256">The retained baseline SHA-256 digest.</param>
    /// <param name="memberCount">The number of documented public members in the baseline.</param>
    /// <returns>The public API baseline evidence.</returns>
    public static SigtranPublicApiBaselineEvidence CreateFromRetainedBaseline(string baselineSha256, int memberCount)
    {
        return new(
            "artifacts/api/Sigtran.NET-public-api.txt",
            baselineSha256,
            memberCount);
    }
}
