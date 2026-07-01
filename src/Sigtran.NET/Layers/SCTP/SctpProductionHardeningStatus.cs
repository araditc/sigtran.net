namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Summarizes native SCTP production hardening status.
/// </summary>
public sealed class SctpProductionHardeningStatusReport
{
    private readonly string[] _capabilities;
    private readonly string[] _blockers;

    /// <summary>Creates an SCTP production hardening status report.</summary>
    /// <param name="label">The status label.</param>
    /// <param name="completedUnitCount">The completed unit count.</param>
    /// <param name="capabilities">The completed capability names.</param>
    /// <param name="foundationReady">Whether the source-level foundation is ready.</param>
    /// <param name="productionReady">Whether production evidence allows production SCTP claims.</param>
    /// <param name="blockers">The remaining production blockers.</param>
    public SctpProductionHardeningStatusReport(
        string label,
        int completedUnitCount,
        IReadOnlyList<string> capabilities,
        bool foundationReady,
        bool productionReady,
        IReadOnlyList<string> blockers)
    {
        Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("SCTP production hardening status label is required.", nameof(label)) : label;
        CompletedUnitCount = completedUnitCount >= 0 ? completedUnitCount : throw new ArgumentOutOfRangeException(nameof(completedUnitCount), "Completed unit count must not be negative.");
        _capabilities = (capabilities ?? throw new ArgumentNullException(nameof(capabilities))).ToArray();
        FoundationReady = foundationReady;
        ProductionReady = productionReady;
        _blockers = (blockers ?? throw new ArgumentNullException(nameof(blockers))).ToArray();
    }

    /// <summary>The status label.</summary>
    public string Label { get; }

    /// <summary>The completed unit count.</summary>
    public int CompletedUnitCount { get; }

    /// <summary>The completed capability names.</summary>
    public IReadOnlyList<string> Capabilities => _capabilities.ToArray();

    /// <summary>Whether the source-level foundation is ready.</summary>
    public bool FoundationReady { get; }

    /// <summary>Whether production evidence allows production SCTP claims.</summary>
    public bool ProductionReady { get; }

    /// <summary>The remaining production blockers.</summary>
    public IReadOnlyList<string> Blockers => _blockers.ToArray();

    /// <summary>Formats a compact status summary.</summary>
    /// <returns>The status summary.</returns>
    public string Describe()
    {
        return $"label={Label} completedUnits={CompletedUnitCount} capabilities={_capabilities.Length} foundationReady={FoundationReady} productionReady={ProductionReady} blockers={_blockers.Length}";
    }
}

/// <summary>
/// Provides the current native SCTP production hardening status.
/// </summary>
public static class SctpProductionHardeningStatus
{
    /// <summary>The status label.</summary>
    public const string StatusLabel = "Native SCTP production hardening foundation";

    /// <summary>The completed unit count for the hardening foundation.</summary>
    public const int CompletedUnitCount = 10;

    private static readonly string[] CurrentCapabilities =
    [
        "Outbound stream and PPID framing",
        "Association lifecycle journal",
        "Reconnect schedule",
        "Send backpressure policy",
        "Cancellation and timeout policy",
        "Multi-homing readiness",
        "Fault recovery decisions",
        "Transport diagnostics snapshot",
        "Production hardening readiness gate",
        "Foundation summary and validation"
    ];

    /// <summary>The completed capability names.</summary>
    public static IReadOnlyList<string> Capabilities => CurrentCapabilities.ToArray();

    /// <summary>Whether the source-level foundation is ready.</summary>
    public static bool FoundationReady => CurrentCapabilities.Length == CompletedUnitCount
        && SctpProductionHardeningReadiness.GetReport().FoundationReady;

    /// <summary>Whether production evidence allows production SCTP claims.</summary>
    public static bool ProductionReady => FoundationReady
        && SctpProductionHardeningReadiness.GetReport().ProductionReady;

    /// <summary>Returns the current production hardening status report.</summary>
    /// <returns>The current status report.</returns>
    public static SctpProductionHardeningStatusReport GetStatus()
    {
        SctpProductionHardeningReadinessSnapshot readiness = SctpProductionHardeningReadiness.GetReport();
        return new(
            StatusLabel,
            CompletedUnitCount,
            CurrentCapabilities,
            FoundationReady,
            readiness.ProductionReady,
            GetBlockers(readiness));
    }

    private static IReadOnlyList<string> GetBlockers(SctpProductionHardeningReadinessSnapshot readiness)
    {
        List<string> blockers = [];
        if (!readiness.HasRetainedLinuxSctpEvidence)
        {
            blockers.Add("retained-linux-sctp-evidence-required");
        }

        if (!readiness.HasRetainedExternalPeerEvidence)
        {
            blockers.Add("retained-external-peer-evidence-required");
        }

        return blockers;
    }
}
