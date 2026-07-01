namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained production evidence ledger entry.
/// </summary>
public sealed class SigtranReleaseEvidenceRetentionLedgerEntry
{
    /// <summary>Creates a retained production evidence ledger entry.</summary>
    /// <param name="kind">The retained artifact kind.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="retainedAtUtc">The UTC retention start time.</param>
    /// <param name="retainUntilUtc">The UTC retention expiry time.</param>
    /// <param name="reviewer">The reviewer that accepted the retained evidence.</param>
    /// <param name="immutableRetention">Whether the retained artifact is marked immutable.</param>
    public SigtranReleaseEvidenceRetentionLedgerEntry(
        SigtranReleaseEvidenceChecklistKind kind,
        string retainedPath,
        string sha256,
        DateTimeOffset retainedAtUtc,
        DateTimeOffset retainUntilUtc,
        string reviewer,
        bool immutableRetention)
    {
        Kind = kind;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        RetainedAtUtc = retainedAtUtc.Offset == TimeSpan.Zero ? retainedAtUtc : retainedAtUtc.ToUniversalTime();
        RetainUntilUtc = retainUntilUtc.Offset == TimeSpan.Zero ? retainUntilUtc : retainUntilUtc.ToUniversalTime();
        Reviewer = string.IsNullOrWhiteSpace(reviewer) ? throw new ArgumentException("Reviewer is required.", nameof(reviewer)) : reviewer;
        ImmutableRetention = immutableRetention;
    }

    /// <summary>The retained artifact kind.</summary>
    public SigtranReleaseEvidenceChecklistKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>The UTC retention start time.</summary>
    public DateTimeOffset RetainedAtUtc { get; }

    /// <summary>The UTC retention expiry time.</summary>
    public DateTimeOffset RetainUntilUtc { get; }

    /// <summary>The reviewer that accepted the retained evidence.</summary>
    public string Reviewer { get; }

    /// <summary>Whether the retained artifact is marked immutable.</summary>
    public bool ImmutableRetention { get; }

    /// <summary>Whether the entry digest is a valid SHA-256 hex value.</summary>
    public bool HasValidDigest => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);

    /// <summary>Whether retention timestamps are normalized to UTC.</summary>
    public bool UsesUtcTimes => RetainedAtUtc.Offset == TimeSpan.Zero
        && RetainUntilUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the retention window has a positive duration.</summary>
    public bool HasPositiveRetentionWindow => RetainUntilUtc > RetainedAtUtc;

    /// <summary>Whether the entry meets the minimum retention duration.</summary>
    /// <param name="minimumRetentionDays">The minimum retention duration in days.</param>
    /// <returns><c>true</c> when the entry retention window meets the minimum duration.</returns>
    public bool MeetsMinimumRetention(int minimumRetentionDays)
    {
        return minimumRetentionDays > 0
            && RetainUntilUtc >= RetainedAtUtc.AddDays(minimumRetentionDays);
    }

    /// <summary>Whether the ledger entry is ready for release evidence retention.</summary>
    public bool IsReady => ImmutableRetention
        && HasValidDigest
        && UsesUtcTimes
        && HasPositiveRetentionWindow;
}

/// <summary>
/// Describes the retained production evidence retention ledger.
/// </summary>
public sealed class SigtranReleaseEvidenceRetentionLedger
{
    /// <summary>Creates a retained production evidence retention ledger.</summary>
    /// <param name="report">The retained file verification report.</param>
    /// <param name="entries">The retained file ledger entries.</param>
    /// <param name="minimumRetentionDays">The minimum required retention duration in days.</param>
    public SigtranReleaseEvidenceRetentionLedger(
        SigtranReleaseEvidenceFileVerificationReport report,
        IReadOnlyList<SigtranReleaseEvidenceRetentionLedgerEntry> entries,
        int minimumRetentionDays)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries.Count == 0 ? throw new ArgumentException("At least one retention ledger entry is required.", nameof(entries)) : entries.ToArray();
        MinimumRetentionDays = minimumRetentionDays > 0 ? minimumRetentionDays : throw new ArgumentOutOfRangeException(nameof(minimumRetentionDays));
    }

    /// <summary>The retained file verification report.</summary>
    public SigtranReleaseEvidenceFileVerificationReport Report { get; }

    /// <summary>The retained file ledger entries.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceRetentionLedgerEntry> Entries { get; }

    /// <summary>The minimum required retention duration in days.</summary>
    public int MinimumRetentionDays { get; }

    /// <summary>Whether every verified file is present in the retention ledger.</summary>
    public bool CoversVerifiedFiles => Report.Manifest.Files
        .Where(static file => file.IsVerified)
        .All(file => Entries.Any(entry => entry.Kind == file.Kind
            && entry.RetainedPath == file.RetainedPath
            && string.Equals(entry.Sha256, file.ActualSha256, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether retained paths are unique in the ledger.</summary>
    public bool UsesUniqueRetainedPaths => Entries.Select(static entry => entry.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Entries.Count;

    /// <summary>Whether every ledger entry is marked immutable.</summary>
    public bool AllEntriesImmutable => Entries.All(static entry => entry.ImmutableRetention);

    /// <summary>Whether all ledger entries use UTC timestamps.</summary>
    public bool UsesUtcRetentionTimes => Entries.All(static entry => entry.UsesUtcTimes);

    /// <summary>Whether every ledger entry is ready and meets the minimum retention duration.</summary>
    public bool AllEntriesReady => Entries.All(entry => entry.IsReady && entry.MeetsMinimumRetention(MinimumRetentionDays));

    /// <summary>Whether the retention ledger is ready for integrity sealing.</summary>
    public bool IsReady => Report.IsVerified
        && CoversVerifiedFiles
        && UsesUniqueRetainedPaths
        && AllEntriesReady;

    /// <summary>Formats a compact retention ledger summary.</summary>
    /// <returns>The retention ledger summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceRetentionLedgerReady={IsReady} entries={Entries.Count} minimumRetentionDays={MinimumRetentionDays}";
    }
}

/// <summary>
/// Provides retained production evidence retention ledger helpers.
/// </summary>
public static class SigtranReleaseEvidenceRetentionLedgers
{
    /// <summary>Creates a default retention ledger from a verified retained file report.</summary>
    /// <param name="report">The retained file verification report.</param>
    /// <param name="reviewer">The reviewer that accepted the retained evidence.</param>
    /// <param name="retainedAtUtc">The UTC retention start time.</param>
    /// <param name="retentionDays">The retention duration in days.</param>
    /// <returns>The retained production evidence retention ledger.</returns>
    public static SigtranReleaseEvidenceRetentionLedger CreateDefault(
        SigtranReleaseEvidenceFileVerificationReport report,
        string reviewer,
        DateTimeOffset retainedAtUtc,
        int retentionDays = 365)
    {
        ArgumentNullException.ThrowIfNull(report);
        DateTimeOffset retainedAt = retainedAtUtc.Offset == TimeSpan.Zero ? retainedAtUtc : retainedAtUtc.ToUniversalTime();
        DateTimeOffset retainUntil = retainedAt.AddDays(retentionDays);

        return new(
            report,
            report.Manifest.Files
                .Select(file => new SigtranReleaseEvidenceRetentionLedgerEntry(
                    file.Kind,
                    file.RetainedPath,
                    file.ActualSha256,
                    retainedAt,
                    retainUntil,
                    reviewer,
                    immutableRetention: true))
                .ToArray(),
            retentionDays);
    }
}
