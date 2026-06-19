namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies the result of an external interoperability evidence item.
/// </summary>
public enum SigtranInteropEvidenceResult
{
    /// <summary>The evidence item passed.</summary>
    Passed,

    /// <summary>The evidence item failed.</summary>
    Failed,

    /// <summary>The evidence item is informational only.</summary>
    Informational
}

/// <summary>
/// Describes one external interoperability evidence item.
/// </summary>
public sealed class SigtranInteropEvidenceItem
{
    /// <summary>Creates an interoperability evidence item.</summary>
    /// <param name="id">The stable evidence id.</param>
    /// <param name="peerStack">The peer stack or product name.</param>
    /// <param name="scenario">The tested scenario.</param>
    /// <param name="traceReference">The trace or artifact reference.</param>
    /// <param name="result">The evidence result.</param>
    public SigtranInteropEvidenceItem(
        string id,
        string peerStack,
        string scenario,
        string traceReference,
        SigtranInteropEvidenceResult result)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Evidence id is required.", nameof(id)) : id;
        PeerStack = string.IsNullOrWhiteSpace(peerStack) ? throw new ArgumentException("Peer stack is required.", nameof(peerStack)) : peerStack;
        Scenario = string.IsNullOrWhiteSpace(scenario) ? throw new ArgumentException("Scenario is required.", nameof(scenario)) : scenario;
        TraceReference = string.IsNullOrWhiteSpace(traceReference) ? throw new ArgumentException("Trace reference is required.", nameof(traceReference)) : traceReference;
        Result = result;
    }

    /// <summary>The stable evidence id.</summary>
    public string Id { get; }

    /// <summary>The peer stack or product name.</summary>
    public string PeerStack { get; }

    /// <summary>The tested scenario.</summary>
    public string Scenario { get; }

    /// <summary>The trace or artifact reference.</summary>
    public string TraceReference { get; }

    /// <summary>The evidence result.</summary>
    public SigtranInteropEvidenceResult Result { get; }
}

/// <summary>
/// Stores external interoperability evidence in deterministic order.
/// </summary>
public sealed class SigtranInteropEvidenceRegistry
{
    private readonly List<SigtranInteropEvidenceItem> _items = [];

    /// <summary>Adds an evidence item.</summary>
    /// <param name="item">The evidence item.</param>
    public void Add(SigtranInteropEvidenceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.Any(existing => string.Equals(existing.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Interop evidence '{item.Id}' already exists.");
        }

        _items.Add(item);
    }

    /// <summary>Returns a deterministic evidence snapshot.</summary>
    /// <returns>The evidence snapshot.</returns>
    public IReadOnlyList<SigtranInteropEvidenceItem> Snapshot()
    {
        return _items.ToArray();
    }

    /// <summary>Returns whether all evidence items passed and at least one item exists.</summary>
    /// <returns>True when the registry has passing evidence; otherwise false.</returns>
    public bool HasPassingEvidence()
    {
        return _items.Count > 0 && _items.All(static item => item.Result == SigtranInteropEvidenceResult.Passed);
    }
}

/// <summary>
/// Provides external interoperability evidence inventory helpers.
/// </summary>
public static class SigtranInteropEvidence
{
    /// <summary>Creates the current external evidence registry.</summary>
    /// <returns>The current external evidence registry.</returns>
    public static SigtranInteropEvidenceRegistry CreateCurrentRegistry()
    {
        return new SigtranInteropEvidenceRegistry();
    }
}

/// <summary>
/// Promotes passing interoperability lab runs into evidence items.
/// </summary>
public static class SigtranInteropEvidencePromotion
{
    /// <summary>Creates a passing evidence item from a lab run report.</summary>
    /// <param name="report">The lab run report.</param>
    /// <param name="id">The optional evidence id.</param>
    /// <returns>The promoted evidence item.</returns>
    public static SigtranInteropEvidenceItem Promote(SigtranInteropLabRunReport report, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (!report.HasPassingEvidence)
        {
            throw new InvalidOperationException("Only passed lab runs with complete artifact manifests can be promoted.");
        }

        string evidenceId = string.IsNullOrWhiteSpace(id)
            ? $"lab-{report.Scenario.Id}-{report.StartedAt:yyyyMMddHHmmss}"
            : id;
        string traceReference = string.Join(";", report.Manifest.Snapshot().Select(static artifact => artifact.Path));

        return new SigtranInteropEvidenceItem(
            evidenceId,
            report.Scenario.PeerStack,
            report.Scenario.Id,
            traceReference,
            SigtranInteropEvidenceResult.Passed);
    }
}
