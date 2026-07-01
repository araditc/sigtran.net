namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed production evidence integrity seal execution.
/// </summary>
public sealed class SigtranReleaseEvidenceFileSystemIntegritySealExecution
{
    /// <summary>Creates filesystem-backed integrity seal execution.</summary>
    /// <param name="ledgerExecution">The filesystem-backed retention ledger execution.</param>
    /// <param name="seal">The integrity seal created from the ledger.</param>
    public SigtranReleaseEvidenceFileSystemIntegritySealExecution(
        SigtranReleaseEvidenceFileSystemRetentionLedgerExecution ledgerExecution,
        SigtranReleaseEvidenceIntegritySeal seal)
    {
        LedgerExecution = ledgerExecution ?? throw new ArgumentNullException(nameof(ledgerExecution));
        Seal = seal ?? throw new ArgumentNullException(nameof(seal));
    }

    /// <summary>The filesystem-backed retention ledger execution.</summary>
    public SigtranReleaseEvidenceFileSystemRetentionLedgerExecution LedgerExecution { get; }

    /// <summary>The integrity seal created from the ledger.</summary>
    public SigtranReleaseEvidenceIntegritySeal Seal { get; }

    /// <summary>Whether the source ledger execution is ready.</summary>
    public bool LedgerReady => LedgerExecution.IsReady;

    /// <summary>Whether the seal digest matches the current ledger.</summary>
    public bool SealMatchesLedger => Seal.MatchesLedgerDigest;

    /// <summary>Whether filesystem-backed integrity seal execution is ready for publication attachment execution.</summary>
    public bool IsReady => LedgerReady
        && Seal.IsReady
        && SealMatchesLedger;

    /// <summary>Formats a compact filesystem integrity seal execution summary.</summary>
    /// <returns>The filesystem integrity seal execution summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceFileSystemIntegritySealReady={IsReady} sealId={Seal.SealId}";
    }
}

/// <summary>
/// Provides filesystem-backed production evidence integrity seal execution helpers.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemIntegritySeals
{
    /// <summary>Creates an integrity seal from a filesystem-backed retention ledger execution.</summary>
    /// <param name="ledgerExecution">The filesystem-backed retention ledger execution.</param>
    /// <param name="sealedAtUtc">The UTC seal creation time.</param>
    /// <returns>The filesystem-backed integrity seal execution.</returns>
    public static SigtranReleaseEvidenceFileSystemIntegritySealExecution Create(
        SigtranReleaseEvidenceFileSystemRetentionLedgerExecution ledgerExecution,
        DateTimeOffset sealedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(ledgerExecution);
        SigtranReleaseEvidenceIntegritySeal seal = SigtranReleaseEvidenceIntegritySeals.CreateDefault(
            ledgerExecution.Ledger,
            sealedAtUtc);

        return new(ledgerExecution, seal);
    }
}
