namespace sigtran.net.Core.Utilities;

/// <summary>
/// Stores OpenSS7/IPSS7 interoperability execution evidence.
/// </summary>
public sealed class SigtranOpenSs7InteropEvidenceRegistry
{
    private readonly List<SigtranOpenSs7InteropRunReport> _reports = [];

    /// <summary>Adds an OpenSS7/IPSS7 execution report.</summary>
    /// <param name="report">The execution report.</param>
    public void Add(SigtranOpenSs7InteropRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
    }

    /// <summary>Returns a deterministic evidence snapshot.</summary>
    /// <returns>The evidence snapshot.</returns>
    public IReadOnlyList<SigtranOpenSs7InteropRunReport> Snapshot()
    {
        return _reports.ToArray();
    }

    /// <summary>Whether passing OpenSS7/IPSS7 ASP-to-SG evidence exists.</summary>
    public bool HasPassingAspToSgEvidence => _reports.Any(static report => report.HasPassingEvidence);
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability evidence helpers.
/// </summary>
public static class SigtranOpenSs7InteropEvidence
{
    /// <summary>Creates the current OpenSS7/IPSS7 evidence registry.</summary>
    /// <returns>The current OpenSS7/IPSS7 evidence registry.</returns>
    public static SigtranOpenSs7InteropEvidenceRegistry CreateCurrentRegistry()
    {
        return new SigtranOpenSs7InteropEvidenceRegistry();
    }
}
