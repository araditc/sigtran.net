namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Summarizes SCCP, TCAP, and MAP SMS protocol evidence upgrade status.
/// </summary>
public sealed class SigtranProtocolEvidenceStatusReport
{
    private readonly string[] _capabilities;
    private readonly string[] _blockers;

    /// <summary>Creates a protocol evidence status report.</summary>
    /// <param name="label">The status label.</param>
    /// <param name="completedUnitCount">The completed unit count.</param>
    /// <param name="capabilities">The completed capability names.</param>
    /// <param name="foundationReady">Whether SCCP, TCAP, and MAP SMS foundations are ready.</param>
    /// <param name="sdkEvidenceBacked">Whether deterministic SDK-side protocol evidence is complete.</param>
    /// <param name="productionEvidenceReady">Whether retained external evidence allows production protocol claims.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranProtocolEvidenceStatusReport(
        string label,
        int completedUnitCount,
        IReadOnlyList<string> capabilities,
        bool foundationReady,
        bool sdkEvidenceBacked,
        bool productionEvidenceReady,
        IReadOnlyList<string> blockers)
    {
        Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Protocol evidence status label is required.", nameof(label)) : label;
        CompletedUnitCount = completedUnitCount >= 0 ? completedUnitCount : throw new ArgumentOutOfRangeException(nameof(completedUnitCount), "Completed unit count must not be negative.");
        _capabilities = (capabilities ?? throw new ArgumentNullException(nameof(capabilities))).ToArray();
        FoundationReady = foundationReady;
        SdkEvidenceBacked = sdkEvidenceBacked;
        ProductionEvidenceReady = productionEvidenceReady;
        _blockers = (blockers ?? throw new ArgumentNullException(nameof(blockers))).ToArray();
    }

    /// <summary>The status label.</summary>
    public string Label { get; }

    /// <summary>The completed unit count.</summary>
    public int CompletedUnitCount { get; }

    /// <summary>The completed capability names.</summary>
    public IReadOnlyList<string> Capabilities => _capabilities.ToArray();

    /// <summary>Whether SCCP, TCAP, and MAP SMS foundations are ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether deterministic SDK-side protocol evidence is complete.</summary>
    public bool SdkEvidenceBacked { get; }

    /// <summary>Whether retained external evidence allows production protocol claims.</summary>
    public bool ProductionEvidenceReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers => _blockers.ToArray();

    /// <summary>Formats a compact protocol evidence status summary.</summary>
    /// <returns>The protocol evidence status summary.</returns>
    public string Describe()
    {
        return $"label={Label} completedUnits={CompletedUnitCount} capabilities={_capabilities.Length} foundationReady={FoundationReady} sdkEvidenceBacked={SdkEvidenceBacked} productionEvidenceReady={ProductionEvidenceReady} blockers={_blockers.Length}";
    }
}

/// <summary>
/// Provides current SCCP, TCAP, and MAP SMS protocol evidence upgrade status.
/// </summary>
public static class SigtranProtocolEvidenceStatus
{
    /// <summary>The status label.</summary>
    public const string StatusLabel = "SCCP TCAP MAP evidence upgrade";

    /// <summary>The number of completed protocol evidence upgrade units.</summary>
    public const int CompletedUnitCount = 9;

    private static readonly string[] CurrentCapabilities =
    [
        "shared-protocol-evidence-contract",
        "sccp-evidence-vectors",
        "tcap-evidence-vectors",
        "map-sms-evidence-vectors",
        "cross-layer-evidence-bundle",
        "ordered-trace-validation",
        "mismatch-classification",
        "evidence-backed-readiness-gate",
        "status-and-documentation-summary"
    ];

    /// <summary>The completed capability names.</summary>
    public static IReadOnlyList<string> Capabilities => CurrentCapabilities.ToArray();

    /// <summary>Whether SCCP, TCAP, and MAP SMS foundations are ready.</summary>
    public static bool FoundationReady => SigtranProtocolEvidenceReadiness.GetReport().FoundationReady;

    /// <summary>Whether deterministic SDK-side protocol evidence is complete.</summary>
    public static bool SdkEvidenceBacked => CurrentCapabilities.Length == CompletedUnitCount
        && SigtranProtocolEvidenceReadiness.GetReport().SdkEvidenceBacked;

    /// <summary>Whether retained external evidence allows production protocol claims.</summary>
    public static bool ProductionEvidenceReady => GetStatus().ProductionEvidenceReady;

    /// <summary>Returns the current protocol evidence status report.</summary>
    /// <param name="hasExternalInteroperabilityEvidence">Whether retained external interoperability evidence is available.</param>
    /// <returns>The current protocol evidence status report.</returns>
    public static SigtranProtocolEvidenceStatusReport GetStatus(bool hasExternalInteroperabilityEvidence = false)
    {
        SigtranProtocolEvidenceReadinessReport readiness = SigtranProtocolEvidenceReadiness.GetReport(hasExternalInteroperabilityEvidence);
        return new(
            StatusLabel,
            CompletedUnitCount,
            CurrentCapabilities,
            readiness.FoundationReady,
            CurrentCapabilities.Length == CompletedUnitCount && readiness.SdkEvidenceBacked,
            readiness.ProductionEvidenceReady,
            readiness.Blockers);
    }

    /// <summary>Formats a compact protocol evidence status summary.</summary>
    /// <returns>The protocol evidence status summary.</returns>
    public static string Describe()
    {
        return GetStatus().Describe();
    }
}
