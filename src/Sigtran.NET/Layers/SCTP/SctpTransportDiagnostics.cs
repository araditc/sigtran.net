namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Identifies the summarized diagnostic state of an SCTP transport.
/// </summary>
public enum SctpTransportDiagnosticState
{
    /// <summary>The transport is established and has no warnings or recovery pressure.</summary>
    Healthy,

    /// <summary>The transport is usable but has warnings, recovery pressure, or a non-established state.</summary>
    Degraded,

    /// <summary>The transport is failed or requires immediate caller/operator attention.</summary>
    Faulted
}

/// <summary>
/// Captures a point-in-time SCTP transport diagnostic snapshot.
/// </summary>
public sealed class SctpTransportDiagnosticsSnapshot
{
    private readonly SctpAssociationJournalEntry[] _associationEvents;

    /// <summary>Creates an SCTP transport diagnostic snapshot.</summary>
    /// <param name="health">The transport health snapshot.</param>
    /// <param name="associationEvents">The association lifecycle events.</param>
    /// <param name="multiHomingReport">The optional multi-homing readiness report.</param>
    /// <param name="lastBackpressureDecision">The optional latest backpressure decision.</param>
    /// <param name="lastRecoveryDecision">The optional latest recovery decision.</param>
    /// <param name="activeOperationBudget">The optional active operation timeout budget.</param>
    public SctpTransportDiagnosticsSnapshot(
        SctpTransportHealth health,
        IReadOnlyList<SctpAssociationJournalEntry> associationEvents,
        SctpMultiHomingReadinessSnapshot? multiHomingReport = null,
        SctpBackpressureDecision? lastBackpressureDecision = null,
        SctpRecoveryDecision? lastRecoveryDecision = null,
        SctpOperationCancellationBudget? activeOperationBudget = null)
    {
        ArgumentNullException.ThrowIfNull(associationEvents);

        Health = health;
        _associationEvents = associationEvents.ToArray();
        MultiHomingReport = multiHomingReport;
        LastBackpressureDecision = lastBackpressureDecision;
        LastRecoveryDecision = lastRecoveryDecision;
        ActiveOperationBudget = activeOperationBudget;
        DiagnosticState = ComputeState();
    }

    /// <summary>The transport health snapshot.</summary>
    public SctpTransportHealth Health { get; }

    /// <summary>The association lifecycle events captured with the snapshot.</summary>
    public IReadOnlyList<SctpAssociationJournalEntry> AssociationEvents => _associationEvents.ToArray();

    /// <summary>The optional multi-homing readiness report.</summary>
    public SctpMultiHomingReadinessSnapshot? MultiHomingReport { get; }

    /// <summary>The optional latest backpressure decision.</summary>
    public SctpBackpressureDecision? LastBackpressureDecision { get; }

    /// <summary>The optional latest recovery decision.</summary>
    public SctpRecoveryDecision? LastRecoveryDecision { get; }

    /// <summary>The optional active operation timeout budget.</summary>
    public SctpOperationCancellationBudget? ActiveOperationBudget { get; }

    /// <summary>The summarized diagnostic state.</summary>
    public SctpTransportDiagnosticState DiagnosticState { get; }

    /// <summary>Whether the snapshot represents a healthy established transport.</summary>
    public bool IsHealthy => DiagnosticState == SctpTransportDiagnosticState.Healthy;

    /// <summary>Whether the snapshot should be surfaced to operators.</summary>
    public bool RequiresAttention => DiagnosticState != SctpTransportDiagnosticState.Healthy;

    /// <summary>Whether the association journal contains a failure event.</summary>
    public bool HasAssociationFailure => _associationEvents.Any(static entry => entry.AssociationEvent.EventType == SctpAssociationEventType.Failed);

    /// <summary>Whether the snapshot contains warnings without being faulted.</summary>
    public bool HasWarnings => MultiHomingReport?.Warnings.Count > 0 || LastBackpressureDecision?.ShouldDrain == true || LastRecoveryDecision?.ShouldReconnect == true;

    /// <summary>Formats a compact diagnostics summary.</summary>
    /// <returns>The diagnostics summary.</returns>
    public string Describe()
    {
        string multiHoming = MultiHomingReport?.Describe() ?? "none";
        string backpressure = LastBackpressureDecision?.Describe() ?? "none";
        string recovery = LastRecoveryDecision?.Describe() ?? "none";
        string operation = ActiveOperationBudget?.Describe() ?? "none";
        return $"state={DiagnosticState} healthy={IsHealthy} attention={RequiresAttention} association={Health.AssociationState} events={_associationEvents.Length} multiHoming=({multiHoming}) backpressure=({backpressure}) recovery=({recovery}) operation=({operation})";
    }

    private SctpTransportDiagnosticState ComputeState()
    {
        if (HasAssociationFailure || LastRecoveryDecision?.ShouldFailFast == true || Health.AssociationState == SctpAssociationState.Failed)
        {
            return SctpTransportDiagnosticState.Faulted;
        }

        if (!Health.IsEstablished || HasWarnings || LastBackpressureDecision?.Kind == SctpBackpressureDecisionKind.Reject)
        {
            return SctpTransportDiagnosticState.Degraded;
        }

        return SctpTransportDiagnosticState.Healthy;
    }
}

/// <summary>
/// Creates SCTP transport diagnostic snapshots from current transport state.
/// </summary>
public static class SctpTransportDiagnostics
{
    /// <summary>Creates a point-in-time diagnostic snapshot.</summary>
    /// <param name="health">The transport health snapshot.</param>
    /// <param name="associationJournal">The association lifecycle journal.</param>
    /// <param name="multiHomingReport">The optional multi-homing readiness report.</param>
    /// <param name="lastBackpressureDecision">The optional latest backpressure decision.</param>
    /// <param name="lastRecoveryDecision">The optional latest recovery decision.</param>
    /// <param name="activeOperationBudget">The optional active operation timeout budget.</param>
    /// <returns>The transport diagnostic snapshot.</returns>
    public static SctpTransportDiagnosticsSnapshot CreateSnapshot(
        SctpTransportHealth health,
        SctpAssociationJournal associationJournal,
        SctpMultiHomingReadinessSnapshot? multiHomingReport = null,
        SctpBackpressureDecision? lastBackpressureDecision = null,
        SctpRecoveryDecision? lastRecoveryDecision = null,
        SctpOperationCancellationBudget? activeOperationBudget = null)
    {
        ArgumentNullException.ThrowIfNull(associationJournal);

        return new(
            health,
            associationJournal.Snapshot(),
            multiHomingReport,
            lastBackpressureDecision,
            lastRecoveryDecision,
            activeOperationBudget);
    }
}
