namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Stores protocol interoperability vector evidence.
/// </summary>
public sealed class SigtranProtocolInteropEvidenceRegistry
{
    private readonly List<SigtranProtocolInteropRunReport> _reports = [];

    /// <summary>Adds a protocol interoperability run report.</summary>
    /// <param name="report">The run report.</param>
    public void Add(SigtranProtocolInteropRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
    }

    /// <summary>Returns a deterministic evidence snapshot.</summary>
    /// <returns>The evidence snapshot.</returns>
    public IReadOnlyList<SigtranProtocolInteropRunReport> Snapshot()
    {
        return _reports.ToArray();
    }

    /// <summary>Whether every required vector has passing evidence.</summary>
    public bool HasCompletePassingEvidence => SigtranProtocolInteropVectorCatalog.GetVectors()
        .All(vector => _reports.Any(report => string.Equals(report.Vector.Id, vector.Id, StringComparison.Ordinal) && report.HasPassingEvidence));
}

/// <summary>
/// Provides protocol interoperability evidence helpers.
/// </summary>
public static class SigtranProtocolInteropEvidence
{
    /// <summary>Creates the current protocol interoperability evidence registry.</summary>
    /// <returns>The current protocol interoperability evidence registry.</returns>
    public static SigtranProtocolInteropEvidenceRegistry CreateCurrentRegistry()
    {
        return new SigtranProtocolInteropEvidenceRegistry();
    }
}
