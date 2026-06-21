namespace sigtran.net.Core.Utilities;

/// <summary>
/// Stores external peer interoperability execution evidence.
/// </summary>
public sealed class SigtranExternalPeerInteropEvidenceRegistry
{
    private readonly List<SigtranExternalPeerInteropRunReport> _reports = [];

    /// <summary>Adds an external peer execution report.</summary>
    /// <param name="report">The execution report.</param>
    public void Add(SigtranExternalPeerInteropRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
    }

    /// <summary>Returns a deterministic evidence snapshot.</summary>
    /// <returns>The evidence snapshot.</returns>
    public IReadOnlyList<SigtranExternalPeerInteropRunReport> Snapshot()
    {
        return _reports.ToArray();
    }

    /// <summary>Whether passing external peer ASP-to-SG evidence exists.</summary>
    public bool HasPassingAspToSgEvidence => _reports.Any(static report => report.HasPassingEvidence);

    /// <summary>Whether passing external peer evidence is ready for commercial review.</summary>
    public bool HasCommercialReviewReadyEvidence => _reports.Any(static report => report.HasCommercialReviewReadyEvidence);
}

/// <summary>
/// Provides external peer interoperability evidence helpers.
/// </summary>
public static class SigtranExternalPeerInteropEvidence
{
    /// <summary>Creates the current external peer evidence registry.</summary>
    /// <returns>The current external peer evidence registry.</returns>
    public static SigtranExternalPeerInteropEvidenceRegistry CreateCurrentRegistry()
    {
        return new SigtranExternalPeerInteropEvidenceRegistry();
    }
}
