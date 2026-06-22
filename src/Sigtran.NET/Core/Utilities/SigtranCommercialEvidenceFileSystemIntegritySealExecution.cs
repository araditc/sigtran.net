namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed commercial evidence integrity seal execution.
/// </summary>
public sealed class SigtranCommercialEvidenceFileSystemIntegritySealExecution
{
    /// <summary>Creates filesystem-backed integrity seal execution.</summary>
    /// <param name="ledgerExecution">The filesystem-backed retention ledger execution.</param>
    /// <param name="seal">The integrity seal created from the ledger.</param>
    public SigtranCommercialEvidenceFileSystemIntegritySealExecution(
        SigtranCommercialEvidenceFileSystemRetentionLedgerExecution ledgerExecution,
        SigtranCommercialEvidenceIntegritySeal seal)
    {
        LedgerExecution = ledgerExecution ?? throw new ArgumentNullException(nameof(ledgerExecution));
        Seal = seal ?? throw new ArgumentNullException(nameof(seal));
    }

    /// <summary>The filesystem-backed retention ledger execution.</summary>
    public SigtranCommercialEvidenceFileSystemRetentionLedgerExecution LedgerExecution { get; }

    /// <summary>The integrity seal created from the ledger.</summary>
    public SigtranCommercialEvidenceIntegritySeal Seal { get; }

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
        return $"commercialEvidenceFileSystemIntegritySealReady={IsReady} sealId={Seal.SealId}";
    }
}

/// <summary>
/// Provides filesystem-backed commercial evidence integrity seal execution helpers.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemIntegritySeals
{
    /// <summary>Creates an integrity seal from a filesystem-backed retention ledger execution.</summary>
    /// <param name="ledgerExecution">The filesystem-backed retention ledger execution.</param>
    /// <param name="sealedAtUtc">The UTC seal creation time.</param>
    /// <returns>The filesystem-backed integrity seal execution.</returns>
    public static SigtranCommercialEvidenceFileSystemIntegritySealExecution Create(
        SigtranCommercialEvidenceFileSystemRetentionLedgerExecution ledgerExecution,
        DateTimeOffset sealedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(ledgerExecution);
        SigtranCommercialEvidenceIntegritySeal seal = SigtranCommercialEvidenceIntegritySeals.CreateDefault(
            ledgerExecution.Ledger,
            sealedAtUtc);

        return new(ledgerExecution, seal);
    }
}
