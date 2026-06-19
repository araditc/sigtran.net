namespace sigtran.net.Core.Utilities;

/// <summary>
/// Stores native SCTP lab evidence.
/// </summary>
public sealed class SigtranNativeSctpLabEvidenceRegistry
{
    private readonly List<SigtranNativeSctpLabRunReport> _reports = [];

    /// <summary>Adds a native SCTP lab run report.</summary>
    /// <param name="report">The run report.</param>
    public void Add(SigtranNativeSctpLabRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
    }

    /// <summary>Returns a deterministic evidence snapshot.</summary>
    /// <returns>The evidence snapshot.</returns>
    public IReadOnlyList<SigtranNativeSctpLabRunReport> Snapshot()
    {
        return _reports.ToArray();
    }

    /// <summary>Returns whether all required native SCTP scenarios passed.</summary>
    /// <returns>True when all required scenarios have passing evidence; otherwise false.</returns>
    public bool HasCompletePassingEvidence()
    {
        IReadOnlyList<SigtranNativeSctpLabScenario> scenarios = SigtranNativeSctpLabScenarios.GetScenarios();
        return scenarios.All(scenario => _reports.Any(report => string.Equals(report.Scenario.Id, scenario.Id, StringComparison.OrdinalIgnoreCase) && report.HasPassingEvidence));
    }
}

/// <summary>
/// Provides native SCTP lab evidence helpers.
/// </summary>
public static class SigtranNativeSctpLabEvidence
{
    /// <summary>Creates the current native SCTP lab evidence registry.</summary>
    /// <returns>The current native SCTP lab evidence registry.</returns>
    public static SigtranNativeSctpLabEvidenceRegistry CreateCurrentRegistry()
    {
        return new SigtranNativeSctpLabEvidenceRegistry();
    }
}
