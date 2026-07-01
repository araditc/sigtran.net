using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes an integrity seal over a retained production evidence ledger.
/// </summary>
public sealed class SigtranReleaseEvidenceIntegritySeal
{
    /// <summary>Creates an integrity seal over a retained production evidence ledger.</summary>
    /// <param name="ledger">The retained production evidence ledger.</param>
    /// <param name="sealId">The stable integrity seal identifier.</param>
    /// <param name="algorithm">The digest algorithm used by the seal.</param>
    /// <param name="aggregateSha256">The aggregate SHA-256 digest over the ledger entries.</param>
    /// <param name="sealedAtUtc">The UTC time when the seal was created.</param>
    public SigtranReleaseEvidenceIntegritySeal(
        SigtranReleaseEvidenceRetentionLedger ledger,
        string sealId,
        string algorithm,
        string aggregateSha256,
        DateTimeOffset sealedAtUtc)
    {
        Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        SealId = string.IsNullOrWhiteSpace(sealId) ? throw new ArgumentException("Seal id is required.", nameof(sealId)) : sealId;
        Algorithm = string.IsNullOrWhiteSpace(algorithm) ? throw new ArgumentException("Algorithm is required.", nameof(algorithm)) : algorithm;
        AggregateSha256 = string.IsNullOrWhiteSpace(aggregateSha256) ? throw new ArgumentException("Aggregate SHA-256 digest is required.", nameof(aggregateSha256)) : aggregateSha256;
        SealedAtUtc = sealedAtUtc.Offset == TimeSpan.Zero ? sealedAtUtc : sealedAtUtc.ToUniversalTime();
    }

    /// <summary>The retained production evidence ledger.</summary>
    public SigtranReleaseEvidenceRetentionLedger Ledger { get; }

    /// <summary>The stable integrity seal identifier.</summary>
    public string SealId { get; }

    /// <summary>The digest algorithm used by the seal.</summary>
    public string Algorithm { get; }

    /// <summary>The aggregate SHA-256 digest over the ledger entries.</summary>
    public string AggregateSha256 { get; }

    /// <summary>The UTC time when the seal was created.</summary>
    public DateTimeOffset SealedAtUtc { get; }

    /// <summary>Whether the seal id is stable enough for retained evidence references.</summary>
    public bool HasStableSealId => SealId.Length >= 8
        && !SealId.Any(char.IsWhiteSpace);

    /// <summary>Whether the seal uses the required SHA-256 algorithm label.</summary>
    public bool UsesSha256 => string.Equals(Algorithm, "SHA-256", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the aggregate digest is a valid SHA-256 hex value.</summary>
    public bool HasValidAggregateDigest => AggregateSha256.Length == 64
        && AggregateSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the seal time is normalized to UTC.</summary>
    public bool HasUtcSealTime => SealedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>The digest computed from the current ledger entries.</summary>
    public string ComputedAggregateSha256 => SigtranReleaseEvidenceIntegritySeals.ComputeAggregateSha256(Ledger);

    /// <summary>Whether the retained aggregate digest matches the current ledger entries.</summary>
    public bool MatchesLedgerDigest => string.Equals(AggregateSha256, ComputedAggregateSha256, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the integrity seal is ready for publication attachment planning.</summary>
    public bool IsReady => Ledger.IsReady
        && HasStableSealId
        && UsesSha256
        && HasValidAggregateDigest
        && HasUtcSealTime
        && MatchesLedgerDigest;

    /// <summary>Formats a compact integrity seal summary.</summary>
    /// <returns>The integrity seal summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceIntegritySealReady={IsReady} sealId={SealId} entries={Ledger.Entries.Count}";
    }
}

/// <summary>
/// Provides retained production evidence integrity seal helpers.
/// </summary>
public static class SigtranReleaseEvidenceIntegritySeals
{
    /// <summary>Creates a default integrity seal over a ready retention ledger.</summary>
    /// <param name="ledger">The retained production evidence ledger.</param>
    /// <param name="sealedAtUtc">The UTC time when the seal was created.</param>
    /// <returns>The retained production evidence integrity seal.</returns>
    public static SigtranReleaseEvidenceIntegritySeal CreateDefault(
        SigtranReleaseEvidenceRetentionLedger ledger,
        DateTimeOffset sealedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        string sealId = $"{ledger.Report.Manifest.Handoff.Report.Target.IntakeId}-integrity-seal";

        return new(
            ledger,
            sealId,
            "SHA-256",
            ComputeAggregateSha256(ledger),
            sealedAtUtc);
    }

    /// <summary>Computes a deterministic aggregate SHA-256 digest for a retention ledger.</summary>
    /// <param name="ledger">The retained production evidence ledger.</param>
    /// <returns>The aggregate SHA-256 digest.</returns>
    public static string ComputeAggregateSha256(SigtranReleaseEvidenceRetentionLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        StringBuilder builder = new();
        builder.Append("minimumRetentionDays=").Append(ledger.MinimumRetentionDays).Append('\n');

        foreach (SigtranReleaseEvidenceRetentionLedgerEntry entry in ledger.Entries
            .OrderBy(static item => item.Kind)
            .ThenBy(static item => item.RetainedPath, StringComparer.OrdinalIgnoreCase))
        {
            builder
                .Append(entry.Kind)
                .Append('|')
                .Append(entry.RetainedPath)
                .Append('|')
                .Append(entry.Sha256.ToLowerInvariant())
                .Append('|')
                .Append(entry.RetainedAtUtc.ToString("O", CultureInfo.InvariantCulture))
                .Append('|')
                .Append(entry.RetainUntilUtc.ToString("O", CultureInfo.InvariantCulture))
                .Append('|')
                .Append(entry.Reviewer)
                .Append('|')
                .Append(entry.ImmutableRetention)
                .Append('\n');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
